import { storeToRefs } from 'pinia'
import { type DailyScheduleDto } from 'src/api/coordination'
import { useInstitutionStore } from 'stores/institution'
import type { useAuthStore } from 'stores/auth'

export interface UseScheduleConfigOptions {
  // Store auth bilgisini kendi aldığı için artık kullanılmaz; imza uyumu için tutulur.
  authStore?: ReturnType<typeof useAuthStore>
}

/**
 * useInstitutionStore üzerine ince wrapper. Ders programı config'i (günlük ders sayısı)
 * artık merkezi store cache'inden okunur — doğrudan API çağrısı yapılmaz.
 * Dönüş şekli korunur ki mevcut tüketiciler (BusinessAssignmentPage) değişmesin.
 */
export function useScheduleConfig(_options: UseScheduleConfigOptions = {}) {
  const institutionStore = useInstitutionStore()
  const { periodCount, scheduleConfigMissing } = storeToRefs(institutionStore)

  async function loadScheduleConfig() {
    await institutionStore.loadScheduleConfig()
  }

  function createEmptySchedule(): DailyScheduleDto[] {
    const days = ['Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday']
    return days.map((day) => ({
      day,
      periods: Array.from({ length: institutionStore.periodCount }, (_, i) => ({
        periodNumber: i + 1,
        status: 'Free',
        courseName: null,
        assignedBusinessId: null,
      })),
    }))
  }

  return {
    periodCount,
    scheduleConfigMissing,
    loadScheduleConfig,
    createEmptySchedule,
  }
}
