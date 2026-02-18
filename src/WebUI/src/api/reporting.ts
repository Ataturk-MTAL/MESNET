import api from 'boot/axios'

export interface ReportRequestDto {
  type: string
  institutionId: string
  filters?: Record<string, string>
}

export const reportingApi = {
  generateReport: (data: ReportRequestDto) =>
    api.post('/reporting/generate', data, { responseType: 'blob' }),

  getInternshipReport: (institutionId: string, params?: { year?: number; branchCode?: string }) =>
    api.get('/reporting/internship', { params: { institutionId, ...params }, responseType: 'blob' }),

  getAttendanceReport: (institutionId: string, params?: { studentId?: string; month?: string }) =>
    api.get('/reporting/attendance', { params: { institutionId, ...params }, responseType: 'blob' }),

  getPaymentReport: (institutionId: string, params?: { month?: string }) =>
    api.get('/reporting/payment', { params: { institutionId, ...params }, responseType: 'blob' }),
}

/** Blob'u tarayıcıda indirme yardımcı fonksiyonu */
export function downloadBlob(blob: Blob, filename: string) {
  const url = URL.createObjectURL(blob)
  const a = document.createElement('a')
  a.href = url
  a.download = filename
  a.click()
  URL.revokeObjectURL(url)
}
