import api from 'boot/axios'
import type { PagedResponse, PaginationParams } from 'src/types/pagination'

export interface GuidanceVisitDto {
  id: string
  teacherId: string
  businessId: string
  institutionId: string
  visitDate: string
  instructorMeetingNotes: string | null
  issuesIdentified: string | null
  actionsTaken: string | null
  generalAssessment: string | null
  status: string
  statusSlug?: string
  createdAt: string
  submittedAt: string | null
  approvedAt: string | null
}

export interface SkillExamDto {
  id: string
  studentId: string
  businessId: string
  institutionId: string
  academicYear: number
  semester: string
  examDate: string
  score: number
  result: string
  createdAt: string
}

export interface MonthlyActivityReportDto {
  id: string
  studentId: string
  businessId: string
  institutionId: string
  teacherId: string
  year: number
  month: number
  instructorComment: string | null
  teacherComment: string | null
  status: string
  createdAt: string
  submittedAt: string | null
  approvedAt: string | null
}

export interface BusinessEvaluationDto {
  id: string
  businessId: string
  institutionId: string
  evaluatorId: string
  evaluationDate: string
  result: string
  notes: string | null
  createdAt: string
}

export interface CreateVisitRequest {
  teacherId: string
  businessId: string
  institutionId: string
  visitDate: string
  instructorMeetingNotes?: string
  issuesIdentified?: string
  actionsTaken?: string
  generalAssessment?: string
}

export interface CreateEvaluationRequest {
  businessId: string
  institutionId: string
  evaluationDate: string
  result: string
  notes?: string
}

export const EVALUATION_RESULTS = [
  { label: 'Uygun', value: 'Suitable' },
  { label: 'Uygun Değil', value: 'Unsuitable' },
  { label: 'Şartlı Uygun', value: 'Conditional' },
] as const

// ── Teacher Schedule DTOs ──

export interface TeacherScheduleDto {
  id: string
  teacherId: string
  institutionId: string
  academicPeriodId: string
  academicYear: number
  semester: string
  weeklySchedule: DailyScheduleDto[]
  createdAt: string
  updatedAt: string | null
  createdBy: string
}

export interface DailyScheduleDto {
  day: string // "Monday" | "Tuesday" | "Wednesday" | "Thursday" | "Friday"
  periods: PeriodSlotDto[]
}

export interface PeriodSlotDto {
  periodNumber: number
  status: string // "Occupied" | "Free"
  courseName: string | null
  assignedBusinessId: string | null
}

export interface FreeSlotDto {
  day: string
  periodNumber: number
  assignedBusinessId: string | null
}

export interface UpsertScheduleRequest {
  institutionId: string
  academicPeriodId: string
  academicYear: number
  semester: string
  weeklySchedule: DailyScheduleInput[]
  updatedBy: string
}

export interface DailyScheduleInput {
  day: string
  periods: PeriodSlotInput[]
}

export interface PeriodSlotInput {
  periodNumber: number
  status: string // "Occupied" | "Free"
  courseName?: string
}

// ── Coordination Config DTOs ──

export interface DistanceHourRule {
  maxDistanceKm: number
  hours: number
}

export interface CoordinationConfigDto {
  id: string
  institutionId: string
  distanceHourRules: DistanceHourRule[]
  isMetropolitan: boolean
  maxWeeklyExtraHours: number
  updatedAt: string
  updatedBy: string
}

export interface UpsertCoordinationConfigRequest {
  distanceHourRules?: DistanceHourRule[]
  isMetropolitan?: boolean
  maxWeeklyExtraHours?: number
  updatedBy: string
}

// ── Business Assignment DTOs ──

export interface BusinessAssignmentDto {
  businessId: string
  businessName: string
  address: string | null
  district: string | null
  distanceToSchoolKm: number | null
  isManualDistance: boolean
  maxCoordinationHours: number
  assignedHours: number
  assignedTeacherId: string | null
  assignedTeacherName: string | null
  assignedDay: string | null
  activeStudentCount: number
  branchCode: string
  branchName: string
}

export interface CoordinationSummaryDto {
  totalAvailableHours: number
  totalAssignedHours: number
  remainingHours: number
  assignedBusinessCount: number
  unassignedBusinessCount: number
  teacherWorkloads: TeacherWorkloadSummaryDto[]
}

export interface TeacherWorkloadSummaryDto {
  teacherId: string
  teacherName: string
  assignedHours: number
  businessCount: number
}

export interface TeacherWorkloadDto {
  teacherId: string
  totalAssignedHours: number
  businessCount: number
  businesses: TeacherBusinessAssignmentDto[]
}

