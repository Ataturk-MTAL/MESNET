import { describe, it, expect, vi, beforeEach } from 'vitest'
import { ref } from 'vue'
import { useHoursSuggestion } from './useHoursSuggestion'
import type { useNotify } from './useNotify'
import type {
  AllocationBucketName,
  BusinessAssignmentDto,
  HoursSuggestionDiagnosticsDto,
  HoursSuggestionDto,
  HoursSuggestionLineDto,
} from 'src/api/coordination'
import { coordinationApi } from 'src/api/coordination'

vi.mock('src/api/coordination', () => ({
  coordinationApi: { suggestAssignedHours: vi.fn() },
}))

const suggestMock = vi.mocked(coordinationApi.suggestAssignedHours)

const notifyStub = {
  success: () => {},
  warning: () => {},
  error: () => {},
  info: () => {},
  apiError: () => {},
} as unknown as ReturnType<typeof useNotify>

function makeAssignment(id: string, maxHours: number): BusinessAssignmentDto {
  return {
    businessId: id,
    businessName: `İşletme ${id}`,
    maxCoordinationHours: maxHours,
    assignedHours: 0,
    isHonoraryVisit: false,
    branchCode: 'EET',
  } as BusinessAssignmentDto
}

function makeLine(
  businessId: string,
  suggestedHours: number,
  bucket: AllocationBucketName = 'InBranchPaid',
  isPinned = false,
): HoursSuggestionLineDto {
  return {
    businessId,
    businessName: `İşletme ${businessId}`,
    branchCode: 'EET',
    maxHours: 8,
    studentCount: 3,
    weight: 24,
    suggestedHours,
    isPinned,
    isHonoraryVisit: suggestedHours <= 0,
    bucket,
    bucketLabel: bucket,
  }
}

function makeDiagnostics(
  overrides: Partial<HoursSuggestionDiagnosticsDto> = {},
): HoursSuggestionDiagnosticsDto {
  return {
    pool: 40,
    teacherCapacity: 40,
    sumOfMax: 40,
    totalAllocated: 40,
    undistributed: 0,
    honoraryCount: 0,
    outOfBranchHours: 0,
    isPoolUndefined: false,
    ...overrides,
  }
}

function resolveWith(suggestion: HoursSuggestionDto) {
  suggestMock.mockResolvedValueOnce({ data: suggestion } as never)
}

interface SetupOptions {
  poolUndefined?: boolean
  rows?: BusinessAssignmentDto[]
  notify?: ReturnType<typeof useNotify>
}

function setup(options: SetupOptions = {}) {
  const rows = options.rows ?? [makeAssignment('a', 8), makeAssignment('b', 6)]
  const assignments = ref<BusinessAssignmentDto[]>(rows)
  const editedHours = ref<Record<string, number>>({})
  const editedHonorary = ref<Record<string, boolean>>({})

  for (const row of rows) {
    editedHours.value[row.businessId] = row.maxCoordinationHours
    editedHonorary.value[row.businessId] = false
  }

  const suggestion = useHoursSuggestion({
    assignments,
    editedHours,
    editedHonorary,
    branchCode: ref<string | null>('EET'),
    academicPeriodId: ref<string | null>('period-1'),
    semester: ref('Fall'),
    poolUndefined: ref(options.poolUndefined ?? false),
    notify: options.notify ?? notifyStub,
  })

  return { suggestion, editedHours, editedHonorary }
}

beforeEach(() => {
  suggestMock.mockReset()
})

