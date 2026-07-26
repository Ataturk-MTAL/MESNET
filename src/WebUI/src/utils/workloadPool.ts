/**
 * Ders yükü havuzu (workload pool) gösterim kuralları — #111.
 *
 * Havuz TANIMSIZ olmak (alan için hesaplanmamış → 0) ile havuzun AŞILMIŞ olması iki
 * ayrı durumdur; kod bunları tek `pool > 0 && assigned > pool` koşuluna sıkıştırınca
 * havuz yapılandırılmamışken 94 saat takdir edilse bile ekran sessiz kalıyordu.
 *
 * Bu modül üç durumu ayırır:
 *   1. Havuz tanımsız  → kıyas YAPILAMAZ; nötr değer + ayrı uyarı bildirimi (hata değil,
 *                        eksik yapılandırma) ile gösterilir.
 *   2. Havuz > 0, aşım → `text-negative-strong`.
 *   3. Havuz > 0, normal → mevcut bilgilendirme tonu korunur (yanlış alarm üretilmez).
 *
 * Renkler #104'teki anlamsal tonlardan gelir (bkz. src/assets/app.css); ham Quasar
 * palet renkleri (red-7, orange-8 ...) KULLANILMAZ.
 *
 * Saf fonksiyonlardır: hem `useAssignedHours` (İşletme Saat Ayarları) hem de
 * İşletme Dağıtımı özet kutuları aynı kararı verir.
 */

/** Havuz tanımsızken sayısal alanların yerine basılan işaret. */
export const UNDEFINED_POOL_PLACEHOLDER = '—'

/** Havuz yapılandırılmamışken gösterilen uyarı metni (iki sayfada da aynı). */
export const WORKLOAD_POOL_MISSING_MESSAGE =
  'Bu alan için ders yükü havuzu tanımlanmamış. Takdir edilen saatler bir üst sınırla ' +
  'karşılaştırılamıyor. Önce Ders Yükü Havuzu ekranından hesaplayın.'

/**
 * Havuz hiç hesaplanmamış mı?
 * Backend yapılandırma yoksa 0 döner; negatif/NaN değerler de tanımsız sayılır.
 */
export function isWorkloadPoolUndefined(pool: number | null | undefined): boolean {
  return pool == null || !Number.isFinite(pool) || pool <= 0
}

/** Havuz değerinin gösterimi — tanımsızken sayı yerine "—". */
export function workloadPoolLabel(pool: number | null | undefined): string {
  return isWorkloadPoolUndefined(pool) ? UNDEFINED_POOL_PLACEHOLDER : String(pool)
}

/** Havuz değerinin rengi — tanımsızken "0 saat havuz var" izlenimi vermeyen nötr ton. */
export function workloadPoolToneClass(pool: number | null | undefined): string {
  return isWorkloadPoolUndefined(pool) ? 'text-neutral-strong' : 'text-positive-strong'
}

/**
 * Takdir/dağıtım toplamının rengi.
 * Havuz tanımsız + saat girilmişse uyarı tonu: aşım DEĞİL ama sessiz de kalmaz.
 */
export function assignedHoursToneClass(pool: number | null | undefined, assigned: number): string {
  if (isWorkloadPoolUndefined(pool)) {
    return assigned > 0 ? 'text-warning-strong' : 'text-neutral-strong'
  }
  return assigned > (pool as number) ? 'text-negative-strong' : 'text-info-strong'
}

/** Kalan saat gösterimi — havuz tanımsızken "kalan" anlamsızdır (yalnız -Σ takdir olurdu). */
export function remainingHoursLabel(pool: number | null | undefined, remaining: number): string {
  return isWorkloadPoolUndefined(pool) ? UNDEFINED_POOL_PLACEHOLDER : String(remaining)
}

/** Kalan saatin rengi — negatif kalan artık nötr/uyarı tonunda kalmaz. */
export function remainingHoursToneClass(pool: number | null | undefined, remaining: number): string {
  if (isWorkloadPoolUndefined(pool)) return 'text-neutral-strong'
  return remaining < 0 ? 'text-negative-strong' : 'text-warning-strong'
}
