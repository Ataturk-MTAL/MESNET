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

  /**
   * Geçmiş alan bazlıdır (#114): aynı işletmenin farklı alanlardaki atama geçmişleri
   * ayrı satırlarda tutulur, bu yüzden alan kodu ve dönem birlikte gönderilir.
   */
  async function showHistory(
    businessId: string,
    businessName: string,
    branchCode: string,
    academicPeriodId: string,
  ) {
    historyBusinessName.value = businessName
    historyEntries.value = []
    historyDialog.value = true
    historyLoading.value = true
    try {
      const { data } = await coordinationApi.getAssignmentHistory(businessId, {
        branchCode,
        academicPeriodId,
      })
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
      case 'Assigned': return 'positive'
      case 'SlotAdded': return 'info'
      case 'SlotRemoved': return 'warning'
      case 'Unassigned': return 'negative'
      case 'HoursUpdated': return 'secondary'
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
