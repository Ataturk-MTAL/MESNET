import { ref, computed, type Ref } from 'vue'
import {
  coordinationApi,
  type AllocationBucketName,
  type BusinessAssignmentDto,
  type HoursSuggestionDiagnosticsDto,
  type HoursSuggestionDto,
} from 'src/api/coordination'
import type { useNotify } from 'src/composables/useNotify'
import { WORKLOAD_POOL_MISSING_MESSAGE } from 'src/utils/workloadPool'

/**
 * "Otomatik Dağıt" akışı (#118) — #116'daki `SuggestAssignedHours` önerisini ekrana taşır.
 *
 * Üç kural bu composable'ın tamamını belirler:
 *
 * 1. **Öneri kaydetmez.** Dönen değerler yalnız `editedHours`/`editedHonorary` alanlarına
 *    yazılır; kaydetme koordinatörün ayrı bir kararıdır (`useAssignedHours.saveHours`).
 * 2. **Kilitli satır korunur.** Kilitler sorguya `pinned` parametresiyle gider, algoritma
 *    kalan havuzu kalan satırlara dağıtır. Kilitli satırın saati geri gelen öneride de
 *    aynıdır — uygulama adımı onu değiştirmez.
 * 3. **Sessiz kırpma yok.** Havuz yetmediyse / kapasite aşıldıysa / artık kaldıysa tanılama
 *    bunu sayıyla söyler; sayfa `AppNotice` ile açık uyarı basar.
 */
export interface UseHoursSuggestionOptions {
  assignments: Ref<BusinessAssignmentDto[]>
  /** `useAssignedHours` state'i — öneri doğrudan buraya uygulanır. */
  editedHours: Ref<Record<string, number>>
  editedHonorary: Ref<Record<string, boolean>>
  branchCode: Ref<string | null>
  academicPeriodId: Ref<string | null>
  /** Yarıyıl — öğretmen kapasitesi (C) o yarıyılın ders programından hesaplanır. */
  semester: Ref<string>
  /** Havuz tanımsız mı (#111) — true iken öneri istenmez, buton devre dışıdır. */
  poolUndefined: Ref<boolean>
  notify: ReturnType<typeof useNotify>
}

/** Geri al için tutulan anlık görüntü. Düz sayı/bool haritaları — derin kopya gerekmez. */
interface HoursSnapshot {
  hours: Record<string, number>
  honorary: Record<string, boolean>
}

