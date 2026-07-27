import { describe, it, expect } from 'vitest'
import type { DistanceHourRule } from 'src/api/coordination'
import {
  CATCH_ALL_DISTANCE,
  CATCH_ALL_DISTANCE_LABEL,
  cloneRules,
  describeRule,
  formatDistanceLabel,
  isCatchAllRule,
  sortRules,
  validateCoordinationConfig,
  type CoordinationConfigDraft,
} from './coordinationConfig'

const CATCH_ALL: DistanceHourRule = { maxDistanceKm: CATCH_ALL_DISTANCE, hours: 8 }

function makeDraft(overrides: Partial<CoordinationConfigDraft> = {}): CoordinationConfigDraft {
  return {
    distanceHourRules: [
      { maxDistanceKm: 1, hours: 2 },
      { maxDistanceKm: 3, hours: 4 },
      { maxDistanceKm: 5, hours: 6 },
      { ...CATCH_ALL },
    ],
    isMetropolitan: true,
    maxWeeklyExtraHours: 20,
    ...overrides,
  }
}

describe('isCatchAllRule / formatDistanceLabel', () => {
  it('sınırsız kuralı Number.MAX_VALUE eşitliğiyle tanır', () => {
    expect(isCatchAllRule(CATCH_ALL)).toBe(true)
    expect(isCatchAllRule({ maxDistanceKm: 5, hours: 6 })).toBe(false)
  })

  it('backend double.MaxValue JSON değeri Number.MAX_VALUE ile birebir eşleşir', () => {
    // Arrange — System.Text.Json'un double.MaxValue çıktısı
    const fromBackend = JSON.parse('{"maxDistanceKm":1.7976931348623157e+308,"hours":8}') as DistanceHourRule

    // Act & Assert
    expect(isCatchAllRule(fromBackend)).toBe(true)
  })

  it('sınırsız satırın mesafesini metinle gösterir, sayıyla değil', () => {
    expect(formatDistanceLabel(CATCH_ALL_DISTANCE)).toBe(CATCH_ALL_DISTANCE_LABEL)
    expect(formatDistanceLabel(3)).toBe('3 km')
  })

  it('kuralın anlamını Türkçe açıklar', () => {
    expect(describeRule({ maxDistanceKm: 3, hours: 4 })).toBe('≤ 3 km → 4 saat')
    expect(describeRule(CATCH_ALL)).toBe('Üzeri → 8 saat')
  })
})

describe('cloneRules / sortRules', () => {
  it('kopya bağımsızdır — kaynağı değiştirmez', () => {
    // Arrange
    const original: DistanceHourRule[] = [{ maxDistanceKm: 1, hours: 2 }]

    // Act
    const copy = cloneRules(original)
    copy[0]!.hours = 9

    // Assert
    expect(original[0]!.hours).toBe(2)
  })

  it('mesafeye göre artan sıraya dizer, sınırsız kural en sona düşer', () => {
    const sorted = sortRules([{ ...CATCH_ALL }, { maxDistanceKm: 5, hours: 6 }, { maxDistanceKm: 1, hours: 2 }])

    expect(sorted.map((r) => r.maxDistanceKm)).toEqual([1, 5, CATCH_ALL_DISTANCE])
  })

  it('sıralama girdiyi yerinde değiştirmez', () => {
    const input: DistanceHourRule[] = [{ maxDistanceKm: 5, hours: 6 }, { maxDistanceKm: 1, hours: 2 }]

    sortRules(input)

    expect(input.map((r) => r.maxDistanceKm)).toEqual([5, 1])
  })
})

describe('validateCoordinationConfig — geçerli yapılandırma', () => {
  it('varsayılan mevzuat tablosunu hatasız kabul eder', () => {
    expect(validateCoordinationConfig(makeDraft())).toEqual([])
  })

  it('yalnız sınırsız kuraldan oluşan tek satırlık tabloyu kabul eder', () => {
    const draft = makeDraft({ distanceHourRules: [{ ...CATCH_ALL }] })

    expect(validateCoordinationConfig(draft)).toEqual([])
  })

  it('sınır değerleri (1 ve 40 saat) dahildir', () => {
    const draft = makeDraft({
      distanceHourRules: [{ maxDistanceKm: 1, hours: 1 }, { maxDistanceKm: CATCH_ALL_DISTANCE, hours: 40 }],
      maxWeeklyExtraHours: 1,
    })

    expect(validateCoordinationConfig(draft)).toEqual([])
    expect(validateCoordinationConfig({ ...draft, maxWeeklyExtraHours: 40 })).toEqual([])
  })
})

