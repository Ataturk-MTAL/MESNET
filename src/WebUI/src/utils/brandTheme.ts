import { setCssVar } from 'quasar'

/**
 * Kiracının (okulun) marka rengini ÇALIŞMA ZAMANINDA uygular.
 *
 * <p><b>Neden bu tek fonksiyon bütün bir zinciri kaydırır — kaynaktan doğrulandı:</b>
 * Quasar marka renklerini `:root` üzerinde CSS değişkeni olarak yayınlar
 * (`quasar/src/css/core/colors.sass`: `--q-primary: #{$primary}`) ve kendi
 * `.bg-primary` / `.text-primary` yardımcıları `var(--q-primary)` okur. `app.css`'teki
 * bütün `-soft`/`-strong`/`-status` tonları da `color-mix(in srgb, var(--q-primary) …)`
 * ile TÜREDİĞİ için değişkeni değiştirmek hepsini birlikte kaydırır — kural başına elle
 * bir renk yazmaya gerek yoktur.</p>
 *
 * <p><b>Hedef `document.documentElement`, `document.body` DEĞİL</b> — Quasar'ın
 * `setCssVar` varsayılanı body'dir ve burada bilerek ezilir. Ölçüldü: `utils/themeColors.ts`
 * ve `components/BusinessClusterMap.vue` tema rengini
 * `getComputedStyle(document.documentElement).getPropertyValue('--q-*')` ile okur. Değişken
 * body'ye yazılsaydı bu iki okuyucu `:root`'taki DERLEME ZAMANI değerini görmeye devam
 * ederdi: ECharts grafikleri ve harita kümeleri varsayılan lacivertte donarken sayfanın
 * geri kalanı kiracı rengine kayardı. Satır içi stil, stil sayfasındaki `:root` bildirimini
 * ezer (`!important` yok), yani html'e yazmak güvenlidir ve body dahil her şeye miras kalır.</p>
 *
 * <p><b>Yalnız primary ve secondary kayar.</b> positive / negative / info / warning ve
 * accent (Resmî Hardal) SABİTTİR: bunlar marka ifadesi değil sistem anlamıdır ve ölçülmüş
 * anlamsal kontrastları ancak sabit kalarak her kiracıda geçerli kalır. Bu listeyi
 * genişletmeyin.</p>
 */

/**
 * Kabul edilen tek biçim: `#RRGGBB`.
 *
 * Üç haneli kısa biçim, `rgb()` ve isimli renk BİLEREK reddedilir — palet kapalı bir
 * kümedir ve sunucu her zaman altı haneli hex döndürür. Beklenmedik bir değer geldiğinde
 * onu "yorumlamaya" çalışmak, kontrastı ölçülmemiş bir rengi ekrana koymaktır.
 */
const BRAND_HEX = /^#[0-9a-fA-F]{6}$/

export function isBrandHex(value: unknown): value is string {
  return typeof value === 'string' && BRAND_HEX.test(value)
}

/**
 * Paletin iki rengini uygular. Değerlerden BİRİ bile geçersizse hiçbiri uygulanmaz ve
 * tema derleme zamanı varsayılanına (Mührü Lacivert) döner — yarım uygulanmış bir palet,
 * primary'si bir kiracıdan secondary'si başkasından gelen ölçülmemiş bir çift demektir.
 *
 * @returns Tema uygulandıysa `true`; geçersiz değer nedeniyle varsayılana düşüldüyse `false`.
 */
export function applyBrandTheme(primary: unknown, secondary: unknown): boolean {
  if (typeof document === 'undefined') return false

  if (!isBrandHex(primary) || !isBrandHex(secondary)) {
    resetBrandTheme()
    return false
  }

  setCssVar('primary', primary, document.documentElement)
  setCssVar('secondary', secondary, document.documentElement)
  return true
}

/**
 * Çalışma zamanı temasını kaldırır; `:root`'taki derleme zamanı değeri (quasar-variables.sass)
 * yeniden yürürlüğe girer. Değişkene "varsayılan hex"i yazmak yerine ÖZELLİK SİLİNİR —
 * yoksa varsayılan renk burada ikinci kez tanımlanmış olurdu ve sass ile sessizce ayrışırdı.
 */
export function resetBrandTheme(): void {
  if (typeof document === 'undefined') return
  document.documentElement.style.removeProperty('--q-primary')
  document.documentElement.style.removeProperty('--q-secondary')
}
