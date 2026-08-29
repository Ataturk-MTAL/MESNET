/**
 * Bağlam seçim ekranının sunucuya ne sorduğunu belirleyen SAF mantık.
 *
 * <p><b>Neden ayrı dosya:</b> sayfa VE testi aynı kaynağı okusun. Bu depoda ölçülmüş
 * sahte-yeşil kalıbı bunun yokluğundan doğdu — eski `InstitutionListPage.spec.ts` sayfayı
 * hiç import etmiyor, değerleri kendi yeniden yazıyordu; sayfanın varsayılanı değiştirilip
 * koşulduğunda test 5/5 yeşil kaldı.</p>
 *
 * <p><b>Karar:</b> seçim ekranı yalnız OKULLARI listeler. İl/ilçe müdürlüğü düğümünün
 * kiracısında okul verisi yoktur; oraya "geçmek" boş ekranlardan başka bir şey üretmez.
 * Kullanıcı kendi düğümüne dönmek isterse bağlamı TEMİZLER (`switchTo(null)`), bir düğüm
 * seçmez.</p>
 */

/** Seçim ekranı OKULLARI listeler — il/ilçe düğümleri seçilebilir bağlam değildir. */
export const DEFAULT_NODE_TYPE_FILTER = 'School'

/** Kurum adına göre sıralı; sırasız liste her yazmadan sonra kayardı. */
export const DEFAULT_SORT_BY = 'fullName'

export interface ContextSelectFilters extends Record<string, unknown> {
  nodeType: string
}

export function buildContextSelectFilters(nodeType: string): ContextSelectFilters {
  return { nodeType }
}
