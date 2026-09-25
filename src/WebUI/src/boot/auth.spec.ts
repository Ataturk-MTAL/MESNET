import { describe, it, expect, beforeEach, vi } from 'vitest'
import { REAUTH_LIMIT } from 'src/utils/authFailure'

const showSessionExpiredScreen = vi.fn()

vi.mock('./sessionExpiredScreen', () => ({ showSessionExpiredScreen }))
vi.mock('keycloak-js', () => ({ default: vi.fn() }))
vi.mock('stores/auth', () => ({ useAuthStore: vi.fn() }))
vi.mock('stores/notifications', () => ({ useNotificationStore: vi.fn() }))
vi.mock('../utils/logger', () => ({ logger: { error: vi.fn(), warn: vi.fn(), info: vi.fn() } }))

/**
 * Döngü kırıldıktan sonra yeniden yönlendirme OLMAMALI.
 *
 * NEDEN VAR: Döngü kırılınca sayaç sıfırlanıyordu. Aynı anda bekleyen öteki istekler (sayfa
 * açılışında 5–10 paralel çağrı) boş sayaçla yeniden `login()` çağırıyor, yönlendirme oturum
 * ekranını eziyor ve döngü sürüyordu. Kullanıcı ekranı hiç görmüyor, yalnız konsolda
 * "Yeniden giriş döngüsü kırıldı" hatası birikiyordu.
 */
describe('reauthenticate döngü kırıcısı', () => {
  beforeEach(() => {
    vi.resetModules()
    showSessionExpiredScreen.mockClear()
    const now = Date.now()
    sessionStorage.setItem(
      'mesnet.reauth.attempts',
      JSON.stringify(Array.from({ length: REAUTH_LIMIT }, (_, i) => now - i)),
    )
  })

  it('döngü kırıldıktan sonra gelen çağrılar yönlendirmez, ekranı ezmez', async () => {
    // Keycloak bilerek başlatılmıyor: yönlendirmeye kalkışan çağrı getKeycloak()'ta patlar.
    const { reauthenticate } = await import('./auth')

    reauthenticate('ilk istek')

    expect(() => reauthenticate('paralel istek 2')).not.toThrow()
    expect(() => reauthenticate('paralel istek 3')).not.toThrow()
    expect(showSessionExpiredScreen).toHaveBeenCalledTimes(1)
  })
})
