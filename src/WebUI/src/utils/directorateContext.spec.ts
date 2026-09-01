import { describe, it, expect } from 'vitest'
import { isActingAsDirectorate } from './directorateContext'
import { resolveIsUpperNode } from 'src/composables/useNavigation'

/**
 * Kod tabanında birbirine çok benzeyen ama AKTİF BAĞLAM açıkken ayrışan iki soru var:
 *
 * - "Aktör üst düğüm mü?"  → resolveIsUpperNode(nodeType, activeInstitutionId)
 *   Aktif bağlam doluyken TRUE: `Kurumlar` ağacı okula geçince de görünmeli.
 *
 * - "Şu an müdürlük olarak mı davranıyorum?" → isActingAsDirectorate(nodeType)
 *   Aktif bağlam doluyken FALSE: kiracı o okuldur, okul panosu doğrudur.
 *
 * Buradaki bariz hata `resolveIsUpperNode`'u kopyalamak olurdu: il yetkilisi bir okula
 * geçtiğinde müdürlük panosunu görür, o pano da okul kiracısında alt ağaç sorar ve okul kendi
 * altında hiçbir şey bulamaz — HATA DEĞİL, BOŞ PANO.
 */
describe('isActingAsDirectorate', () => {
  it('il müdürlüğü bağlamında true döner', () => {
    expect(isActingAsDirectorate('Province')).toBe(true)
  })

  it('ilçe müdürlüğü bağlamında true döner', () => {
    expect(isActingAsDirectorate('District')).toBe(true)
  })

  it('okul bağlamında false döner', () => {
    expect(isActingAsDirectorate('School')).toBe(false)
  })

  it('düğüm tipi bilinmiyorsa false döner — okul panosu güvenli varsayılandır', () => {
    expect(isActingAsDirectorate(null)).toBe(false)
    expect(isActingAsDirectorate(undefined)).toBe(false)
    expect(isActingAsDirectorate('')).toBe(false)
  })

  it('resolveIsUpperNode ile AYNI ŞEY DEĞİLDİR — aktif bağlam açıkken ayrışırlar', () => {
    // İl yetkilisi bir okula geçti: institutionStore aktif bağlama bağlı olduğu için
    // nodeType artık 'School', activeInstitutionId ise dolu.
    const nodeType = 'School'
    const activeInstitutionId = 'ataturk-id'

    // Aktör hâlâ üst düğümdür (Kurumlar ağacı görünmeli)...
    expect(resolveIsUpperNode(nodeType, activeInstitutionId)).toBe(true)
    // ...ama müdürlük olarak DAVRANMIYOR (okul panosu görmeli).
    expect(isActingAsDirectorate(nodeType)).toBe(false)
  })
})
