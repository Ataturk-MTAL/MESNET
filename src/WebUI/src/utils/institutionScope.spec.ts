import { describe, it, expect } from 'vitest'
import { resolveEditableInstitutionId } from './institutionScope'

/**
 * Bu testin varlık nedeni ÖLÇÜLMÜŞ bir hatadır (27.08.2026):
 *
 * `InstitutionPage` düzenlenecek kurumu `institutions[0].id` ile seçiyordu. Okul rollerinde
 * liste zaten tek elemanlı olduğu için hata görünmüyordu; `platform:tenant:manage` taşıyan
 * aktörde (SystemAdmin) liste bütün okulları döndürür ve sorgunun `ORDER BY`'ı yoktu —
 * Postgres güncellenen satırı heap'te yerinden oynattığı için sıra HER YAZMADAN SONRA
 * değişebiliyordu.
 *
 * Sonuç: admin, ekranda "Cumhuriyet" yazarken kendi okulu "Atatürk" olan bir oturumda
 * Cumhuriyet'in paletini kaydetti; tema ise `institutionStore` üzerinden kendi okulundan
 * uygulandığı için ilk sayfa geçişinde eski renge döndü. Kayıp veri yoktu — yazma YANLIŞ
 * OKULA gitti.
 *
 * Kurum ağacıyla birlikte fonksiyon ÜÇÜNCÜ bir girdi kazandı: rota parametresi. İl yetkilisi
 * `/institutions/:id` ile alt ağacındaki bir okulu açtığında hedef O OKULDUR — kendi kurumu
 * (İl MEM) değil.
 */
describe('resolveEditableInstitutionId', () => {
  const list = [{ id: 'cumhuriyet-id' }, { id: 'gazi-id' }, { id: 'ataturk-id' }]

  it('rota parametresi her şeyden önce gelir — alt ağaçtaki okul açılıyordur', () => {
    // Arrange: aktörün kendi kurumu İl MEM, rota bir okulu işaret ediyor
    // Act
    const id = resolveEditableInstitutionId('okul-id', 'il-mem-id', list)
    // Assert
    expect(id).toBe('okul-id')
  })

  it('rota parametresi yoksa aktörün kendi kurumunu seçer — liste başka okulla başlasa bile', () => {
    expect(resolveEditableInstitutionId(null, 'ataturk-id', list)).toBe('ataturk-id')
  })

  it('kendi kurumu listede yoksa yine kendi kurumunu seçer — yetki kararı sunucunun', () => {
    expect(resolveEditableInstitutionId(null, 'baska-okul-id', list)).toBe('baska-okul-id')
  })

  it('kurumu olmayan platform aktöründe listeye düşer ama SIRAYA BAĞLI KALMAZ', () => {
    // Aynı içerik, farklı sıra → aynı sonuç. Sıra bağımlılığı hatanın kendisiydi.
    const karisik = [{ id: 'gazi-id' }, { id: 'ataturk-id' }, { id: 'cumhuriyet-id' }]
    expect(resolveEditableInstitutionId(null, null, list)).toBe(
      resolveEditableInstitutionId(null, null, karisik),
    )
  })

  it('kurum yok ve liste boşsa null döner — çağıran hata mesajı gösterir', () => {
    expect(resolveEditableInstitutionId(null, null, [])).toBeNull()
  })

  it('boş string kurum kimliği yokmuş sayılır', () => {
    expect(resolveEditableInstitutionId('', '', [{ id: 'gazi-id' }])).toBe('gazi-id')
  })

  /**
   * ÜÇÜNCÜ KOPYA (29.08.2026): `InstitutionPage` ve `InstitutionFormPage` `ownId`
   * argümanını `authStore.user?.institutionId` (EV kurumu) ile besliyordu. Aktif bağlam
   * açıkken (il yetkilisi bir okula geçtiğinde) rota parametresi yoksa (menüden
   * "Kurum Bilgileri") sonuç EV kurumu oluyordu — üst barda "X Okulu adına
   * çalışıyorsunuz" rozeti dururken il yetkilisi kendi İl MEM kaydını düzenliyordu.
   *
   * `resolveEditableInstitutionId`'nin kendisi hangi kaynaktan geldiğini bilmez — sözleşme
   * ÇAĞIRANDA: `ownId` her zaman DAVRANILAN kurum (aktif bağlam varsa o, yoksa ev kurumu)
   * olmalıdır. Bu test o sözleşimi kilitler: ownId'ye aktif bağlam geçilince sonuç aktif
   * bağlam olur, ev kurumu OLMAZ — düzeltilen hata tam bu ayrımın kaybolmasıydı.
   */
  it('aktif bağlam varken ownId davranılan (bağlamdaki) okul olmalı — ev kurumu DEĞİL', () => {
    const evKurumu = 'il-mem-id'
    const aktifBaglam = 'ataturk-id'

    // Doğru çağrı: sayfa ownId'yi authStore.currentInstitutionId'den besler → aktif bağlam
    expect(resolveEditableInstitutionId(null, aktifBaglam, list)).toBe(aktifBaglam)
    // ownId ev kurumuyla beslenseydi (düzeltilen hata) sonuç aktif bağlamla eşleşmezdi
    expect(resolveEditableInstitutionId(null, evKurumu, list)).not.toBe(aktifBaglam)
  })
})
