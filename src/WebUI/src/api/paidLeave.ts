import api from 'boot/axios'
import type { PagedResponse, PaginationParams } from 'src/types/pagination'

/**
 * MESEM ücretli izin başvurusu (#177).
 *
 * Zincir: öğrenci başvurur → işletme onaylar → okul (müdür yrd./müdür) onaylar → resmileşir.
 * Resmileşince tarih aralığındaki çalışma günleri için "Ücretli İzin" devamsızlık kaydı açılır;
 * o tür ücret kesintisi doğurmaz. Onay öncesi hiçbir kayıt açılmaz.
 */
export type PaidLeaveStatus = 'PendingBusiness' | 'PendingSchool' | 'Approved' | 'Rejected'

export interface PaidLeaveRequestDto {
  id: string
  studentId: string
  businessId: string
  institutionId: string
  academicPeriodId: string
  startDate: string
  endDate: string
  dayCount: number
  reason: string
  status: PaidLeaveStatus
  statusSlug: string
  // Aktör alanları hem kimlik hem çözümlenmiş ad taşır (#139); ad backend'de saklanmaz.
  requestedById: string
  requestedByName: string | null
  requestedAt: string
  businessApprovedById: string | null
  businessApprovedByName: string | null
  businessApprovedAt: string | null
  approvedById: string | null
  approvedByName: string | null
  approvedAt: string | null
  rejectedById: string | null
  rejectedByName: string | null
  rejectedAt: string | null
  rejectionReason: string | null
}

export interface CreatePaidLeaveRequest {
  startDate: string
  endDate: string
  reason: string
}

export const PAID_LEAVE_STATUSES = [
  { label: 'İşletme Onayı Bekliyor', value: 'PendingBusiness' },
  { label: 'Okul Onayı Bekliyor', value: 'PendingSchool' },
  { label: 'Resmileşti', value: 'Approved' },
  { label: 'Reddedildi', value: 'Rejected' },
] as const

export const paidLeaveApi = {
  // Kapsam sunucuda claim'lerden çözülür: okul kurumu, işletme kendi başvurularını,
  // öğrenci yalnız kendisininkini görür.
  list: (params?: { status?: string; academicPeriodId?: string } & PaginationParams) =>
    api.get<PagedResponse<PaidLeaveRequestDto>>('/attendance/paid-leave', { params }),

  // StudentId gönderilmez — sunucu token'daki student_id claim'inden alır.
  create: (academicPeriodId: string, data: CreatePaidLeaveRequest) =>
    api.post<{ requestId: string }>('/attendance/paid-leave', data, {
      params: { academicPeriodId },
    }),

  businessApprove: (requestId: string) =>
    api.post(`/attendance/paid-leave/${requestId}/business-approve`),

  businessReject: (requestId: string, reason: string) =>
    api.post(`/attendance/paid-leave/${requestId}/business-reject`, { reason }),

  approve: (requestId: string) => api.post(`/attendance/paid-leave/${requestId}/approve`),

  reject: (requestId: string, reason: string) =>
    api.post(`/attendance/paid-leave/${requestId}/reject`, { reason }),
}

/** Tek başvurunun kapsayabileceği azami gün — backend PaidLeaveApprovalPolicy ile aynı. */
export const PAID_LEAVE_MAX_DAYS = 60
