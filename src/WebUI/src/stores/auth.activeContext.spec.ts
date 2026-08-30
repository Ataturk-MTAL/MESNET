import { describe, it, expect, beforeEach } from 'vitest'
import { setActivePinia, createPinia } from 'pinia'
import { useAuthStore, type AuthUser } from './auth'

/**
 * Aktif bağlamın ön yüzdeki tek doğruluk kaynağı.
 *
 * <p><b>Neden tek bir computed:</b> kuruma bağlı her store (`institutionStore`,
 * `academicPeriodStore`, `entityOptionsStore`) bugün `user.institutionId`'yi okuyor. Bağlam
 * geldiğinde her biri kendi başına "hangi kurum" sorusunu cevaplasaydı, biri değişip diğeri
 * kalırdı ve kullanıcı bir ekranda A okulunu, diğerinde B okulunu görürdü.</p>
 *
 * <p><b>`institutionId` EV KURUMUDUR ve değişmez.</b> Denetim izinin "kim olduğun / nerede
 * davrandığın" ayrımı ona bağlıdır; ön yüzde de ezilmemelidir.</p>
 */
describe('authStore — aktif bağlam', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
  })

  it('aktif bağlam yokken geçerli kurum EV kurumudur', () => {
    const store = useAuthStore()
    store.user = { institutionId: 'ev-kurumu', activeInstitutionId: null } as unknown as AuthUser

    expect(store.currentInstitutionId).toBe('ev-kurumu')
  })

  it('aktif bağlam varken geçerli kurum ODUR', () => {
    const store = useAuthStore()
    store.user = { institutionId: 'ev-kurumu', activeInstitutionId: 'okul' } as unknown as AuthUser

    expect(store.currentInstitutionId).toBe('okul')
  })

  it('aktif bağlam EV kurumunu EZMEZ', () => {
    // Ezilseydi denetim izindeki CrossedTenantBoundary ayrımı ön yüzde de kaybolurdu.
    const store = useAuthStore()
    store.user = { institutionId: 'ev-kurumu', activeInstitutionId: 'okul' } as unknown as AuthUser

    expect(store.user?.institutionId).toBe('ev-kurumu')
  })

  it('kullanıcı yokken geçerli kurum null olur', () => {
    const store = useAuthStore()
    store.user = null

    expect(store.currentInstitutionId).toBeNull()
  })
})
