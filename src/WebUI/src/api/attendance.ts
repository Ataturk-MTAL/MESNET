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
  // Aktör alanları hem kimlik hem çözümlenmiş ad taşır (#139). Ad backend'de saklanmaz;
  // okuma anında UserNameView'dan çözülür ve bilinmiyorsa null gelir.
  markedById: string
  markedByName: string | null
  markedAt: string
  approvedById: string | null
  approvedByName: string | null
  approvedAt: string | null
  verifiedById: string | null
  verifiedByName: string | null
  verifiedAt: string | null
  // Sağlık raporu onay zinciri (#172). Rapor yüklenmiş olması tek başına hüküm doğurmaz:
  // devamsızlık türü ancak `healthReportStatus === 'Approved'` olduğunda "Sağlık Raporu"na
  // döner ve ücret kesintisi kalkar.
  healthReportStatus: HealthReportStatus
  healthReportStatusSlug: string
  healthReportAttachedById: string | null
  healthReportAttachedByName: string | null
  healthReportAttachedAt: string | null
  healthReportReviewedById: string | null
  healthReportReviewedByName: string | null
  healthReportReviewedAt: string | null
  healthReportRejectionReason: string | null
}

export type HealthReportStatus = 'None' | 'Pending' | 'Approved' | 'Rejected'

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

  // Sağlık raporu yükleme (#172). Alan adı backend'in okuduğu adla birebir aynı olmalıdır:
  // önceki hâlinde 'file' gönderiliyordu ve uç zaten JSON bekliyordu — çağrı hiç yapılmıyordu.
  uploadHealthReport: (attendanceId: string, file: File) => {
    const formData = new FormData()
    formData.append('ReportFile', file)
    return api.post(`/attendance/${attendanceId}/health-report`, formData, {
      headers: { 'Content-Type': 'multipart/form-data' },
    })
  },

  approveHealthReport: (attendanceId: string) =>
    api.post(`/attendance/${attendanceId}/health-report/approve`),

  rejectHealthReport: (attendanceId: string, reason: string) =>
    api.post(`/attendance/${attendanceId}/health-report/reject`, { reason }),
}

// Yüklenebilecek rapor türleri — backend AttachHealthReportHandler ile aynı küme.
export const HEALTH_REPORT_ACCEPT = '.pdf,.jpg,.jpeg,.png'
export const HEALTH_REPORT_MAX_BYTES = 10 * 1024 * 1024