describe('useHoursSuggestion — öneriyi doldurma (#118)', () => {
  it('öneriyi tabloya doldurur ama KAYDETMEZ', async () => {
    // Arrange
    const { suggestion, editedHours } = setup()
    resolveWith({
      lines: [makeLine('a', 5), makeLine('b', 3)],
      diagnostics: makeDiagnostics({ totalAllocated: 8 }),
    })

    // Act
    await suggestion.autoDistribute()

    // Assert — yalnız öneri uç noktası çağrıldı; kaydetme ayrı adım
    expect(suggestMock).toHaveBeenCalledTimes(1)
    expect(editedHours.value.a).toBe(5)
    expect(editedHours.value.b).toBe(3)
    expect(suggestion.diagnostics.value?.totalAllocated).toBe(8)
  })

  it('fahri satırı 0 saat + fahri işaretiyle doldurur', async () => {
    const { suggestion, editedHours, editedHonorary } = setup()
    resolveWith({
      lines: [makeLine('a', 8), makeLine('b', 0, 'Honorary')],
      diagnostics: makeDiagnostics({ totalAllocated: 8, honoraryCount: 1 }),
    })

    await suggestion.autoDistribute()

    expect(editedHours.value.b).toBe(0)
    expect(editedHonorary.value.b).toBe(true)
    expect(suggestion.hasHonoraryFallback.value).toBe(true)
  })

  it('kova adını satır bazında saklar — rozetler buradan basılır', async () => {
    const { suggestion } = setup()
    resolveWith({
      lines: [makeLine('a', 8), makeLine('b', 4, 'OutOfBranchSuggested')],
      diagnostics: makeDiagnostics({ outOfBranchHours: 4 }),
    })

    await suggestion.autoDistribute()

    expect(suggestion.bucketOf('a')).toBe('InBranchPaid')
    expect(suggestion.bucketOf('b')).toBe('OutOfBranchSuggested')
    expect(suggestion.hasOutOfBranchOverflow.value).toBe(true)
  })

  it('istek başarısızsa saatler değişmez', async () => {
    const { suggestion, editedHours } = setup()
    suggestMock.mockRejectedValueOnce(new Error('sunucu hatası'))

    await suggestion.autoDistribute()

    expect(editedHours.value.a).toBe(8)
    expect(suggestion.diagnostics.value).toBeNull()
    expect(suggestion.canUndo.value).toBe(false)
    expect(suggestion.suggesting.value).toBe(false)
  })
})

describe('useHoursSuggestion — satır kilitleme (#118)', () => {
  it('kilitli satır sorguya pinned olarak gider', async () => {
    // Arrange — a satırı 6 saatte kilitli
    const { suggestion, editedHours } = setup()
    editedHours.value.a = 6
    suggestion.togglePin('a')
    resolveWith({
      lines: [makeLine('a', 6, 'InBranchPaid', true), makeLine('b', 4)],
      diagnostics: makeDiagnostics(),
    })

    // Act
    await suggestion.autoDistribute()

    // Assert
    expect(suggestion.isPinned('a')).toBe(true)
    expect(suggestion.pinnedCount.value).toBe(1)
    expect(suggestMock).toHaveBeenCalledWith({
      branchCode: 'EET',
      academicPeriodId: 'period-1',
      semester: 'Fall',
      pinned: 'a:6',
    })
  })

  it('kilitli satırın saati yeniden dağıtımda korunur', async () => {
    const { suggestion, editedHours } = setup()
    editedHours.value.a = 6
    suggestion.togglePin('a')
    // Sunucu kilitli satırı aynen geri verir; kalan havuz b'ye gider
    resolveWith({
      lines: [makeLine('a', 6, 'InBranchPaid', true), makeLine('b', 2)],
      diagnostics: makeDiagnostics({ totalAllocated: 8 }),
    })

    await suggestion.autoDistribute()

    expect(editedHours.value.a).toBe(6)
    expect(editedHours.value.b).toBe(2)
  })

  it('fahri kilit 0 saatle gönderilir', async () => {
    const { suggestion, editedHours, editedHonorary } = setup()
    editedHours.value.a = 0
    editedHonorary.value.a = true
    suggestion.togglePin('a')
    resolveWith({ lines: [], diagnostics: makeDiagnostics() })

    await suggestion.autoDistribute()

    expect(suggestMock.mock.calls[0]?.[0].pinned).toBe('a:0')
  })

  it('kilit kaldırılınca satır sorgudan çıkar', async () => {
    const { suggestion } = setup()
    suggestion.togglePin('a')
    suggestion.togglePin('a')
    resolveWith({ lines: [], diagnostics: makeDiagnostics() })

    await suggestion.autoDistribute()

    expect(suggestion.pinnedCount.value).toBe(0)
    expect(suggestMock.mock.calls[0]?.[0].pinned).toBeUndefined()
  })

  it('kilitler havuzu aşınca açık uyarı bayrağı yükselir', async () => {
    const { suggestion } = setup()
    resolveWith({
      lines: [makeLine('a', 30, 'InBranchPaid', true)],
      diagnostics: makeDiagnostics({ totalAllocated: 50, undistributed: -10 }),
    })

    await suggestion.autoDistribute()

    expect(suggestion.pinnedOverPool.value).toBe(true)
    expect(suggestion.pinnedOverflowHours.value).toBe(10)
  })
})

