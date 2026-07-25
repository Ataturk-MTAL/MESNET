import api from 'boot/axios'
import type { PagedResponse, PaginationParams } from 'src/types/pagination'

export interface PaymentSummaryDto {
  id: string
  studentId: string
  studentName: string
  studentNumber: string
  branchCode: string
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
  list: (params?: { studentId?: string; businessId?: string; institutionId?: string; academicPeriodId?: string; phase?: string; month?: string; branchCode?: string; monthFrom?: string; monthTo?: string } & PaginationParams) =>
    api.get<PagedResponse<PaymentSummaryDto>>('/payments', { params }),

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

  /**
   * Asgari ücret güncelleme. Alan adları backend'deki UpdateMinimumWage komutuyla birebir
   * olmalı — önceki hâli `{ amount, effectiveDate }` gönderiyordu ve hiçbir alan eşleşmiyordu.
   * minimumWageUnder16: 16 yaşından küçükler için yaşa uygun asgari ücret (#85).
   */
  updateMinimumWage: (
    institutionId: string,
    newMinimumWage: number,
    effectiveFrom: string,
    updatedBy: string,
    newMinimumWageUnder16?: number,
  ) =>
    api.put('/payments/config/minimum-wage', {
      institutionId,
      newMinimumWage,
      newMinimumWageUnder16: newMinimumWageUnder16 ?? null,
      effectiveFrom,
      updatedBy,
    }),
}
