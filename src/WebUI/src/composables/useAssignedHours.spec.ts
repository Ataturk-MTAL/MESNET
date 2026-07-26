import { describe, it, expect, vi, beforeEach } from 'vitest'
import { ref } from 'vue'
import { useAssignedHours } from './useAssignedHours'
import type { useNotify } from './useNotify'
import type { BusinessAssignmentDto, BranchWorkloadConfigDto } from 'src/api/coordination'
import { coordinationApi } from 'src/api/coordination'

vi.mock('src/api/coordination', () => ({
  coordinationApi: { updateAssignedHours: vi.fn(() => Promise.resolve({ data: null })) },
}))

const updateAssignedHoursMock = vi.mocked(coordinationApi.updateAssignedHours)

/** Testlerde bildirim tarafı ilgisiz — computed davranışı ölçülüyor. */
const notifyStub = {
  success: () => {},
  warning: () => {},
  error: () => {},
  info: () => {},
  apiError: () => {},
} as unknown as ReturnType<typeof useNotify>

function makeAssignment(
  id: string,
  maxHours: number,
  overrides: Partial<BusinessAssignmentDto> = {},
): BusinessAssignmentDto {
  return {
    businessId: id,
    businessName: `İşletme ${id}`,
    maxCoordinationHours: maxHours,
    assignedHours: 0,
    isHonoraryVisit: false,
    branchCode: 'EET',
    ...overrides,
  } as BusinessAssignmentDto
}

function makeConfig(pool: number): BranchWorkloadConfigDto {
  return { totalWorkloadPool: pool } as BranchWorkloadConfigDto
}

function setupWith(pool: number | null, rows: BusinessAssignmentDto[]) {
  const assignments = ref<BusinessAssignmentDto[]>(rows)
  const workloadConfig = ref<BranchWorkloadConfigDto | null>(pool === null ? null : makeConfig(pool))
  const hours = useAssignedHours({
    assignments,
    workloadConfig,
    academicPeriodId: ref<string | null>('period-1'),
    notify: notifyStub,
    loadData: () => Promise.resolve(),
  })
  hours.initEditedHours()
  return hours
}

function setup(pool: number | null, maxHoursList: number[]) {
  return setupWith(pool, maxHoursList.map((h, i) => makeAssignment(`b${i}`, h)))
}

beforeEach(() => {
  updateAssignedHoursMock.mockClear()
})

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

describe('useAssignedHours — fahri ziyaret (#115)', () => {
  it('kayıtlı fahri satır 0 saatle yüklenir, tavana düşmez', () => {
    const hours = setupWith(100, [
      makeAssignment('b0', 8, { isHonoraryVisit: true }),
      makeAssignment('b1', 6, { assignedHours: 4 }),
    ])

    expect(hours.editedHours.value.b0).toBe(0)
    expect(hours.editedHonorary.value.b0).toBe(true)
    expect(hours.honoraryCount.value).toBe(1)
    // Σ Takdir yalnız ücretli satırı sayar — fahri 8 saat tüketiyormuş gibi görünmez
    expect(hours.hoursTotalAssigned.value).toBe(4)
  })

  it('fahri işaretlemek saati 0 yapar ve havuz toplamından düşer', () => {
    const hours = setupWith(100, [makeAssignment('b0', 8), makeAssignment('b1', 6)])
    expect(hours.hoursTotalAssigned.value).toBe(14) // 8 + 6 (tavan önerisi)

    hours.setHonorary('b0', true)

    expect(hours.editedHours.value.b0).toBe(0)
    expect(hours.hoursTotalAssigned.value).toBe(6)
    expect(hours.honoraryCount.value).toBe(1)
  })

  it('fahri işareti kaldırılınca tavan yeniden önerilir', () => {
    const hours = setupWith(100, [makeAssignment('b0', 8, { isHonoraryVisit: true })])

    hours.setHonorary('b0', false)

    expect(hours.editedHours.value.b0).toBe(8)
    expect(hours.honoraryCount.value).toBe(0)
    expect(hours.hoursTotalAssigned.value).toBe(8)
  })

  it('yalnız fahri işareti değişse bile satır "değişti" sayılır', () => {
    const hours = setupWith(100, [makeAssignment('b0', 8, { assignedHours: 8 })])
    expect(hours.changedHoursCount.value).toBe(0)

    hours.setHonorary('b0', true)

    expect(hours.changedHoursCount.value).toBe(1)
  })

  it('kaydederken fahri satır 0 saat + fahri bayrağı ile gönderilir', async () => {
    const hours = setupWith(100, [makeAssignment('b0', 8)])
    hours.setHonorary('b0', true)

    await hours.saveHours()

    expect(updateAssignedHoursMock).toHaveBeenCalledTimes(1)
    expect(updateAssignedHoursMock).toHaveBeenCalledWith(
      'b0',
      { assignedHours: 0, isHonoraryVisit: true },
      { branchCode: 'EET', academicPeriodId: 'period-1' },
    )
  })

  it('kaydederken ücretli satır girilen saat + fahri=false ile gönderilir', async () => {
    const hours = setupWith(100, [makeAssignment('b0', 8)])
    hours.editedHours.value.b0 = 5

    await hours.saveHours()

    expect(updateAssignedHoursMock).toHaveBeenCalledWith(
      'b0',
      { assignedHours: 5, isHonoraryVisit: false },
      { branchCode: 'EET', academicPeriodId: 'period-1' },
    )
  })

  it('değişmemiş fahri satır tekrar kaydedilmez', async () => {
    const hours = setupWith(100, [makeAssignment('b0', 8, { isHonoraryVisit: true })])

    await hours.saveHours()

    expect(updateAssignedHoursMock).not.toHaveBeenCalled()
  })
})
