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
  /**
   * Kurum ağacındaki düğüm tipi — `Province` / `District` / `School`.
   * İstemci mantığı BUNA bakar; `nodeTypeSlug` yalnız gösterim içindir.
   * Geçiş ucu koşturulmamış eski kayıtlar `School` döner (sunucu çözer).
   */
  nodeType: string
  /** Türkçe etiket — "İl Millî Eğitim Müdürlüğü" / "İlçe Millî Eğitim Müdürlüğü" / "Okul". */
  nodeTypeSlug: string
  /** Üst düğüm kimliği. Kök (il müdürlüğü) için `null`. */
  parentId: string | null
  /** Üst düğümün adı — sunucuda toplu çözülür, istemci ikinci istek atmaz. */
  parentName: string | null
  /**
   * Marka paleti — anahtar VE çözülmüş hex'ler birlikte döner, dördü de ASLA null değildir:
   * kurum hiç seçim yapmadıysa ya da kayıttaki anahtar tanınmıyorsa sunucu varsayılan paleti
   * (Lacivert) çözer. Arayüz bu yüzden temayı TEK istekle uygular; null kontrolü ve ikinci bir
   * katalog isteği gerekmez (katalog yalnız seçim listesi içindir).
   */
  brandPaletteName: string
  brandPaletteSlug: string
  brandPrimary: string
  brandSecondary: string
  branches: InstitutionBranchDto[]
  staff: StaffMemberDto[]
}

/**
 * Küratörlü palet seçeneği (GET /api/institutions/brand-palettes).
 *
 * `name` yetkili anahtardır — PUT gövdesine BU yazılır; `slug` Türkçe etiket, hex'ler
 * önizleme içindir. Hex frontend'de İKİNCİ KEZ TANIMLANMAZ: önizleme de bu uçtan gelen
 * değerle boyanır, yoksa iki kopyadan biri güncellenir diğeri ölçülmemiş renkle kalır.
 */
export interface BrandPaletteDto {
  name: string
  slug: string
  primary: string
  secondary: string
  isDefault: boolean
}

/** Gövde yalnız anahtar taşır; hex GÖNDERİLMEZ, sunucu kabul etmez. */
export interface SetBrandPaletteRequest {
  paletteName: string
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

/** `GET /api/institutions` süzgeçleri. */
export interface InstitutionListParams {
  /** `Province` / `District` / `School`. Verilmezse sunucu OKULLARI döndürür. */
  nodeType?: string
  /** Belirli bir düğümün doğrudan çocukları. */
  parentId?: string
}

export const institutionApi = {
  /**
   * Görünür kurumların SAYFALI listesi.
   *
   * Kapsam sunucudadır: aktörün kurum ağacındaki alt ağacı döner. Kurum üstü aktör
   * (`platform:tenant:manage`) tüm ağacı görür.
   *
   * Varsayılan süzgeç OKUL'dur — il/ilçe müdürlüğü düğümleri açılır listelerde okul gibi
   * görünmesin diye. Üst düğümleri istemek için `nodeType` verin.
   */
  list: (params?: InstitutionListParams & PaginationParams) =>
    api.get<PagedResponse<InstitutionDto>>('/institutions', { params }),

  get: (institutionId: string) =>
    api.get<InstitutionDto>(`/institutions/${institutionId}`),

  listProvinces: () =>
    api.get<ProvinceDto[]>('/institutions/provinces'),

  /** Küratörlü palet kataloğu — kurum kapsamı yok, sorgu parametresi yok. */
  getBrandPalettes: () =>
    api.get<BrandPaletteDto[]>('/institutions/brand-palettes'),

  /** Palet seçimi (institution:manage). Yol parametresi gövdedeki kimliği ezer. */
  setBrandPalette: (institutionId: string, data: SetBrandPaletteRequest) =>
    api.put(`/institutions/${institutionId}/brand-palette`, data),

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
