import api from 'boot/axios'
import type { PagedResponse, PaginationParams } from 'src/types/pagination'

export interface InternshipSummaryDto {
  id: string
  placementId: string
  studentId: string
  studentName: string
  businessId: string
  businessName: string
  institutionId: string
  academicPeriodId: string
  contractId: string | null
  phase: string
  phaseSlug: string
  startedAt: string
  completedAt: string | null
  totalAbsenceDays: number
  completedVisits: number
  confirmedPayments: number
  lastUpdated: string
}

/** Zincirin tek adımı. İzin ve uç adı **sunucudan** gelir (#191). */
export interface TerminationStepDto {
  /** İngilizce ad — `Parent`, `Teacher`, `Deputy`, `Director`, `BusinessRep`. */
  name: string
  /** Türkçe görünen ad. */
  slug: string
  /** `POST /internships/{id}/approve/{endpoint}` yolundaki son parça. */
  endpoint: string
  /**
   * Adımı yapabilmek için gereken izin.
   *
   * Arayüz butonu buna bakar; adım→izin eşlemesi burada **tekrarlanmaz**. Tekrarlansaydı
   * biri değişip diğeri unutulduğunda buton yanlış kişiye görünürdü.
   */
  permission: string
}

export interface TerminationApprovalChainDto {
  parentApproved: boolean
  teacherApproved: boolean
  deputyApproved: boolean
  directorApproved: boolean
  businessRepApproved: boolean
  isOverridden: boolean
  overriddenBy: string | null
  overriddenAt: string | null
  completedAt: string | null
}

export interface TerminationChainStatusDto {
  /** Fesih süreci açık mı. `false` ise `chain` null'dır. */
  isActive: boolean
  /** Veli adımı aranıyor mu — kararı sunucu verir, arayüz yaş hesaplamaz. */
  requiresParentApproval: boolean
  chain: TerminationApprovalChainDto | null
  /**
   * Henüz onaylanmamış adımlar, kanonik sırada.
   *
   * **Sıra dayatma değildir** — zincir sıralı değil, müdür öğretmenden önce onaylayabilir.
   */
  pendingSteps: TerminationStepDto[]
  terminationReason: string | null
  terminationReasonType: string | null
}

export const internshipApi = {
  listInternships: (params?: {
    studentId?: string
    businessId?: string
    institutionId?: string
    academicPeriodId?: string
    phase?: string
    minAbsenceDays?: number
  } & PaginationParams) =>
    api.get<PagedResponse<InternshipSummaryDto>>('/internships', { params }),

  markAsFailedToComplete: (placementId: string) =>
    api.post(`/placements/${placementId}/mark-failed`),

  // ─── Fesih onay zinciri (#191) ───

  getTerminationChain: (internshipId: string) =>
    api.get<TerminationChainStatusDto>(`/internships/${internshipId}/termination-chain`),

  /**
   * Fesih talebi açar.
   *
   * Talebi **kimin** açtığı gövdede taşınmaz — sunucu token'dan damgalar. Alan gönderilse
   * bile yok sayılır.
   */
  requestTermination: (internshipId: string, body: { reason: string; reasonType: string }) =>
    api.post(`/internships/${internshipId}/terminate`, body),

  /**
   * Zincirin bir adımını onaylar.
   *
   * `endpoint` sunucudan gelen adım tanımından okunur; arayüz adım adlarını kendi
   * listesinde tutmaz.
   */
  approveTerminationStep: (internshipId: string, endpoint: string) =>
    api.post(`/internships/${internshipId}/approve/${endpoint}`),

  /**
   * Onay zincirini atlar.
   *
   * Zinciri **kimin** atladığı gövdede taşınmaz — sunucu token'dan damgalar.
   */
  overrideTermination: (internshipId: string, body: { reason: string }) =>
    api.post(`/internships/${internshipId}/approve/override`, body),
}
