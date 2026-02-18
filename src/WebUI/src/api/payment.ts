import api from 'boot/axios'

export interface PaymentSummaryDto {
  id: string
  studentId: string
  businessId: string
  institutionId: string
  month: string
  baseWage: number
  deductionAmount: number
  netAmount: number
  governmentContribution: number
  employerPayment: number
  phase: string
  phaseSlug: string
  receiptId: string | null
  receiptObjectPath: string | null
  uploadedByStudent: boolean
  receiptDueDate: string | null
  studentConfirmedAt: string | null
  teacherApprovedAt: string | null
  deputyApprovedAt: string | null
  lastUpdated: string
}

export const PAYMENT_PHASES = [
  { label: 'Hesaplandı', value: 'Calculated' },
  { label: 'Dekont Bekleniyor', value: 'AwaitingReceipt' },
  { label: 'Dekont Yüklendi', value: 'ReceiptUploaded' },
  { label: 'Öğrenci Onayladı', value: 'StudentConfirmed' },
  { label: 'Öğretmen Onayladı', value: 'TeacherApproved' },
  { label: 'Müdür Yardımcısı Onayladı', value: 'DeputyApproved' },
  { label: 'Tamamlandı', value: 'Completed' },
  { label: 'Reddedildi', value: 'Rejected' },
] as const

export const paymentApi = {
  list: (params?: { studentId?: string; businessId?: string; institutionId?: string; phase?: string; month?: string }) =>
    api.get<PaymentSummaryDto[]>('/payments', { params }),

  get: (paymentId: string) =>
    api.get<PaymentSummaryDto>(`/payments/${paymentId}`),

  uploadReceiptBusiness: (paymentId: string, file: File) => {
    const formData = new FormData()
    formData.append('file', file)
    return api.post(`/payments/${paymentId}/upload-receipt/business`, formData, {
      headers: { 'Content-Type': 'multipart/form-data' },
    })
  },

  uploadReceiptStudent: (paymentId: string, file: File) => {
    const formData = new FormData()
    formData.append('file', file)
    return api.post(`/payments/${paymentId}/upload-receipt/student`, formData, {
      headers: { 'Content-Type': 'multipart/form-data' },
    })
  },

  confirm: (paymentId: string) =>
    api.post(`/payments/${paymentId}/confirm`),

  approveTeacher: (paymentId: string) =>
    api.post(`/payments/${paymentId}/approve/teacher`),

  approveDeputy: (paymentId: string) =>
    api.post(`/payments/${paymentId}/approve/deputy`),

  reject: (paymentId: string, reason: string) =>
    api.post(`/payments/${paymentId}/reject`, { reason }),

  updateMinimumWage: (amount: number, effectiveDate: string) =>
    api.put('/payments/config/minimum-wage', { amount, effectiveDate }),
}
