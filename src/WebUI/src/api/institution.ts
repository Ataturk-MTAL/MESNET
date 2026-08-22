import api from 'boot/axios'
import type { PagedResponse, PaginationParams } from 'src/types/pagination'

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
  departmentHeadCount: number
  workshopHeadCount: number
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
  /** MEB il kodu (01–81) — kapsam anahtarı, serbest metin il adı DEĞİL. */
  provinceCode: string | null
  /** Sunucuda çözülen il adı — yalnız görüntü içindir. */
  provinceName: string | null
  districtName: string | null
  branches: InstitutionBranchDto[]
  staff: StaffMemberDto[]
}

/** İl açılır listesi öğesi — kod yetkili, ad görüntü. */
export interface ProvinceDto {
  code: string
  name: string
}

export interface UpdateInstitutionRequest {
  fullName?: string
  address?: string
  phoneNumber?: string
  email?: string
  webUrl?: string
  location?: GeoLocation
  provinceCode?: string
  districtName?: string
  /** MEB kurum kodu — kayıtta girilir, sonradan düzeltilebilir. */
  institutionCode?: number
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
  updatedById?: string
  updatedByName?: string | null
}

export interface UpdateScheduleConfigRequest {
  dailyPeriodCount: number
}

export interface UpdateSpecializationsRequest {
  activeSpecializations: string[]
}

export interface AcademicPeriodDto {
  id: string
  institutionId: string
  name: string
  startYear: number
  endYear: number
  startDate: string
  endDate: string
  status: string
  statusSlug: string
  createdAt: string
  closedAt: string | null
  gradeEntryStartDate: string | null
  gradeEntryEndDate: string | null
}

export interface CreateAcademicPeriodRequest {
  name: string
  startYear: number
  endYear: number
  startDate: string
  endDate: string
}

export const institutionApi = {
  list: () =>
    api.get<InstitutionDto[]>('/institutions'),

  get: (institutionId: string) =>
    api.get<InstitutionDto>(`/institutions/${institutionId}`),

  listProvinces: () =>
    api.get<ProvinceDto[]>('/institutions/provinces'),

  /** İlin ilçeleri, alfabetik. Listesi tanımlı olmayan il için boş dizi döner. */
  listDistricts: (provinceCode: string) =>
    api.get<string[]>(`/institutions/provinces/${provinceCode}/districts`),

  update: (institutionId: string, data: UpdateInstitutionRequest) =>
    api.patch(`/institutions/${institutionId}`, data),

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

  listAcademicPeriods: (institutionId: string, params?: PaginationParams) =>
    api.get<PagedResponse<AcademicPeriodDto>>(`/institutions/${institutionId}/academic-periods`, { params }),

  getActiveAcademicPeriod: (institutionId: string) =>
    api.get<AcademicPeriodDto>(`/institutions/${institutionId}/academic-periods/active`),

  createAcademicPeriod: (institutionId: string, data: CreateAcademicPeriodRequest) =>
    api.post<{ id: string }>(`/institutions/${institutionId}/academic-periods`, data),

  closeAcademicPeriod: (institutionId: string, periodId: string) =>
    api.post(`/institutions/${institutionId}/academic-periods/${periodId}/close`),

  // Dönem sonu not giriş penceresini aç/güncelle (müdür / müdür yardımcısı)
  setGradeEntryWindow: (
    institutionId: string,
    periodId: string,
    data: { startDate: string; endDate: string },
  ) =>
    api.post(`/institutions/${institutionId}/academic-periods/${periodId}/grade-entry-window`, data),
}
