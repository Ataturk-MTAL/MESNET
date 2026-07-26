import { describe, it, expect, beforeEach } from 'vitest'
import { setActivePinia, createPinia } from 'pinia'
import { useAuthStore, ALL_BRANCHES_PERMISSION, type AuthUser } from './auth'

/**
 * Alan (branş) kapsamının frontend tarafı (#126).
 *
 * Karar rol adına DEĞİL permission'a bakar; muafiyet varsa alan listesine hiç bakılmaz.
 * Boş liste muafiyetli kullanıcıda normaldir, hata değildir.
 */
function makeUser(branchCodes: string[]): AuthUser {
  return {
    id: 'u1',
    username: 'test',
    email: 'test@mesnet.local',
    firstName: 'Test',
    lastName: 'Kullanıcı',
    fullName: 'Test Kullanıcı',
    roles: [],
    institutionId: 'i1',
    branchCode: branchCodes[0] ?? null,
    branchCodes,
  }
}

describe('authStore — alan kapsamı', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
  })

  it('muafiyet izni olan kullanıcıda kapsam kısıtı yoktur (null döner)', () => {
    // Arrange — yöneticinin branşı YOKTUR, bu normaldir
    const store = useAuthStore()
    store.user = makeUser([])
    store.permissions = [ALL_BRANCHES_PERMISSION]

    // Act & Assert
    expect(store.canManageAllBranches).toBe(true)
    expect(store.writableBranchCodes).toBeNull()
    expect(store.canWriteBranch('EET')).toBe(true)
    expect(store.canWriteBranch('MTT')).toBe(true)
  })

  it('muafiyet wildcard izinle de tanınır', () => {
    // Arrange — InstitutionManager "institution:*" taşır
    const store = useAuthStore()
    store.user = makeUser([])
    store.permissions = ['institution:*']

    // Act & Assert
    expect(store.canManageAllBranches).toBe(true)
    expect(store.writableBranchCodes).toBeNull()
  })

  it('muafiyeti olmayan kullanıcı yalnız kendi alanlarına yazabilir', () => {
    // Arrange — alan şefi
    const store = useAuthStore()
    store.user = makeUser(['EET'])
    store.permissions = ['department:distribution:manage']

    // Act & Assert
    expect(store.canManageAllBranches).toBe(false)
    expect(store.writableBranchCodes).toEqual(['EET'])
    expect(store.canWriteBranch('EET')).toBe(true)
    expect(store.canWriteBranch('MTT')).toBe(false)
  })

  it('birden çok alandan sorumlu kullanıcı her ikisine de yazabilir', () => {
    // Arrange
    const store = useAuthStore()
    store.user = makeUser(['EET', 'MTT'])
    store.permissions = ['department:distribution:manage']

    // Act & Assert
    expect(store.canWriteBranch('EET')).toBe(true)
    expect(store.canWriteBranch('MTT')).toBe(true)
    expect(store.canWriteBranch('BLS')).toBe(false)
  })

  it('muafiyeti olmayan ve alanı olmayan kullanıcı hiçbir alana yazamaz', () => {
    // Arrange — branşı girilmemiş alan şefi
    const store = useAuthStore()
    store.user = makeUser([])
    store.permissions = ['department:distribution:manage']

    // Act & Assert
    expect(store.writableBranchCodes).toEqual([])
    expect(store.canWriteBranch('EET')).toBe(false)
  })

  it('alan kodu verilmezse muafiyetsiz kullanıcı yazamaz', () => {
    // Arrange
    const store = useAuthStore()
    store.user = makeUser(['EET'])
    store.permissions = ['department:distribution:manage']

    // Act & Assert — kapsamı bilinmeyen istek kabul edilmez
    expect(store.canWriteBranch(null)).toBe(false)
    expect(store.canWriteBranch('')).toBe(false)
  })

  it('alan kodu karşılaştırması büyük/küçük harfe duyarsızdır', () => {
    // Arrange
    const store = useAuthStore()
    store.user = makeUser(['EET'])
    store.permissions = ['department:distribution:manage']

    // Act & Assert
    expect(store.canWriteBranch('eet')).toBe(true)
  })
})
