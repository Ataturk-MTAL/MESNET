import { ref, reactive, computed } from 'vue'
import type { EChartsOption } from 'echarts'
import type { useAuthStore } from 'stores/auth'
import { Permissions } from 'utils/permissions'
import { statusTone, NEUTRAL_GREY } from 'utils/themeColors'
import { enrollmentApi } from 'src/api/enrollment'
import { businessApi } from 'src/api/business'
import { contractApi } from 'src/api/contract'
import { attendanceApi } from 'src/api/attendance'
import { securityApi } from 'src/api/security'
import { useInstitutionStore } from 'stores/institution'

export interface UseDashboardStatsOptions {
  authStore: ReturnType<typeof useAuthStore>
  institutionId: string
}

// Status label maps
const STUDENT_STATUS_LABELS: Record<string, string> = {
  Registered: 'Kayıtlı',
  Applied: 'Başvurdu',
  Placed: 'Yerleştirildi',
  ActiveInternship: 'Aktif Staj',
  Completed: 'Tamamladı',
}

// Grafik renkleri tema değişkeninden türer (#104) ve StatusBadge tonlarıyla eşleşir —
// aynı durum listede ve grafikte aynı renkte görünsün. Fonksiyon olarak çağrılıyor,
// çünkü modül yüklenirken CSS henüz uygulanmamış olabilir.
const STUDENT_STATUS_COLORS: Record<string, () => string> = {
  Registered: () => NEUTRAL_GREY,
  Applied: statusTone.pending,
  Placed: statusTone.progress,
  ActiveInternship: statusTone.active,
  Completed: statusTone.success,
}

const CONTRACT_STATUS_LABELS: Record<string, string> = {
  Draft: 'Taslak',
  AwaitingSignature: 'İmza Bekliyor',
  Active: 'Aktif',
  Suspended: 'Askıda',
  Terminated: 'Feshedildi',
  Completed: 'Tamamlandı',
}

const CONTRACT_STATUS_COLORS: Record<string, () => string> = {
  Draft: () => NEUTRAL_GREY,
  AwaitingSignature: statusTone.pending,
  Active: statusTone.active,
  Suspended: statusTone.warning,
  Terminated: statusTone.negative,
  Completed: statusTone.success,
}

