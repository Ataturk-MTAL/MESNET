import { ref } from 'vue'
import type { useNotify } from 'src/composables/useNotify'
import type { StuckApprovalByInstitutionDto } from 'src/api/internship'

/** Yöneticisiz okul özeti: toplam sayı + gösterilecek ilk adlar. */
export interface UnmanagedSummary {
  total: number
  names: string[]
}

export interface StuckSummary {
  totalCount: number
  thresholdDays: number
  byInstitution: StuckApprovalByInstitutionDto[]
}

/**
 * Veri kaynakları DIŞARIDAN verilir (CLAUDE.md: composable store/service'e doğrudan erişmek
 * yerine parametre alır). Böylece test axios'u taklit etmeden koşar.
 */
export interface UseDirectorateDashboardOptions {
  fetchDistrictCount: () => Promise<number>
  fetchSchoolCount: () => Promise<number>
  fetchUnmanaged: () => Promise<UnmanagedSummary>
  fetchStuck: () => Promise<StuckSummary>
  notify: ReturnType<typeof useNotify>
}

/** Eşik belgesi hiç yazılmamışsa sunucunun kullandığı varsayılan (backend ile aynı sayı). */
const DEFAULT_THRESHOLD_DAYS = 14

export function useDirectorateDashboard(options: UseDirectorateDashboardOptions) {
  const { fetchDistrictCount, fetchSchoolCount, fetchUnmanaged, fetchStuck, notify } = options

  const districtCount = ref(0)
  const schoolCount = ref(0)
  const unmanagedCount = ref(0)
  const unmanagedNames = ref<string[]>([])
  const stuckCount = ref(0)
  const stuckThresholdDays = ref(DEFAULT_THRESHOLD_DAYS)
  const stuckByInstitution = ref<StuckApprovalByInstitutionDto[]>([])
  const loading = ref(false)

  /**
   * Dört çağrı BİRBİRİNDEN BAĞIMSIZ yürür ve her biri kendi hatasını yutar. Tek bir
   * `Promise.all` kullanılsaydı ilk reddedilen çağrı diğer üçünün sonucunu da düşürürdü ve
   * bir ucun 403'ü panoyu tümden söndürürdü.
   */
  async function load() {
    loading.value = true

    await Promise.all([
      run(async () => {
        districtCount.value = await fetchDistrictCount()
      }, 'İlçe sayısı alınamadı.'),
      run(async () => {
        schoolCount.value = await fetchSchoolCount()
      }, 'Okul sayısı alınamadı.'),
      run(async () => {
        const summary = await fetchUnmanaged()
        unmanagedCount.value = summary.total
        unmanagedNames.value = summary.names
      }, 'Yöneticisi olmayan okullar alınamadı.'),
      run(async () => {
        const summary = await fetchStuck()
        stuckCount.value = summary.totalCount
        stuckThresholdDays.value = summary.thresholdDays
        stuckByInstitution.value = summary.byInstitution
      }, 'Tıkanmış onaylar alınamadı.'),
    ])

    loading.value = false
  }

  async function run(action: () => Promise<void>, message: string) {
    try {
      await action()
    } catch (e) {
      notify.apiError(e, message)
    }
  }

  return {
    districtCount,
    schoolCount,
    unmanagedCount,
    unmanagedNames,
    stuckCount,
    stuckThresholdDays,
    stuckByInstitution,
    loading,
    load,
  }
}
