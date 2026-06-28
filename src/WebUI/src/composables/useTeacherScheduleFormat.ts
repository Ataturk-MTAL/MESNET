/** TeacherSchedulePage tarih biçimlendirme yardımcıları (tr-TR). */
export function useTeacherScheduleFormat() {
  function formatDate(dateStr: string): string {
    return new Date(dateStr).toLocaleDateString('tr-TR', {
      day: '2-digit',
      month: '2-digit',
      year: 'numeric',
    })
  }

  function formatDateTime(dateStr: string): string {
    return new Date(dateStr).toLocaleString('tr-TR', {
      day: '2-digit',
      month: '2-digit',
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
    })
  }

  return {
    formatDate,
    formatDateTime,
  }
}
