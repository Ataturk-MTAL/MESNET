import { ref, type Ref } from 'vue'
import { coordinationApi, type BusinessClusterDto } from 'src/api/coordination'
import type { useNotify } from 'src/composables/useNotify'

/**
 * Küme verisini çeker ve kümeleme parametrelerini tutar.
 *
 * Küme RENGİ burada üretilmez (#110): tek kaynak `utils/clusterColors.ts`, tek
 * tüketici `BusinessClusterMap.vue` — marker ile legend aynı paletten gelsin diye.
 * Burada ikinci bir palet tutulduğunda aynı küme sayfada iki farklı renkte
 * görünüyordu.
 */
export interface UseClusterMapOptions {
  notify: ReturnType<typeof useNotify>
  loadData: () => Promise<void>
  branchFilter: Ref<string | null>
}

export function useClusterMap(options: UseClusterMapOptions) {
  const { notify, loadData, branchFilter } = options

  const clusterData = ref<BusinessClusterDto[]>([])
  const clusterLoading = ref(false)
  const clusterError = ref(false)
  const clusterEps = ref(1000)
  const clusterMinPoints = ref(3)
  const recalculating = ref(false)
  const resyncing = ref(false)

  async function loadClusters() {
    clusterLoading.value = true
    clusterError.value = false
    try {
      const { data } = await coordinationApi.getBusinessClusters(
        clusterEps.value,
        clusterMinPoints.value,
        branchFilter.value,
      )
      clusterData.value = data
    } catch {
      clusterError.value = true
      clusterData.value = []
    } finally {
      clusterLoading.value = false
    }
  }

  async function recalculateDistances() {
    recalculating.value = true
    try {
      await coordinationApi.recalculateDistances()
      notify.success('Mesafeler yeniden hesaplandı.')
      await loadData()
    } catch (e) {
      notify.apiError(e, 'Mesafe hesaplama sırasında hata oluştu.')
    } finally {
      recalculating.value = false
    }
  }

  /**
   * Koordinasyon satırlarını çok-alanlı modele göre yeniden kurar (#114).
   * Çok-alanlı modele geçmeden önce yazılmış tek-satır kayıtları temizlenir,
   * her (alan, dönem) için satır ve öğrenci sayacı yeniden üretilir.
   */
  async function resyncCoordinationViews() {
    resyncing.value = true
    try {
      const { data } = await coordinationApi.resyncCoordinationViews()
      notify.success(
        `${data?.branchRows ?? 0} alan satırı yeniden kuruldu, ` +
        `${data?.removedLegacyRows ?? 0} eski kayıt temizlendi.`,
      )
      await Promise.all([loadData(), loadClusters()])
    } catch (e) {
      notify.apiError(e, 'Koordinasyon kayıtları yeniden kurulurken hata oluştu.')
    } finally {
      resyncing.value = false
    }
  }

  return {
    clusterData,
    clusterLoading,
    clusterError,
    clusterEps,
    clusterMinPoints,
    recalculating,
    resyncing,
    loadClusters,
    recalculateDistances,
    resyncCoordinationViews,
  }
}
