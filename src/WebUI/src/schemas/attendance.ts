import { z } from 'zod'
import { isoDateString } from './common'

/**
 * Devamsızlık girişi. Tarih yalnız içinde bulunulan hafta içinden seçilebilir; input'un
 * `min`/`max` özniteliği elle yazılan tarihi engellemediği için sınır şemada da denetlenir.
 * `YYYY-AA-GG` biçimi sözlük sırasıyla karşılaştırılabilir.
 */
export function createAttendanceSchema(bounds: { min: string; max: string }) {
  return z.object({
    studentId: z.string().min(1, 'Öğrenci seçilmelidir'),
    businessId: z.string().min(1, 'Öğrencinin aktif yerleştirmesi bulunamadı'),
    date: isoDateString('Tarih belirtilmelidir').refine(
      (v) => v === '' || (v >= bounds.min && v <= bounds.max),
      'Yalnız içinde bulunulan haftanın bir günü seçilebilir',
    ),
    absenceType: z.string().min(1, 'Devamsızlık türü seçilmelidir'),
    reason: z.string().optional(),
  })
}
