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

export interface RolePermissionsDto {
  roleName: string
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

export const securityApi = {
  listUsers: (params?: { institutionId?: string; businessId?: string; role?: string; isEnabled?: boolean } & PaginationParams) =>
    api.get<PagedResponse<UserAccountDto>>('/security/users', { params }),

  getUser: (userAccountId: string) =>
    api.get<UserAccountDto>(`/security/users/${userAccountId}`),

  updateUser: (userAccountId: string, data: { firstName?: string; lastName?: string; email?: string }) =>
    api.put(`/security/users/${userAccountId}`, data),

  changeRoles: (userAccountId: string, data: ChangeRolesRequest) =>
    api.post(`/security/users/${userAccountId}/roles`, data),

  changePermissions: (userAccountId: string, data: ChangePermissionsRequest) =>
    api.post(`/security/users/${userAccountId}/permissions`, data),

  toggleStatus: (userAccountId: string) =>
    api.post(`/security/users/${userAccountId}/toggle-status`),

  deleteUser: (userAccountId: string) =>
    api.delete(`/security/users/${userAccountId}`),

  syncUsers: () =>
    api.post<{ total: number; created: number; updated: number }>('/security/users/sync'),

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
}
