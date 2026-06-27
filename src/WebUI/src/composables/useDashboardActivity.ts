import { computed } from 'vue'
import type { useNotificationStore } from 'stores/notifications'
import { MODULE_ICONS, eventLabel, timeAgo } from 'src/utils/notificationFormat'

export interface UseDashboardActivityOptions {
  notificationStore: ReturnType<typeof useNotificationStore>
}

export function useDashboardActivity(options: UseDashboardActivityOptions) {
  const { notificationStore } = options

  // Son bildirimler (en fazla 8)
  const recentNotifications = computed(() => notificationStore.notifications.slice(0, 8))

  // Formatlama tek kaynaktan (src/utils/notificationFormat) — MainLayout ile paylaşılır.
  return {
    MODULE_ICONS,
    recentNotifications,
    eventLabel,
    timeAgo,
  }
}
