import { ref, computed, type Ref } from 'vue'
import {
  coordinationApi,
  type BusinessAssignmentDto,
  type BranchWorkloadConfigDto,
} from 'src/api/coordination'
import type { useNotify } from 'src/composables/useNotify'
import {
  isWorkloadPoolUndefined,
  assignedHoursToneClass,
  remainingHoursLabel,
  remainingHoursToneClass,
} from 'src/utils/workloadPool'
import { billableTargetHours, isHonorary } from 'src/utils/coordinationHours'

export interface UseAssignedHoursOptions {
  assignments: Ref<BusinessAssignmentDto[]>
  workloadConfig: Ref<BranchWorkloadConfigDto | null>
  /** Seçili akademik dönem — koordinasyon satırı alan+dönem bazlıdır (#114) */
  academicPeriodId: Ref<string | null>
  notify: ReturnType<typeof useNotify>
  loadData: () => Promise<void>
}

export function useAssignedHours(options: UseAssignedHoursOptions) {
  const { assignments, workloadConfig, academicPeriodId, notify, loadData } = options

  const hoursSaving = ref(false)
  const editedHours = ref<Record<string, number>>({})
  /** İşletme → fahri ziyaret işareti (#115). Saatten ayrı tutulur: "0 saat" ≠ "fahri". */
  const editedHonorary = ref<Record<string, boolean>>({})

  function initEditedHours() {
    const hoursMap: Record<string, number> = {}
    const honoraryMap: Record<string, boolean> = {}
    for (const a of assignments.value) {
      // Fahri satırda giriş 0'dan başlar — tavana düşürmek fahri anlamını yok ederdi.
      hoursMap[a.businessId] = billableTargetHours(a)
      honoraryMap[a.businessId] = isHonorary(a)
    }
    editedHours.value = hoursMap
    editedHonorary.value = honoraryMap
  }

  /**
   * Fahri işaretini değiştirir. Fahri seçilince saat girişi 0'a düşer; işaret
   * kaldırılınca kullanıcı bir saat girene kadar mesafe tavanı önerilir.
   */
  function setHonorary(businessId: string, value: boolean) {
    editedHonorary.value[businessId] = value
    if (value) {
      editedHours.value[businessId] = 0
      return
    }
    const biz = assignments.value.find((a) => a.businessId === businessId)
    editedHours.value[businessId] = biz?.maxCoordinationHours ?? 0
  }

  /** Σ MaxCoordinationHours — işletmelerin mesafe bazlı max saatlerinin toplamı (ikincil referans) */
  const hoursTotalMaxHours = computed(() =>
    assignments.value.reduce((sum, a) => sum + a.maxCoordinationHours, 0),
  )

  /** Ders yükü havuzu — birincil kısıt */
  const hoursWorkloadPool = computed(() => workloadConfig.value?.totalWorkloadPool ?? 0)

  /**
   * Σ Takdir — havuzdan düşen saat. Fahri işaretli satırlar ücret doğurmadığı için
   * toplama girmez (#115).
   */
  const hoursTotalAssigned = computed(() =>
    Object.entries(editedHours.value).reduce(
      (sum, [businessId, h]) => (editedHonorary.value[businessId] ? sum : sum + h),
      0,
    ),
  )

  /** Fahri işaretli işletme sayısı — havuz dışında kalan satırlar. */
  const honoraryCount = computed(
    () => Object.values(editedHonorary.value).filter(Boolean).length,
  )

  const hoursRemaining = computed(() => hoursWorkloadPool.value - hoursTotalAssigned.value)

  /**
   * Havuz hiç hesaplanmamış (#111). `hoursOverLimit` bu durumda bilinçli olarak false —
   * havuz bilinmeden "aşıyor" demek yanlış olurdu — ama sessiz kalmak da yanlıştı:
   * sayfa bu bayrağa bakıp ayrı bir "havuz tanımlanmamış" uyarısı gösterir.
   */
  const hoursPoolUndefined = computed(() => isWorkloadPoolUndefined(hoursWorkloadPool.value))

  const hoursOverLimit = computed(() =>
    hoursWorkloadPool.value > 0 && hoursTotalAssigned.value > hoursWorkloadPool.value,
  )

  /** Σ Takdir'in anlamsal rengi — havuz tanımsız + saat girilmişse uyarı tonu. */
  const hoursTotalAssignedClass = computed(() =>
    assignedHoursToneClass(hoursWorkloadPool.value, hoursTotalAssigned.value),
  )

  /** Kalan gösterimi — havuz tanımsızken sayı yerine "—". */
  const hoursRemainingLabel = computed(() =>
    remainingHoursLabel(hoursWorkloadPool.value, hoursRemaining.value),
  )

  const hoursRemainingClass = computed(() =>
    remainingHoursToneClass(hoursWorkloadPool.value, hoursRemaining.value),
  )

  const hoursNearLimit = computed(() =>
    hoursWorkloadPool.value > 0 &&
    !hoursOverLimit.value &&
    hoursTotalAssigned.value > hoursWorkloadPool.value * 0.9,
  )

  /** Satır kaydedilmeye değer mi — saat ya da fahri işareti değişmişse evet. */
  function isRowChanged(a: BusinessAssignmentDto): boolean {
    const editedIsHonorary = editedHonorary.value[a.businessId] ?? false
    if (editedIsHonorary !== isHonorary(a)) return true
    if (editedIsHonorary) return false // fahri satırda saat zaten 0'a sabit
    const edited = editedHours.value[a.businessId]
    return edited !== undefined && edited !== billableTargetHours(a)
  }

  const changedHoursCount = computed(
    () => assignments.value.filter(isRowChanged).length,
  )

  async function saveHours() {
    hoursSaving.value = true
    let successCount = 0
    const errors: string[] = []

    for (const a of assignments.value) {
      if (!isRowChanged(a)) continue

      const honorary = editedHonorary.value[a.businessId] ?? false

      try {
        await coordinationApi.updateAssignedHours(
          a.businessId,
          // Fahri satırda saat her zaman 0 gönderilir; backend de aynı kuralı uygular.
          { assignedHours: honorary ? 0 : (editedHours.value[a.businessId] ?? 0), isHonoraryVisit: honorary },
          { branchCode: a.branchCode, academicPeriodId: academicPeriodId.value ?? '' },
        )
        successCount++
      } catch (e: unknown) {
        const msg = e instanceof Error ? e.message : 'Bilinmeyen hata'
        errors.push(`${a.businessName}: ${msg}`)
      }
    }

    hoursSaving.value = false

    if (successCount > 0) {
      notify.success(`${successCount} işletmenin takdir edilen saati güncellendi.`)
      await loadData()
      initEditedHours()
    }
    if (errors.length > 0) {
      notify.warning(`Hatalar: ${errors.join(', ')}`)
    }
  }

  return {
    editedHours,
    editedHonorary,
    honoraryCount,
    setHonorary,
    hoursSaving,
    hoursTotalMaxHours,
    hoursWorkloadPool,
    hoursTotalAssigned,
    hoursTotalAssignedClass,
    hoursRemaining,
    hoursRemainingLabel,
    hoursRemainingClass,
    hoursPoolUndefined,
    hoursOverLimit,
    hoursNearLimit,
    changedHoursCount,
    initEditedHours,
    saveHours,
  }
}
