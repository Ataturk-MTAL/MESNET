import { ref, type Ref } from 'vue'
import { type DailyScheduleDto } from 'src/api/coordination'
import type { PendingChange } from 'src/composables/useAssignmentDnD'

export interface UseTeacherChangeFlowOptions {
  selectedTeacherId: Ref<string | null>
  rawSchedule: Ref<DailyScheduleDto[]>
  pendingChanges: Ref<PendingChange[]>
  clearPending: () => void
  loadTeacherSchedule: (teacherId: string) => Promise<void>
}

export function useTeacherChangeFlow(options: UseTeacherChangeFlowOptions) {
  const {
    selectedTeacherId, rawSchedule, pendingChanges,
    clearPending, loadTeacherSchedule,
  } = options

  const showDiscardDialog = ref(false)
  const pendingTeacherId = ref<string | null>(null)

  function onTeacherChange(teacherId: string | null) {
    if (pendingChanges.value.length > 0 && teacherId !== selectedTeacherId.value) {
      pendingTeacherId.value = teacherId
      showDiscardDialog.value = true
      return
    }
    doTeacherChange(teacherId)
  }

  function confirmDiscard() {
    showDiscardDialog.value = false
    clearPending()
    doTeacherChange(pendingTeacherId.value)
    pendingTeacherId.value = null
  }

  function doTeacherChange(teacherId: string | null) {
    selectedTeacherId.value = teacherId
    rawSchedule.value = []
    if (teacherId) {
      loadTeacherSchedule(teacherId)
    }
  }

  function selectTeacher(teacherId: string) {
    if (pendingChanges.value.length > 0) {
      pendingTeacherId.value = teacherId
      showDiscardDialog.value = true
      return
    }
    doTeacherChange(teacherId)
  }

  return {
    showDiscardDialog,
    pendingTeacherId,
    onTeacherChange,
    confirmDiscard,
    doTeacherChange,
    selectTeacher,
  }
}
