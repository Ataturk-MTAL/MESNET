import api from 'boot/axios'
import type { GeoLocation } from './institution'
import type { PagedResponse, PaginationParams } from 'src/types/pagination'

export interface BusinessCapacityDto {
  totalSlots: number
  occupiedSlots: number
  availableSlots: number
  isFull: boolean
}

export interface BusinessRepresentativeDto {
  id: string
  fullName: string
  phoneNumber: string
  email: string
  authorizedAt: string
}

export interface BusinessDocumentDto {
  id: string
  type: string
  typeSlug: string
  status: string
  statusSlug: string
  fileName: string
  uploadedAt: string
  approvedAt: string | null
  expiresAt: string | null
  rejectionReason: string | null
}

export interface SectorDto {
  name: string
  slug: string
}

/** İşletmenin bir alandan öğrenci alma yetkisi — iptal edilenler de listelenir (#119). */
export interface BranchAuthorizationDto {
  branchCode: string
  basedOnDocumentId: string | null
  authorizedAt: string
  authorizedBy: string
  revokedAt: string | null
  isActive: boolean
}

export interface AuthorizeBranchesRequest {
  branches: { branchCode: string; basedOnDocumentId?: string | null }[]
}

export interface BusinessDto {
  id: string
  name: string
  address: string
  phoneNumber: string | null
  email: string | null
  website: string | null
  status: string
  statusSlug: string
  source: string
  sourceSlug: string
  personnelCount: number
  /**
   * Kamu kurum/kuruluşu mu (#157). 3308 Geçici Madde 12 gereği kamu kurumlarına devlet
   * katkısı ödenmez — bu işaret doğrudan maaş/teşvik hesabını etkiler.
   */
  isPublicInstitution: boolean
  location: GeoLocation | null
  capacity: BusinessCapacityDto
  representatives: BusinessRepresentativeDto[]
  documents: BusinessDocumentDto[]
  sectors: SectorDto[]
  authorizedBranches: BranchAuthorizationDto[]
  /** Yalnız aktif (iptal edilmemiş) alan kodları — boş liste "öğrenci alamaz" demektir. */
  activeBranchCodes: string[]
  createdAt: string
  approvedAt: string | null
  closedAt: string | null
}

export interface RegisterBusinessRequest {
  name: string
  address: string
  phoneNumber?: string
  email?: string
  website?: string
  personnelCount?: number
  /** Kamu kurum/kuruluşu mu (#157) — devlet katkısı hesabını etkiler, kayıt anında girilir. */
  isPublicInstitution?: boolean
  location?: GeoLocation
  sectors?: string[]
}

export interface UpdateBusinessRequest {
  name?: string
  address?: string
  phoneNumber?: string
  email?: string
  website?: string
  personnelCount?: number
  /** Kamu/özel ayrımı düzeltmesi (#157). Gönderilmezse dokunulmaz. */
  isPublicInstitution?: boolean
  location?: GeoLocation
  sectors?: string[]
}

export interface UpdateCapacityRequest {
  totalSlots: number
}

export const businessApi = {
  list: (params?: { status?: string; sector?: string; branchCode?: string } & PaginationParams) =>
    api.get<PagedResponse<BusinessDto>>('/businesses', { params }),

  sectors: () => api.get<SectorDto[]>('/businesses/sectors'),

  get: (businessId: string) =>
    api.get<BusinessDto>(`/businesses/${businessId}`),

  register: (data: RegisterBusinessRequest) =>
    api.post<{ businessId: string }>('/businesses', data),

  update: (businessId: string, data: UpdateBusinessRequest) =>
    api.patch(`/businesses/${businessId}`, data),

  approve: (businessId: string) =>
    api.post(`/businesses/${businessId}/approve`),

  reject: (businessId: string, reason: string) =>
    api.post(`/businesses/${businessId}/reject`, { reason }),

  deactivate: (businessId: string, reason: string) =>
    api.post(`/businesses/${businessId}/deactivate`, { reason }),

  activate: (businessId: string) =>
    api.post(`/businesses/${businessId}/activate`),

  close: (businessId: string) =>
    api.post(`/businesses/${businessId}/close`),

  updateCapacity: (businessId: string, data: UpdateCapacityRequest) =>
    api.put(`/businesses/${businessId}/capacity`, data),

  uploadDocument: (businessId: string, file: File, type: string) => {
    const formData = new FormData()
    formData.append('documentFile', file)
    formData.append('type', type)
    return api.post(`/businesses/${businessId}/documents`, formData)
  },

  getDocumentUrl: (businessId: string, documentId: string) =>
    api.get<{ url: string }>(`/businesses/${businessId}/documents/${documentId}/url`),

  deleteDocument: (businessId: string, documentId: string) =>
    api.delete(`/businesses/${businessId}/documents/${documentId}`),

  approveDocument: (businessId: string, documentId: string) =>
    api.post(`/businesses/${businessId}/documents/${documentId}/approve`),

  /** Alan yetkilerini NİHAİ liste olarak yazar — gönderilmeyen mevcut yetkiler iptal edilir. */
  authorizeBranches: (businessId: string, data: AuthorizeBranchesRequest) =>
    api.put(`/businesses/${businessId}/branch-authorizations`, data),

  searchNearby: (latitude: number, longitude: number, radiusKm: number) =>
    api.get<BusinessDto[]>('/businesses/nearby', { params: { latitude, longitude, radiusKm } }),
}
