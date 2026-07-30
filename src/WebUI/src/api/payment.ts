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

/**
 * Bir asgari ücret yürürlük dönemi. Asgari ücret yıl içinde birden fazla kez artabildiği için
 * kayıtlar tarih aralıklı tutulur ve maaş hesabı ayın içinde yürürlükte olanı seçer.
 */
export interface SalaryConfigDto {
  id: string
  minimumWage: number
  minimumWageUnder16: number | null
  effectiveFrom: string
  effectiveTo: string | null
  /** Bugün yürürlükte olan dönem. */
  isCurrent: boolean
  /** Yürürlüğü henüz başlamamış (ileri tarihli) dönem. */
  isScheduled: boolean
  updatedById: string
  updatedBy: string | null
  smallBusinessRate: number
  largeBusinessRate: number
  personnelThreshold: number
  apprenticeRate: number
  mem12thGradeRate: number
  govContribSmallNonMEM: number
  govContribLargeNonMEM: number
  govContribMEM: number
}

export interface SalaryConfigHistoryDto {
  items: SalaryConfigDto[]
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
  /**
   * Asgari ücret yürürlük geçmişi — geçmiş, yürürlükteki ve ileri tarihli dönemler.
   * Kurum kapsamı parametre olarak GİTMEZ; backend token'daki institution_id'den okur.
   */
  salaryConfigHistory: () =>
    api.get<SalaryConfigHistoryDto>('/payments/config/minimum-wage'),

  updateMinimumWage: (
    institutionId: string,
    newMinimumWage: number,
    effectiveFrom: string,
    newMinimumWageUnder16?: number,
  ) =>
    api.put('/payments/config/minimum-wage', {
      institutionId,
      newMinimumWage,
      newMinimumWageUnder16: newMinimumWageUnder16 ?? null,
      effectiveFrom,
      // updatedBy gönderilmez — aktör token'dan damgalanır (#137)
    }),
}
