import { ref } from 'vue'
import {
  coordinationApi,
  type AssignmentHistoryEntryDto,
} from 'src/api/coordination'
import type { useNotify } from 'src/composables/useNotify'

export interface UseAssignmentHistoryOptions {
  notify: ReturnType<typeof useNotify>
}

export function useAssignmentHistory(options: UseAssignmentHistoryOptions) {
  const { notify } = options

  const historyDialog = ref(false)
  const historyLoading = ref(false)
  const historyBusinessName = ref('')
  const historyEntries = ref<AssignmentHistoryEntryDto[]>([])

  async function showHistory(businessId: string, businessName: string) {
    historyBusinessName.value = businessName
    historyEntries.value = []
    historyDialog.value = true
    historyLoading.value = true
    try {
      const { data } = await coordinationApi.getAssignmentHistory(businessId)
      historyEntries.value = data ?? []
    } catch (e) {
      notify.apiError(e, 'Geçmiş yüklenirken hata oluştu.')
    } finally {
      historyLoading.value = false
    }
  }

  function historyIcon(action: string): string {
    switch (action) {
      case 'Assigned': return 'person_add'
      case 'SlotAdded': return 'add_circle'
      case 'SlotRemoved': return 'remove_circle'
      case 'Unassigned': return 'person_remove'
      case 'HoursUpdated': return 'schedule'
      default: return 'info'
    }
  }

  function historyColor(action: string): string {
    switch (action) {
      case 'Assigned': return 'green'
      case 'SlotAdded': return 'blue'
      case 'SlotRemoved': return 'orange'
      case 'Unassigned': return 'red'
      case 'HoursUpdated': return 'teal'
      default: return 'grey'
    }
  }

  function formatDate(isoDate: string): string {
    return new Date(isoDate).toLocaleString('tr-TR', {
      day: '2-digit',
      month: '2-digit',
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
    })
  }

  return {
    historyDialog,
    historyLoading,
    historyBusinessName,
    historyEntries,
    showHistory,
    historyIcon,
    historyColor,
    formatDate,
  }
}
