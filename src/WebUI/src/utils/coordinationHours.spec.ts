import { describe, it, expect } from 'vitest'
import {
  HONORARY_VISIT_SLOTS,
  billableHours,
  billableTargetHours,
  isHonorary,
  slotTargetHours,
  type CoordinationHoursShape,
} from './coordinationHours'

const MAX_HOURS = 8

function row(assignedHours: number, isHonoraryVisit: boolean): CoordinationHoursShape {
  return { assignedHours, maxCoordinationHours: MAX_HOURS, isHonoraryVisit }
}

describe('coordinationHours — üç durumun ayrışması (#115)', () => {
  it('takdir edilmemiş satırda hedef saat mesafe tavanına düşer', () => {
    const biz = row(0, false)

    expect(billableHours(biz)).toBe(0)
    expect(billableTargetHours(biz)).toBe(MAX_HOURS)
    expect(slotTargetHours(biz)).toBe(MAX_HOURS)
  })

  it('fahri satır tavana DÜŞMEZ — ücret doğuran saati 0', () => {
    const biz = row(0, true)

    expect(billableHours(biz)).toBe(0)
    expect(billableTargetHours(biz)).toBe(0)
  })

  it('fahri satır ders programında slot işgal eder', () => {
    const biz = row(0, true)

    expect(slotTargetHours(biz)).toBe(HONORARY_VISIT_SLOTS)
    expect(slotTargetHours(biz)).toBeGreaterThan(0)
  })

  it('ücretli satırda takdir edilen saat aynen geçerlidir', () => {
    const biz = row(5, false)

    expect(billableHours(biz)).toBe(5)
    expect(billableTargetHours(biz)).toBe(5)
    expect(slotTargetHours(biz)).toBe(5)
  })
})

describe('coordinationHours — eksik alan', () => {
  it('alanı taşımayan eski yükte fahri sayılmaz', () => {
    // #115 öncesi API yanıtında alan yok
    const legacy = { assignedHours: 0, maxCoordinationHours: MAX_HOURS } as CoordinationHoursShape

    expect(isHonorary(legacy)).toBe(false)
    expect(billableTargetHours(legacy)).toBe(MAX_HOURS)
  })
})
