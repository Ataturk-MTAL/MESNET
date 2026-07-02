import api from 'boot/axios'
import type { PagedResponse, PaginationParams } from 'src/types/pagination'

export interface GeneratedDocumentSummaryDto {
  id: string
  formType: string
  formTypeSlug: string
  status: string
  statusSlug: string
  studentId: string | null
  businessId: string | null
  institutionId: string | null
  teacherId: string | null
  generatedByName: string
  generatedAt: string
  printedAt: string | null
  printedByName: string | null
  signedAndReturnedAt: string | null
  returnedByName: string | null
  archivedAt: string | null
  archivedByName: string | null
  academicYear: string | null
  description: string | null
}

export interface PdfDownloadResult {
  url?: string
}

export interface BatchGenerateResult {
  generated: number
  skipped: number
  formTypeSlug: string
}

export const reportingApi = {
  listDocuments: (params?: {
    status?: string
    formType?: string
    teacherId?: string
    institutionId?: string
  } & PaginationParams) => api.get<PagedResponse<GeneratedDocumentSummaryDto>>('/reports/documents', { params }),

  getDocument: (documentId: string) =>
    api.get<GeneratedDocumentSummaryDto>(`/reports/documents/${documentId}`),

  getDocumentsByStudent: (studentId: string) =>
    api.get<GeneratedDocumentSummaryDto[]>(`/reports/documents/by-student/${studentId}`),

  getDocumentPdfUrl: (documentId: string) =>
    api.get<PdfDownloadResult>(`/reports/documents/${documentId}/pdf`),

  getDocumentPdfBlob: (documentId: string) =>
    api.get(`/reports/documents/${documentId}/pdf`, { responseType: 'blob' }),

  markAsPrinted: (documentId: string) =>
    api.post(`/reports/documents/${documentId}/print`),

  markAsSignedAndReturned: (documentId: string) =>
    api.post(`/reports/documents/${documentId}/sign-and-return`),

  markAsArchived: (documentId: string) =>
    api.post(`/reports/documents/${documentId}/archive`),

  deleteDocument: (documentId: string) =>
    api.delete(`/reports/documents/${documentId}`),

  deleteDocumentsBatch: (documentIds: string[]) =>
    api.post('/reports/documents/batch-delete', { documentIds }),

  downloadDocumentsZip: (documentIds: string[]) =>
    api.post('/reports/documents/download-zip', { documentIds }, { responseType: 'blob' }),

  generateBatch: (params: {
    formType: string
    year: number
    month: number
    institutionId: string
    academicPeriodId: string
    academicYear: string
    institutionName?: string
  }) => api.post<BatchGenerateResult>('/reports/documents/generate-batch', params),

  previewBatchMonthlyAttendance: (params: {
    institutionId: string
    academicPeriodId: string
    year: number
    month: number
    institutionName: string
    academicYear: string
  }) =>
    api.get('/reports/monthly-attendance/preview-batch', {
      params,
      responseType: 'blob',
    }),

  // Dönem Not Fişi'ni işletmenin gönderdiği gerçek notlardan üret (koordinatör/okul)
  generateTermGradeSlipFromGrades: (data: {
    studentId: string
    academicPeriodId: string
    institutionName: string
    academicYear: string
    semester: string
    makeupTrainingScore: number | null
    skillCompetitionScore: number | null
    vicePrincipalName: string | null
    principalName: string | null
  }) => api.post<{ documentId: string }>('/reports/term-grade-slip/generate', data),
}

export const MEB_FORM_LABELS: Record<string, string> = {
  InternshipContract: 'Staj Sözleşmesi',
  MonthlyActivityReport: 'Aylık Eğitim Faaliyeti',
  GuidanceVisit: 'Rehberlik Ziyareti',
  AttendanceSheet: 'Devamsızlık Çizelgesi',
  SkillExam: 'Beceri Sınavı Not Fişi',
  BusinessEvaluation: 'İşletme Değerlendirme',
  MonthlyAttendanceReport: 'Aylık Devamsızlık Formu',
}

/** Toplu oluşturmaya uygun form tipleri */
export const BATCH_FORM_TYPES: Record<string, string> = {
  MonthlyAttendanceReport: 'Aylık Devamsızlık Formu (Form 7)',
  MonthlyActivityReport: 'Aylık Eğitim Faaliyeti (Form 2)',
  GuidanceVisit: 'Günlük Rehberlik Formu (Form 3)',
}

export const DOCUMENT_STATUS_LABELS: Record<string, string> = {
  Generated: 'Oluşturuldu',
  Printed: 'Yazdırıldı',
  SignedAndReturned: 'İmzalanıp Teslim Edildi',
  Archived: 'Arşivlendi',
}

export const DOCUMENT_STATUS_COLORS: Record<string, string> = {
  Generated: 'blue',
  Printed: 'orange',
  SignedAndReturned: 'green',
  Archived: 'grey',
}

/** Blob'u tarayıcıda indirme yardımcı fonksiyonu */
export function downloadBlob(blob: Blob, filename: string) {
  const url = URL.createObjectURL(blob)
  const a = document.createElement('a')
  a.href = url
  a.download = filename
  a.click()
  URL.revokeObjectURL(url)
}
