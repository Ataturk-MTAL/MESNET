/**
 * Koordinasyon saat muhasebesi (#115).
 *
 * Backend'deki `BusinessCoordinationView.BillableHours/BillableTargetHours/SlotTargetHours`
 * metotlarının birebir karşılığı. Üç durum ayrışır:
 *
 * | Durum                | assignedHours | isHonoraryVisit |
 * |----------------------|---------------|-----------------|
 * | Henüz takdir edilmedi| 0             | false           |
 * | Fahri ziyaret        | 0             | **true**        |
 * | Ücretli              | > 0           | false           |
 *
 * Bu fonksiyonlar olmadan `assignedHours > 0 ? assignedHours : maxCoordinationHours`
 * fallback'i kopyala-yapıştır ile üç ayrı yerde yaşıyordu ve fahri satırı sessizce
 * mesafe TAVANINA çeviriyordu.
 */

/** Fahri ziyaretin ders programında işgal ettiği slot sayısı. */
export const HONORARY_VISIT_SLOTS = 1

/** Rozet metni — ayrım yalnız renkle değil metinle de taşınır (renk körlüğü). */
export const HONORARY_LABEL = 'Fahri'

/** Rozet/ipucu açıklaması. */
export const HONORARY_HINT = 'Fahri ziyaret — öğretmen gider, ek ders ücreti doğmaz'

/** Saat muhasebesi için gereken en küçük işletme şekli. */
export interface CoordinationHoursShape {
  assignedHours: number
  maxCoordinationHours: number
  isHonoraryVisit: boolean
}

/** Eski/eksik yüklerde alan gelmemiş olabilir — kesin boolean'a indirger. */
export function isHonorary(biz: Partial<CoordinationHoursShape>): boolean {
  return biz.isHonoraryVisit === true
}

/** Ek ders ücreti doğuran saat. Fahri ziyarette her zaman 0. */
export function billableHours(biz: CoordinationHoursShape): number {
  return isHonorary(biz) ? 0 : biz.assignedHours
}

/**
 * Ücret doğuran hedef saat: takdir edilmişse o, edilmemişse mesafe tavanı.
 * Fahri satırda tavana DÜŞMEZ — 0 döner.
 */
export function billableTargetHours(biz: CoordinationHoursShape): number {
  if (isHonorary(biz)) return 0
  return biz.assignedHours > 0 ? biz.assignedHours : biz.maxCoordinationHours
}

/**
 * Ders programında doldurulması beklenen slot sayısı. Fahri ziyaret ücret doğurmaz
 * ama takvimde yerini alır → `HONORARY_VISIT_SLOTS` kadar slot.
 */
export function slotTargetHours(biz: CoordinationHoursShape): number {
  return isHonorary(biz) ? HONORARY_VISIT_SLOTS : billableTargetHours(biz)
}
