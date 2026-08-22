import { describe, it, expect } from 'vitest'
import { filterBusinessesByBranch, type BusinessOption } from './useEntityOptions'

function option(label: string, authorizedBranches: string[]): BusinessOption {
  return { label, value: label, authorizedBranches }
}

describe('filterBusinessesByBranch', () => {
  it('öğrencinin alanına yetkisiz işletmeleri listeden çıkarır', () => {
    // Arrange
    const businesses = [
      option('Mezitli Elektrik Sanayi', ['EET']),
      option('Datamarin Yazılım', ['BT']),
      option('Akdeniz Otomasyon', ['EET', 'MTT']),
    ]

    // Act
    const filtered = filterBusinessesByBranch(businesses, 'EET')

    // Assert
    expect(filtered.map((b) => b.label)).toEqual([
      'Mezitli Elektrik Sanayi',
      'Akdeniz Otomasyon',
    ])
  })

  it('yetki listesi boş olan işletmeyi hiçbir alanda göstermez', () => {
    // Arrange
    const businesses = [option('Yetkisiz İşletme', [])]

    // Act & Assert
    expect(filterBusinessesByBranch(businesses, 'EET')).toEqual([])
  })

  it('alan kodu verilmezse listeyi olduğu gibi döndürür', () => {
    // Arrange
    const businesses = [option('Yetkisiz İşletme', []), option('Datamarin Yazılım', ['BT'])]

    // Act & Assert
    expect(filterBusinessesByBranch(businesses, null)).toHaveLength(2)
    expect(filterBusinessesByBranch(businesses, '')).toHaveLength(2)
    expect(filterBusinessesByBranch(businesses, '   ')).toHaveLength(2)
  })

  it('alan kodunu boşluk ve büyük/küçük harf farkına rağmen eşleştirir', () => {
    // Arrange
    const businesses = [option('Datamarin Yazılım', ['BT'])]

    // Act & Assert
    expect(filterBusinessesByBranch(businesses, ' bt ')).toHaveLength(1)
  })
})
