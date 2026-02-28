import { z } from 'zod'

export const registerBusinessSchema = z.object({
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
