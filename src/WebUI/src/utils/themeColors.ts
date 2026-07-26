/**
 * Tema renklerini JS tarafına taşır (#104).
 *
 * Neden gerekli: ECharts canvas'a çiziyor, CSS sınıfı ya da `color-mix()` kabul etmiyor —
 * somut renk değeri istiyor. Grafik renklerini elle hex yazmak temayı ikinci kez
 * çatallandırırdı; burada Quasar'ın yayınladığı `--q-*` değişkenleri okunup karışım
 * JS'te yapılıyor, yani tema tek kaynak olarak kalıyor.
 *
 * Karışım app.css'teki `color-mix(in srgb, ...)` ile AYNI semantiği kullanır: gama
 * kodlanmış sRGB değerleri üzerinde doğrusal interpolasyon. Böylece grafikteki
 * "Tamamlandı" ile rozetteki "Tamamlandı" birebir aynı rengi verir.
 */

export type ThemeRole =
  | 'primary'
  | 'secondary'
  | 'accent'
  | 'positive'
  | 'negative'
  | 'warning'
  | 'info'

/** Tema okunamazsa kullanılacak değerler (tarayıcı dışı bağlam ya da değişken tanımsız). */
const FALLBACK: Record<ThemeRole, string> = {
  primary: '#1e3a5f',
  secondary: '#4a6fa5',
  accent: '#c9a227',
  positive: '#2e7d5b',
  negative: '#b3261e',
  warning: '#9a6b00',
  info: '#3e6b89',
}

/** Durum taşımayan gri — grafiklerde "Taslak"/"Kayıtlı" gibi nötr dilimler. */
export const NEUTRAL_GREY = '#9e9e9e'

type Rgb = [number, number, number]

function toRgb(hex: string): Rgb {
  const h = hex.replace('#', '')
  return [
    parseInt(h.slice(0, 2), 16),
    parseInt(h.slice(2, 4), 16),
    parseInt(h.slice(4, 6), 16),
  ]
}

function toHex(rgb: Rgb): string {
  return '#' + rgb.map((v) => Math.round(v).toString(16).padStart(2, '0')).join('')
}

function mix(a: Rgb, b: Rgb, ratio: number): Rgb {
  return [
    a[0] * ratio + b[0] * (1 - ratio),
    a[1] * ratio + b[1] * (1 - ratio),
    a[2] * ratio + b[2] * (1 - ratio),
  ]
}

/** Tema rolünün hex değeri. Tarayıcı dışında ya da değişken yoksa yedeğe düşer. */
export function themeColor(role: ThemeRole): string {
  if (typeof document === 'undefined') return FALLBACK[role]
  const value = getComputedStyle(document.documentElement)
    .getPropertyValue(`--q-${role}`)
    .trim()
  return /^#[0-9a-fA-F]{6}$/.test(value) ? value : FALLBACK[role]
}

/**
 * Tema rengini siyaha (koyulaştırma) ya da beyaza (açma) doğru karıştırır.
 * `ratio` rengin payıdır: 1 saf renk, 0.74 → %74 renk + %26 siyah.
 */
export function themeTone(
  role: ThemeRole,
  ratio: number,
  toward: 'black' | 'white' = 'black',
): string {
  const other: Rgb = toward === 'black' ? [0, 0, 0] : [255, 255, 255]
  return toHex(mix(toRgb(themeColor(role)), other, ratio))
}

/** İki tema rengini karıştırır (ör. positive + info → teal ara ton). */
export function themeBlend(a: ThemeRole, b: ThemeRole, ratio = 0.5): string {
  return toHex(mix(toRgb(themeColor(a)), toRgb(themeColor(b)), ratio))
}

/**
 * Durum tonları — app.css'teki `.bg-status-*` sınıflarıyla AYNI oranlar.
 * Grafik ile rozet aynı durumu aynı renkte göstersin diye ikisi BİRLİKTE güncellenmeli.
 *
 * Fonksiyon olarak tutuluyor, sabit olarak değil: modül yüklenirken CSS henüz
 * uygulanmamış olabilir, çağrı anında okumak doğru değeri garanti eder.
 */
export const statusTone = {
  pending: () => themeColor('warning'),
  active: () => themeColor('positive'),
  success: () => themeTone('positive', 0.74),
  progress: () => themeBlend('positive', 'info', 0.5),
  info: () => themeColor('info'),
  warning: () => themeTone('warning', 0.78),
  negative: () => themeColor('negative'),
  done: () => themeTone('secondary', 0.72),
}
