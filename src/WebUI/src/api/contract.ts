import api from 'boot/axios'

export interface SignatureStatusDto {
  isSigned: boolean
  signedBy: string | null
  signedAt: string | null
}

export interface ContractDocumentDto {
  documentId: string
  documentType: string
  documentTypeSlug: string
  description: string | null
  objectPath: string
  uploadedBy: string
  uploadedAt: string
}

export interface InternshipContractDto {
  id: string
  studentId: string
  businessId: string
  institutionId: string
  teacherId: string | null
  status: string
  statusSlug: string
  startDate: string
  endDate: string | null
  institutionSignature: SignatureStatusDto
  businessSignature: SignatureStatusDto
  studentSignature: SignatureStatusDto
  terminationReason: string | null
  terminationReasonType: string | null
  terminationReasonTypeSlug: string | null
  documents: ContractDocumentDto[]
  createdAt: string
}

export interface CreateContractRequest {
  studentId: string
  businessId: string
  institutionId: string
  teacherId?: string
  startDate: string
}

export interface SignContractRequest {
  party: 'Institution' | 'Business' | 'Student' | 'Parent'
  signedBy: string
}

export interface SuspendContractRequest {
  reason: string
}

export interface TerminateContractRequest {
  reason: string
  reasonType: string
}

export interface RequestTerminationRequest {
  reason: string
  reasonType: string
  requestedBy: string
}

export interface RejectTerminationRequest {
  rejectedBy: string
  rejectionNote?: string
}

export interface UploadContractDocumentRequest {
  documentType: 'SignedContract' | 'TerminationLetter' | 'Other'
  description?: string
  uploadedBy: string
  file: File
}

export const DOCUMENT_TYPES = [
  { label: 'Islak İmzalı Sözleşme', value: 'SignedContract' },
  { label: 'Fesih Belgesi', value: 'TerminationLetter' },
  { label: 'Diğer', value: 'Other' },
] as const

export const TERMINATION_REASONS = [
  { label: 'Disiplin', value: 'Discipline' },
  { label: 'Sağlık', value: 'Health' },
  { label: 'Devamsızlık Aşımı', value: 'AttendanceLimitExceeded' },
  { label: 'İşletme Fesih Talebi', value: 'BusinessRequest' },
  { label: 'Öğrenci Talebi', value: 'StudentRequest' },
  { label: 'Diğer', value: 'Other' },
] as const

export const contractApi = {
  list: (params?: { studentId?: string; businessId?: string; institutionId?: string; status?: string }) =>
    api.get<InternshipContractDto[]>('/contracts', { params }),

  get: (contractId: string) =>
    api.get<InternshipContractDto>(`/contracts/${contractId}`),

  create: (data: CreateContractRequest) =>
    api.post<{ contractId: string }>('/contracts', data),

  submit: (contractId: string) =>
    api.post(`/contracts/${contractId}/submit`),

  sign: (contractId: string, data: SignContractRequest) =>
    api.post(`/contracts/${contractId}/sign`, data),

  activate: (contractId: string) =>
    api.post(`/contracts/${contractId}/activate`),

  suspend: (contractId: string, data: SuspendContractRequest) =>
    api.post(`/contracts/${contractId}/suspend`, data),

  resume: (contractId: string) =>
    api.post(`/contracts/${contractId}/resume`),

  terminate: (contractId: string, data: TerminateContractRequest) =>
    api.post(`/contracts/${contractId}/terminate`, data),

  complete: (contractId: string) =>
    api.post(`/contracts/${contractId}/complete`),

  requestTermination: (contractId: string, data: RequestTerminationRequest) =>
    api.post(`/contracts/${contractId}/request-termination`, data),

  rejectTermination: (contractId: string, data: RejectTerminationRequest) =>
    api.post(`/contracts/${contractId}/reject-termination`, data),

  uploadDocument: (contractId: string, req: UploadContractDocumentRequest) => {
    const formData = new FormData()
    formData.append('DocumentFile', req.file)
    formData.append('DocumentType', req.documentType)
    formData.append('UploadedBy', req.uploadedBy)
    if (req.description) formData.append('Description', req.description)
    return api.post(`/contracts/${contractId}/documents`, formData, {
      headers: { 'Content-Type': 'multipart/form-data' },
    })
  },
}
