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
  version: number
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

// ── Schedule History DTOs ──

export interface ScheduleStreamSummaryDto {
  scheduleId: string
  academicYear: number
  semester: string
  versionCount: number
  createdAt: string
  lastUpdatedAt: string | null
  createdBy: string
  lastUpdatedBy: string | null
}

export interface ScheduleVersionDto {
  version: number
  eventType: string
  timestamp: string
  updatedBy: string
  weeklySchedule: DailyScheduleDto[]
}

export interface ScheduleHistoryDto {
  scheduleId: string
  teacherId: string
  academicYear: number
  semester: string
  currentVersion: number
  versions: ScheduleVersionDto[]
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

export interface AssignedSlotInfo {
  day: string
  periodNumber: number
}

export interface BusinessAssignmentDto {
  businessId: string
  businessName: string
  address: string | null
  district: string | null
  distanceToSchoolKm: number | null
  isManualDistance: boolean
  maxCoordinationHours: number
  assignedHours: number
  /**
   * Fahri (ücretsiz) ziyaret (#115). true ise `assignedHours` her zaman 0'dır ve satır
   * havuz/öğretmen kapasitesi toplamlarına girmez. false + 0 saat = "henüz takdir edilmedi".
   */
  isHonoraryVisit: boolean
  assignedTeacherId: string | null
  assignedTeacherName: string | null
  assignedDay: string | null
  assignedPeriodNumber: number | null
  activeStudentCount: number
  branchCode: string
  branchName: string
  assignedSlots: AssignedSlotInfo[]
  lastModifiedAt: string | null
  lastModifiedBy: string | null
}

export interface AssignmentHistoryEntryDto {
  timestamp: string
  action: string       // "Assigned" | "SlotAdded" | "SlotRemoved" | "Unassigned" | "HoursUpdated"
  performedBy: string
  teacherName: string | null
  slotDay: string | null
  slotPeriod: number | null
  assignedHours: number | null
  details: string | null
}

export interface CoordinationSummaryDto {
  totalWorkloadPool: number
  /** Ücret doğuran toplam saat — fahri ziyaretler dahil DEĞİLDİR (#115) */
  totalAssignedHours: number
  remainingHours: number
  totalMaxHours: number
  assignedBusinessCount: number
  unassignedBusinessCount: number
  /** Havuza girmeyen fahri ziyaret satırı sayısı */
  honoraryBusinessCount: number
  teacherWorkloads: TeacherWorkloadSummaryDto[]
}

export interface TeacherWorkloadSummaryDto {
  teacherId: string
  teacherName: string
  /** Ücret doğuran saat — fahri ziyaretler hariç (#115) */
  assignedHours: number
  businessCount: number
  /** Fahri ziyaret edilen işletme sayısı — slot işgal eder, ek ders saatine sayılmaz */
  honoraryVisitCount: number
}

export interface TeacherWorkloadDto {
  teacherId: string
  totalAssignedHours: number
  businessCount: number
  honoraryVisitCount: number
  businesses: TeacherBusinessAssignmentDto[]
}

export interface TeacherBusinessAssignmentDto {
  businessId: string
  businessName: string
  assignedHours: number
  assignedDay: string | null
  isHonoraryVisit: boolean
}

/**
 * Koordinasyon satırı alan bazlıdır (#114): aynı işletmeye iki farklı alandan
 * bağımsız atama yapılabildiği için hedef satır
 * `(businessId, branchCode, academicPeriodId)` üçlüsüyle belirlenir.
 */
export interface BranchRowParams {
  branchCode: string
  academicPeriodId: string
}

export interface AssignBusinessRequest extends BranchRowParams {
  businessId: string
  teacherId: string
  teacherName: string
  assignedHours: number
  assignedDay: string
  periodNumber?: number
  assignedBy: string
}

export interface ResyncCoordinationViewsResult {
  baseRows: number
  branchRows: number
  removedLegacyRows: number
}

// ── Teacher Overview DTOs ──

export interface TeacherOverviewDto {
  teacherId: string
  totalAssignedHours: number
  businessCount: number
  honoraryVisitCount: number
  scheduleExists: boolean
  freeSlotsByDay: Record<string, number>   // gün → boş slot sayısı
  totalSlotsByDay: Record<string, number>  // gün → toplam serbest slot
  businesses: TeacherBusinessAssignmentDto[]
}

export interface TeacherSummaryRowDto {
  teacherId: string
  teacherName: string
  businessCount: number
  /** Ücret doğuran saat — fahri ziyaretler hariç (#115) */
  assignedHours: number
  /** Fahri ziyaret edilen işletme sayısı */
  honoraryVisitCount: number
  /** Fahri ziyaretlerin ders programında işgal ettiği slot sayısı */
  honorarySlotCount: number
  scheduleExists: boolean
  freeSlotsByDay: Record<string, number>
  assignedSlotsByDay: Record<string, number>
}

// ── Business Cluster DTO ──

export interface BusinessClusterDto {
  businessId: string
  businessName: string
  latitude: number
  longitude: number
  district: string | null
  branchCode: string
  branchName: string
  clusterId: number | null   // null = gürültü (outlier)
  assignedTeacherName: string | null
  isAssigned: boolean
  activeStudentCount: number
  distanceToSchoolKm: number | null
  maxCoordinationHours: number
  /** Fahri (ücretsiz) ziyaret — saat takdiri yapılmaz (#115) */
  isHonoraryVisit: boolean
}

export interface SetManualDistanceRequest {
  distanceKm: number
}

// ── Branch Workload Config DTOs ──

export interface ClassLevelConfig {
  classYear: number
  weeklyLessonHours: number
  studentCount: number
  groupCount: number
  subTotal: number
}

export interface BranchWorkloadConfigDto {
  id: string
  institutionId: string
  academicPeriodId: string
  branchCode: string
  educationType: string
  departmentHeadCount: number
  workshopHeadCount: number
  departmentHeadHours: number
  workshopHeadHours: number
  classLevels: ClassLevelConfig[]
  totalSupervisorHours: number
  totalTeachingHours: number
  totalWorkloadPool: number
  updatedAt: string
  updatedBy: string
}

export interface UpsertBranchWorkloadConfigRequest {
  academicPeriodId: string
  educationType: string
  departmentHeadCount: number
  workshopHeadCount: number
  departmentHeadHours: number
  workshopHeadHours: number
  classLevels: { classYear: number; weeklyLessonHours: number }[]
}

// ── Weekly Visit DTOs ──

export interface WeeklyVisitPlanDto {
  id: string
  academicPeriodId: string
  year: number
  weekNumber: number
  weekStartDate: string
  weekEndDate: string
  scope: string          // "Teacher" | "Branch" | "All"
  scopeTeacherId: string | null
  scopeBranchCode: string | null
  assignmentCount: number
  generatedBy: string
  generatedAt: string
}

export interface WeeklyVisitAssignmentDto {
  id: string
  planId: string
  teacherId: string
  teacherName: string
  businessId: string
  businessName: string
  branchCode: string
  branchName: string
  visitDate: string
  day: string
  periodCount: number
  weekNumber: number
}

export interface GenerateWeeklyVisitsRequest {
  academicPeriodId: string
  year: number
  weekNumber: number
  scope: string
  teacherId?: string
  branchCode?: string
}

export interface AddWeeklyVisitAssignmentRequest {
  teacherId: string
  teacherName: string
  businessId: string
  businessName: string
  branchCode: string
  branchName: string
  day: string
  periodCount: number
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

