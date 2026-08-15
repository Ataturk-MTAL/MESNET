/**
 * İstemci tarafı seviyeli logger (#144).
 *
 * Neden: WebUI'daki tanı satırlarının tamamı yalnız kullanıcının kendi konsoluna yazıyor ve
 * orada ölüyordu. #136'da (süresi dolmuş token → sonsuz yeniden giriş döngüsü) kullanıcı
 * **beyaz ekran** gördü, "açılmıyor" diyebildi; teşhis konsolda zaten yazılıydı ama kimse
 * görmedi — hata ancak sunucu logundaki ikinci bir izden fark edildi.
 *
 * `error` seviyesi hem konsola yazar hem sunucuya gönderir. Diğer seviyeler yalnız konsola.
 */

const ENDPOINT = '/api/telemetry/client-errors'

/**
 * DÖNGÜ KORUMASI — bu modülün en önemli kuralı.
 *
 * Telemetri gönderimi başarısız olursa o hata **asla** telemetriye yazılmaz ve **asla** tekrar
 * denenmez; korumayı `send()` içindeki **bilerek boş** `catch` sağlar. Aksi hâlde tam da
 * #136'nın kendisi üretilir: hata → gönder → gönderim hatası → hata → gönder …
 *
 * Burada ayrıca bir "gönderim sürüyor" kilidi **YOKTU** — ilk sürümde vardı ve testte
 * yakalandı: kilit, uçuştaki gönderim sırasında oluşan **farklı** bir hatayı da sessizce
 * düşürüyordu. Tekrarı önleyen şey imza penceresidir, kilit değil. Sel riski istemcide imza
 * penceresi, sunucuda "ClientTelemetry" hız sınırı (30/dk) ile karşılanır.
 */

/** Aynı hatanın tekrarı sunucuyu boğmasın — pencere içinde aynı imza bir kez gider. */
const recentSignatures = new Map<string, number>()
const DEDUPE_WINDOW_MS = 10_000

export type LogLevel = 'info' | 'warn' | 'error'

export interface ClientErrorReport {
  level: LogLevel
  message: string
  stack?: string
  path?: string
  appVersion?: string
  correlationId?: string
}

/** Sunucuya gönderilecek imza — mesaj + yol. Aynı hata döngüde tekrarlarsa bir kez gider. */
function signatureOf(report: ClientErrorReport): string {
  return `${report.message}|${report.path ?? ''}`
}

function isDuplicate(report: ClientErrorReport): boolean {
  const signature = signatureOf(report)
  const now = Date.now()
  const seenAt = recentSignatures.get(signature)

  if (seenAt !== undefined && now - seenAt < DEDUPE_WINDOW_MS) return true

  recentSignatures.set(signature, now)

  // Sınırsız büyümesin: pencere dışı kayıtları at.
  for (const [key, at] of recentSignatures) {
    if (now - at >= DEDUPE_WINDOW_MS) recentSignatures.delete(key)
  }

  return false
}

/**
 * Hatayı okunabilir metne çevirir.
 *
 * Ham nesne göndermek YASAK: `useNotify.ts` bugün ham API hata nesnesini konsola basıyor;
 * aynısı sunucuya gitseydi içinde token, e-posta veya öğrenci verisi bulunabilirdi. Sunucu
 * tarafında da temizleme var (`ClientErrorSanitizer`) ama iki katman da gerekli — istemci
 * gönderdiğini bilmeli.
 */
export function describe(value: unknown): string {
  if (value === null || value === undefined) return String(value)
  if (typeof value === 'string') return value
  if (value instanceof Error) return `${value.name}: ${value.message}`

  // Nesneler serileştirilmez — ne taşıdıkları denetlenemez.
  return Object.prototype.toString.call(value)
}

function send(report: ClientErrorReport): void {
  if (isDuplicate(report)) return

  // Yanıt beklenmez, hata yutulur, TEKRAR DENENMEZ. `keepalive` sayfa kapanırken de gitmesini
  // sağlar — boot çökmesi ve navigasyon sırasındaki hatalar bugün tamamen kayboluyor.
  fetch(ENDPOINT, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(report),
    keepalive: true,
  })
    .catch(() => {
      // BİLEREK BOŞ. Buraya bir console.error ya da yeniden gönderim koymak döngü üretir.
    })
}

function buildReport(level: LogLevel, args: unknown[]): ClientErrorReport {
  const parts = args.map(describe)
  const error = args.find((a): a is Error => a instanceof Error)

  return {
    level,
    message: parts.join(' '),
    stack: error?.stack,
    path: typeof window !== 'undefined' ? window.location?.pathname : undefined,
    appVersion: import.meta.env.VITE_APP_VERSION as string | undefined,
  }
}

export const logger = {
  info(...args: unknown[]): void {
    console.info(...args)
  },

  warn(...args: unknown[]): void {
    console.warn(...args)
  },

  /** Konsola yazar VE sunucuya gönderir. */
  error(...args: unknown[]): void {
    console.error(...args)
    send(buildReport('error', args))
  },
}

/**
 * Yakalanmamış hataları toplar — bugün tamamen sessiz kaybolan sınıf.
 *
 * `window.onerror` render/boot çökmelerini, `unhandledrejection` beklenmeyen promise
 * reddini yakalar. İkisi de sunucuya hiç istek üretmediği için bugün hiçbir iz bırakmıyor.
 */
export function installGlobalErrorHandlers(): void {
  if (typeof window === 'undefined') return

  window.addEventListener('error', (event) => {
    send({
      level: 'error',
      message: `[window.onerror] ${event.message}`,
      stack: event.error instanceof Error ? event.error.stack : undefined,
      path: window.location?.pathname,
      appVersion: import.meta.env.VITE_APP_VERSION as string | undefined,
    })
  })

  window.addEventListener('unhandledrejection', (event) => {
    send({
      level: 'error',
      message: `[unhandledrejection] ${describe(event.reason)}`,
      stack: event.reason instanceof Error ? event.reason.stack : undefined,
      path: window.location?.pathname,
      appVersion: import.meta.env.VITE_APP_VERSION as string | undefined,
    })
  })
}

/** Yalnız test içindir — modül düzeyi durumu sıfırlar. */
export function __resetLoggerStateForTests(): void {
  recentSignatures.clear()
}
