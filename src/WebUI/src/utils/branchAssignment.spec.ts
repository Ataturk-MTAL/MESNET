import { describe, it, expect } from 'vitest'
import {
  resolveBranchCellState,
  filterMissingBranch,
  type BranchAssignmentState,
} from './branchAssignment'

function user(over: Partial<BranchAssignmentState> = {}): BranchAssignmentState {
  return {
    branchCodes: [],
    branchRequired: false,
    branchMissing: false,
    ...over,
  }
}

describe('resolveBranchCellState — alan hücresi (#126)', () => {
  it('alan beklenip girilmemişse uyarı durumu döner', () => {
    // Arrange — rol değişimiyle alan şefi yapılmış, branşı girilmemiş kullanıcı
    const row = user({ branchRequired: true, branchMissing: true })

    // Act & Assert
    expect(resolveBranchCellState(row)).toBe('missing')
  })

  it('alan girilmişse kod rozetleri gösterilir', () => {
    // Arrange
    const row = user({ branchCodes: ['EET'], branchRequired: true })

    // Act & Assert
    expect(resolveBranchCellState(row)).toBe('assigned')
  })

  it('birden çok alanı olan kullanıcı da rozetli durumdadır', () => {
    // Arrange
    const row = user({ branchCodes: ['EET', 'MTT'], branchRequired: true })

    // Act & Assert
    expect(resolveBranchCellState(row)).toBe('assigned')
  })

  it('alan beklenmeyen kullanıcıda boş liste UYARI DEĞİL, nötr durumdur', () => {
    // Arrange — okul müdürü: hiçbir alana bağlı değil, bu doğru durum
    const row = user({ branchCodes: [], branchRequired: false, branchMissing: false })

    // Act & Assert
    expect(resolveBranchCellState(row)).toBe('none')
  })
})

describe('filterMissingBranch — "yalnız branş atanmamış" filtresi (#126)', () => {
  it('yalnız alan beklenip girilmemiş kullanıcıları listeler', () => {
    // Arrange
    const rows = [
      user({ branchCodes: ['EET'], branchRequired: true }),
      user({ branchRequired: true, branchMissing: true }),
      user({ branchRequired: false }),
    ]

    // Act
    const filtered = filterMissingBranch(rows)

    // Assert
    expect(filtered).toHaveLength(1)
    expect(filtered[0]?.branchMissing).toBe(true)
  })

  it('muafiyetli (alan beklenmeyen) kullanıcıları listeye ALMAZ', () => {
    // Arrange — müdür ve müdür yardımcısı: branşsız ama eksik değil
    const rows = [user({ branchRequired: false }), user({ branchRequired: false })]

    // Act & Assert
    expect(filterMissingBranch(rows)).toEqual([])
  })

  it('hiç eksik yoksa boş liste döner', () => {
    // Arrange
    const rows = [user({ branchCodes: ['EET'], branchRequired: true })]

    // Act & Assert
    expect(filterMissingBranch(rows)).toEqual([])
  })
})
