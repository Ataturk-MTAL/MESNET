import api from 'boot/axios'
import type { PagedResponse, PaginationParams } from 'src/types/pagination'

export interface InternshipSummaryDto {
  id: string
  placementId: string
  studentId: string
  studentName: string
  businessId: string
  businessName: string
  institutionId: string
  academicPeriodId: string
  contractId: string | null
  phase: string
  phaseSlug: string
  startedAt: string
  completedAt: string | null
  totalAbsenceDays: number
  completedVisits: number
  confirmedPayments: number
  lastUpdated: string
}

export const internshipApi = {
  listInternships: (params?: {
    studentId?: string
    businessId?: string
    institutionId?: string
    academicPeriodId?: string
    phase?: string
    minAbsenceDays?: number
  } & PaginationParams) =>
    api.get<PagedResponse<InternshipSummaryDto>>('/internships', { params }),

  markAsFailedToComplete: (placementId: string) =>
    api.post(`/placements/${placementId}/mark-failed`),
}