describe('validateCoordinationConfig — kural listesi', () => {
  it('boş listeyi reddeder', () => {
    const errors = validateCoordinationConfig(makeDraft({ distanceHourRules: [] }))

    expect(errors.some((e) => e.includes('boş bırakılamaz'))).toBe(true)
  })

  it('0 veya negatif mesafeyi reddeder', () => {
    const zero = validateCoordinationConfig(
      makeDraft({ distanceHourRules: [{ maxDistanceKm: 0, hours: 2 }, { ...CATCH_ALL }] }),
    )
    const negative = validateCoordinationConfig(
      makeDraft({ distanceHourRules: [{ maxDistanceKm: -3, hours: 2 }, { ...CATCH_ALL }] }),
    )

    expect(zero.some((e) => e.startsWith('1. satır') && e.includes('0 kilometreden büyük'))).toBe(true)
    expect(negative.some((e) => e.includes('0 kilometreden büyük'))).toBe(true)
  })

  it('saat 1-40 aralığının dışındaysa reddeder', () => {
    const tooLow = validateCoordinationConfig(
      makeDraft({ distanceHourRules: [{ maxDistanceKm: 1, hours: 0 }, { ...CATCH_ALL }] }),
    )
    const tooHigh = validateCoordinationConfig(
      makeDraft({ distanceHourRules: [{ maxDistanceKm: 1, hours: 41 }, { ...CATCH_ALL }] }),
    )

    expect(tooLow.some((e) => e.startsWith('1. satır') && e.includes('saat 1 ile 40'))).toBe(true)
    expect(tooHigh.some((e) => e.includes('saat 1 ile 40'))).toBe(true)
  })

  it('aynı mesafeyi iki kez reddeder ve yalnız bir kez bildirir', () => {
    const errors = validateCoordinationConfig(
      makeDraft({
        distanceHourRules: [
          { maxDistanceKm: 3, hours: 2 },
          { maxDistanceKm: 3, hours: 4 },
          { maxDistanceKm: 3, hours: 6 },
          { ...CATCH_ALL },
        ],
      }),
    )
    const duplicateErrors = errors.filter((e) => e.includes('Aynı mesafe'))

    expect(duplicateErrors).toHaveLength(1)
    expect(duplicateErrors[0]).toContain('3 km')
  })

  it('sınırsız kural yoksa reddeder', () => {
    const errors = validateCoordinationConfig(
      makeDraft({ distanceHourRules: [{ maxDistanceKm: 1, hours: 2 }] }),
    )

    expect(errors.some((e) => e.includes('tam olarak bir kez'))).toBe(true)
  })

  it('birden fazla sınırsız kuralı reddeder', () => {
    const errors = validateCoordinationConfig(
      makeDraft({ distanceHourRules: [{ ...CATCH_ALL }, { ...CATCH_ALL, hours: 6 }] }),
    )

    expect(errors.some((e) => e.includes('tam olarak bir kez'))).toBe(true)
  })

  it('sayı olmayan girdileri (NaN) hata olarak bildirir', () => {
    const errors = validateCoordinationConfig(
      makeDraft({
        distanceHourRules: [{ maxDistanceKm: Number.NaN, hours: Number.NaN }, { ...CATCH_ALL }],
      }),
    )

    expect(errors.some((e) => e.includes('0 kilometreden büyük'))).toBe(true)
    expect(errors.some((e) => e.includes('saat 1 ile 40'))).toBe(true)
  })
})

describe('validateCoordinationConfig — azami haftalık ek ders saati', () => {
  it('1-40 aralığının dışını reddeder', () => {
    expect(
      validateCoordinationConfig(makeDraft({ maxWeeklyExtraHours: 0 })).some((e) =>
        e.includes('Azami haftalık ek ders saati'),
      ),
    ).toBe(true)
    expect(
      validateCoordinationConfig(makeDraft({ maxWeeklyExtraHours: 41 })).some((e) =>
        e.includes('Azami haftalık ek ders saati'),
      ),
    ).toBe(true)
  })

  it('sayı olmayan değeri reddeder', () => {
    const errors = validateCoordinationConfig(makeDraft({ maxWeeklyExtraHours: Number.NaN }))

    expect(errors.some((e) => e.includes('Azami haftalık ek ders saati'))).toBe(true)
  })
})