  getCurrentSchedule: (teacherId: string, academicPeriodId: string, semester: string) =>
    api.get<TeacherScheduleDto>(`/coordination/teachers/${teacherId}/schedule/current`, {
      params: { academicPeriodId, semester },
    }),

  getScheduleStreams: (teacherId: string) =>
    api.get<ScheduleStreamSummaryDto[]>(`/coordination/teachers/${teacherId}/schedule/streams`),

  getScheduleHistory: (teacherId: string, scheduleId?: string) =>
    api.get<ScheduleHistoryDto>(`/coordination/teachers/${teacherId}/schedule/history`, {
      params: scheduleId ? { scheduleId } : undefined,
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

  listAssignments: (params?: { branchCode?: string; teacherId?: string; assignedOnly?: boolean; academicPeriodId?: string }) =>
    api.get<BusinessAssignmentDto[]>('/coordination/teachers/assignments', { params }),

  assignBusiness: (data: AssignBusinessRequest) =>
    api.post('/coordination/teachers/assignments', data),

  setManualDistance: (businessId: string, data: SetManualDistanceRequest) =>
    api.post(`/coordination/teachers/assignments/${businessId}/distance`, data),

  getCoordinationSummary: (params?: { branchCode?: string; academicPeriodId?: string }) =>
    api.get<CoordinationSummaryDto>('/coordination/teachers/summary', { params }),

  getTeacherWorkload: (teacherId: string) =>
    api.get<TeacherWorkloadDto>(`/coordination/teachers/${teacherId}/workload`),

  unassignBusiness: (businessId: string, params: BranchRowParams) =>
    api.delete(`/coordination/teachers/assignments/${businessId}`, { params }),

  /**
   * Takdir edilen saati günceller. `isHonoraryVisit: true` gönderildiğinde backend
   * `assignedHours` değerini 0'a sabitler ve havuz/kapasite kısıtlarını uygulamaz (#115).
   */
  updateAssignedHours: (
    businessId: string,
    data: { assignedHours: number; isHonoraryVisit: boolean },
    params: BranchRowParams,
  ) =>
    api.patch(`/coordination/teachers/assignments/${businessId}/hours`, data, { params }),

  unassignBusinessSlot: (
    businessId: string,
    day: string,
    periodNumber: number,
    params: BranchRowParams,
  ) =>
    api.delete(`/coordination/teachers/assignments/${businessId}/slot`, {
      params: { day, periodNumber, ...params },
    }),

  getAssignmentHistory: (businessId: string, params: BranchRowParams) =>
    api.get<AssignmentHistoryEntryDto[]>(
      `/coordination/teachers/assignments/${businessId}/history`,
      { params },
    ),

  recalculateDistances: () =>
    api.post('/coordination/teachers/recalculate-distances'),

  /**
   * Koordinasyon satırlarını çok-alanlı modele göre yeniden kurar (#114).
   * Eski tek-satır kayıtlarını temizler; alan/dönem satırlarını ve öğrenci
   * sayaçlarını yeniden üretir. Tekrar çalıştırmak güvenlidir.
   */
  resyncCoordinationViews: () =>
    api.post<ResyncCoordinationViewsResult>('/coordination/teachers/resync-views'),

  // ── Teacher Overview ──

  getTeacherOverview: (teacherId: string, academicPeriodId: string, semester: string) =>
    api.get<TeacherOverviewDto>(`/coordination/teachers/${teacherId}/overview`, {
      params: { academicPeriodId, semester },
    }),

  getAllTeachersOverview: (academicPeriodId: string, semester: string, branchCode?: string) =>
    api.get<TeacherSummaryRowDto[]>('/coordination/teachers/overview-all', {
      params: { academicPeriodId, semester, branchCode },
    }),

  // ── Business Clusters ──

  getBusinessClusters: (epsMeters = 1000, minPoints = 3, branchCode?: string | null) =>
    api.get<BusinessClusterDto[]>('/coordination/teachers/business-clusters', {
      params: { epsMeters, minPoints, branchCode: branchCode ?? undefined },
    }),

  // ── Branch Workload Config ──

  getBranchWorkloadConfig: (branchCode: string, academicPeriodId: string, educationType: string) =>
    api.get<BranchWorkloadConfigDto | null>(`/coordination/teachers/branch-workload/${branchCode}`, {
      params: { academicPeriodId, educationType },
    }),

  upsertBranchWorkloadConfig: (branchCode: string, data: UpsertBranchWorkloadConfigRequest) =>
    api.put(`/coordination/teachers/branch-workload/${branchCode}`, data),

  // ── Weekly Visit ──

  generateWeeklyVisits: (data: GenerateWeeklyVisitsRequest) =>
    api.post<{ planId: string }>('/coordination/weekly-visits/generate', data),

  deleteWeeklyVisitPlan: (planId: string) =>
    api.delete(`/coordination/weekly-visits/plans/${planId}`),

  listWeeklyVisitPlans: (params?: { academicPeriodId?: string; year?: number; weekNumber?: number } & PaginationParams) =>
    api.get<PagedResponse<WeeklyVisitPlanDto>>('/coordination/weekly-visits/plans', { params }),

  listWeeklyVisitAssignments: (planId: string, params?: { teacherId?: string; branchCode?: string } & PaginationParams) =>
    api.get<PagedResponse<WeeklyVisitAssignmentDto>>(`/coordination/weekly-visits/plans/${planId}/assignments`, { params }),

  addWeeklyVisitAssignment: (planId: string, data: AddWeeklyVisitAssignmentRequest) =>
    api.post<{ assignmentId: string }>(`/coordination/weekly-visits/plans/${planId}/assignments`, data),

  deleteWeeklyVisitAssignment: (planId: string, assignmentId: string) =>
    api.delete(`/coordination/weekly-visits/plans/${planId}/assignments/${assignmentId}`),

  resyncWeeklyVisitEvents: (data: { institutionId: string; academicPeriodId: string }) =>
    api.post<{ resyncedAssignments: number }>('/coordination/weekly-visits/resync', data),

  // ── Dönem Notu (Dönem Not Fişi kaynağı) ──

  // İşletme: kendi öğrencileri + not durumu
  getMyStudentsForGrading: (academicPeriodId: string) =>
    api.get<{ students: StudentGradeRow[] }>('/coordination/term-grades/my-students', { params: { academicPeriodId } }),

  // Koordinatör/okul: gönderilmiş notlar (fiş üretilecekler)
  getSubmittedTermGrades: (academicPeriodId: string) =>
    api.get<{ students: StudentGradeRow[] }>('/coordination/term-grades/submitted', { params: { academicPeriodId } }),

  enterTermGrade: (data: EnterTermGradeRequest) =>
    api.post<{ id: string }>('/coordination/term-grades', data),

  submitTermGrade: (id: string) =>
    api.post(`/coordination/term-grades/${id}/submit`),
}

export interface StudentGradeRow {
  studentId: string
  studentName: string
  branchName: string
  gradeId: string | null
  status: string | null
  statusSlug: string | null
  practiceGrades: number[]
  serviceGrades: number[]
  projectGrades: number[]
  experimentGrades: number[]
  masterInstructorName: string | null
  termAverage: number | null
}

export interface EnterTermGradeRequest {
  studentId: string
  academicPeriodId: string
  practiceGrades: number[]
  serviceGrades: number[]
  projectGrades: number[]
  experimentGrades: number[]
  masterInstructorName: string | null
}