export function useDashboardStats(options: UseDashboardStatsOptions) {
  const { authStore, institutionId } = options

  const institutionStore = useInstitutionStore()
  const institutionName = computed(() => institutionStore.institution?.fullName ?? '')

  // Stats
  const stats = reactive({
    students: 0,
    studentsLoading: true,
    businesses: 0,
    businessesLoading: true,
    activeContracts: 0,
    contractsLoading: true,
    pendingTotal: 0,
    pendingLoading: true,
  })

  // Chart data
  const studentChartOption = ref<EChartsOption | null>(null)
  const contractChartOption = ref<EChartsOption | null>(null)

  // Raw data holders for chart generation
  const allStudents = ref<{ status: string }[]>([])
  const allContracts = ref<{ status: string }[]>([])

  // Chart builders
  function buildStudentChart() {
    const grouped: Record<string, number> = {}
    for (const s of allStudents.value) {
      grouped[s.status] = (grouped[s.status] ?? 0) + 1
    }

    const data = Object.entries(grouped).map(([status, count]) => ({
      name: STUDENT_STATUS_LABELS[status] ?? status,
      value: count,
      itemStyle: { color: (STUDENT_STATUS_COLORS[status] ?? (() => NEUTRAL_GREY))() },
    }))

    if (data.length === 0) return

    studentChartOption.value = {
      tooltip: { trigger: 'item', formatter: '{b}: {c} ({d}%)' },
      legend: { bottom: 0, left: 'center' },
      series: [{
        type: 'pie',
        radius: ['45%', '70%'],
        center: ['50%', '45%'],
        avoidLabelOverlap: true,
        label: { show: false },
        emphasis: { label: { show: true, fontWeight: 'bold' } },
        data,
      }],
    }
  }

  function buildContractChart() {
    const grouped: Record<string, number> = {}
    for (const c of allContracts.value) {
      grouped[c.status] = (grouped[c.status] ?? 0) + 1
    }

    const order = ['Draft', 'AwaitingSignature', 'Active', 'Suspended', 'Terminated', 'Completed']
    const categories: string[] = []
    const values: number[] = []
    const colors: string[] = []

    for (const status of order) {
      if (grouped[status]) {
        categories.push(CONTRACT_STATUS_LABELS[status] ?? status)
        values.push(grouped[status])
        colors.push((CONTRACT_STATUS_COLORS[status] ?? (() => NEUTRAL_GREY))())
      }
    }

    if (categories.length === 0) return

    contractChartOption.value = {
      tooltip: { trigger: 'axis', axisPointer: { type: 'shadow' } },
      grid: { left: 100, right: 30, top: 10, bottom: 20 },
      xAxis: { type: 'value', minInterval: 1 },
      yAxis: { type: 'category', data: categories },
      series: [{
        type: 'bar',
        data: values.map((v, i) => ({ value: v, itemStyle: { color: colors[i] } })),
        barMaxWidth: 30,
      }],
    }
  }

  // Data loaders
  async function loadStudents() {
    try {
      const res = await enrollmentApi.listStudents({ pageSize: 100 })
      const data = res.data?.items ?? []
      stats.students = res.data?.totalCount ?? 0
      allStudents.value = data
      buildStudentChart()
    } catch { /* sessiz */ }
    stats.studentsLoading = false
  }

  async function loadBusinesses() {
    try {
      const res = await businessApi.list({ status: 'Approved', pageSize: 1 })
      stats.businesses = res.data?.totalCount ?? 0
    } catch { /* sessiz */ }
    stats.businessesLoading = false
  }

  async function loadContracts() {
    try {
      const res = await contractApi.list({ pageSize: 100 })
      const data = res.data?.items ?? []
      stats.activeContracts = data.filter((c: { status: string }) => c.status === 'Active').length
      allContracts.value = data
      buildContractChart()
    } catch { /* sessiz */ }
    stats.contractsLoading = false
  }

  async function loadPendingActions() {
    let total = 0
    const tasks: Promise<void>[] = []

    if (authStore.hasPermission(Permissions.Internship.Contract)) {
      tasks.push(
        contractApi.list({ status: 'AwaitingSignature', pageSize: 1 })
          .then((res) => { total += res.data?.totalCount ?? 0 })
          .catch(() => {}),
      )
    }

    if (authStore.hasPermission(Permissions.Attendance.View)) {
      tasks.push(
        attendanceApi.list({ status: 'Recorded', pageSize: 1 })
          .then((res) => { total += res.data?.totalCount ?? 0 })
          .catch(() => {}),
      )
    }

    if (authStore.hasPermission(Permissions.Company.View)) {
      tasks.push(
        businessApi.list({ status: 'PendingApproval', pageSize: 1 })
          .then((res) => { total += res.data?.totalCount ?? 0 })
          .catch(() => {}),
      )
    }

    if (authStore.hasPermission(Permissions.UserManagement.View)) {
      tasks.push(
        securityApi.listInvitations({ status: 'Pending', pageSize: 1 })
          .then((res) => { total += res.data?.totalCount ?? 0 })
          .catch(() => {}),
      )
    }

    await Promise.allSettled(tasks)
    stats.pendingTotal = total
    stats.pendingLoading = false
  }

  async function init() {
    const tasks: Promise<void>[] = []

    if (authStore.hasPermission(Permissions.Student.View)) tasks.push(loadStudents())
    if (authStore.hasPermission(Permissions.Company.View)) tasks.push(loadBusinesses())
    if (authStore.hasPermission(Permissions.Internship.Contract)) tasks.push(loadContracts())
    tasks.push(loadPendingActions())
    if (authStore.hasPermission(Permissions.Institution.View) && institutionId) {
      tasks.push(institutionStore.loadInstitution())
    }

    await Promise.allSettled(tasks)
  }

  return {
    institutionName,
    stats,
    studentChartOption,
    contractChartOption,
    init,
  }
}
