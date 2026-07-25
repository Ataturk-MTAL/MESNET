/**
 * Kullanıcının girdiği URL'leri `:href`/`:src` bağlamadan önce süzer.
 *
 * Neden gerekli: kurum web sitesi gibi alanlar serbest metin girdisidir. Oraya
 * `javascript:alert(document.cookie)` ya da `data:text/html,...` yazan bir kullanıcı,
 * bağlantıya tıklayan HERKESİN oturumunda kod çalıştırır (depolanmış XSS). Tarayıcı
 * bu şemaları `<a href>` üzerinden çalıştırır; Vue kaçışlaması bunu engellemez.
 *
 * Yalnız http ve https geçer. Şemasız girdi (ör. "okulum.com") kullanıcı için doğal
 * olduğundan reddedilmez, başına https:// eklenir.
 */

const ALLOWED_PROTOCOLS = ['http:', 'https:']

/** Bağlanabilir güvenli bir mutlak URL döndürür; güvenli değilse null. */
export function toSafeUrl(raw: string | null | undefined): string | null {
  if (!raw) return null

  const trimmed = raw.trim()
  if (!trimmed) return null

  // Şema yoksa https varsay — "okulum.com" gibi girdiler kullanıcı için normaldir.
  // Tehlikeli şemalar (javascript:, data:, vbscript:) bu dalın dışında kalır çünkü
  // şema deseniyle eşleşirler; URL kurucusu onları ayrıştırır ve aşağıdaki liste eler.
  const candidate = /^[a-z][a-z0-9+.-]*:/i.test(trimmed) ? trimmed : `https://${trimmed}`

  let parsed: URL
  try {
    parsed = new URL(candidate)
  } catch {
    return null
  }

  return ALLOWED_PROTOCOLS.includes(parsed.protocol) ? parsed.href : null
}

/** Girdi güvenli bir http(s) URL'ine çözülüyor mu? Zod `.refine()` için de kullanılır. */
export function isSafeUrl(raw: string | null | undefined): boolean {
  return toSafeUrl(raw) !== null
}
