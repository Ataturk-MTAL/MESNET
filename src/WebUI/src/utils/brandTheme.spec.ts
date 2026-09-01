import { describe, it, expect, beforeEach } from 'vitest'
import { applyBrandTheme, isBrandHex, resetBrandTheme } from './brandTheme'

/** Satır içi stilden okur — getComputedStyle jsdom'da CSS değişkenini çözmez. */
function inlineVar(name: string): string {
  return document.documentElement.style.getPropertyValue(name)
}

describe('brandTheme', () => {
  beforeEach(() => {
    resetBrandTheme()
    document.body.style.removeProperty('--q-primary')
  })

  describe('isBrandHex', () => {
    it('altı haneli hex kabul eder', () => {
      expect(isBrandHex('#1E3A5F')).toBe(true)
      expect(isBrandHex('#b54b5c')).toBe(true)
    })

    it('kısa biçimi, isimli rengi ve hex olmayan değerleri reddeder', () => {
      expect(isBrandHex('#1E3')).toBe(false)
      expect(isBrandHex('rebeccapurple')).toBe(false)
      expect(isBrandHex('rgb(30,58,95)')).toBe(false)
      expect(isBrandHex(null)).toBe(false)
      expect(isBrandHex(undefined)).toBe(false)
      expect(isBrandHex(0x1e3a5f)).toBe(false)
    })
  })

  describe('applyBrandTheme', () => {
    it('geçerli çifti uygular ve true döner', () => {
      // Arrange
      const palette = { primary: '#6B1F2E', secondary: '#B54B5C' }

      // Act
      const applied = applyBrandTheme(palette.primary, palette.secondary)

      // Assert
      expect(applied).toBe(true)
      expect(inlineVar('--q-primary')).toBe('#6B1F2E')
      expect(inlineVar('--q-secondary')).toBe('#B54B5C')
    })

    it('değişkenleri html üzerine yazar, body üzerine DEĞİL', () => {
      // themeColors.ts ve BusinessClusterMap.vue tema rengini documentElement'ten okur;
      // body'ye yazmak grafikleri varsayılan renkte dondururdu.
      applyBrandTheme('#0E4146', '#387980')

      expect(inlineVar('--q-primary')).toBe('#0E4146')
      expect(document.body.style.getPropertyValue('--q-primary')).toBe('')
    })

    it('kiracı değişince önceki rengi ezer', () => {
      applyBrandTheme('#1E3A5F', '#4870A4')
      applyBrandTheme('#2A3072', '#5763C0')

      expect(inlineVar('--q-primary')).toBe('#2A3072')
      expect(inlineVar('--q-secondary')).toBe('#5763C0')
    })

    it('çiftin BİR yarısı bozuksa hiçbirini uygulamaz ve varsayılana döner', () => {
      // Arrange — önce geçerli bir tema uygulanmış olsun
      applyBrandTheme('#6B1F2E', '#B54B5C')

      // Act — secondary bozuk geliyor
      const applied = applyBrandTheme('#4A2352', 'mor')

      // Assert — yarım palet ekrana konmaz, satır içi değişken tümüyle kalkar
      expect(applied).toBe(false)
      expect(inlineVar('--q-primary')).toBe('')
      expect(inlineVar('--q-secondary')).toBe('')
    })

    it('null/undefined değerlerde varsayılana düşer', () => {
      expect(applyBrandTheme(null, undefined)).toBe(false)
      expect(inlineVar('--q-primary')).toBe('')
    })
  })

  describe('resetBrandTheme', () => {
    it('çalışma zamanı temasını kaldırır — varsayılan hex YAZMAZ', () => {
      applyBrandTheme('#1B422C', '#467B5B')

      resetBrandTheme()

      // Boş string = özellik silinmiş; derleme zamanı :root değeri yeniden yürürlükte.
      expect(inlineVar('--q-primary')).toBe('')
      expect(inlineVar('--q-secondary')).toBe('')
    })
  })
})
