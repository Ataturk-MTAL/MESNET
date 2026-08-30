import api from 'boot/axios'
import type { PagedResponse, PaginationParams } from 'src/types/pagination'

/** Sunucudan gelen denetim satırı. `commandLabel` Türkçe etikettir — arayüz eşleme TUTMAZ. */
export interface AuditEntryDto {
  id: string
  occurredAt: string
  actorId: string
  actorName: string
  commandType: string
  commandLabel: string
  module: string
  subjectInstitutionId: string | null
  crossedTenantBoundary: boolean
  outcome: string
  outcomeSlug: string
  errorCode: string | null
  targetIds: Record<string, string>
  durationMs: number
}

export interface AuditListParams extends Record<string, unknown> {
  commandType?: string
  outcome?: string
  from?: string
  to?: string
  crossedTenantBoundary?: boolean
}

export const auditApi = {
  /** Aktörün kendi işlemleri. Ek izin gerektirmez. */
  listMine: (params?: AuditListParams & PaginationParams) =>
    api.get<PagedResponse<AuditEntryDto>>('/audit/mine', { params }),

  /** Kurum ağacının izi. `audit:view:institution` gerektirir; kapsam sunucudadır. */
  listForInstitution: (params?: AuditListParams & PaginationParams) =>
    api.get<PagedResponse<AuditEntryDto>>('/audit/institution', { params }),
}