export interface TeacherBusinessAssignmentDto {
  businessId: string
  businessName: string
  assignedHours: number
  assignedDay: string | null
}

export interface AssignBusinessRequest {
  businessId: string
  teacherId: string
  teacherName: string
  assignedHours: number
  assignedDay: string
  assignedBy: string
}

export interface SetManualDistanceRequest {
  distanceKm: number
}

export const coordinationApi = {
  listVisits: (params?: { teacherId?: string; businessId?: string; academicPeriodId?: string; fromDate?: string; toDate?: string } & PaginationParams) =>
    api.get<PagedResponse<GuidanceVisitDto>>('/coordination/guidance-visits', { params }),

  getVisit: (visitId: string) =>
    api.get<GuidanceVisitDto>(`/coordination/guidance-visits/${visitId}`),

  createVisit: (data: CreateVisitRequest) =>
    api.post<{ visitId: string }>('/coordination/guidance-visits', data),

  updateVisit: (visitId: string, data: Partial<CreateVisitRequest>) =>
    api.put(`/coordination/guidance-visits/${visitId}`, data),

  submitVisit: (visitId: string) =>
    api.post(`/coordination/guidance-visits/${visitId}/submit`),

  approveVisit: (visitId: string) =>
    api.post(`/coordination/guidance-visits/${visitId}/approve`),

  listEvaluations: (params?: { businessId?: string; institutionId?: string } & PaginationParams) =>
    api.get<PagedResponse<BusinessEvaluationDto>>('/coordination/business-evaluations', { params }),

  createEvaluation: (data: CreateEvaluationRequest) =>
    api.post<{ evaluationId: string }>('/coordination/business-evaluations', data),

  listSkillExams: (params?: { studentId?: string; businessId?: string; academicPeriodId?: string; academicYear?: number } & PaginationParams) =>
    api.get<PagedResponse<SkillExamDto>>('/coordination/skill-exams', { params }),

  listActivityReports: (params?: { studentId?: string; businessId?: string; academicPeriodId?: string; year?: number; month?: number } & PaginationParams) =>
    api.get<PagedResponse<MonthlyActivityReportDto>>('/coordination/activity-reports', { params }),

  submitActivityReport: (reportId: string) =>
    api.post(`/coordination/activity-reports/${reportId}/submit`),

  approveActivityReport: (reportId: string) =>
    api.post(`/coordination/activity-reports/${reportId}/approve`),

  // ── Teacher Schedule ──

  getTeacherSchedule: (teacherId: string, year: number, semester: string) =>
    api.get<TeacherScheduleDto>(`/coordination/teachers/${teacherId}/schedule`, {
      params: { year, semester },
    }),

  upsertTeacherSchedule: (teacherId: string, data: UpsertScheduleRequest) =>
    api.post<{ scheduleId: string }>(`/coordination/teachers/${teacherId}/schedule`, data),

  getTeacherFreeSlots: (teacherId: string, year: number, semester: string, day?: string) =>
    api.get<{ freeSlots: FreeSlotDto[] }>(`/coordination/teachers/${teacherId}/free-slots`, {
      params: { year, semester, day },
    }),

  assignBusinessToSlot: (teacherId: string, data: {
    academicYear: number
    semester: string
    day: string
    periodNumber: number
    businessId: string
    assignedBy: string
  }) =>
    api.post(`/coordination/teachers/${teacherId}/assign-business`, data),

  // ── Coordination Config ──

  getConfig: () =>
    api.get<CoordinationConfigDto>('/coordination/teachers/config'),

  upsertConfig: (data: UpsertCoordinationConfigRequest) =>
    api.post('/coordination/teachers/config', data),

  // ── Business Assignment ──

  listAssignments: (params?: { branchCode?: string; teacherId?: string; assignedOnly?: boolean }) =>
    api.get<BusinessAssignmentDto[]>('/coordination/teachers/assignments', { params }),

  assignBusiness: (data: AssignBusinessRequest) =>
    api.post('/coordination/teachers/assignments', data),

  setManualDistance: (businessId: string, data: SetManualDistanceRequest) =>
    api.post(`/coordination/teachers/assignments/${businessId}/distance`, data),

  getCoordinationSummary: (params?: { branchCode?: string }) =>
    api.get<CoordinationSummaryDto>('/coordination/teachers/summary', { params }),

  getTeacherWorkload: (teacherId: string) =>
    api.get<TeacherWorkloadDto>(`/coordination/teachers/${teacherId}/workload`),

  recalculateDistances: () =>
    api.post('/coordination/teachers/recalculate-distances'),
}
