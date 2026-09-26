import { z } from 'zod'

/** 10 hane VKN (tüzel kişi) ya da 11 hane TCKN (şahıs işletmesi) — backend `TaxNumberPolicy` ile aynı. */
const TAX_NUMBER_PATTERN = /^\d{10}$|^\d{11}$/
const TAX_NUMBER_MESSAGE = 'Vergi kimlik numarası 10 haneli (VKN) ya da 11 haneli (TC kimlik no) olmalıdır'

export const registerBusinessSchema = z.object({
  // #150 — paylaşımlı kataloğun doğal anahtarı: aynı firmayı iki okulun ayrı ayrı
  // kaydetmesini engelleyen tek alan. 10 hane VKN (tüzel), 11 hane TCKN (şahıs).
  taxNumber: z.string().regex(TAX_NUMBER_PATTERN, TAX_NUMBER_MESSAGE),
  name: z.string().min(1, 'İşletme adı belirtilmelidir').max(200, 'İşletme adı en fazla 200 karakter olmalıdır'),
  address: z.string().min(1, 'Adres belirtilmelidir'),
  phoneNumber: z.string().optional(),
  email: z.string().email('Geçerli bir e-posta adresi giriniz').optional().or(z.literal('')),
  personnelCount: z.number().min(0, 'Personel sayısı sıfırdan küçük olamaz').optional(),
  sectors: z.array(z.string()).optional(),
  location: z.object({
    latitude: z.number(),
    longitude: z.number(),
  }).nullable().optional(),
})

export const editBusinessSchema = z.object({
  // #150 öncesi kaydedilen işletmelerde VKN boş olabilir: düzenlemede boş bırakmak serbesttir
  // (backend boş değeri "dokunma" sayar), doluysa kayıttaki biçim kuralı aynen uygulanır.
  taxNumber: z
    .string()
    .refine(
      (v) => v === '' || TAX_NUMBER_PATTERN.test(v),
      TAX_NUMBER_MESSAGE,
    ),
  name: z.string().min(1, 'İşletme adı belirtilmelidir').max(200, 'İşletme adı en fazla 200 karakter olmalıdır'),
  address: z.string().min(1, 'Adres belirtilmelidir'),
  phoneNumber: z.string().optional(),
  email: z.string().email('Geçerli bir e-posta adresi giriniz').optional().or(z.literal('')),
  website: z.string().optional(),
  personnelCount: z.number().min(0, 'Personel sayısı sıfırdan küçük olamaz').optional(),
  sectors: z.array(z.string()).optional(),
  location: z.object({
    latitude: z.number(),
    longitude: z.number(),
  }).nullable().optional(),
})

export const rejectBusinessSchema = z.object({
  reason: z.string().min(1, 'Gerekçe belirtilmelidir'),
})
