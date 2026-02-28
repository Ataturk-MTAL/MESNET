import api from 'boot/axios'

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

export const enrollmentApi = {
  listStudents: (params?: { institutionId?: string; academicPeriodId?: string; branchCode?: string; section?: string; status?: string }) =>
    api.get<StudentProfileDto[]>('/students', { params }),

  getStudent: (studentId: string) =>
    api.get<StudentProfileDto>(`/students/${studentId}`),

  registerStudent: (data: RegisterStudentRequest) =>
    api.post<{ studentId: string }>('/students', data),

  updateStudent: (studentId: string, data: UpdateStudentRequest) =>
    api.patch(`/students/${studentId}`, data),

  listPlacements: (params?: { businessId?: string; studentId?: string; academicPeriodId?: string; status?: string }) =>
    api.get<InternshipPlacementDto[]>('/placements', { params }),

  getPlacement: (placementId: string) =>
    api.get<InternshipPlacementDto>(`/placements/${placementId}`),

  createPlacement: (data: CreatePlacementRequest) =>
    api.post<{ placementId: string }>('/placements', data),

  transferStudent: (placementId: string, data: TransferRequest) =>
    api.post(`/placements/${placementId}/transfer`, data),

  listTeachers: (params?: { institutionId?: string; academicPeriodId?: string }) =>
    api.get<TeacherProfileDto[]>('/teachers', { params }),
}
