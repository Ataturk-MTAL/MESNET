import type { DailyActivity, ExamCriterion } from 'src/api/coordination'

/**
 * Koordinasyon kayıt formlarının (beceri sınavı, faaliyet raporu, rehberlik ziyareti) saf
 * kuralları. Backend doğrulayıcısının reddedeceği girdiyi kaydetmeden önce, alanın yanında
 * açıklamak için vardır; yetki kararı değildir.
 */

const PERCENT = 100

/** Kriter puanlarının toplamını 100 üzerinden yüzdeye çevirir (sınav puanı 0-100 aralığında). */
export function percentScore(criteria: readonly ExamCriterion[]): number {
  const max = criteria.reduce((sum, c) => sum + c.maxScore, 0)
  if (max <= 0) return 0
  const got = criteria.reduce((sum, c) => sum + c.score, 0)
  return Math.round((got / max) * PERCENT)
}

/** Kriter setindeki ilk sorun (Türkçe mesaj) ya da `null`. */
export function criteriaProblem(criteria: readonly ExamCriterion[]): string | null {
  if (criteria.length === 0) return 'En az bir değerlendirme kriteri girilmelidir.'
  for (const c of criteria) {
    if (!c.name.trim()) return 'Her kriterin bir adı olmalıdır.'
    // Boşaltılan sayı alanı `v-model.number` ile '' kalır ve karşılaştırmada 0 gibi davranır.
    if (!Number.isFinite(c.maxScore) || !Number.isFinite(c.score))
      return `"${c.name}" kriterinin puanları sayı olmalıdır.`
    if (c.maxScore <= 0) return `"${c.name}" kriterinin en yüksek puanı sıfırdan büyük olmalıdır.`
    if (c.score < 0 || c.score > c.maxScore)
      return `"${c.name}" kriterinin puanı 0 ile ${c.maxScore} arasında olmalıdır.`
  }
  return null
}

/** `month` 1-12. */
export function daysInMonth(year: number, month: number): number {
  return new Date(year, month, 0).getDate()
}

/** Günlük faaliyet listesindeki ilk sorun (Türkçe mesaj) ya da `null`. */
export function activitiesProblem(
  activities: readonly DailyActivity[], year: number, month: number,
): string | null {
  if (activities.length === 0) return 'En az bir günlük faaliyet girilmelidir.'
  const lastDay = daysInMonth(year, month)
  const seen = new Set<number>()
  for (const a of activities) {
    if (!Number.isInteger(a.dayNumber)) return 'Her faaliyetin gün numarası girilmelidir.'
    if (a.dayNumber < 1 || a.dayNumber > lastDay)
      return `${a.dayNumber}. gün bu ayda yok (1-${lastDay}).`
    if (seen.has(a.dayNumber)) return `${a.dayNumber}. gün birden fazla kez girilmiş.`
    if (!a.description.trim()) return `${a.dayNumber}. günün açıklaması boş.`
    seen.add(a.dayNumber)
  }
  return null
}

export interface PlacementLike {
  value: string
  label: string
  businessId: string
}

/** Seçili işletmede yerleşik öğrenciler, ada göre sıralı. */
export function studentsAtBusiness(
  placements: readonly PlacementLike[], businessId: string | null,
): { studentId: string; name: string }[] {
  if (!businessId) return []
  return placements
    .filter((p) => p.businessId === businessId)
    .map((p) => ({ studentId: p.value, name: p.label }))
    .sort((a, b) => a.name.localeCompare(b.name, 'tr'))
}
