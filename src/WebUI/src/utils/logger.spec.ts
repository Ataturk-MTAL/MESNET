import { describe as vitestDescribe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import { logger, describe as describeValue, __resetLoggerStateForTests } from './logger'

/**
 * İstemci logger'ı (#144).
 *
 * Buradaki testlerin çoğu "gönderdi mi" değil, **göndermemesi gereken durumları** kilitler:
 * telemetrinin kendi hatası, tekrar eden aynı hata ve ham nesne gövdesi. #136 (sonsuz yeniden
 * giriş döngüsü) tam olarak bu sınıftan bir hataydı; telemetri onu tekrar üretmemeli.
 */
vitestDescribe('logger', () => {
  let fetchMock: ReturnType<typeof vi.fn>

  beforeEach(() => {
    __resetLoggerStateForTests()
    fetchMock = vi.fn().mockResolvedValue(new Response(null, { status: 202 }))
    vi.stubGlobal('fetch', fetchMock)
    vi.spyOn(console, 'error').mockImplementation(() => {})
    vi.spyOn(console, 'warn').mockImplementation(() => {})
    vi.spyOn(console, 'info').mockImplementation(() => {})
  })

  afterEach(() => {
    vi.unstubAllGlobals()
    vi.restoreAllMocks()
  })

  it('error seviyesi hem konsola yazar hem sunucuya gönderir', () => {
    logger.error('patladı')

    expect(console.error).toHaveBeenCalled()
    expect(fetchMock).toHaveBeenCalledOnce()
    expect(fetchMock.mock.calls[0]![0]).toBe('/api/telemetry/client-errors')
  })

  it('info ve warn sunucuya GİTMEZ — yalnız hata görünürlüğü hedefleniyor', () => {
    logger.info('bilgi')
    logger.warn('uyarı')

    expect(fetchMock).not.toHaveBeenCalled()
  })

  /**
   * DÖNGÜ KORUMASI. Gönderim başarısız olursa hata yutulur; tekrar denenmez ve o hata
   * telemetriye yazılmaz. Aksi hâlde hata → gönder → gönderim hatası → hata … döngüsü doğar.
   */
  it('gönderim hatası yutulur ve tekrar denenmez', async () => {
    fetchMock.mockRejectedValue(new Error('ağ yok'))

    expect(() => logger.error('patladı')).not.toThrow()
    await Promise.resolve()

    expect(fetchMock).toHaveBeenCalledOnce()
  })

  it('aynı hata pencere içinde bir kez gönderilir', async () => {
    logger.error('aynı hata')
    await Promise.resolve()
    logger.error('aynı hata')
    await Promise.resolve()

    expect(fetchMock).toHaveBeenCalledOnce()
  })

  it('farklı hata ayrıca gönderilir', async () => {
    logger.error('birinci')
    await Promise.resolve()
    logger.error('ikinci')
    await Promise.resolve()

    expect(fetchMock).toHaveBeenCalledTimes(2)
  })

  it('gövde seviye ve mesaj taşır', () => {
    logger.error('bir şey oldu')

    const body = JSON.parse(fetchMock.mock.calls[0]![1].body as string)
    expect(body.level).toBe('error')
    expect(body.message).toContain('bir şey oldu')
  })

  it('Error nesnesinin yığını gönderilir', () => {
    logger.error(new Error('kaboom'))

    const body = JSON.parse(fetchMock.mock.calls[0]![1].body as string)
    expect(body.message).toContain('kaboom')
    expect(body.stack).toBeTruthy()
  })
})

/**
 * Ham nesne serileştirilmez: `useNotify.ts` bugün ham API hata nesnesini konsola basıyor ve
 * aynısı sunucuya gitseydi içinde token, e-posta ya da öğrenci verisi bulunabilirdi.
 */
vitestDescribe('describe', () => {
  it('metni olduğu gibi bırakır', () => {
    expect(describeValue('düz metin')).toBe('düz metin')
  })

  it('Error için ad ve mesaj verir', () => {
    expect(describeValue(new Error('kaboom'))).toBe('Error: kaboom')
  })

  it('nesneyi SERİLEŞTİRMEZ — içeriği denetlenemez', () => {
    const gizli = { token: 'eyJhbGciOiJIUzI1NiJ9.x.y', email: 'a@b.com' }

    const result = describeValue(gizli)

    expect(result).not.toContain('eyJ')
    expect(result).not.toContain('a@b.com')
  })

  it('null ve undefined bozulmaz', () => {
    expect(describeValue(null)).toBe('null')
    expect(describeValue(undefined)).toBe('undefined')
  })
})
