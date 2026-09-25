import { describe, it, expect, vi, beforeEach } from 'vitest'
import { AxiosError, AxiosHeaders } from 'axios'

const { loggerErrorMock } = vi.hoisted(() => ({ loggerErrorMock: vi.fn() }))

vi.mock('quasar', () => ({ Notify: { create: vi.fn() } }))
vi.mock('../utils/logger', async (importOriginal) => {
  const actual = await importOriginal<typeof import('../utils/logger')>()
  return { ...actual, logger: { ...actual.logger, error: loggerErrorMock } }
})

import { summarizeApiError, useNotify } from './useNotify'

function apiFailure(status: number, data: unknown): AxiosError {
  const config = {
    method: 'post',
    url: '/students?search=Ay%C5%9Fe',
    headers: new AxiosHeaders({ Authorization: 'Bearer eyJhbGciOiJIUzI1NiJ9.x.y' }),
    data: '{"tcKimlikNo":"12345678901"}',
  }
  return new AxiosError('Request failed', 'ERR_BAD_REQUEST', config, null, {
    status,
    statusText: '',
    headers: {},
    config,
    data,
  })
}

describe('summarizeApiError', () => {
  it('durum, yöntem, sorgusuz yol, iş kuralı kodu ve sunucu mesajını metne döker', () => {
    // Arrange
    const err = apiFailure(422, {
      message: 'Dönem kapalı',
      errors: { code: 'Coordination.AcademicPeriodClosed' },
    })

    // Act
    const summary = summarizeApiError(err)

    // Assert
    expect(summary).toBe(
      '422 POST /students | kod: Coordination.AcademicPeriodClosed | sunucu: Dönem kapalı',
    )
  })

  it('token, istek gövdesi ve sorgu dizesindeki kişisel veriyi içermez', () => {
    const summary = summarizeApiError(apiFailure(500, { message: 'Beklenmeyen hata' }))

    expect(summary).not.toContain('eyJ')
    expect(summary).not.toContain('12345678901')
    expect(summary).not.toContain('search')
  })

  it('HTTP hatası olmayan değerde nesneyi serileştirmez', () => {
    expect(summarizeApiError(new TypeError('x is undefined'))).toBe('TypeError')
    expect(summarizeApiError({ token: 'eyJ...' })).toBe('bilinmeyen hata')
  })

  it('uzun sunucu mesajını kırpar', () => {
    const summary = summarizeApiError(apiFailure(400, { message: 'a'.repeat(1000) }))

    expect(summary.length).toBeLessThan(400)
  })
})

describe('useNotify.apiError', () => {
  beforeEach(() => loggerErrorMock.mockReset())

  it('logger.error\'a ham nesne değil yalnız metin verir — sunucuya "[object Object]" gitmez', () => {
    // Arrange
    const { apiError } = useNotify()

    // Act
    apiError(apiFailure(500, { message: 'Beklenmeyen hata' }), 'İşlem başarısız.')

    // Assert
    expect(loggerErrorMock).toHaveBeenCalledOnce()
    const args = loggerErrorMock.mock.calls[0]!
    expect(args.every((a: unknown) => typeof a === 'string')).toBe(true)
    expect(args.join(' ')).toContain('500 POST /students')
  })
})
