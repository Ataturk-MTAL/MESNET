/**
 * `AuditLogPage`'in sunucuya ne sorduğunu belirleyen SAF mantık.
 *
 * <p><b>Neden ayrı dosya:</b> sayfa VE testi aynı kaynağı okusun. Bu depoda ölçülmüş
 * sahte-yeşil kalıbı bunun yokluğundan doğdu: eski `InstitutionListPage.spec.ts` sayfayı hiç
 * import etmiyor, değerleri kendi yeniden yazıyordu; sayfanın varsayılanı değiştirilip
 * koşulduğunda test 5/5 yeşil kaldı.</p>
 */

import type { auditApi } from 'src/api/audit'

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

/**
 * `useServerPagination`'a geçilecek filtre gövdesi.
 *
 * <p><b>`scope` neden burada:</b> `GetMine` ucu (`AuditEndpoints.cs`) `crossedTenantBoundary`
 * parametresini HİÇ almıyor — göndersek bile sessizce yok sayılır. Bu kararı yalnız görünürlük
 * (`v-if`) ile çözmek yetmez: anahtar UI'da açık kalıp kapsam `institution`'dan `mine`'a
 * geçerse, `crossedOnly` state'i true kalır ve bir sonraki `institution`'a dönüşte tekrar
 * gövdeye sızar. Kural burada, tek kaynakta, sabitlenir: `mine` kapsamında bu alan HİÇBİR
 * girdiyle gövdeye giremez.</p>
 */
export function buildAuditListFilters(
  scope: AuditScope,
  outcome: string | null,
  crossedOnly: boolean,
): AuditListFilters {
  const filters: AuditListFilters = {}
  // Boş süzgeç GÖNDERİLMEZ: sunucuda `outcome=""` hiçbir satırla eşleşmez ve liste sessizce
  // boşalırdı.
  if (outcome) filters.outcome = outcome
  // `mine` ucu bu parametreyi almıyor — göndermek sunucuda sessizce yok sayılırdı ve anahtar
  // "açık görünüp hiçbir şey yapmayan" bir yalana dönüşürdü.
  if (crossedOnly && scope === 'institution') filters.crossedTenantBoundary = true
  return filters
}

/** `resolveAuditEndpoint`'in kabul ettiği en dar sözleşme — testte gerçek `auditApi` yerine
 *  sahte fonksiyonlar geçilebilsin diye `Pick` ile daraltıldı. */
export type AuditApiClient = Pick<typeof auditApi, 'listMine' | 'listForInstitution'>

/**
 * Kapsam → uç eşlemesi.
 *
 * <p><b>Madde 5 düzeltmesi — neden ayrı fonksiyon:</b> bu eşleme eskiden
 * `AuditLogPage.vue`'nin `fetchFn`'i içinde satır içi bir üçlü operatördü
 * (`scope === 'institution' ? listForInstitution : listMine`). Sayfa testi
 * `useServerPagination`'a `fetchFn`'i doğrudan mock'layarak geçtiği için bu satır TESTTE HİÇ
 * ÇALIŞMIYORDU — eşleme ters çevrilse (institution ↔ mine) bile 8/8 yeşil kalırdı. Eşleme
 * buraya taşınınca hem sayfa hem test AYNI fonksiyonu çağırır; ters çevrilirse test kırmızı
 * olur. Aynı desen: `utils/institutionScope.ts`.</p>
 *
 * <p>İki ucun izni FARKLIDIR (`listForInstitution` → `audit:view:institution`); bu fonksiyon
 * yalnız HANGİ ucun çağrılacağına karar verir, yetki kararı sunucudadır.</p>
 */
export function resolveAuditEndpoint(
  scope: AuditScope,
  api: AuditApiClient,
): AuditApiClient['listMine'] {
  return scope === 'institution' ? api.listForInstitution : api.listMine
}
