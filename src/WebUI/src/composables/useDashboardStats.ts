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
import { logger } from 'utils/logger'
import type { PagedResponse } from 'src/types/pagination'

export interface UseDashboardStatsOptions {
  authStore: ReturnType<typeof useAuthStore>
  institutionId: string
}

// Status label maps
// Sıra = backend StudentStatus değer sırası. Adlar backend SmartEnum Name'leriyle BİREBİR
// aynı olmalı: geçersiz ad TryFromName'de düşer ve süzgeç SESSİZCE atlanır (tüm kayıtlar sayılır).
const STUDENT_STATUSES = ['Registered', 'Applied', 'Placed', 'ActiveInternship', 'Completed', 'Deregistered'] as const

const STUDENT_STATUS_LABELS: Record<string, string> = {
  Registered: 'Kayıtlı',
  Applied: 'Başvurdu',
  Placed: 'Yerleştirildi',
  ActiveInternship: 'Aktif Staj',
  Completed: 'Tamamladı',
  Deregistered: 'Kayıt Silindi',
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
  Deregistered: statusTone.negative,
}

// Sıra = backend ContractStatus değer sırası (grafik bu sırayla çizilir).
const CONTRACT_STATUSES = [
  'Draft', 'AwaitingSignature', 'Active', 'Suspended', 'TerminationRequested', 'Terminated', 'Completed',
] as const

const CONTRACT_STATUS_LABELS: Record<string, string> = {
  Draft: 'Taslak',
  AwaitingSignature: 'İmza Bekliyor',
  Active: 'Aktif',
  Suspended: 'Askıda',
  TerminationRequested: 'Fesih Talep Edildi',
  Terminated: 'Feshedildi',
  Completed: 'Tamamlandı',
}

const CONTRACT_STATUS_COLORS: Record<string, () => string> = {
  Draft: () => NEUTRAL_GREY,
  AwaitingSignature: statusTone.pending,
  Active: statusTone.active,
  Suspended: statusTone.warning,
  TerminationRequested: statusTone.warning,
  Terminated: statusTone.negative,
  Completed: statusTone.success,
}

/**
 * Sayım için tek satırlık sayfa istenir; sayı yanıttaki `totalCount`tan okunur. Sayfadaki
 * satırları saymak (`items.length` / `filter`) sayfa boyutunu aşan veride YANLIŞ sonuç verir.
 */
const COUNT_PAGE_SIZE = 1

type CountFetcher = (params: { status?: string; pageSize: number }) => Promise<{ data: PagedResponse<unknown> }>

async function fetchCount(fetcher: CountFetcher, status?: string): Promise<number> {
  const res = await fetcher({ status, pageSize: COUNT_PAGE_SIZE })
  return res.data?.totalCount ?? 0
}

/** Durum başına toplam sayım — sayfalamadan bağımsız, tüm veri çekilmez. */
async function fetchStatusCounts(
  fetcher: CountFetcher,
  statuses: readonly string[],
): Promise<Record<string, number>> {
  const counts = await Promise.all(statuses.map((status) => fetchCount(fetcher, status)))
  return Object.fromEntries(statuses.map((status, i) => [status, counts[i] ?? 0]))
}

