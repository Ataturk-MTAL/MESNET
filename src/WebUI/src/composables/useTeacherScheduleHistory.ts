import { ref, type Ref } from 'vue'
import {
  coordinationApi,
  type ScheduleHistoryDto,
} from 'src/api/coordination'

export interface UseTeacherScheduleHistoryOptions {
  selectedTeacherId: Ref<string | null>
  currentScheduleId: Ref<string | null>
}

export function useTeacherScheduleHistory(options: UseTeacherScheduleHistoryOptions) {
  const { selectedTeacherId, currentScheduleId } = options

  const historyLoading = ref(false)
  const scheduleHistory = ref<ScheduleHistoryDto | null>(null)

  async function loadHistory() {
    if (!selectedTeacherId.value || !currentScheduleId.value) {
      scheduleHistory.value = null
      return
    }

    historyLoading.value = true
    try {
      const { data } = await coordinationApi.getScheduleHistory(
        selectedTeacherId.value,
        currentScheduleId.value,
      )
      scheduleHistory.value = data
    } catch {
      scheduleHistory.value = null
    } finally {
      historyLoading.value = false
    }
  }

  return {
    historyLoading,
    scheduleHistory,
    loadHistory,
  }
}
