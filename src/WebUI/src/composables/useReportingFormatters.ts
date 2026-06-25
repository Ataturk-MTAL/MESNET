import {
  MEB_FORM_LABELS,
  BATCH_FORM_TYPES,
  DOCUMENT_STATUS_LABELS,
  DOCUMENT_STATUS_COLORS,
} from 'src/api/reporting'

export function useReportingFormatters() {
  const formTypeOptions = Object.entries(MEB_FORM_LABELS).map(([value, label]) => ({ value, label }))
  const statusOptions = Object.entries(DOCUMENT_STATUS_LABELS).map(([value, label]) => ({ value, label }))
  const batchFormTypeOptions = Object.entries(BATCH_FORM_TYPES).map(([value, label]) => ({ value, label }))

  function formTypeLabel(formType: string): string {
    return MEB_FORM_LABELS[formType] ?? formType
  }

  function statusLabel(status: string): string {
    return DOCUMENT_STATUS_LABELS[status] ?? status
  }

  function statusColor(status: string): string {
    return DOCUMENT_STATUS_COLORS[status] ?? 'grey'
  }

  function formatDate(dateStr: string): string {
    return new Date(dateStr).toLocaleDateString('tr-TR', {
      day: '2-digit',
      month: '2-digit',
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
    })
  }

  return {
    formTypeOptions,
    statusOptions,
    batchFormTypeOptions,
    formTypeLabel,
    statusLabel,
    statusColor,
    formatDate,
  }
}
