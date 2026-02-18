import api from 'boot/axios'

export interface SignatureStatusDto {
  isSigned: boolean
  signedBy: string | null
  signedAt: string | null
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
  endDate: string
  institutionSignature: SignatureStatusDto
  businessSignature: SignatureStatusDto
  studentSignature: SignatureStatusDto
  terminationReason: string | null
  terminationReasonType: string | null
  terminationReasonTypeSlug: string | null
  createdAt: string
}

export interface CreateContractRequest {
  studentId: string
  businessId: string
  institutionId: string
  teacherId?: string
  startDate: string
  endDate: string
}

export interface SignContractRequest {
  party: 'Institution' | 'Business' | 'Student'
  signedBy: string
}

export interface SuspendContractRequest {
  reason: string
}

export interface TerminateContractRequest {
  reason: string
  reasonType: string
}

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

  uploadSigned: (contractId: string, file: File, uploadedBy: string) => {
    const formData = new FormData()
    formData.append('DocumentFile', file)
    formData.append('UploadedBy', uploadedBy)
    return api.post(`/contracts/${contractId}/upload-signed`, formData, {
      headers: { 'Content-Type': 'multipart/form-data' },
    })
  },

  uploadTermination: (contractId: string, file: File, uploadedBy: string) => {
    const formData = new FormData()
    formData.append('DocumentFile', file)
    formData.append('UploadedBy', uploadedBy)
    return api.post(`/contracts/${contractId}/upload-termination`, formData, {
      headers: { 'Content-Type': 'multipart/form-data' },
    })
  },
}
