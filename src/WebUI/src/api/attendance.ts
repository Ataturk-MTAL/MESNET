import api from 'boot/axios'
import type { PagedResponse, PaginationParams } from 'src/types/pagination'

export interface AttendanceRecordDto {
  id: string
  studentId: string
  businessId: string
  institutionId: string
  date: string
  absenceType: string
  absenceTypeSlug: string
  reason: string | null
  status: string
  statusSlug: string
  healthReportUrl: string | null
  markedBy: string
  markedAt: string
  approvedBy: string | null
  approvedAt: string | null
  verifiedBy: string | null
  verifiedAt: string | null
}

export interface AttendanceViewDto {
  id: string
  studentId: string
  businessId: string
  totalAbsenceDays: number
  excusedDays: number
  unexcusedDays: number
  limitExceeded: boolean
  lastUpdated: string
}

export interface CreateAttendanceRequest {
  studentId: string
  businessId: string
  institutionId: string
  academicPeriodId: string
  date: string
  absenceType: string
  reason?: string
}

export interface CorrectAttendanceRequest {
  absenceType: string
  reason?: string
}

// Ücret kesintisi yalnız "Mazeretsiz" ve "Ücretsiz İzin" günlerinde yapılır
// (3308 / MEB Ortaöğretim Kurumları Yönetmeliği — bkz. backend AbsenceType.AffectsSalary).
export const ABSENCE_TYPES = [
  { label: 'Mazeretli', value: 'Excused' },
  { label: 'Mazeretsiz', value: 'Unexcused' },
  { label: 'Sağlık Raporu', value: 'HealthReport' },
  { label: 'Ücretsiz İzin', value: 'UnpaidLeave' },
  { label: 'Ücretli İzin', value: 'PaidLeave' },
] as const

export const attendanceApi = {
  list: (params?: { studentId?: string; businessId?: string; institutionId?: string; academicPeriodId?: string; status?: string; year?: number; month?: number } & PaginationParams) =>
    api.get<PagedResponse<AttendanceRecordDto>>('/attendance', { params }),

  get: (attendanceId: string) =>
    api.get<AttendanceRecordDto>(`/attendance/${attendanceId}`),

  create: (data: CreateAttendanceRequest) =>
    api.post<{ attendanceId: string }>('/attendance', data),

  approve: (attendanceId: string) =>
    api.post(`/attendance/${attendanceId}/approve`),

  verify: (attendanceId: string) =>
    api.post(`/attendance/${attendanceId}/verify`),

  correct: (attendanceId: string, data: CorrectAttendanceRequest) =>
    api.post(`/attendance/${attendanceId}/correct`, data),

  remove: (attendanceId: string) =>
    api.delete(`/attendance/${attendanceId}`),

  uploadHealthReport: (attendanceId: string, file: File) => {
    const formData = new FormData()
    formData.append('file', file)
    return api.post(`/attendance/${attendanceId}/health-report`, formData, {
      headers: { 'Content-Type': 'multipart/form-data' },
    })
  },
}
