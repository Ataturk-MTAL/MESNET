import { ref, computed, type Ref } from 'vue'
import { date as qdate } from 'quasar'
import {
  coordinationApi,
  type WeeklyVisitPlanDto,
  type WeeklyVisitAssignmentDto,
  type GenerateWeeklyVisitsRequest,
  type AddWeeklyVisitAssignmentRequest,
} from 'src/api/coordination'
import { useNotify } from './useNotify'
import { useServerPagination } from './useServerPagination'

export interface UseWeeklyVisitsOptions {
  academicPeriodId: Ref<string | null>
}

/** QDate range model tipi */
export interface QDateRange {
  from: string   // 'YYYY/MM/DD'
  to: string     // 'YYYY/MM/DD'
}

/**
 * ISO hafta numarası hesaplar (ISO 8601).
 */
export function getISOWeek(d: Date): { year: number; week: number } {
  const dt = new Date(Date.UTC(d.getFullYear(), d.getMonth(), d.getDate()))
  dt.setUTCDate(dt.getUTCDate() + 4 - (dt.getUTCDay() || 7))
  const yearStart = new Date(Date.UTC(dt.getUTCFullYear(), 0, 1))
  const week = Math.ceil(((dt.getTime() - yearStart.getTime()) / 86400000 + 1) / 7)
  return { year: dt.getUTCFullYear(), week }
}

/**
 * ISO hafta numarasından Pazartesi ve Pazar tarihlerini hesaplar.
 */
export function getWeekRange(year: number, week: number): { start: Date; end: Date } {
  const jan4 = new Date(Date.UTC(year, 0, 4))
  const dayOfWeek = jan4.getUTCDay() || 7
  const monday = new Date(jan4)
  monday.setUTCDate(jan4.getUTCDate() - dayOfWeek + 1 + (week - 1) * 7)
  const sunday = new Date(monday)
  sunday.setUTCDate(monday.getUTCDate() + 6)
  return { start: monday, end: sunday }
}

/** Quasar QDate model formatı: 'YYYY/MM/DD' */
function toQDate(d: Date): string {
  return qdate.formatDate(d, 'YYYY/MM/DD')
}

function fromQDate(s: string): Date {
  const [y, m, d] = s.split('/').map(Number)
  return new Date(y, m - 1, d)
}

/** Bir tarihin Pazartesi-Pazar aralığını QDate range olarak döndürür */
function weekRangeModel(d: Date): QDateRange {
  const iso = getISOWeek(d)
  const { start, end } = getWeekRange(iso.year, iso.week)
  return { from: toQDate(start), to: toQDate(end) }
}

const DAY_LABELS: Record<string, string> = {
  Monday: 'Pazartesi',
  Tuesday: 'Salı',
  Wednesday: 'Çarşamba',
  Thursday: 'Perşembe',
  Friday: 'Cuma',
}

export function dayLabel(day: string): string {
  return DAY_LABELS[day] ?? day
}

const SCOPE_LABELS: Record<string, string> = {
  All: 'Tümü',
  Teacher: 'Öğretmen',
  Branch: 'Alan',
}

export function scopeLabel(scope: string): string {
  return SCOPE_LABELS[scope] ?? scope
}

