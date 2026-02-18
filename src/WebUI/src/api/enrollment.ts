import api from 'boot/axios'

export interface StudentProfileDto {
  id: string
  keycloakUserId: string
  fullName: string
  institutionId: string
  branchCode: string
  branchName: string
  classYear: number
  section: string | null
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
}

export interface InternshipPlacementDto {
  id: string
  studentId: string
  businessId: string
  institutionId: string
  teacherId: string | null
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
  classYear: number
  section?: string
}

export interface CreatePlacementRequest {
  studentId: string
  businessId: string
  teacherId?: string
}

export interface TransferRequest {
  newBusinessId: string
  reason: string
}

export const enrollmentApi = {
  listStudents: (params?: { institutionId?: string; branchCode?: string; section?: string; status?: string }) =>
    api.get<StudentProfileDto[]>('/students', { params }),

  getStudent: (studentId: string) =>
    api.get<StudentProfileDto>(`/students/${studentId}`),

  registerStudent: (data: RegisterStudentRequest) =>
    api.post<{ studentId: string }>('/students', data),

  updateStudent: (studentId: string, data: Partial<RegisterStudentRequest>) =>
    api.put(`/students/${studentId}`, data),

  listPlacements: (params?: { businessId?: string; studentId?: string; status?: string }) =>
    api.get<InternshipPlacementDto[]>('/placements', { params }),

  getPlacement: (placementId: string) =>
    api.get<InternshipPlacementDto>(`/placements/${placementId}`),

  createPlacement: (data: CreatePlacementRequest) =>
    api.post<{ placementId: string }>('/placements', data),

  transferStudent: (placementId: string, data: TransferRequest) =>
    api.post(`/placements/${placementId}/transfer`, data),

  listTeachers: (institutionId?: string) =>
    api.get<TeacherProfileDto[]>('/teachers', { params: institutionId ? { institutionId } : undefined }),
}
