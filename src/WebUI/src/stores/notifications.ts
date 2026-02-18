import { ref } from 'vue'
import { defineStore } from 'pinia'
import { useAuthStore } from 'stores/auth'

export interface SseNotification {
  eventType: string
  module: string
  payload: unknown
  occurredAt: string
}

/**
 * SSE Notification Store
 *
 * EventSource API header desteklemediğinden fetch() + ReadableStream kullanılır.
 * Axios interceptor'ındaki Bearer token, fetch çağrısına manuel eklenir.
 *
 * Bağlantı akışı:
 *   auth.ts boot → connect() → /api/notifications/stream (Bearer header ile)
 *   → heartbeat her 30s → disconnect() uygulama kapanırken
 */
export const useNotificationStore = defineStore('notifications', () => {
  const notifications = ref<SseNotification[]>([])
  const connected = ref(false)

  let abortController: AbortController | null = null

  async function connect() {
    if (connected.value) return

    const authStore = useAuthStore()
    const token = authStore.accessToken
    if (!token) return

    abortController = new AbortController()

    try {
      const response = await fetch('/api/notifications/stream', {
        headers: {
          Authorization: `Bearer ${token}`,
          Accept: 'text/event-stream',
        },
        signal: abortController.signal,
      })

      if (!response.ok || !response.body) {
        console.warn('[SSE] Bağlantı başarısız:', response.status)
        return
      }

      connected.value = true

      const reader = response.body.getReader()
      const decoder = new TextDecoder()
      let buffer = ''

      while (true) {
        const { done, value } = await reader.read()
        if (done) break

        buffer += decoder.decode(value, { stream: true })

        // SSE mesajlarını çift newline ile ayır
        const parts = buffer.split('\n\n')
        buffer = parts.pop() ?? ''

        for (const part of parts) {
          const notification = parseSseBlock(part)
          if (notification) {
            // Sistem eventlerini (connection.established, keepalive) filtrele
            if (notification.eventType !== 'connection.established') {
              notifications.value.unshift(notification)
              // Max 50 bildirim tut
              if (notifications.value.length > 50) {
                notifications.value.pop()
              }
            }
          }
        }
      }
    } catch (err) {
      if ((err as Error).name !== 'AbortError') {
        console.warn('[SSE] Stream hatası:', err)
      }
    } finally {
      connected.value = false
    }
  }

  function disconnect() {
    abortController?.abort()
    abortController = null
    connected.value = false
  }

  function clear() {
    notifications.value = []
  }

  return { notifications, connected, connect, disconnect, clear }
})

/**
 * SSE text bloğunu parse eder.
 * Format:
 *   id:uuid
 *   event:eventType
 *   data:{"eventType":"...", "module":"...", "payload":{...}, "occurredAt":"..."}
 */
function parseSseBlock(block: string): SseNotification | null {
  const lines = block.split('\n')
  let dataLine = ''

  for (const line of lines) {
    if (line.startsWith('data:')) {
      dataLine = line.slice(5).trim()
    }
  }

  if (!dataLine) return null

  try {
    return JSON.parse(dataLine) as SseNotification
  } catch {
    return null
  }
}
