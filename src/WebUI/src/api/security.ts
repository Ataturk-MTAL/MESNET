import api from 'boot/axios'
import type { PagedResponse, PaginationParams } from 'src/types/pagination'

export interface UserAccountDto {
  id: string
  keycloakUserId: string
  username: string
  email: string
  firstName: string
  lastName: string
  fullName: string
  isEnabled: boolean
  institutionId: string | null
  businessId: string | null
  roles: string[]
  directPermissions: string[]
  createdAt: string
  updatedAt: string | null
  /** Kullanıcının sorumlu olduğu alan (branş) kodları (#126). Boş olabilir. */
  branchCodes: string[]
  /**
   * Bu kullanıcı için alan girilmesi zorunlu mu? Backend'de permission'dan türetilir.
   * `false` ise boş `branchCodes` beklenen normal durumdur (müdür, müdür yardımcısı).
   */
  branchRequired: boolean
  /** Alan zorunlu ama girilmemiş — "branş atanmamış" rozetiyle gösterilir. */
  branchMissing: boolean
}

export interface InvitationDto {
  id: string
  email: string
  firstName: string
  lastName: string
  fullName: string
  targetRole: string
  status: string
  institutionId: string | null
  businessId: string | null
  createdAt: string
  createdByName: string | null
  approvedAt: string | null
  approvedByName: string | null
  expiresAt: string
  metadata: Record<string, string>
}

/**
 * Rol kataloğu kaydı (#129). Türkçe etiket ve açıklama backend'den gelir — SmartEnum
 * `Name`/`Slug` deseniyle aynı mantık: `roleName` İngilizce ve serialize edilir,
 * `label` yalnız gösterim içindir. Arayüz kendi etiket haritasını TUTMAZ.
 */
export interface RolePermissionsDto {
  roleName: string
  label: string
  description: string
  permissions: string[]
}

export interface CreateInvitationRequest {
  email: string
  firstName: string
  lastName: string
  targetRole: string
  institutionId?: string
  businessId?: string
  metadata?: Record<string, string>
}

export interface ChangeRolesRequest {
  roles: string[]
}

export interface ChangePermissionsRequest {
  permissions: string[]
}

/** Alan (branş) kapsamı güncelleme (#126). Boş dizi kapsamı kaldırır — geçerli bir işlemdir. */
export interface ChangeBranchesRequest {
  branchCodes: string[]
}

/**
 * Rol modeli tutarlılık raporu (#129) — **yalnız tespit**.
 * `suggestedRole` bir öneridir ve hiçbir yerde otomatik uygulanmaz; kimin müdür yardımcısı
 * kimin personel olduğu okulun bilgisidir.
 */
export interface InvalidRoleInvitationDto {
  invitationId: string
  email: string
  fullName: string
  targetRole: string
  status: string
  suggestedRole: string | null
}

export interface InvalidRoleAccountDto {
  userAccountId: string
  username: string
  fullName: string
  roles: string[]
  unknownRoles: string[]
  suggestedRoles: string[]
}

/** Keycloak'ta hiç realm rolü olmayan hesap — bozulmanın en net belirtisi. */
export interface RolelessAccountDto {
  keycloakUserId: string
  username: string
  email: string
}

export interface RoleIntegrityReport {
  knownRoles: string[]
  invitationsWithUnknownRole: InvalidRoleInvitationDto[]
  accountsWithUnknownRole: InvalidRoleAccountDto[]
  /** Kimlik sunucusuna ulaşılamadıysa `false` — eksik tarama "temiz" sayılmaz. */
  keycloakChecked: boolean
  accountsWithoutRealmRole: RolelessAccountDto[]
  keycloakCheckError: string | null
  totalFindings: number
}

export interface PermissionScopeData {
  roles: string[]
  allDomains: string[]
  allowedDomainsByRole: Record<string, string[]>
}

export const securityApi = {
  listUsers: (params?: {
    institutionId?: string
    businessId?: string
    role?: string
    isEnabled?: boolean
    /** Yalnız alan beklenen ama girilmemiş kullanıcılar (#126) */
    missingBranchOnly?: boolean
  } & PaginationParams) =>
    api.get<PagedResponse<UserAccountDto>>('/security/users', { params }),

  getUser: (userAccountId: string) =>
    api.get<UserAccountDto>(`/security/users/${userAccountId}`),

  updateUser: (userAccountId: string, data: { firstName?: string; lastName?: string; email?: string }) =>
    api.put(`/security/users/${userAccountId}`, data),

  changeRoles: (userAccountId: string, data: ChangeRolesRequest) =>
    api.post(`/security/users/${userAccountId}/roles`, data),

  changePermissions: (userAccountId: string, data: ChangePermissionsRequest) =>
    api.post(`/security/users/${userAccountId}/permissions`, data),

  changeBranches: (userAccountId: string, data: ChangeBranchesRequest) =>
    api.post(`/security/users/${userAccountId}/branches`, data),

  toggleStatus: (userAccountId: string) =>
    api.post(`/security/users/${userAccountId}/toggle-status`),

  deleteUser: (userAccountId: string) =>
    api.delete(`/security/users/${userAccountId}`),

  syncUsers: () =>
    api.post<{ total: number; created: number; updated: number }>('/security/users/sync'),

  // Rol → atanabilir yetki domain kapsamı (yapılandırılabilir guardrail)
  getPermissionScopes: () =>
    api.get<PermissionScopeData>('/security/permission-scopes'),

  updatePermissionScopes: (data: { allowedDomainsByRole: Record<string, string[]> }) =>
    api.put('/security/permission-scopes', data),

  listInvitations: (params?: { institutionId?: string; status?: string; targetRole?: string } & PaginationParams) =>
    api.get<PagedResponse<InvitationDto>>('/security/invitations', { params }),

  createInvitation: (data: CreateInvitationRequest) =>
    api.post<{ invitationId: string }>('/security/invitations', data),

  approveInvitation: (invitationId: string) =>
    api.post(`/security/invitations/${invitationId}/approve`),

  rejectInvitation: (invitationId: string) =>
    api.post(`/security/invitations/${invitationId}/reject`),

  resendInvitation: (invitationId: string) =>
    api.post(`/security/invitations/${invitationId}/resend`),

  listRoles: () =>
    api.get<RolePermissionsDto[]>('/security/roles'),

  listPermissions: () =>
    api.get<string[]>('/security/permissions'),

  /** Rol modeli tutarlılık taraması (#129) — salt okunur, düzeltme yapmaz. */
  getRoleIntegrity: () =>
    api.get<RoleIntegrityReport>('/security/role-integrity'),
}
