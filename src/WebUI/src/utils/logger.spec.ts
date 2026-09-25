import { describe as vitestDescribe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import { AxiosError, AxiosHeaders } from 'axios'
import {
  logger,
  describe as describeValue,
  describeHttpContext,
  stripQuery,
  __resetLoggerStateForTests,
} from './logger'

/** Gövdesinde ve başlığında kişisel veri/token taşıyan bir axios hatası. */
function sensitiveAxiosError(status?: number): AxiosError {
  const config = {
    method: 'get',
    url: '/students?search=Ay%C5%9Fe&tc=12345678901',
    headers: new AxiosHeaders({ Authorization: 'Bearer eyJhbGciOiJIUzI1NiJ9.x.y' }),
    data: '{"email":"a@b.com"}',
  }
  const response =
    status === undefined
      ? undefined
      : { status, statusText: '', headers: {}, config, data: { email: 'a@b.com' } }
  return new AxiosError('Request failed', 'ERR_BAD_RESPONSE', config, null, response)
}

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

  it('HTTP hatasına durum, yöntem ve sorgusuz yolu ekler', () => {
    expect(describeValue(sensitiveAxiosError(500))).toBe(
      'AxiosError: Request failed [500 GET /students]',
    )
  })

  it('HTTP hatasından token, gövde ve sorgu dizesindeki kişisel veriyi SIZDIRMAZ', () => {
    const result = describeValue(sensitiveAxiosError(403))

    expect(result).not.toContain('eyJ')
    expect(result).not.toContain('a@b.com')
    expect(result).not.toContain('12345678901')
    expect(result).not.toContain('search')
  })
})

vitestDescribe('describeHttpContext', () => {
  it('yanıt yoksa (ağ hatası) bunu açıkça söyler', () => {
    expect(describeHttpContext(sensitiveAxiosError())).toBe('yanıt yok GET /students')
  })

  it('HTTP hatası olmayan değer için null döner', () => {
    expect(describeHttpContext(new Error('x'))).toBeNull()
    expect(describeHttpContext({ status: 500 })).toBeNull()
    expect(describeHttpContext(null)).toBeNull()
  })

  it('sorgu dizesini ve parçayı atar', () => {
    expect(stripQuery('/a/b?x=1#c')).toBe('/a/b')
    expect(stripQuery('/a/b#c')).toBe('/a/b')
    expect(stripQuery('/a/b')).toBe('/a/b')
  })
})
