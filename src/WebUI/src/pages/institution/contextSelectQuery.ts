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

/**
 * `Security.ActiveContextOutOfScope` — sunucunun hata açıklaması hedef kurumun ham GUID'ini
 * taşır (kararı `SetActiveInstitutionHandler.CanSwitchTo` verir). "errors name the problem
 * and the recovery" (craft floor): bir kimlik ikisini de yapmaz. Bu ekran hangi kuruma
 * geçilmeye çalışıldığını zaten biliyor (satırdaki `fullName`), o yüzden GUID'i kurum ADIYLA
 * değiştirir. Sunucunun kodu/açıklaması DEĞİŞMEZ — makine tarafı `Security.
 * ActiveContextOutOfScope` kodunda okunabilir kalır, yalnız kullanıcıya ne YAZILACAĞI burada
 * kararlaştırılır.
 */
export const ACTIVE_CONTEXT_OUT_OF_SCOPE_CODE = 'Security.ActiveContextOutOfScope'

/**
 * @param errorCode `extractApiErrorCode(err)` ile çıkarılan sunucu hata kodu.
 * @param institutionName Geçilmeye çalışılan kurumun adı (satırdan gelir).
 * @returns Tanınan kod için insan-okunur mesaj; tanınmayan kod için `null` — çağıran genel
 * `notify.apiError` yoluna düşer.
 */
export function resolveActiveContextErrorMessage(
  errorCode: string | undefined,
  institutionName: string,
): string | null {
  if (errorCode !== ACTIVE_CONTEXT_OUT_OF_SCOPE_CODE) return null
  return `${institutionName} yetki alanınızda değil.`
}
