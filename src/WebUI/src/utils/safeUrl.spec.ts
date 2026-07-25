import { describe, it, expect } from 'vitest'
import { toSafeUrl, isSafeUrl } from './safeUrl'

describe('toSafeUrl', () => {
  it('http ve https adreslerini geçirir', () => {
    expect(toSafeUrl('https://okulum.meb.k12.tr')).toBe('https://okulum.meb.k12.tr/')
    expect(toSafeUrl('http://okulum.meb.k12.tr/iletisim')).toBe('http://okulum.meb.k12.tr/iletisim')
  })

  it('şemasız girdiye https ekler', () => {
    expect(toSafeUrl('okulum.meb.k12.tr')).toBe('https://okulum.meb.k12.tr/')
  })

  it('javascript: şemasını reddeder', () => {
    expect(toSafeUrl('javascript:alert(document.cookie)')).toBeNull()
  })

  it('büyük/karışık harfli ve boşluklu javascript: varyantlarını reddeder', () => {
    expect(toSafeUrl('JavaScript:alert(1)')).toBeNull()
    expect(toSafeUrl('  javascript:alert(1)  ')).toBeNull()
  })

  it('data: ve vbscript: şemalarını reddeder', () => {
    expect(toSafeUrl('data:text/html,<script>alert(1)</script>')).toBeNull()
    expect(toSafeUrl('vbscript:msgbox(1)')).toBeNull()
  })

  it('file: şemasını reddeder', () => {
    expect(toSafeUrl('file:///etc/passwd')).toBeNull()
  })

  it('boş ve tanımsız girdide null döner', () => {
    expect(toSafeUrl('')).toBeNull()
    expect(toSafeUrl('   ')).toBeNull()
    expect(toSafeUrl(null)).toBeNull()
    expect(toSafeUrl(undefined)).toBeNull()
  })
})

describe('isSafeUrl', () => {
  it('toSafeUrl sonucunu boole olarak yansıtır', () => {
    expect(isSafeUrl('https://okulum.meb.k12.tr')).toBe(true)
    expect(isSafeUrl('javascript:alert(1)')).toBe(false)
    expect(isSafeUrl(null)).toBe(false)
  })
})
