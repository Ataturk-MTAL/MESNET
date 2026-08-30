import { describe, it, expect } from 'vitest'
import { resolveEditableInstitutionId } from './institutionScope'

/**
 * Bu testin varlık nedeni ÖLÇÜLMÜŞ bir hatadır (27.08.2026):
 *
 * `InstitutionPage` düzenlenecek kurumu `institutions[0].id` ile seçiyordu. Okul rollerinde
 * liste zaten tek elemanlı olduğu için hata görünmüyordu; `platform:tenant:manage` taşıyan
 * aktörde (SystemAdmin) liste bütün okulları döndürür ve sorgunun `ORDER BY`'ı yoktur —
 * Postgres güncellenen satırı heap'te yerinden oynattığı için sıra HER YAZMADAN SONRA
 * değişebilir.
 *
 * Sonuç: admin, ekranda "Cumhuriyet" yazarken kendi okulu "Atatürk" olan bir oturumda
 * Cumhuriyet'in paletini kaydetti; tema ise `institutionStore` üzerinden kendi okulundan
 * uygulandığı için ilk sayfa geçişinde eski renge döndü. Kayıp veri yoktu — yazma YANLIŞ
 * OKULA gitti.
 */
describe('resolveEditableInstitutionId', () => {
  const list = [
    { id: 'cumhuriyet-id' },
    { id: 'gazi-id' },
    { id: 'ataturk-id' },
  ]

  it('aktörün kendi kurumunu seçer — liste başka bir okulla başlasa bile', () => {
    // Arrange: liste Cumhuriyet ile başlıyor, aktörün kurumu Atatürk
    // Act
    const id = resolveEditableInstitutionId('ataturk-id', list)
    // Assert
    expect(id).toBe('ataturk-id')
  })

  it('kendi kurumu listede yoksa yine kendi kurumunu seçer — yetki kararı sunucunun', () => {
    expect(resolveEditableInstitutionId('baska-okul-id', list)).toBe('baska-okul-id')
  })

  it('kurumu olmayan platform aktöründe listeye düşer ama SIRAYA BAĞLI KALMAZ', () => {
    // Aynı içerik, farklı sıra → aynı sonuç. Sıra bağımlılığı hatanın kendisiydi.
    const karisik = [{ id: 'gazi-id' }, { id: 'ataturk-id' }, { id: 'cumhuriyet-id' }]
    expect(resolveEditableInstitutionId(null, list)).toBe(
      resolveEditableInstitutionId(null, karisik),
    )
  })

  it('kurum yok ve liste boşsa null döner — çağıran hata mesajı gösterir', () => {
    expect(resolveEditableInstitutionId(null, [])).toBeNull()
  })

  it('boş string kurum kimliği yokmuş sayılır', () => {
    expect(resolveEditableInstitutionId('', [{ id: 'gazi-id' }])).toBe('gazi-id')
  })
})
