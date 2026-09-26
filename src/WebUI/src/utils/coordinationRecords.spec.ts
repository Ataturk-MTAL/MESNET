import { describe, it, expect } from 'vitest'
import {
  percentScore,
  criteriaProblem,
  daysInMonth,
  activitiesProblem,
  studentsAtBusiness,
} from './coordinationRecords'

describe('percentScore', () => {
  it('kriter puanlarını 100 üzerinden yüzdeye çevirir', () => {
    expect(percentScore([
      { name: 'Hazırlık', maxScore: 20, score: 15 },
      { name: 'Uygulama', maxScore: 60, score: 45 },
      { name: 'Güvenlik', maxScore: 20, score: 20 },
    ])).toBe(80)
  })

  it('en yüksek puanı 100 olmayan kriter setini de ölçekler', () => {
    expect(percentScore([{ name: 'A', maxScore: 40, score: 30 }])).toBe(75)
  })

  it('kriter yoksa 0 döner', () => {
    expect(percentScore([])).toBe(0)
  })
})

describe('criteriaProblem', () => {
  it('geçerli kriter setinde sorun yok', () => {
    expect(criteriaProblem([{ name: 'A', maxScore: 10, score: 5 }])).toBeNull()
  })

  it('en az bir kriter ister (backend doğrulayıcısıyla aynı kural)', () => {
    expect(criteriaProblem([])).toMatch(/en az bir/i)
  })

  it('puanı en yüksek puanı aşan kriteri yakalar', () => {
    expect(criteriaProblem([{ name: 'A', maxScore: 10, score: 11 }])).toMatch(/A/)
  })

  it('boşaltılmış puan alanını yakalar (v-model.number boş dize bırakır)', () => {
    const cleared = { name: 'A', maxScore: 10, score: '' as unknown as number }
    expect(criteriaProblem([cleared])).toMatch(/sayı/)
  })

  it('adsız kriteri yakalar', () => {
    expect(criteriaProblem([{ name: '  ', maxScore: 10, score: 5 }])).toMatch(/ad/i)
  })
})

describe('daysInMonth', () => {
  it('şubatı artık yıla göre hesaplar', () => {
    expect(daysInMonth(2028, 2)).toBe(29)
    expect(daysInMonth(2027, 2)).toBe(28)
    expect(daysInMonth(2026, 9)).toBe(30)
  })
})

describe('activitiesProblem', () => {
  it('geçerli günlerde sorun yok', () => {
    expect(activitiesProblem([{ dayNumber: 1, description: 'Montaj' }], 2026, 9)).toBeNull()
  })

  it('en az bir gün ister', () => {
    expect(activitiesProblem([], 2026, 9)).toMatch(/en az bir/i)
  })

  it('ayda olmayan günü yakalar', () => {
    expect(activitiesProblem([{ dayNumber: 31, description: 'x' }], 2026, 9)).toMatch(/31/)
  })

  it('boşaltılmış gün numarasını yakalar', () => {
    const cleared = { dayNumber: '' as unknown as number, description: 'x' }
    expect(activitiesProblem([cleared], 2026, 9)).toMatch(/gün numarası/)
  })

  it('aynı günün iki kez girilmesini yakalar', () => {
    expect(activitiesProblem([
      { dayNumber: 3, description: 'a' },
      { dayNumber: 3, description: 'b' },
    ], 2026, 9)).toMatch(/3/)
  })

  it('açıklaması boş günü yakalar', () => {
    expect(activitiesProblem([{ dayNumber: 2, description: ' ' }], 2026, 9)).toMatch(/2/)
  })
})

describe('studentsAtBusiness', () => {
  it('yalnız o işletmedeki öğrencileri, adıyla döner', () => {
    const placements = [
      { value: 's1', label: 'Ali', businessId: 'b1' },
      { value: 's2', label: 'Veli', businessId: 'b2' },
      { value: 's3', label: 'Ayşe', businessId: 'b1' },
    ]
    expect(studentsAtBusiness(placements, 'b1')).toEqual([
      { studentId: 's3', name: 'Ayşe' },
      { studentId: 's1', name: 'Ali' },
    ].sort((a, b) => a.name.localeCompare(b.name, 'tr')))
  })

  it('işletme seçilmemişse boş döner', () => {
    expect(studentsAtBusiness([{ value: 's1', label: 'Ali', businessId: 'b1' }], null)).toEqual([])
  })
})