export function useDashboardStats(options: UseDashboardStatsOptions) {
  const { authStore, institutionId } = options

  const institutionStore = useInstitutionStore()
  const institutionName = computed(() => institutionStore.institution?.fullName ?? '')

  // Stats — `null` = yüklenemedi. Kart bunu nötr "—" olarak gösterir; 0 gibi uydurma bir
  // sayı gösterilmez (0 "hiç kayıt yok" demektir, hata değil).
  const stats = reactive({
    students: null as number | null,
    studentsLoading: true,
    businesses: null as number | null,
    businessesLoading: true,
    activeContracts: null as number | null,
    contractsLoading: true,
    pendingTotal: null as number | null,
    pendingLoading: true,
  })

  // Chart data
  const studentChartOption = ref<EChartsOption | null>(null)
  const contractChartOption = ref<EChartsOption | null>(null)

  // Chart builders
  function buildStudentChart(grouped: Record<string, number>) {
    const data = Object.entries(grouped).filter(([, count]) => count > 0).map(([status, count]) => ({
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

  function buildContractChart(grouped: Record<string, number>) {
    const categories: string[] = []
    const values: number[] = []
    const colors: string[] = []

    for (const status of CONTRACT_STATUSES) {
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

  // Data loaders — hata yutulmaz: loglanır ve kart "—" gösterir (değer null kalır).
  async function loadStudents() {
    try {
      const [total, byStatus] = await Promise.all([
        fetchCount(enrollmentApi.listStudents),
        fetchStatusCounts(enrollmentApi.listStudents, STUDENT_STATUSES),
      ])
      stats.students = total
      buildStudentChart(byStatus)
    } catch (error: unknown) {
      logger.warn('[Pano] Öğrenci sayıları yüklenemedi', error)
    } finally {
      stats.studentsLoading = false
    }
  }

  async function loadBusinesses() {
    try {
      stats.businesses = await fetchCount(businessApi.list, 'Active')
    } catch (error: unknown) {
      logger.warn('[Pano] Aktif işletme sayısı yüklenemedi', error)
    } finally {
      stats.businessesLoading = false
    }
  }

  async function loadContracts() {
    try {
      const byStatus = await fetchStatusCounts(contractApi.list, CONTRACT_STATUSES)
      stats.activeContracts = byStatus.Active ?? 0
      buildContractChart(byStatus)
    } catch (error: unknown) {
      logger.warn('[Pano] Sözleşme sayıları yüklenemedi', error)
    } finally {
      stats.contractsLoading = false
    }
  }

  /**
   * Bekleyen iş toplamı. Kuyruklardan biri bile yüklenemezse toplam "—" olur: eksik kuyrukla
   * toplanmış sayı gerçek iş yükünden az görünür ve "sıra sizde" sinyalini yanlış söndürür.
   */
  async function loadPendingActions() {
    const tasks: Promise<number>[] = []

    if (authStore.hasPermission(Permissions.Internship.Contract)) {
      tasks.push(fetchCount(contractApi.list, 'AwaitingSignature'))
    }

    if (authStore.hasPermission(Permissions.Attendance.View)) {
      // 'Recorded' onaylanmış kayıttır — bekleyen iş değildir; sayaç yanlış satırı sayıyordu.
      // İşletmenin bildirdiği kayıt 'Pending' doğar ve onaylanana kadar fesih sayacına da
      // girmez (#252), yani öğretmenin onay kuyruğu artık hükmün tek kapısıdır.
      tasks.push(fetchCount(attendanceApi.list, 'Pending'))
    }

    if (authStore.hasPermission(Permissions.Company.View)) {
      tasks.push(fetchCount(businessApi.list, 'PendingApproval'))
    }

    if (authStore.hasPermission(Permissions.UserManagement.View)) {
      // 'Pending' geçerli bir InvitationStatus adı DEĞİLDİR; TryFromName başarısız olur ve
      // durum süzgeci SESSİZCE düşerdi — kart tüm durumların davetini sayıyordu.
      tasks.push(fetchCount(securityApi.listInvitations, 'PendingApproval'))
    }

    const results = await Promise.allSettled(tasks)
    const failures = results.filter((r): r is PromiseRejectedResult => r.status === 'rejected')

    if (failures.length > 0) {
      logger.warn('[Pano] Bekleyen işlem sayılarından bazıları yüklenemedi', ...failures.map((f) => f.reason))
      stats.pendingTotal = null
    } else {
      stats.pendingTotal = results.reduce(
        (sum, r) => sum + (r.status === 'fulfilled' ? r.value : 0),
        0,
      )
    }
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