export function useWeeklyVisits(options: UseWeeklyVisitsOptions) {
  const { academicPeriodId } = options
  const notify = useNotify()

  // ── Hafta seçimi (QDate range ile) ──
  // QDate range model: { from: 'YYYY/MM/DD', to: 'YYYY/MM/DD' }
  const dateRangeModel = ref<QDateRange>(weekRangeModel(new Date()))

  const isoWeek = computed(() => {
    const d = fromQDate(dateRangeModel.value.from)
    return getISOWeek(d)
  })

  const selectedYear = computed(() => isoWeek.value.year)
  const selectedWeek = computed(() => isoWeek.value.week)

  const weekRange = computed(() => getWeekRange(selectedYear.value, selectedWeek.value))
  const weekLabel = computed(() => {
    const { start } = weekRange.value
    const friday = new Date(start)
    friday.setUTCDate(start.getUTCDate() + 4)
    const fmtStart = qdate.formatDate(start, 'DD MMM YYYY')
    const fmtEnd = qdate.formatDate(friday, 'DD MMM YYYY')
    return `${selectedWeek.value}. hafta — ${fmtStart} / ${fmtEnd}`
  })

  /**
   * QDate'ten herhangi bir gün seçildiğinde tüm haftayı (Pzt-Paz) seç.
   * QDate range modunda model string veya {from,to} olabilir.
   */
  function onDateSelect(value: string | QDateRange | null) {
    if (!value) return
    // Tek gün seçildiyse string gelir, range ise obje
    const dateStr = typeof value === 'string' ? value : value.from
    const d = fromQDate(dateStr)
    dateRangeModel.value = weekRangeModel(d)
  }

  // ── Kapsam seçimi ──
  const scope = ref<string>('All')
  const scopeTeacherId = ref<string | null>(null)
  const scopeBranchCode = ref<string | null>(null)

  // ── Plan listesi ──
  const planFilters = computed(() => ({
    academicPeriodId: academicPeriodId.value ?? undefined,
    year: selectedYear.value,
    weekNumber: selectedWeek.value,
  }))

  const {
    rows: plans,
    loading: plansLoading,
    pagination: plansPagination,
    load: loadPlans,
    onRequest: onPlansRequest,
  } = useServerPagination<WeeklyVisitPlanDto>({
    fetchFn: (params) => coordinationApi.listWeeklyVisitPlans(params),
    filters: planFilters,
    defaultSortBy: 'weekNumber',
    defaultDescending: true,
  })

  // ── Atama detayları ──
  const selectedPlanId = ref<string | null>(null)
  const detailDialogOpen = ref(false)

  const {
    rows: assignments,
    loading: assignmentsLoading,
    pagination: assignmentsPagination,
    search: assignmentSearch,
    load: loadAssignments,
    onRequest: onAssignmentsRequest,
    onSearch: onAssignmentSearch,
  } = useServerPagination<WeeklyVisitAssignmentDto>({
    fetchFn: (params) =>
      coordinationApi.listWeeklyVisitAssignments(selectedPlanId.value!, params),
    defaultSortBy: 'visitDate',
    defaultPageSize: 50,
  })

  function openDetail(planId: string) {
    selectedPlanId.value = planId
    detailDialogOpen.value = true
    loadAssignments().catch(() => {})
  }

  // ── Ziyaret oluşturma ──
  const generating = ref(false)

  async function generate() {
    if (!academicPeriodId.value) {
      notify.warning('Lütfen akademik dönem seçin.')
      return
    }

    generating.value = true
    try {
      const req: GenerateWeeklyVisitsRequest = {
        academicPeriodId: academicPeriodId.value,
        year: selectedYear.value,
        weekNumber: selectedWeek.value,
        scope: scope.value,
        ...(scope.value === 'Teacher' && scopeTeacherId.value
          ? { teacherId: scopeTeacherId.value }
          : {}),
        ...(scope.value === 'Branch' && scopeBranchCode.value
          ? { branchCode: scopeBranchCode.value }
          : {}),
      }
      await coordinationApi.generateWeeklyVisits(req)
      notify.success('Haftalık ziyaret planı oluşturuldu.')
      loadPlans().catch(() => {})
    } catch (err) {
      notify.apiError(err, 'Ziyaret planı oluşturulamadı.')
    } finally {
      generating.value = false
    }
  }

  // ── Plan silme ──
  const deleting = ref(false)

  async function deletePlan(planId: string) {
    deleting.value = true
    try {
      await coordinationApi.deleteWeeklyVisitPlan(planId)
      notify.success('Haftalık ziyaret planı silindi.')
      loadPlans().catch(() => {})
    } catch (err) {
      notify.apiError(err, 'Ziyaret planı silinemedi.')
    } finally {
      deleting.value = false
    }
  }

  // ── Tekil atama silme ──
  const deletingAssignment = ref(false)

  async function deleteAssignment(assignmentId: string) {
    if (!selectedPlanId.value) return
    deletingAssignment.value = true
    try {
      await coordinationApi.deleteWeeklyVisitAssignment(selectedPlanId.value, assignmentId)
      notify.success('Ziyaret ataması silindi.')
      loadAssignments().catch(() => {})
      loadPlans().catch(() => {})
    } catch (err) {
      notify.apiError(err, 'Ziyaret ataması silinemedi.')
    } finally {
      deletingAssignment.value = false
    }
  }

  // ── Tekil atama ekleme ──
  const addingAssignment = ref(false)
  const addDialogOpen = ref(false)

  async function addAssignment(data: AddWeeklyVisitAssignmentRequest) {
    if (!selectedPlanId.value) return
    addingAssignment.value = true
    try {
      await coordinationApi.addWeeklyVisitAssignment(selectedPlanId.value, data)
      notify.success('Ziyaret ataması eklendi.')
      addDialogOpen.value = false
      loadAssignments().catch(() => {})
      loadPlans().catch(() => {})
    } catch (err) {
      notify.apiError(err, 'Ziyaret ataması eklenemedi.')
    } finally {
      addingAssignment.value = false
    }
  }

  return {
    // Hafta
    dateRangeModel,
    onDateSelect,
    selectedYear,
    selectedWeek,
    weekRange,
    weekLabel,
    // Kapsam
    scope,
    scopeTeacherId,
    scopeBranchCode,
    // Planlar
    plans,
    plansLoading,
    plansPagination,
    loadPlans,
    onPlansRequest,
    // Atamalar
    selectedPlanId,
    detailDialogOpen,
    assignments,
    assignmentsLoading,
    assignmentsPagination,
    assignmentSearch,
    loadAssignments,
    onAssignmentsRequest,
    onAssignmentSearch,
    openDetail,
    // Aksiyonlar
    generating,
    generate,
    deleting,
    deletePlan,
    // Tekil atama CRUD
    deletingAssignment,
    deleteAssignment,
    addingAssignment,
    addDialogOpen,
    addAssignment,
  }
}
