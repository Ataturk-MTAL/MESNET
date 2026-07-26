import { describe, it, expect } from 'vitest'
import { ref } from 'vue'
import { useAssignedHours } from './useAssignedHours'
import type { useNotify } from './useNotify'
import type { BusinessAssignmentDto, BranchWorkloadConfigDto } from 'src/api/coordination'

/** Testlerde bildirim tarafı ilgisiz — computed davranışı ölçülüyor. */
const notifyStub = {
  success: () => {},
  warning: () => {},
  error: () => {},
  info: () => {},
  apiError: () => {},
} as unknown as ReturnType<typeof useNotify>

function makeAssignment(id: string, maxHours: number): BusinessAssignmentDto {
  return { businessId: id, maxCoordinationHours: maxHours, assignedHours: 0 } as BusinessAssignmentDto
}

function makeConfig(pool: number): BranchWorkloadConfigDto {
  return { totalWorkloadPool: pool } as BranchWorkloadConfigDto
}

function setup(pool: number | null, maxHoursList: number[]) {
  const assignments = ref<BusinessAssignmentDto[]>(
    maxHoursList.map((h, i) => makeAssignment(`b${i}`, h)),
  )
  const workloadConfig = ref<BranchWorkloadConfigDto | null>(pool === null ? null : makeConfig(pool))
  const hours = useAssignedHours({
    assignments,
    workloadConfig,
    notify: notifyStub,
    loadData: () => Promise.resolve(),
  })
  hours.initEditedHours()
  return hours
}

describe('useAssignedHours — havuz tanımsız (#111)', () => {
  it('yapılandırma yokken havuzu tanımsız işaretler', () => {
    const hours = setup(null, [40, 54])

    expect(hours.hoursWorkloadPool.value).toBe(0)
    expect(hours.hoursPoolUndefined.value).toBe(true)
  })

  it('havuz 0 iken aşım/eşik bayrakları yanlış alarm üretmez ama durum sessiz kalmaz', () => {
    const hours = setup(0, [40, 54])

    expect(hours.hoursTotalAssigned.value).toBe(94)
    expect(hours.hoursOverLimit.value).toBe(false)
    expect(hours.hoursNearLimit.value).toBe(false)
    expect(hours.hoursPoolUndefined.value).toBe(true)
    expect(hours.hoursTotalAssignedClass.value).toBe('text-warning-strong')
  })

  it('havuz 0 iken "Kalan: -94" yerine tire gösterir', () => {
    const hours = setup(0, [40, 54])

    expect(hours.hoursRemaining.value).toBe(-94)
    expect(hours.hoursRemainingLabel.value).toBe('—')
    expect(hours.hoursRemainingClass.value).toBe('text-neutral-strong')
  })
})

describe('useAssignedHours — havuz tanımlı (mevcut davranış korunur)', () => {
  it('havuz aşıldığında aşım bayrağı ve olumsuz ton', () => {
    const hours = setup(80, [40, 54])

    expect(hours.hoursPoolUndefined.value).toBe(false)
    expect(hours.hoursOverLimit.value).toBe(true)
    expect(hours.hoursNearLimit.value).toBe(false)
    expect(hours.hoursTotalAssignedClass.value).toBe('text-negative-strong')
    expect(hours.hoursRemainingLabel.value).toBe('-14')
    expect(hours.hoursRemainingClass.value).toBe('text-negative-strong')
  })

  it('havuzun %90ını geçince eşiğe yaklaşma bayrağı', () => {
    const hours = setup(100, [95])

    expect(hours.hoursOverLimit.value).toBe(false)
    expect(hours.hoursNearLimit.value).toBe(true)
    expect(hours.hoursRemainingLabel.value).toBe('5')
    expect(hours.hoursRemainingClass.value).toBe('text-warning-strong')
  })

  it('sınır içinde kalınca uyarı üretmez', () => {
    const hours = setup(200, [40, 54])

    expect(hours.hoursOverLimit.value).toBe(false)
    expect(hours.hoursNearLimit.value).toBe(false)
    expect(hours.hoursTotalAssignedClass.value).toBe('text-info-strong')
    expect(hours.hoursRemainingLabel.value).toBe('106')
  })
})
