import { z } from 'zod'

export const registerStudentSchema = z.object({
  keycloakUserId: z.string().min(1, 'Kullanıcı seçilmelidir'),
  fullName: z.string().min(1, 'Ad Soyad belirtilmelidir'),
  branchCode: z.string().min(1, 'Alan seçilmelidir'),
  educationType: z.string().min(1, 'Eğitim tipi seçilmelidir'),
  classYear: z.number().min(9, 'Sınıf 9-12 arasında olmalıdır').max(12, 'Sınıf 9-12 arasında olmalıdır'),
  specializationCode: z.string().optional(),
  section: z.string().max(5, 'Şube en fazla 5 karakter olmalıdır').optional(),
  studentNumber: z.string().max(20, 'Öğrenci numarası en fazla 20 karakter olmalıdır').optional(),
  tcKimlikNo: z.string().length(11, 'T.C. Kimlik No 11 haneli olmalıdır').optional().or(z.literal('')),
  phoneNumber: z.string().max(20, 'Telefon numarası en fazla 20 karakter olmalıdır').optional(),
  guardianName: z.string().optional(),
  guardianPhone: z.string().max(20, 'Veli telefon numarası en fazla 20 karakter olmalıdır').optional(),
})

export const editStudentSchema = z.object({
  fullName: z.string().min(1, 'Ad Soyad belirtilmelidir'),
  branchCode: z.string().min(1, 'Alan seçilmelidir'),
  specializationCode: z.string().optional(),
  classYear: z.number().min(9, 'Sınıf 9-12 arasında olmalıdır').max(12, 'Sınıf 9-12 arasında olmalıdır'),
  section: z.string().max(5, 'Şube en fazla 5 karakter olmalıdır').optional(),
  studentNumber: z.string().max(20, 'Öğrenci numarası en fazla 20 karakter olmalıdır').optional(),
  tcKimlikNo: z.string().length(11, 'T.C. Kimlik No 11 haneli olmalıdır').optional().or(z.literal('')),
  phoneNumber: z.string().max(20, 'Telefon numarası en fazla 20 karakter olmalıdır').optional(),
  guardianName: z.string().optional(),
  guardianPhone: z.string().max(20, 'Veli telefon numarası en fazla 20 karakter olmalıdır').optional(),
})

export const placementSchema = z.object({
  businessId: z.string().min(1, 'İşletme seçilmelidir'),
  teacherId: z.string().optional(),
})

export const transferSchema = z.object({
  newBusinessId: z.string().min(1, 'Yeni işletme seçilmelidir'),
  reason: z.string().optional(),
})
