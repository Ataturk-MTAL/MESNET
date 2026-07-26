/**
 * Kullanıcı listesinde alan (branş) durumunun gösterimi (#126).
 *
 * Karar backend'de permission'dan türetilir (`BranchRequirement`) ve DTO'da
 * `branchRequired` / `branchMissing` olarak gelir. Buradaki yardımcılar yalnız o
 * bilgiyi sunum diline çevirir — kuralı yeniden hesaplamazlar, iki ayrı doğruluk
 * kaynağı oluşmasın diye.
 */

/** Listede alan durumu gösterilecek kullanıcının ilgili alanları. */
export interface BranchAssignmentState {
  branchCodes: string[]
  branchRequired: boolean
  branchMissing: boolean
}

/** Alan hücresinde gösterilecek görsel durum. */
export type BranchCellState =
  /** Alan beklenip girilmemiş — uyarı rozeti. */
  | 'missing'
  /** Alan(lar) girilmiş — kod rozetleri. */
  | 'assigned'
  /** Alan beklenmez ve girilmemiş — nötr tire. Yöneticide NORMAL durum. */
  | 'none'

/**
 * Alan hücresinin durumunu belirler.
 *
 * Boş liste tek başına uyarı sebebi DEĞİLDİR: okul müdürü ve müdür yardımcısı hiçbir
 * alana bağlı değildir ve bu doğru durumdur. Uyarı yalnız alan beklenip girilmemişse
 * gösterilir.
 */
export function resolveBranchCellState(user: BranchAssignmentState): BranchCellState {
  if (user.branchMissing) return 'missing'
  if (user.branchCodes.length > 0) return 'assigned'
  return 'none'
}

/**
 * "Yalnız branş atanmamış kullanıcılar" filtresinin istemci tarafı karşılığı.
 *
 * Sunucu filtresi (`missingBranchOnly`) ile aynı kararı verir; muafiyeti olan
 * kullanıcılar (alan beklenmeyenler) listeye ASLA girmez.
 */
export function filterMissingBranch<T extends BranchAssignmentState>(users: T[]): T[] {
  return users.filter((u) => u.branchMissing)
}
