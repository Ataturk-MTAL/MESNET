import { describe, it, expect } from 'vitest'
import {
  UNDEFINED_POOL_PLACEHOLDER,
  isWorkloadPoolUndefined,
  workloadPoolLabel,
  workloadPoolToneClass,
  assignedHoursToneClass,
  remainingHoursLabel,
  remainingHoursToneClass,
} from './workloadPool'

describe('isWorkloadPoolUndefined', () => {
  it('havuz hesaplanmamışken (0, negatif, null, NaN) tanımsız sayar', () => {
    expect(isWorkloadPoolUndefined(0)).toBe(true)
    expect(isWorkloadPoolUndefined(-5)).toBe(true)
    expect(isWorkloadPoolUndefined(null)).toBe(true)
    expect(isWorkloadPoolUndefined(undefined)).toBe(true)
    expect(isWorkloadPoolUndefined(Number.NaN)).toBe(true)
  })

  it('pozitif havuzu tanımlı sayar', () => {
    expect(isWorkloadPoolUndefined(1)).toBe(false)
    expect(isWorkloadPoolUndefined(120)).toBe(false)
  })
})

describe('workloadPoolLabel / workloadPoolToneClass', () => {
  it('tanımsız havuzu "0" yerine tire ve nötr tonla gösterir', () => {
    expect(workloadPoolLabel(0)).toBe(UNDEFINED_POOL_PLACEHOLDER)
    expect(workloadPoolToneClass(0)).toBe('text-neutral-strong')
  })

  it('tanımlı havuzu sayı ve olumlu tonla gösterir', () => {
    expect(workloadPoolLabel(120)).toBe('120')
    expect(workloadPoolToneClass(120)).toBe('text-positive-strong')
  })
})

describe('assignedHoursToneClass', () => {
  it('havuz tanımsız ve saat girilmişse uyarı tonu verir (sessiz kalmaz) — #111', () => {
    expect(assignedHoursToneClass(0, 94)).toBe('text-warning-strong')
  })

  it('havuz tanımsız ve saat girilmemişse nötr kalır (yanlış alarm yok)', () => {
    expect(assignedHoursToneClass(0, 0)).toBe('text-neutral-strong')
  })

  it('havuz tanımlıyken aşımı olumsuz tonla gösterir', () => {
    expect(assignedHoursToneClass(100, 101)).toBe('text-negative-strong')
  })

  it('havuz tanımlı ve sınır içindeyken mevcut bilgi tonunu korur', () => {
    expect(assignedHoursToneClass(100, 100)).toBe('text-info-strong')
    expect(assignedHoursToneClass(100, 20)).toBe('text-info-strong')
  })
})

describe('remainingHoursLabel', () => {
  it('havuz tanımsızken kalan yerine tire basar (-94 gösterilmez)', () => {
    expect(remainingHoursLabel(0, -94)).toBe(UNDEFINED_POOL_PLACEHOLDER)
  })

  it('havuz tanımlıyken kalanı sayı olarak basar', () => {
    expect(remainingHoursLabel(100, 6)).toBe('6')
    expect(remainingHoursLabel(100, -6)).toBe('-6')
  })
})

describe('remainingHoursToneClass', () => {
  it('havuz tanımsızken nötr kalır', () => {
    expect(remainingHoursToneClass(0, -94)).toBe('text-neutral-strong')
  })

  it('havuz tanımlı ve kalan negatifken olumsuz tonu kullanır', () => {
    expect(remainingHoursToneClass(100, -20)).toBe('text-negative-strong')
  })

  it('havuz tanımlı ve kalan sıfır/pozitifken uyarı tonunu korur', () => {
    expect(remainingHoursToneClass(100, 0)).toBe('text-warning-strong')
    expect(remainingHoursToneClass(100, 40)).toBe('text-warning-strong')
  })
})
