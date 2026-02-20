import api from 'boot/axios'

export interface GeoLocation {
  latitude: number
  longitude: number
}

export interface SpecializationDto {
  code: string
  name: string
  isActive: boolean
}

export interface InstitutionBranchDto {
  fieldCode: string
  fieldName: string
  type: string
  typeSlug: string
  activeSpecializations: string[]
  availableCount: number
  atWorkCount: number
  totalCount: number
  isActive: boolean
}

export interface StaffMemberDto {
  id: string
  fullName: string
  role: string
  roleSlug: string
  branchCode: string | null
  authorizedAt: string
}

export interface InstitutionDto {
  id: string
  institutionCode: number
  fullName: string
  address: string | null
  phoneNumber: string | null
  email: string | null
  webUrl: string | null
  location: GeoLocation | null
  branches: InstitutionBranchDto[]
  staff: StaffMemberDto[]
}

export interface UpdateInstitutionRequest {
  fullName: string
  address?: string
  phoneNumber?: string
  email?: string
  webUrl?: string
  location?: GeoLocation
}

export interface AuthorizeStaffRequest {
  keycloakUserId: string
  fullName: string
  role: string
  branchCode?: string
}

export interface FieldOfStudyDto {
  id: string
  code: string
  name: string
  type: string
  typeSlug: string
  specializations: SpecializationDto[]
  isProtocolBased: boolean
  isActive: boolean
}

export interface ScheduleConfigDto {
  configured: boolean
  dailyPeriodCount?: number
  updatedAt?: string
  updatedBy?: string
}

export interface UpdateScheduleConfigRequest {
  dailyPeriodCount: number
  updatedBy: string
}

export interface UpdateSpecializationsRequest {
  activeSpecializations: string[]
}

export const institutionApi = {
  get: (institutionId: string) =>
    api.get<InstitutionDto>(`/institutions/${institutionId}`),

  update: (institutionId: string, data: UpdateInstitutionRequest) =>
    api.put(`/institutions/${institutionId}`, data),

  authorizeStaff: (institutionId: string, data: AuthorizeStaffRequest) =>
    api.post(`/institutions/${institutionId}/staff`, data),

  getFieldCatalog: (educationType?: string) =>
    api.get<FieldOfStudyDto[]>('/field-catalog', {
      params: educationType ? { educationType } : undefined,
    }),

  activateBranch: (institutionId: string, fieldCode: string) =>
    api.post(`/institutions/${institutionId}/branches`, { fieldCode }),

  deactivateBranch: (institutionId: string, fieldCode: string) =>
    api.delete(`/institutions/${institutionId}/branches/${fieldCode}`),

  getScheduleConfig: (institutionId: string) =>
    api.get<ScheduleConfigDto>(`/institutions/${institutionId}/schedule-config`),

  updateScheduleConfig: (institutionId: string, data: UpdateScheduleConfigRequest) =>
    api.put(`/institutions/${institutionId}/schedule-config`, data),

  updateSpecializations: (institutionId: string, fieldCode: string, data: UpdateSpecializationsRequest) =>
    api.put(`/institutions/${institutionId}/branches/${fieldCode}/specializations`, data),
}
