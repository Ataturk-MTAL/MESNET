import { ref, computed } from 'vue'
import { defineStore } from 'pinia'
import {
  institutionApi,
  type InstitutionDto,
  type FieldOfStudyDto,
  type ScheduleConfigDto,
} from 'src/api/institution'
import { useAuthStore } from './auth'

/**
 * Kurum referans/katalog verisi için merkezi cache.
 * Kurum profili (branches dahil), MEB alan/dal kataloğu ve ders programı config'i
 * bir kez yüklenip tüm sayfalarda paylaşılır (isLoaded guard). academicPeriod store deseni.
 * Mutasyon sonrası ilgili load*(true) ile tazelenir → tüm tüketicilere reaktif yansır.
 */
export const useInstitutionStore = defineStore('institution', () => {
  function currentInstitutionId(): string | null {
    return useAuthStore().user?.institutionId ?? null
  }

  // ── Kurum profili (branches + staff dahil) ──
  const institution = ref<InstitutionDto | null>(null)
  const isLoaded = ref(false)

  const branches = computed(() => institution.value?.branches ?? [])
  const activeBranches = computed(() => branches.value.filter((b) => b.isActive))

  // ── MEB alan/dal kataloğu (tam katalog — educationType filtreli sorgular lokal kalır) ──
  const fieldCatalog = ref<FieldOfStudyDto[]>([])
  const isFieldCatalogLoaded = ref(false)

  // ── Ders programı config ──
  const scheduleConfig = ref<ScheduleConfigDto | null>(null)
  const isScheduleConfigLoaded = ref(false)

  const periodCount = computed(() =>
    scheduleConfig.value?.configured && scheduleConfig.value.dailyPeriodCount
      ? scheduleConfig.value.dailyPeriodCount
      : 0,
  )
  const scheduleConfigMissing = computed(
    () => isScheduleConfigLoaded.value && periodCount.value === 0,
  )

  async function loadInstitution(force = false): Promise<void> {
    const id = currentInstitutionId()
    if (!id) return
    if (isLoaded.value && !force) return
    try {
      const { data } = await institutionApi.get(id)
      institution.value = data
    } finally {
      isLoaded.value = true
    }
  }

  async function loadFieldCatalog(force = false): Promise<void> {
    if (isFieldCatalogLoaded.value && !force) return
    try {
      const { data } = await institutionApi.getFieldCatalog()
      fieldCatalog.value = data ?? []
    } finally {
      isFieldCatalogLoaded.value = true
    }
  }

  async function loadScheduleConfig(force = false): Promise<void> {
    const id = currentInstitutionId()
    if (!id) return
    if (isScheduleConfigLoaded.value && !force) return
    try {
      const { data } = await institutionApi.getScheduleConfig(id)
      scheduleConfig.value = data
    } catch {
      scheduleConfig.value = { configured: false }
    } finally {
      isScheduleConfigLoaded.value = true
    }
  }

  function clear(): void {
    institution.value = null
    isLoaded.value = false
    fieldCatalog.value = []
    isFieldCatalogLoaded.value = false
    scheduleConfig.value = null
    isScheduleConfigLoaded.value = false
  }

  return {
    // Kurum profili
    institution,
    isLoaded,
    branches,
    activeBranches,
    loadInstitution,
    // Katalog
    fieldCatalog,
    isFieldCatalogLoaded,
    loadFieldCatalog,
    // Ders programı config
    scheduleConfig,
    periodCount,
    scheduleConfigMissing,
    isScheduleConfigLoaded,
    loadScheduleConfig,
    // Yardımcı
    clear,
  }
})
