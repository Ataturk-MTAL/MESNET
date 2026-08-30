/**
 * Kurum ağacı görünümünün SAF kararları — store'a, API'ye dokunmaz, testte tek başına koşar.
 *
 * <p><b>Neden ayrı dosya:</b> `InstitutionPage`/`InstitutionChildrenTree` hangi düğüm tipinin
 * hangi çocuk tipini listeleyeceğini bilmek zorunda; bu eşleme İl→İlçe, İlçe→Okul iki basamaklı
 * sabit bir kuraldır ve bileşen montajı olmadan test edilebilmelidir (depo deseni:
 * `utils/institutionScope.ts`).</p>
 */

export type InstitutionChildNodeType = 'District' | 'School'

/**
 * Bir düğümün DOĞRUDAN çocuklarının hangi tipte olduğunu belirler.
 *
 * İl müdürlüğünün çocuğu ilçe müdürlükleridir; ilçe müdürlüğünün çocuğu okullardır. Okul
 * düğümünün (ve tanınmayan bir tipin) çocuğu YOKTUR — çağıran bu durumda ağaç görünümünü hiç
 * açmamalıdır (bkz. `isSchoolNode`).
 */
export function childNodeTypeFor(parentNodeType: string): InstitutionChildNodeType | null {
  if (parentNodeType === 'Province') return 'District'
  if (parentNodeType === 'District') return 'School'
  return null
}

/** Okul-özel sekmeler (Alanlar/Personel/Dönemler) ve Kurum Teması yalnız bu tip için görünür. */
export function isSchoolNode(nodeType: string): boolean {
  return nodeType === 'School'
}
