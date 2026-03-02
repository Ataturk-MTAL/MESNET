import { ref, computed } from 'vue'
import { defineStore } from 'pinia'
import { institutionApi, type AcademicPeriodDto } from 'src/api/institution'
import { useAuthStore } from './auth'

// Aya göre varsayılan yarıyıl
function getDefaultSemester(): string {
  const month = new Date().getMonth() + 1 // 1-12
  if (month >= 9 || month <= 1) return 'Fall'    // Eylül-Ocak → 1. Dönem
  if (month >= 2 && month <= 6) return 'Spring'   // Şubat-Haziran → 2. Dönem
  return 'Summer'                                  // Temmuz-Ağustos → Yaz Dönemi
}

export const semesterOptions = [
  { label: '1. Dönem', value: 'Fall', number: 1 },
  { label: '2. Dönem', value: 'Spring', number: 2 },
  { label: 'Yaz Dönemi', value: 'Summer', number: 3 },
] as const

export const useAcademicPeriodStore = defineStore('academicPeriod', () => {
  // ── Akademik Dönem (yıl bazlı: 2024-2025) ──
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

  // ── Yarıyıl (1. Dönem / 2. Dönem / Yaz Dönemi) ──
  const selectedSemester = ref<string>(getDefaultSemester())

  const selectedSemesterLabel = computed(() =>
    semesterOptions.find((s) => s.value === selectedSemester.value)?.label ?? '',
  )

  // Seçili akademik dönemin başlangıç yılı (schedule kaydetmede academicYear olarak kullanılır)
  const academicYear = computed(() =>
    selectedPeriod.value?.startYear ?? new Date().getFullYear(),
  )

  async function loadPeriods(): Promise<void> {
    const authStore = useAuthStore()
    const institutionId = authStore.user?.institutionId
    if (!institutionId) return

    try {
      const { data } = await institutionApi.listAcademicPeriods(institutionId, { pageSize: 100 })
      periods.value = data?.items ?? []

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

  function selectSemester(semester: string): void {
    selectedSemester.value = semester
  }

  function clear(): void {
    periods.value = []
    selectedPeriodId.value = null
    selectedSemester.value = getDefaultSemester()
    isLoaded.value = false
  }

  return {
    // Akademik dönem
    periods,
    selectedPeriodId,
    selectedPeriod,
    activePeriod,
    isReadOnly,
    isLoaded,
    loadPeriods,
    selectPeriod,
    // Yarıyıl
    selectedSemester,
    selectedSemesterLabel,
    selectSemester,
    // Yardımcı
    academicYear,
    clear,
  }
})
