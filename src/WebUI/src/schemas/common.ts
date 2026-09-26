import { z } from 'zod'

const ISO_DATE_PATTERN = /^\d{4}-\d{2}-\d{2}$/

/**
 * `<input type="date">` değeri (YYYY-AA-GG). Boş ya da çözümlenemeyen değer, sunucuya gitmeden
 * önce alan hatası olarak yakalanır — aksi hâlde `new Date('').toISOString()` RangeError atar
 * ve kullanıcı bunu anlamsız bir "API hatası" olarak görür.
 */
export function isoDateString(requiredMessage: string) {
  return z
    .string()
    .min(1, requiredMessage)
    .refine(
      (v) => v === '' || (ISO_DATE_PATTERN.test(v) && !Number.isNaN(Date.parse(v))),
      'Geçerli bir tarih giriniz',
    )
}
