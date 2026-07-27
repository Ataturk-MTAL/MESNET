import type { DistanceHourRule } from 'src/api/coordination'

/**
 * Kurum koordinasyon yapılandırması — saf kurallar (#134).
 *
 * Buradaki doğrulama, backend `UpsertCoordinationConfig` doğrulamasıyla **birebir** aynıdır.
 * İkisi ayrışırsa kullanıcı ya kaydedemediği bir formu doldurur ya da sunucudan 422 yer;
 * her iki durumda da hata mesajı ekranda değil, kafada oluşur. Bir kural burada değişirse
 * backend tarafı da aynı turda değişmelidir.
 */

/**
 * "Üzeri" (catch-all) kuralının mesafe değeri.
 *
 * Backend `double.MaxValue` tutar; JSON'a `1.7976931348623157e+308` olarak yazılır ve
 * `Number.MAX_VALUE` ile **birebir** eşleşir. Bu yüzden karşılaştırma eşitlikle yapılır,
 * "çok büyük sayı" eşiğiyle değil.
 */
export const CATCH_ALL_DISTANCE = Number.MAX_VALUE

/** Catch-all satırının mesafe hücresinde gösterilen metin — düzenlenemez. */
export const CATCH_ALL_DISTANCE_LABEL = 'Üzeri (sınırsız)'

export const MIN_RULE_HOURS = 1
export const MAX_RULE_HOURS = 40
export const MIN_WEEKLY_EXTRA_HOURS = 1
export const MAX_WEEKLY_EXTRA_HOURS = 40

/** Yeni eklenen satırın başlangıç değerleri. */
export const NEW_RULE_DEFAULT_DISTANCE = 1
export const NEW_RULE_DEFAULT_HOURS = 2

/** Kaydedilebilir yapılandırmanın form karşılığı. */
export interface CoordinationConfigDraft {
  distanceHourRules: DistanceHourRule[]
  isMetropolitan: boolean
  maxWeeklyExtraHours: number
}

/** Satır, mesafe üst sınırı olmayan "üzeri" kuralı mı? */
export function isCatchAllRule(rule: DistanceHourRule): boolean {
  return rule.maxDistanceKm === CATCH_ALL_DISTANCE
}

/**
 * Kural listesinin derin kopyası.
 *
 * `structuredClone` KULLANILMAZ: `ref()` ile sarılı diziler derin reaktif proxy'dir ve
 * `structuredClone` proxy'yi klonlayamayıp `DataCloneError` fırlatır. `.map()` ile elle
 * kopyalanır.
 */
export function cloneRules(rules: readonly DistanceHourRule[]): DistanceHourRule[] {
  return rules.map((rule) => ({ maxDistanceKm: rule.maxDistanceKm, hours: rule.hours }))
}

/**
 * Mesafeye göre artan sıraya dizer. Backend değerlendirmeyi (`CalculateMaxHours`) zaten
 * `OrderBy(MaxDistanceKm)` ile yapar; sıralama görsel bir kolaylıktır, davranışı değiştirmez.
 * Catch-all `Number.MAX_VALUE` olduğu için doğal olarak en sona düşer.
 */
export function sortRules(rules: readonly DistanceHourRule[]): DistanceHourRule[] {
  return cloneRules(rules).sort((a, b) => a.maxDistanceKm - b.maxDistanceKm)
}

/** Mesafe hücresinin okunur metni. */
export function formatDistanceLabel(maxDistanceKm: number): string {
  if (maxDistanceKm === CATCH_ALL_DISTANCE) return CATCH_ALL_DISTANCE_LABEL
  return `${maxDistanceKm} km`
}

/** Satırın "hangi mesafeye kadar" açıklaması — tablo altındaki özet satırında kullanılır. */
export function describeRule(rule: DistanceHourRule): string {
  if (isCatchAllRule(rule)) return `Üzeri → ${rule.hours} saat`
  return `≤ ${rule.maxDistanceKm} km → ${rule.hours} saat`
}

function isFiniteNumber(value: unknown): value is number {
  return typeof value === 'number' && Number.isFinite(value)
}

/**
 * Yapılandırmayı doğrular ve Türkçe hata mesajlarını döndürür. Boş dizi = geçerli.
 *
 * Kural kümesi backend ile aynıdır:
 * en az bir kural / her mesafe > 0 / her saat 1-40 / mesafeler benzersiz /
 * tam olarak bir "üzeri" kuralı / azami haftalık ek ders saati 1-40.
 */
export function validateCoordinationConfig(draft: CoordinationConfigDraft): string[] {
  const errors: string[] = []
  const rules = draft.distanceHourRules

  if (rules.length === 0) {
    errors.push('Mesafe-saat tablosu boş bırakılamaz; en az bir kural tanımlanmalıdır.')
  }

  rules.forEach((rule, index) => {
    const rowNo = index + 1
    const isCatchAll = isCatchAllRule(rule)

    if (!isCatchAll && (!isFiniteNumber(rule.maxDistanceKm) || rule.maxDistanceKm <= 0)) {
      errors.push(`${rowNo}. satır: mesafe 0 kilometreden büyük olmalıdır.`)
    }

    if (
      !isFiniteNumber(rule.hours) ||
      rule.hours < MIN_RULE_HOURS ||
      rule.hours > MAX_RULE_HOURS
    ) {
      errors.push(
        `${rowNo}. satır: saat ${MIN_RULE_HOURS} ile ${MAX_RULE_HOURS} arasında olmalıdır.`,
      )
    }
  })

  const seen = new Set<number>()
  const reportedDuplicates = new Set<number>()
  for (const rule of rules) {
    if (!isFiniteNumber(rule.maxDistanceKm)) continue
    if (seen.has(rule.maxDistanceKm) && !reportedDuplicates.has(rule.maxDistanceKm)) {
      reportedDuplicates.add(rule.maxDistanceKm)
      errors.push(
        `Aynı mesafe birden fazla kez tanımlanamaz: ${formatDistanceLabel(rule.maxDistanceKm)}.`,
      )
    }
    seen.add(rule.maxDistanceKm)
  }

  const catchAllCount = rules.filter(isCatchAllRule).length
  if (catchAllCount !== 1) {
    errors.push(
      `Sınırsız ("${CATCH_ALL_DISTANCE_LABEL}") kuralı tam olarak bir kez bulunmalıdır.`,
    )
  }

  if (
    !isFiniteNumber(draft.maxWeeklyExtraHours) ||
    draft.maxWeeklyExtraHours < MIN_WEEKLY_EXTRA_HOURS ||
    draft.maxWeeklyExtraHours > MAX_WEEKLY_EXTRA_HOURS
  ) {
    errors.push(
      `Azami haftalık ek ders saati ${MIN_WEEKLY_EXTRA_HOURS} ile ${MAX_WEEKLY_EXTRA_HOURS} arasında olmalıdır.`,
    )
  }

  return errors
}
