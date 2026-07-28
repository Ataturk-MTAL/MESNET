/**
 * Oturum hatalarının sınıflandırılması ve yeniden giriş döngüsü kırıcısı (#136).
 *
 * Buradaki her şey **saf fonksiyondur**: ağ yok, Keycloak yok, `window` yok. Sebebi
 * test edilebilirlik değil sadece — #136'daki döngü sayfa yüklemeleri ARASINDA dönüyordu,
 * yani JS bağlamı her turda sıfırlanıyordu. Kararın kendisini durumdan ayırmak, kararı
 * bağlamdan bağımsız doğrulanabilir kılan tek yol.
 */

/** Yeniden giriş kararının üç sonucu. */
export type AuthFailureAction =
  /** Geçici hata — aynı token ile tekrar dene. */
  | 'retry'
  /** Token gerçekten ölü — yeniden giriş gerekir, tekrar denemek anlamsız. */
  | 'reauth'
  /** Denemeler tükendi ya da hata kalıcı — kullanıcıya görünür ekran. */
  | 'give-up'

export interface AuthFailureInput {
  /** HTTP durum kodu; ağ hatasında `undefined`. */
  status?: number
  /** Axios hata kodu — `ERR_NETWORK`, `ECONNABORTED`, `ERR_CANCELED`. */
  code?: string
  /** Gönderilen token'ın `exp` alanı (saniye, epoch). Çözülemiyorsa `null`. */
  tokenExp: number | null
  /** Şu an (milisaniye, epoch). Dışarıdan verilir — test saat kurgusuna bağlı kalmasın. */
  now: number
  /** Kaçıncı deneme (1'den başlar). */
  attempt: number
  maxAttempts: number
}

/** JWT `exp` alanı saniye cinsindendir; JS zamanı milisaniye. */
const MS_PER_SECOND = 1000

/**
 * Token'ın `exp` alanını **ağa gitmeden** çözer. Bozuk/eksik girdide `null` döner —
 * bu bir hata değildir, "bilinmiyor" demektir ve çağıran tarafta ihtiyatlı davranılır.
 *
 * <p>Base64URL çözümü elle yapılır: `atob` yalnız standart alfabeyi bilir, JWT ise
 * `-` ve `_` kullanır. Dönüştürülmezse geçerli token'lar bozuk sanılırdı.</p>
 */
export function decodeTokenExp(token: string | null | undefined): number | null {
  if (!token) return null

  const parts = token.split('.')
  if (parts.length !== 3) return null

  const payload = parts[1]
  if (!payload) return null

  try {
    const base64 = payload.replace(/-/g, '+').replace(/_/g, '/')
    const padded = base64.padEnd(base64.length + ((4 - (base64.length % 4)) % 4), '=')
    const parsed: unknown = JSON.parse(atob(padded))

    if (typeof parsed !== 'object' || parsed === null) return null

    const exp = (parsed as { exp?: unknown }).exp
    return typeof exp === 'number' && Number.isFinite(exp) ? exp : null
  } catch {
    return null
  }
}

/**
 * Token yerel saate göre ölmüş mü? `exp` bilinmiyorsa **ölü sayılmaz** — bilinmeyeni
 * ölü saymak, çözülemeyen ama geçerli bir token yüzünden sonsuz yeniden girişe yol açardı.
 */
export function isTokenExpired(exp: number | null, now: number): boolean {
  if (exp === null) return false
  return exp * MS_PER_SECOND <= now
}

/**
 * Bir isteğin başarısızlığını sınıflandırır (#136).
 *
 * <p><b>Ayırt edici ölçüt token'ın yerel <c>exp</c>'idir, <c>WWW-Authenticate</c> başlığı
 * DEĞİL.</b> İki farklı 401 var ve ikisi de başlıkta <c>error="invalid_token"</c> üretir:</p>
 *
 * <ul>
 *   <li><b>API'nin JWKS önbelleği soğuk</b> (Aspire restart sonrası) — token GEÇERLİ,
 *       tekrar denemek doğru. Eski kod bunun için yazılmıştı ve haklıydı.</li>
 *   <li><b>Token gerçekten ölü</b> — tekrar denemek 15 saniye beyaz ekrandan başka bir şey
 *       üretmez; doğrudan yeniden giriş gerekir.</li>
 * </ul>
 *
 * <p>Yerel <c>exp</c> ikisini kesin ayırır: ağ gerektirmez, ortamdan bağımsızdır ve
 * <c>error_description</c>'ın serbest metnine bağlı değildir (o metin yalnız
 * <c>IncludeErrorDetails</c> açıkken gelir — üretimde gelmez).</p>
 */
export function classifyAuthFailure(input: AuthFailureInput): AuthFailureAction {
  const { status, code, tokenExp, now, attempt, maxAttempts } = input

  // Ölü token ile 401: tekrar denemek aynı sonucu verir. Deneme sayısına BAKILMAZ.
  if (status === 401 && isTokenExpired(tokenExp, now)) return 'reauth'

  const isTransient =
    status === undefined ||
    status === 401 ||
    code === 'ERR_NETWORK' ||
    code === 'ECONNABORTED' ||
    code === 'ERR_CANCELED'

  if (!isTransient) return 'give-up'
  if (attempt < maxAttempts) return 'retry'

  // Denemeler tükendi. Israrlı 401 = token reddedildi → yeniden giriş.
  // Israrlı ağ hatası = backend yok → yeniden giriş bunu çözmez, ekran göster.
  return status === 401 ? 'reauth' : 'give-up'
}

// ── Yeniden giriş döngüsü kırıcısı ────────────────────────────────────────────────

/** Bu pencerede bu sayıya ulaşılırsa yönlendirme durur. */
export const REAUTH_LIMIT = 3

/** Sayımın penceresi (ms). */
export const REAUTH_WINDOW_MS = 60_000

export type ReauthDecision = 'redirect' | 'halt'

/**
 * Son yönlendirme zaman damgalarına bakarak yeniden giriş yapılıp yapılmayacağına karar verir.
 *
 * <p><b>Neden zaman damgası listesi, bellekte bir sayaç değil:</b> #136'daki döngü sayfa
 * yüklemeleri arasında dönüyordu. Her yönlendirme JS bağlamını yok ediyor, dolayısıyla
 * bellekteki her sayaç sıfırlanıyor ve döngüyü asla göremiyordu. Durum yönlendirmeden
 * sağ çıkan bir yere yazılmalıdır — <c>sessionStorage</c> tam da bu kapsamdadır
 * (sekmeye özel, yönlendirmeyi aşar, sekme kapanınca gider).</p>
 */
export function decideReauth(
  timestamps: readonly number[],
  now: number,
  limit: number = REAUTH_LIMIT,
  windowMs: number = REAUTH_WINDOW_MS,
): ReauthDecision {
  const recent = timestamps.filter((t) => now - t < windowMs)
  return recent.length >= limit ? 'halt' : 'redirect'
}

/** Pencere dışında kalanları atıp yeni damgayı ekler — liste sınırsız büyümesin. */
export function recordReauth(
  timestamps: readonly number[],
  now: number,
  windowMs: number = REAUTH_WINDOW_MS,
): number[] {
  return [...timestamps.filter((t) => now - t < windowMs), now]
}
