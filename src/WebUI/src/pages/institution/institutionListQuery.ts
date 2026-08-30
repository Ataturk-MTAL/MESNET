/**
 * `InstitutionListPage`'in sunucuya ne sorduğunu belirleyen SAF mantık.
 *
 * <p><b>Neden ayrı dosya:</b> eski `InstitutionListPage.spec.ts` sayfayı hiç import etmiyordu —
 * `useServerPagination`'ı kendi kurduğu yerel bir `nodeType` ref'iyle test ediyordu. Sayfanın
 * gerçek varsayılanı (`'School'`) ya da `filters` computed'ının kurduğu gövde değişse test
 * bunu HİÇ göremiyordu (ölçüldü: varsayılanı `'Province'`e çevirip koşuldu, 5/5 yeşil kaldı).
 * Bu dosya sayfanın kullandığı gerçek değerleri taşır; sayfa VE test aynı kaynağı okur — aynı
 * desen `utils/institutionScope.ts`, `utils/brandTheme.ts`, `utils/coordinationHours.ts`
 * dosyalarında da kullanılıyor.</p>
 */

/** Kurum türü süzgecinin varsayılanı. İl yetkilisinin aradığı şey neredeyse her zaman bir okuldur. */
export const DEFAULT_NODE_TYPE_FILTER = 'School'

/** Liste sayfasının varsayılan sıralama alanı — sıralamasız liste her yazmadan sonra kayardı. */
export const DEFAULT_SORT_BY = 'fullName'

// Index imzası `useServerPagination<T, F extends Record<string, unknown>>` kısıtı için
// zorunludur — yalnız `{ nodeType: string }` bu kısıta uymaz (TS2322).
export interface InstitutionListFilters extends Record<string, unknown> {
  nodeType: string
}

/** `useServerPagination`'a geçilecek filtre gövdesi. */
export function buildInstitutionListFilters(nodeType: string): InstitutionListFilters {
  return { nodeType }
}
