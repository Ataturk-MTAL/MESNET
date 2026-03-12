import { ref, type Ref, type ComputedRef } from 'vue'
import { coordinationApi, type TeacherSummaryRowDto } from 'src/api/coordination'
import type { useTeacherOptions } from 'src/composables/useEntityOptions'
import type { useNotify } from 'src/composables/useNotify'

export const teacherOverviewColumns = [
  { name: 'teacherName', label: 'Öğretmen', field: 'teacherId', align: 'left' as const, sortable: false },
  { name: 'businessCount', label: 'İşletme', field: 'businessCount', align: 'center' as const, sortable: true },
  { name: 'assignedHours', label: 'Saat', field: 'assignedHours', align: 'center' as const, sortable: true },
  { name: 'monday', label: 'Pzt', field: 'freeSlotsByDay', align: 'center' as const, sortable: false },
  { name: 'tuesday', label: 'Sal', field: 'freeSlotsByDay', align: 'center' as const, sortable: false },
  { name: 'wednesday', label: 'Çar', field: 'freeSlotsByDay', align: 'center' as const, sortable: false },
  { name: 'thursday', label: 'Per', field: 'freeSlotsByDay', align: 'center' as const, sortable: false },
  { name: 'friday', label: 'Cum', field: 'freeSlotsByDay', align: 'center' as const, sortable: false },
  { name: 'workload', label: 'Yük', field: 'assignedHours', align: 'center' as const, sortable: false },
]

export interface UseTeacherOverviewOptions {
  periodId: ComputedRef<string | null> | Ref<string | null>
  semester: ComputedRef<string> | Ref<string>
  branchFilter: Ref<string | null>
  teacherOpts: ReturnType<typeof useTeacherOptions>
  notify: ReturnType<typeof useNotify>
}

export function useTeacherOverview(options: UseTeacherOverviewOptions) {
  const { periodId, semester, branchFilter, teacherOpts, notify } = options

  const teacherOverviewRows = ref<TeacherSummaryRowDto[]>([])
  const teacherOverviewLoading = ref(false)

  function teacherName(teacherId: string): string {
    return teacherOpts.allOptions.value.find((o) => o.value === teacherId)?.label ?? teacherId
  }

  async function loadTeacherOverview() {
    if (!periodId.value) return

    teacherOverviewLoading.value = true
    try {
      const { data } = await coordinationApi.getAllTeachersOverview(
        periodId.value,
        semester.value,
        branchFilter.value ?? undefined,
      )
      teacherOverviewRows.value = data
    } catch (e) {
      notify.apiError(e, 'Öğretmen özeti yüklenirken hata oluştu.')
    } finally {
      teacherOverviewLoading.value = false
    }
  }

  return {
    teacherOverviewRows,
    teacherOverviewLoading,
    teacherName,
    loadTeacherOverview,
  }
}
