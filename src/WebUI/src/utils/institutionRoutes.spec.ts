import { describe, it, expect } from 'vitest'
import { institutionEditRoute, institutionReturnRoute } from './institutionRoutes'

/**
 * Bu testin varlık nedeni ÖLÇÜLMÜŞ bir hatadır (30.08.2026, tarayıcı):
 *
 * İl yetkilisi `Kurumlar` ağacından Mersin İl Millî Eğitim Müdürlüğü sayfasını açıp
 * "Düzenle"ye bastığında id'siz `/institution/edit` rotasına gidiliyordu. Rota parametresi
 * olmayınca `resolveEditableInstitutionId` DAVRANILAN kuruma düşer ve form başka bir kurumu
 * (aktif bağlamdaki okulu) açar. Kullanıcı müdürlüğü düzenlediğini sanır, yazma yanlış
 * kuruma gider.
 */
describe('institutionEditRoute', () => {
  it('ağaçtan açılan kurumda id taşıyan düzenleme rotasını üretir', () => {
    // Arrange
    const viewedId = '22df21ed-dd96-4026-bcdd-c351199e3692'
    // Act
    const target = institutionEditRoute(viewedId)
    // Assert
    expect(target).toBe(`/institutions/${viewedId}/edit`)
  })

  it('menüden gelen kendi kurum sayfasında id taşımayan rotayı korur', () => {
    expect(institutionEditRoute(null)).toBe('/institution/edit')
    expect(institutionEditRoute(undefined)).toBe('/institution/edit')
  })

  it('boş string rota parametresi yokmuş sayılır', () => {
    expect(institutionEditRoute('')).toBe('/institution/edit')
  })
})

describe('institutionReturnRoute', () => {
  it('ağaçtan gelen kullanıcı ağaçtaki kuruma döner', () => {
    expect(institutionReturnRoute('okul-id')).toBe('/institutions/okul-id')
  })

  it('kendi kurumunu düzenleyen menü sayfasına döner', () => {
    expect(institutionReturnRoute(null)).toBe('/institution')
  })
})
