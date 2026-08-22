import { describe, it, expect, beforeEach, vi } from 'vitest'
import { setActivePinia, createPinia } from 'pinia'

const listRoles = vi.fn()

vi.mock('src/api/security', () => ({
  securityApi: {
    listRoles: () => listRoles(),
  },
}))

const { useRoleCatalogStore } = await import('./roleCatalog')

/**
 * Rol kataloğu — arayüzün tek doğruluk kaynağı (#129).
 *
 * Sabit rol listesi kaldırıldı: elle yazılan liste gerçek rollerle eşleşmiyordu ve karşılığı
 * olmayan adlar (`deputy_director` vb.) sunucuya gidiyordu. Bu testler listenin ve Türkçe
 * etiketlerin API'den geldiğini kilitler.
 */
function apiRoles() {
  return {
    data: [
      { roleName: 'InstitutionManager', label: 'Kurum Müdürü', description: 'Müdür', permissions: ['institution:*'] },
      { roleName: 'DeputyDirector', label: 'Müdür Yardımcısı', description: 'Müdür yrd.', permissions: ['user:*'] },
      { roleName: 'MasterTrainer', label: 'Usta Öğretici', description: 'Usta öğretici', permissions: ['company:attendance:manage'] },
    ],
  }
}

describe('roleCatalog store', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
    listRoles.mockReset()
    listRoles.mockResolvedValue(apiRoles())
  })

  it('rol listesi API den gelir, sabit liste yoktur', async () => {
    // Arrange
    const store = useRoleCatalogStore()

    // Act
    await store.load()

    // Assert — seçenekler API cevabıyla birebir
    expect(listRoles).toHaveBeenCalledTimes(1)
    expect(store.options.map((o) => o.value)).toEqual([
      'InstitutionManager',
      'DeputyDirector',
      'MasterTrainer',
    ])
  })

  it('seçenek etiketleri Türkçedir ve backend rol adı value olarak gider', async () => {
    // Arrange
    const store = useRoleCatalogStore()

    // Act
    await store.load()

    // Assert
    expect(store.options[1]).toMatchObject({
      label: 'Müdür Yardımcısı',
      value: 'DeputyDirector',
      caption: 'Müdür yrd.',
    })
  })

  it('rol adı Türkçe etikete çözülür', async () => {
    // Arrange
    const store = useRoleCatalogStore()

    // Act
    await store.load()

    // Assert
    expect(store.labelFor('MasterTrainer')).toBe('Usta Öğretici')
  })

  it('tanınmayan rol adı gizlenmez, ham hâliyle gösterilir', async () => {
    // Arrange — bozuk kayıt (eski uydurma ad) görünür kalmalı
    const store = useRoleCatalogStore()

    // Act
    await store.load()

    // Assert
    expect(store.labelFor('deputy_director')).toBe('deputy_director')
  })

  it('katalog bir kez yüklenir, ikinci çağrı ağa çıkmaz', async () => {
    // Arrange
    const store = useRoleCatalogStore()

    // Act
    await store.load()
    await store.load()

    // Assert
    expect(listRoles).toHaveBeenCalledTimes(1)
  })

  it('invalidate sonrası katalog yeniden çekilir', async () => {
    // Arrange
    const store = useRoleCatalogStore()
    await store.load()

    // Act
    store.invalidate()
    await store.load()

    // Assert
    expect(listRoles).toHaveBeenCalledTimes(2)
  })

  it('yükleme hatası yutulmaz ve katalog yüklendi sayılmaz', async () => {
    // Arrange
    const store = useRoleCatalogStore()
    listRoles.mockRejectedValueOnce(new Error('ağ hatası'))

    // Act & Assert — hata çağırana ulaşır (sessizce boş liste göstermek yanlış olurdu)
    await expect(store.load()).rejects.toThrow('ağ hatası')
    expect(store.loaded).toBe(false)
    expect(store.loading).toBe(false)
  })
})
