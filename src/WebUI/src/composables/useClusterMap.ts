import { ref, computed, type Ref } from 'vue'
import { coordinationApi, type BusinessClusterDto } from 'src/api/coordination'
import type { useNotify } from 'src/composables/useNotify'

const CLUSTER_COLORS = [
  '#e53935', '#8e24aa', '#1e88e5', '#00897b', '#43a047',
  '#f4511e', '#6d4c41', '#546e7a', '#c0ca33', '#00acc1',
  '#5e35b1', '#d81b60', '#039be5', '#00e676', '#ffb300',
  '#fb8c00', '#f06292', '#4db6ac', '#9575cd', '#64b5f6',
]

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

  function clusterColor(clusterId: number | null): string {
    if (clusterId === null) return '#9e9e9e'
    return CLUSTER_COLORS[clusterId % CLUSTER_COLORS.length] ?? '#9e9e9e'
  }

  const clusterCounts = computed(() => {
    const counts: Record<string, number> = {}
    for (const b of clusterData.value) {
      const key = b.clusterId === null ? 'null' : String(b.clusterId)
      counts[key] = (counts[key] ?? 0) + 1
    }
    const sorted: Record<string, number> = {}
    Object.keys(counts)
      .sort((a, b) => {
        if (a === 'null') return 1
        if (b === 'null') return -1
        return Number(a) - Number(b)
      })
      .forEach((k) => {
        sorted[k] = counts[k]!
      })
    return sorted
  })

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

  return {
    clusterData,
    clusterLoading,
    clusterError,
    clusterEps,
    clusterMinPoints,
    clusterCounts,
    recalculating,
    clusterColor,
    loadClusters,
    recalculateDistances,
  }
}
