import { z } from 'zod'
import { isoDateString } from './common'

export const createContractSchema = z.object({
  studentId: z.string().min(1, 'Öğrenci seçilmelidir'),
  businessId: z.string().min(1, 'İşletme seçilmelidir'),
  // TeacherSelector `clearable` — temizlenince null gelir
  teacherId: z.string().nullable().optional(),
  startDate: isoDateString('Başlangıç tarihi belirtilmelidir'),
  // Boş bırakılabilir (yasal taban uygulanır); `v-model.number` boş alanı '' olarak verir
  agreedMonthlyWage: z
    .unknown()
    .refine(
      (v) => v === null || v === undefined || v === '' || (typeof v === 'number' && v >= 0),
      'Ücret sıfırdan küçük olamaz',
    ),
})
