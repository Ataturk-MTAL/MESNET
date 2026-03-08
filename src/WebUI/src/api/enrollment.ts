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
  teacherId: string | null
  teacherName: string | null
  status: string
  statusSlug: string
  source: string
  sourceSlug: string
  placedAt: string
  transferredAt: string | null
  transferReason: string | null
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

export interface TransferRequest {
  newBusinessId: string
  reason: string
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

  listPlacements: (params?: { businessId?: string; studentId?: string; academicPeriodId?: string; status?: string } & PaginationParams) =>
    api.get<PagedResponse<InternshipPlacementDto>>('/placements', { params }),

  getPlacement: (placementId: string) =>
    api.get<InternshipPlacementDto>(`/placements/${placementId}`),

  createPlacement: (data: CreatePlacementRequest) =>
    api.post<{ placementId: string }>('/placements', data),

  deregisterStudent: (studentId: string, reason: string) =>
    api.post(`/students/${studentId}/deregister`, { reason }),

  transferStudent: (placementId: string, data: TransferRequest) =>
    api.post(`/placements/${placementId}/transfer`, data),

  listTeachers: (params?: { institutionId?: string; academicPeriodId?: string; branchCode?: string } & PaginationParams) =>
    api.get<PagedResponse<TeacherProfileDto>>('/teachers', { params }),
}