describe('useHoursSuggestion — geri alma (#118)', () => {
  it('öneri uygulandıktan sonra önceki saatlere döner', async () => {
    // Arrange
    const { suggestion, editedHours, editedHonorary } = setup()
    editedHours.value.a = 7
    editedHours.value.b = 5
    resolveWith({
      lines: [makeLine('a', 2), makeLine('b', 0, 'Honorary')],
      diagnostics: makeDiagnostics({ honoraryCount: 1 }),
    })

    // Act
    await suggestion.autoDistribute()
    expect(suggestion.canUndo.value).toBe(true)
    suggestion.undoSuggestion()

    // Assert — saat, fahri işareti, rozet ve tanılama eski hâline döner
    expect(editedHours.value.a).toBe(7)
    expect(editedHours.value.b).toBe(5)
    expect(editedHonorary.value.b).toBe(false)
    expect(suggestion.bucketOf('b')).toBeNull()
    expect(suggestion.diagnostics.value).toBeNull()
    expect(suggestion.canUndo.value).toBe(false)
  })

  it('öneri uygulanmadan geri alma bir şey değiştirmez', () => {
    const { suggestion, editedHours } = setup()

    suggestion.undoSuggestion()

    expect(editedHours.value.a).toBe(8)
    expect(suggestion.canUndo.value).toBe(false)
  })

  it('alan değişince kilitler ve öneri sıfırlanır', async () => {
    const { suggestion } = setup()
    suggestion.togglePin('a')
    resolveWith({ lines: [makeLine('a', 8)], diagnostics: makeDiagnostics() })
    await suggestion.autoDistribute()

    suggestion.resetAll()

    expect(suggestion.pinnedCount.value).toBe(0)
    expect(suggestion.diagnostics.value).toBeNull()
    expect(suggestion.canUndo.value).toBe(false)
  })
})

describe('useHoursSuggestion — havuz tanımsız (#111/#118)', () => {
  it('havuz tanımsızken buton devre dışı kalır', () => {
    const { suggestion } = setup({ poolUndefined: true })

    expect(suggestion.canAutoDistribute.value).toBe(false)
  })

  it('havuz tanımsızken istek hiç atılmaz ve uyarı gösterilir', async () => {
    const warningSpy = vi.fn()
    const notify = { ...notifyStub, warning: warningSpy } as unknown as ReturnType<typeof useNotify>
    const { suggestion, editedHours } = setup({ poolUndefined: true, notify })

    await suggestion.autoDistribute()

    expect(suggestMock).not.toHaveBeenCalled()
    expect(warningSpy).toHaveBeenCalledTimes(1)
    expect(editedHours.value.a).toBe(8)
  })

  it('işletme yokken dağıtılacak bir şey olmadığı için buton devre dışı', () => {
    const { suggestion } = setup({ rows: [] })

    expect(suggestion.canAutoDistribute.value).toBe(false)
  })

  it('sunucu havuzu tanımsız bildirirse saatler değişmez ama tanılama gösterilir', async () => {
    const { suggestion, editedHours } = setup()
    resolveWith({
      lines: [],
      diagnostics: makeDiagnostics({ pool: 0, totalAllocated: 0, isPoolUndefined: true }),
    })

    await suggestion.autoDistribute()

    expect(editedHours.value.a).toBe(8)
    expect(suggestion.diagnostics.value?.isPoolUndefined).toBe(true)
    expect(suggestion.canUndo.value).toBe(false)
  })
})