export function useHoursSuggestion(options: UseHoursSuggestionOptions) {
  const {
    assignments,
    editedHours,
    editedHonorary,
    branchCode,
    academicPeriodId,
    semester,
    poolUndefined,
    notify,
  } = options

  const suggesting = ref(false)
  /** İşletme → koordinatör bu satırı kilitledi mi. */
  const pinnedRows = ref<Record<string, boolean>>({})
  /** İşletme → son önerinin kova adı. Öneri yokken boştur, rozet basılmaz. */
  const bucketByBusiness = ref<Record<string, AllocationBucketName>>({})
  const diagnostics = ref<HoursSuggestionDiagnosticsDto | null>(null)
  /** Öneri uygulanmadan önceki değerler — "Geri Al" bunu geri yazar. */
  const previousValues = ref<HoursSnapshot | null>(null)

  const pinnedCount = computed(
    () => Object.values(pinnedRows.value).filter(Boolean).length,
  )

  /**
   * Kilitli satırların sorgu dizesi gösterimi: `"işletmeKimliği:saat,..."`.
   * Fahri kilit 0 saatle gider — backend 0 saatlik satırı fahri kovasına koyar.
   */
  const pinnedParam = computed(() =>
    Object.entries(pinnedRows.value)
      .filter(([, pinned]) => pinned)
      .map(([businessId]) => `${businessId}:${pinnedHoursOf(businessId)}`)
      .join(','),
  )

  /** Öneri istenebilir mi — havuz tanımsızken buton devre dışı kalır (#111). */
  const canAutoDistribute = computed(() =>
    !poolUndefined.value &&
    !!branchCode.value &&
    !!academicPeriodId.value &&
    assignments.value.length > 0,
  )

  const canUndo = computed(() => previousValues.value !== null)

  /** Havuz tüm işletmelere yetmedi — bazı satırlar fahriye düştü. */
  const hasHonoraryFallback = computed(() => (diagnostics.value?.honoraryCount ?? 0) > 0)

  /** Alan öğretmenlerinin boş saati havuzu karşılamıyor. */
  const hasOutOfBranchOverflow = computed(() => (diagnostics.value?.outOfBranchHours ?? 0) > 0)

  /** Havuzda dağıtılamayan artık kaldı (herkes tavanında). */
  const hasUndistributedSurplus = computed(() => (diagnostics.value?.undistributed ?? 0) > 0)

  /** Kilitli satırların toplamı havuzu aşıyor — artık negatif. */
  const pinnedOverPool = computed(() => (diagnostics.value?.undistributed ?? 0) < 0)

  /** Havuzun kilitlerle aşılan miktarı (pozitif sayı olarak). */
  const pinnedOverflowHours = computed(() =>
    Math.max(0, -(diagnostics.value?.undistributed ?? 0)),
  )

  function pinnedHoursOf(businessId: string): number {
    if (editedHonorary.value[businessId]) return 0
    const value = editedHours.value[businessId]
    if (typeof value !== 'number' || !Number.isFinite(value)) return 0
    return Math.max(0, Math.trunc(value))
  }

  function isPinned(businessId: string): boolean {
    return pinnedRows.value[businessId] ?? false
  }

  function setPinned(businessId: string, value: boolean) {
    pinnedRows.value = { ...pinnedRows.value, [businessId]: value }
  }

  function togglePin(businessId: string) {
    setPinned(businessId, !isPinned(businessId))
  }

  function bucketOf(businessId: string): AllocationBucketName | null {
    return bucketByBusiness.value[businessId] ?? null
  }

  /** Öneri çıktısını temizler (rozetler, tanılama, geri al). Kilitler korunur. */
  function clearSuggestion() {
    bucketByBusiness.value = {}
    diagnostics.value = null
    previousValues.value = null
  }

  /** Alan değişince kilitler de anlamını yitirir — hepsi sıfırlanır. */
  function resetAll() {
    pinnedRows.value = {}
    clearSuggestion()
  }

  function takeSnapshot(): HoursSnapshot {
    return {
      hours: { ...editedHours.value },
      honorary: { ...editedHonorary.value },
    }
  }

  function applySuggestion(suggestion: HoursSuggestionDto) {
    const hoursMap = { ...editedHours.value }
    const honoraryMap = { ...editedHonorary.value }
    const buckets: Record<string, AllocationBucketName> = {}

    for (const line of suggestion.lines) {
      hoursMap[line.businessId] = line.suggestedHours
      honoraryMap[line.businessId] = line.isHonoraryVisit
      buckets[line.businessId] = line.bucket
    }

    editedHours.value = hoursMap
    editedHonorary.value = honoraryMap
    bucketByBusiness.value = buckets
    diagnostics.value = suggestion.diagnostics
  }

  /**
   * Öneriyi ister ve tabloya doldurur. **Kaydetmez** — kaydetme koordinatörün ayrı kararı.
   * Havuz tanımsızsa istek hiç atılmaz; buton zaten devre dışıdır, bu ikinci koruma.
   */
  async function autoDistribute() {
    if (poolUndefined.value) {
      notify.warning(WORKLOAD_POOL_MISSING_MESSAGE)
      return
    }

    const branch = branchCode.value
    const period = academicPeriodId.value
    if (!branch || !period) return

    const snapshot = takeSnapshot()
    const pinned = pinnedParam.value

    suggesting.value = true
    let suggestion: HoursSuggestionDto | null = null
    try {
      const response = await coordinationApi.suggestAssignedHours({
        branchCode: branch,
        academicPeriodId: period,
        semester: semester.value,
        ...(pinned ? { pinned } : {}),
      })
      suggestion = response.data ?? null
    } catch (e: unknown) {
      notify.apiError(e, 'Otomatik dağıtım önerisi alınamadı.')
      return
    } finally {
      suggesting.value = false
    }

    if (!suggestion) {
      notify.warning('Sunucu öneri döndürmedi; saatler değiştirilmedi.')
      return
    }

    // Havuz tanımsızsa algoritma bilinçli olarak boş öneri döndürür — tanılama gösterilir,
    // hiçbir saat değiştirilmez.
    if (suggestion.diagnostics.isPoolUndefined) {
      diagnostics.value = suggestion.diagnostics
      notify.warning(WORKLOAD_POOL_MISSING_MESSAGE)
      return
    }

    applySuggestion(suggestion)
    previousValues.value = snapshot

    notify.success(
      `${suggestion.lines.length} işletme için öneri dolduruldu. ` +
      'Değerleri gözden geçirip "Saatleri Kaydet" ile onaylayın.',
    )
  }

  /** Öneriyi uygulamadan önceki saatlere döner. Kaydedilmiş veriye dokunmaz. */
  function undoSuggestion() {
    const snapshot = previousValues.value
    if (!snapshot) return

    editedHours.value = { ...snapshot.hours }
    editedHonorary.value = { ...snapshot.honorary }
    clearSuggestion()
    notify.info('Öneri geri alındı; önceki saatler yeniden yüklendi.')
  }

  return {
    suggesting,
    pinnedRows,
    pinnedCount,
    pinnedParam,
    diagnostics,
    bucketByBusiness,
    canAutoDistribute,
    canUndo,
    hasHonoraryFallback,
    hasOutOfBranchOverflow,
    hasUndistributedSurplus,
    pinnedOverPool,
    pinnedOverflowHours,
    isPinned,
    setPinned,
    togglePin,
    bucketOf,
    autoDistribute,
    undoSuggestion,
    clearSuggestion,
    resetAll,
  }
}
