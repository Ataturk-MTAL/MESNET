import { ref } from 'vue'
import { type DailyScheduleDto } from 'src/api/coordination'
import { institutionApi } from 'src/api/institution'
import type { useAuthStore } from 'stores/auth'

export interface UseScheduleConfigOptions {
  authStore: ReturnType<typeof useAuthStore>
}

export function useScheduleConfig(options: UseScheduleConfigOptions) {
  const { authStore } = options

  const periodCount = ref(0)
  const scheduleConfigMissing = ref(false)

  async function loadScheduleConfig() {
    const instId = authStore.user?.institutionId
    if (!instId) return
    try {
      const { data } = await institutionApi.getScheduleConfig(instId)
      if (data.configured && data.dailyPeriodCount) {
        periodCount.value = data.dailyPeriodCount
        scheduleConfigMissing.value = false
      } else {
        periodCount.value = 0
        scheduleConfigMissing.value = true
      }
    } catch {
      periodCount.value = 0
      scheduleConfigMissing.value = true
    }
  }

  function createEmptySchedule(): DailyScheduleDto[] {
    const days = ['Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday']
    return days.map((day) => ({
      day,
      periods: Array.from({ length: periodCount.value }, (_, i) => ({
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
