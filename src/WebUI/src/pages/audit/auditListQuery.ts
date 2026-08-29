/**
 * `AuditLogPage`'in sunucuya ne sorduğunu belirleyen SAF mantık.
 *
 * <p><b>Neden ayrı dosya:</b> sayfa VE testi aynı kaynağı okusun. Bu depoda ölçülmüş
 * sahte-yeşil kalıbı bunun yokluğundan doğdu: eski `InstitutionListPage.spec.ts` sayfayı hiç
 * import etmiyor, değerleri kendi yeniden yazıyordu; sayfanın varsayılanı değiştirilip
 * koşulduğunda test 5/5 yeşil kaldı.</p>
 */

/** Varsayılan kapsam. Herkesin izni olan tek kapsam — açılışta 403 riski yok. */
export const DEFAULT_SCOPE = 'mine'

/** Varsayılan sıralama alanı: en yeni işlem en üstte. */
export const DEFAULT_SORT_BY = 'occurredAt'

/** Denetim izinde varsayılan yön AZALANDIR — "az önce ne oldu" en sık sorulan sorudur. */
export const DEFAULT_DESCENDING = true

export type AuditScope = 'mine' | 'institution'

// Index imzası `useServerPagination<T, F extends Record<string, unknown>>` kısıtı için
// zorunludur — yalnız `{ outcome?: string }` bu kısıta uymaz (TS2322).
export interface AuditListFilters extends Record<string, unknown> {
  outcome?: string
  crossedTenantBoundary?: boolean
}

/** `useServerPagination`'a geçilecek filtre gövdesi. */
export function buildAuditListFilters(
  outcome: string | null,
  crossedOnly: boolean,
): AuditListFilters {
  const filters: AuditListFilters = {}
  // Boş süzgeç GÖNDERİLMEZ: sunucuda `outcome=""` hiçbir satırla eşleşmez ve liste sessizce
  // boşalırdı.
  if (outcome) filters.outcome = outcome
  if (crossedOnly) filters.crossedTenantBoundary = true
  return filters
}
