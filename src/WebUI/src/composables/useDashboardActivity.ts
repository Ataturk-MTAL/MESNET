import { computed } from 'vue'
import type { useNotificationStore } from 'stores/notifications'

export interface UseDashboardActivityOptions {
  notificationStore: ReturnType<typeof useNotificationStore>
}

// Module icons
const MODULE_ICONS: Record<string, string> = {
  Institution: 'account_balance',
  Business: 'business',
  Enrollment: 'school',
  Contract: 'description',
  Attendance: 'event_available',
  Payment: 'payments',
  Coordination: 'supervisor_account',
  Internship: 'work_history',
  Reporting: 'bar_chart',
  Security: 'manage_accounts',
}

// Event labels
const EVENT_LABELS: Record<string, string> = {
  'contract.created': 'Yeni sözleşme oluşturuldu',
  'contract.submitted': 'Sözleşme imzaya gönderildi',
  'contract.signed': 'Sözleşme imzalandı',
  'contract.activated': 'Sözleşme aktifleştirildi',
  'contract.suspended': 'Sözleşme askıya alındı',
  'contract.terminated': 'Sözleşme feshedildi',
  'contract.completed': 'Sözleşme tamamlandı',
  'attendance.recorded': 'Devamsızlık kaydedildi',
  'attendance.verified': 'Devamsızlık doğrulandı',
  'attendance.corrected': 'Devamsızlık düzeltildi',
  'payment.calculated': 'Maaş hesaplandı',
  'payment.receipt.uploaded': 'Dekont yüklendi',
  'payment.approved': 'Ödeme onaylandı',
  'business.registered': 'Yeni işletme kaydedildi',
  'business.approved': 'İşletme onaylandı',
  'student.registered': 'Öğrenci kaydedildi',
  'placement.created': 'Öğrenci yerleştirildi',
  'placement.transferred': 'Öğrenci transfer edildi',
  'visit.created': 'Rehberlik ziyareti eklendi',
  'visit.approved': 'Ziyaret onaylandı',
  'evaluation.created': 'İşletme değerlendirmesi eklendi',
  'invitation.created': 'Yeni davet gönderildi',
  'invitation.approved': 'Davet onaylandı',
}

export function useDashboardActivity(options: UseDashboardActivityOptions) {
  const { notificationStore } = options

  function eventLabel(eventType: string): string {
    return EVENT_LABELS[eventType] ?? eventType
  }

  function timeAgo(iso: string): string {
    const diff = Date.now() - new Date(iso).getTime()
    const mins = Math.floor(diff / 60000)
    if (mins < 1) return 'Az önce'
    if (mins < 60) return `${mins} dk önce`
    const hours = Math.floor(mins / 60)
    if (hours < 24) return `${hours} sa önce`
    const days = Math.floor(hours / 24)
    return `${days} gün önce`
  }

  // Recent notifications (max 8)
  const recentNotifications = computed(() => notificationStore.notifications.slice(0, 8))

  return {
    MODULE_ICONS,
    recentNotifications,
    eventLabel,
    timeAgo,
  }
}
