import api from 'boot/axios'
import type { PagedResponse, PaginationParams } from 'src/types/pagination'

export interface StudentProfileDto {
  id: string
  keycloakUserId: string
  fullName: string
  institutionId: string
  branchCode: string
  branchName: string
  specializationCode: string | null
  specializationName: string | null
  classYear: number
  educationType: string
  educationTypeSlug: string
  section: string | null
  studentNumber: string | null
  phoneNumber: string | null
  tcKimlikNo: string | null
  guardianName: string | null
  guardianPhone: string | null
  status: string
  statusSlug: string
  registeredAt: string
}

export interface TeacherProfileDto {
  id: string
  keycloakUserId: string
  fullName: string
  institutionId: string
  registeredAt: string
  branchCode?: string | null
}

export interface InternshipPlacementDto {
  id: string
  studentId: string
  studentName: string
  businessId: string
  businessName: string
  institutionId: string
  academicPeriodId: string
  teacherId: string | null
  teacherName: string | null
  branchCode: string
  status: string
  statusSlug: string
  source: string
  sourceSlug: string
  placedAt: string
}

export interface RegisterStudentRequest {
  keycloakUserId: string
  fullName: string
  branchCode: string
  branchName?: string
  academicPeriodId?: string
  educationType: string
  specializationCode?: string
  specializationName?: string
  classYear: number
  section?: string
  studentNumber?: string
  phoneNumber?: string
  tcKimlikNo?: string
  guardianName?: string
  guardianPhone?: string
  /** Yaşa uygun asgari ücret hesabı için (3308 md.25) — ISO tarih (#85) */
  birthDate?: string
  /** Student | CandidateApprentice | Apprentice — ücret tabanını belirler (#85) */
  category?: string
}

export interface CreatePlacementRequest {
  studentId: string
  businessId: string
  teacherId?: string
}

export interface UpdateStudentRequest {
  fullName?: string
  branchCode?: string
  branchName?: string
  specializationCode?: string
  specializationName?: string
  classYear?: number
  section?: string
  studentNumber?: string
  phoneNumber?: string
  tcKimlikNo?: string
  guardianName?: string
  guardianPhone?: string
}

export const EDUCATION_TYPES = [
  { label: 'Örgün', value: 'Formal' },
  { label: 'MESEM', value: 'Mesem' },
] as const

export const enrollmentApi = {
  listStudents: (params?: { institutionId?: string; academicPeriodId?: string; branchCode?: string; section?: string; status?: string } & PaginationParams) =>
    api.get<PagedResponse<StudentProfileDto>>('/students', { params }),

  getStudent: (studentId: string) =>
    api.get<StudentProfileDto>(`/students/${studentId}`),

  registerStudent: (data: RegisterStudentRequest) =>
    api.post<{ studentId: string }>('/students', data),

  updateStudent: (studentId: string, data: UpdateStudentRequest) =>
    api.patch(`/students/${studentId}`, data),

  listPlacements: (params?: { businessId?: string; studentId?: string; academicPeriodId?: string; status?: string; branchCode?: string } & PaginationParams) =>
    api.get<PagedResponse<InternshipPlacementDto>>('/placements', { params }),

  // Durum-bazında TOPLAM sayım (sayfalamadan bağımsız) — overview kartları için. status filtresi taşımaz.
  getPlacementStatusCounts: (params?: { academicPeriodId?: string; branchCode?: string }) =>
    api.get<Record<string, number>>('/placements/status-counts', { params }),

  getPlacement: (placementId: string) =>
    api.get<InternshipPlacementDto>(`/placements/${placementId}`),

  createPlacement: (data: CreatePlacementRequest) =>
    api.post<{ placementId: string }>('/placements', data),

  deregisterStudent: (studentId: string, reason: string) =>
    api.post(`/students/${studentId}/deregister`, { reason }),

  syncStudentCounts: (institutionId: string, academicPeriodId: string) =>
    api.post<{ syncedBranches: number; counts: Record<string, Record<string, number>> }>('/students/sync-counts', { institutionId, academicPeriodId }),

  listTeachers: (params?: { institutionId?: string; academicPeriodId?: string; branchCode?: string } & PaginationParams) =>
    api.get<PagedResponse<TeacherProfileDto>>('/teachers', { params }),
}
