import { ref, computed } from 'vue'
import { defineStore } from 'pinia'
import { institutionApi, type AcademicPeriodDto } from 'src/api/institution'
import { useAuthStore } from './auth'

export const useAcademicPeriodStore = defineStore('academicPeriod', () => {
  const periods = ref<AcademicPeriodDto[]>([])
  const selectedPeriodId = ref<string | null>(null)
  const isLoaded = ref(false)

  const selectedPeriod = computed(() =>
    periods.value.find((p) => p.id === selectedPeriodId.value) ?? null,
  )

  const activePeriod = computed(() =>
    periods.value.find((p) => p.status === 'Active') ?? null,
  )

  const isReadOnly = computed(() => selectedPeriod.value?.status === 'Closed')

  async function loadPeriods(): Promise<void> {
    const authStore = useAuthStore()
    const institutionId = authStore.user?.institutionId
    if (!institutionId) return

    try {
      const { data } = await institutionApi.listAcademicPeriods(institutionId)
      periods.value = data ?? []

      // İlk yükleme: aktif dönemi seç
      if (!selectedPeriodId.value && activePeriod.value) {
        selectedPeriodId.value = activePeriod.value.id
      }
      isLoaded.value = true
    } catch {
      // Dönem henüz oluşturulmamış olabilir
      periods.value = []
      isLoaded.value = true
    }
  }

  function selectPeriod(periodId: string): void {
    selectedPeriodId.value = periodId
  }

  function clear(): void {
    periods.value = []
    selectedPeriodId.value = null
    isLoaded.value = false
  }

  return {
    periods,
    selectedPeriodId,
    selectedPeriod,
    activePeriod,
    isReadOnly,
    isLoaded,
    loadPeriods,
    selectPeriod,
    clear,
  }
})
