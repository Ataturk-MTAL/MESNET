import { describe, it, expect } from 'vitest'
import {
  classifyAuthFailure,
  decideReauth,
  decodeTokenExp,
  isTokenExpired,
  recordReauth,
  REAUTH_LIMIT,
  REAUTH_WINDOW_MS,
} from './authFailure'

/**
 * #136 regresyonu: süresi dolmuş token sonsuz yeniden giriş döngüsü üretiyordu.
 *
 * <p>Buradaki testler bilinçli olarak <b>saf mantığı</b> hedefler. Gerçek döngü sayfa
 * yüklemeleri arasında dönüyordu; jsdom bir sayfa yüklemesini temsil edemez (yönlendirme
 * JS bağlamını yok eder), dolayısıyla keycloak-js'in init/redirect dansını taklit eden bir
 * test hiçbir şey kanıtlamaz — yalnızca kanıtlıyormuş gibi görünür. Döngünün sonlandığını
 * gerçekten garanti eden iki şey sınıflandırıcı ve döngü kırıcısıdır; ikisi de saf ve
 * burada doğrulanır. Uçtan uca doğrulama Playwright + sahte kimlik sağlayıcı ister —
 * bu depodaki test yığınında yok.</p>
 */

/** Test için JWT üretir — imza doğrulanmadığından gövde yeterlidir. */
function tokenWithExp(expSeconds: number): string {
  const encode = (obj: unknown) =>
    btoa(JSON.stringify(obj)).replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/, '')
  return `${encode({ alg: 'RS256' })}.${encode({ exp: expSeconds })}.imza-dogrulanmiyor`
}

const NOW = 1_700_000_000_000 // sabit an; testler duvar saatine bağlı olmasın

describe('decodeTokenExp', () => {
  it('geçerli token için exp döndürür', () => {
    expect(decodeTokenExp(tokenWithExp(1_700_000_060))).toBe(1_700_000_060)
  })

  it('base64url alfabesini çözer', () => {
    // '-' ve '_' içeren gövde: atob standart alfabeyi bilir, dönüşüm olmadan patlardı.
    const token = tokenWithExp(1_700_000_060)
    expect(token).not.toContain('+')
    expect(decodeTokenExp(token)).toBe(1_700_000_060)
  })

  it('exp alanı yoksa null döner', () => {
    const encode = (obj: unknown) => btoa(JSON.stringify(obj)).replace(/=+$/, '')
    expect(decodeTokenExp(`${encode({})}.${encode({ sub: 'x' })}.imza`)).toBeNull()
  })

  it('bozuk girdide fırlatmaz, null döner', () => {
    expect(decodeTokenExp('bu-bir-jwt-degil')).toBeNull()
    expect(decodeTokenExp('a.b.c')).toBeNull()
    expect(decodeTokenExp(null)).toBeNull()
    expect(decodeTokenExp('')).toBeNull()
  })
})

describe('isTokenExpired', () => {
  it('geçmiş exp için true', () => {
    expect(isTokenExpired(NOW / 1000 - 1, NOW)).toBe(true)
  })

  it('gelecek exp için false', () => {
    expect(isTokenExpired(NOW / 1000 + 60, NOW)).toBe(false)
  })

  it('exp bilinmiyorsa ölü SAYMAZ', () => {
    // Bilinmeyeni ölü saymak, çözülemeyen ama geçerli bir token yüzünden
    // sonsuz yeniden girişe yol açardı — düzeltilen hatanın aynısı.
    expect(isTokenExpired(null, NOW)).toBe(false)
  })
})

