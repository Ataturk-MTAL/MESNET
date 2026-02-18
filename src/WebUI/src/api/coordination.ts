import api from 'boot/axios'

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

export const coordinationApi = {
  listVisits: (params?: { teacherId?: string; businessId?: string; fromDate?: string; toDate?: string }) =>
    api.get<GuidanceVisitDto[]>('/coordination/guidance-visits', { params }),

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

  listEvaluations: (params?: { businessId?: string; institutionId?: string }) =>
    api.get<BusinessEvaluationDto[]>('/coordination/business-evaluations', { params }),

  createEvaluation: (data: CreateEvaluationRequest) =>
    api.post<{ evaluationId: string }>('/coordination/business-evaluations', data),

  listSkillExams: (params?: { studentId?: string; businessId?: string; academicYear?: number }) =>
    api.get<SkillExamDto[]>('/coordination/skill-exams', { params }),

  listActivityReports: (params?: { studentId?: string; businessId?: string; year?: number; month?: number }) =>
    api.get<MonthlyActivityReportDto[]>('/coordination/activity-reports', { params }),

  submitActivityReport: (reportId: string) =>
    api.post(`/coordination/activity-reports/${reportId}/submit`),

  approveActivityReport: (reportId: string) =>
    api.post(`/coordination/activity-reports/${reportId}/approve`),
}