describe('classifyAuthFailure', () => {
  const base = { now: NOW, attempt: 1, maxAttempts: 5 }
  const expired = NOW / 1000 - 3600
  const valid = NOW / 1000 + 3600

  it('ölü token + 401 → tek istekte reauth, tekrar denemez', () => {
    // #136'nın çekirdeği: eskiden bu durum "geçici" sayılıp 15+ saniye beyaz ekran üretiyordu.
    expect(classifyAuthFailure({ ...base, status: 401, tokenExp: expired })).toBe('reauth')
  })

  it('ölü token + 401 son denemede bile reauth', () => {
    expect(
      classifyAuthFailure({ ...base, status: 401, tokenExp: expired, attempt: 5 }),
    ).toBe('reauth')
  })

  it('GEÇERLİ token + 401 → retry (JWKS soğuk başlangıcı korunur)', () => {
    // Aspire restart sonrası API'nin JWKS önbelleği soğuk olabilir; token geçerlidir ve
    // beklemek doğrudur. Bu davranışı kaybetmek eski hatayı diriltirdi.
    expect(classifyAuthFailure({ ...base, status: 401, tokenExp: valid })).toBe('retry')
  })

  it('geçerli token + 401 denemeler tükenince reauth', () => {
    expect(
      classifyAuthFailure({ ...base, status: 401, tokenExp: valid, attempt: 5 }),
    ).toBe('reauth')
  })

  it('ağ hatası → retry', () => {
    expect(
      classifyAuthFailure({ ...base, code: 'ERR_NETWORK', tokenExp: valid }),
    ).toBe('retry')
    expect(
      classifyAuthFailure({ ...base, code: 'ECONNABORTED', tokenExp: valid }),
    ).toBe('retry')
  })

  it('ağ hatası denemeler tükenince give-up — yeniden giriş bunu çözmez', () => {
    expect(
      classifyAuthFailure({ ...base, code: 'ERR_NETWORK', tokenExp: valid, attempt: 5 }),
    ).toBe('give-up')
  })

  it('403/500 gibi kalıcı hatalar doğrudan give-up', () => {
    expect(classifyAuthFailure({ ...base, status: 403, tokenExp: valid })).toBe('give-up')
    expect(classifyAuthFailure({ ...base, status: 500, tokenExp: valid })).toBe('give-up')
  })
})

describe('decideReauth — döngü kırıcı', () => {
  it('geçmiş yoksa yönlendirir', () => {
    expect(decideReauth([], NOW)).toBe('redirect')
  })

  it('pencere içinde limitin altında yönlendirir', () => {
    expect(decideReauth([NOW - 1000, NOW - 2000], NOW)).toBe('redirect')
  })

  it('pencere içinde limite ulaşınca DURUR', () => {
    // #136'nın kabul kriteri: döngü yok. Bunu garanti eden tek şey bu.
    const recent = [NOW - 1000, NOW - 2000, NOW - 3000]
    expect(recent.length).toBe(REAUTH_LIMIT)
    expect(decideReauth(recent, NOW)).toBe('halt')
  })

  it('eski denemeler pencereden düşer, yönlendirmeyi engellemez', () => {
    const old = NOW - REAUTH_WINDOW_MS - 1
    expect(decideReauth([old, old, old], NOW)).toBe('redirect')
  })
})

describe('recordReauth', () => {
  it('yeni damgayı ekler', () => {
    expect(recordReauth([], NOW)).toEqual([NOW])
  })

  it('pencere dışındakileri atar — liste sınırsız büyümez', () => {
    const old = NOW - REAUTH_WINDOW_MS - 1
    const recent = NOW - 1000
    expect(recordReauth([old, recent], NOW)).toEqual([recent, NOW])
  })

  it('üç kez üst üste çağrılınca durdurma eşiğine ulaşır', () => {
    // Kırıcının uçtan uca davranışı: kaydet → karar ver → kaydet → ...
    let log: number[] = []
    expect(decideReauth(log, NOW)).toBe('redirect')
    log = recordReauth(log, NOW)
    expect(decideReauth(log, NOW + 100)).toBe('redirect')
    log = recordReauth(log, NOW + 100)
    expect(decideReauth(log, NOW + 200)).toBe('redirect')
    log = recordReauth(log, NOW + 200)
    expect(decideReauth(log, NOW + 300)).toBe('halt')
  })
})
