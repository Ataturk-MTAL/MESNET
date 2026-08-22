import { ref, computed, type Ref, type ComputedRef } from 'vue'
import { coordinationApi, type DistanceHourRule } from 'src/api/coordination'
import type { useNotify } from 'src/composables/useNotify'
import {
  CATCH_ALL_DISTANCE,
  NEW_RULE_DEFAULT_DISTANCE,
  NEW_RULE_DEFAULT_HOURS,
  cloneRules,
  isCatchAllRule,
  sortRules,
  validateCoordinationConfig,
  type CoordinationConfigDraft,
} from 'src/utils/coordinationConfig'

/**
 * Kurum koordinasyon yapılandırması ekranının durumu (#134).
 *
 * Okuma yetkisi (`department:distribution:manage`) ile yazma yetkisi
 * (`institution:coordination-config:manage`) ayrıdır: alan şefi tabloyu görür, değiştiremez.
 * Bu composable yazma kararını `canManage` üzerinden dışarıdan alır — rol adına BAKMAZ.
 *
 * Ayar dönem-bağımsızdır: `CoordinationConfig` üzerinde `AcademicPeriodId` yoktur, mevzuat
 * tablosu kurum genelidir. Bu yüzden kapalı dönem kilidi (`isReadOnly`) burada uygulanmaz.
 */
export interface UseCoordinationConfigOptions {
  notify: ReturnType<typeof useNotify>
  /** Yazma yetkisi — `Permissions.Institution.CoordinationConfigManage`. */
  canManage: ComputedRef<boolean> | Ref<boolean>
}

/** Kaydedilmemiş bir kurum için backend varsayılanı — ekranda boş tablo gösterilmez. */
const FALLBACK_RULES: DistanceHourRule[] = [
  { maxDistanceKm: 1, hours: 2 },
  { maxDistanceKm: 3, hours: 4 },
  { maxDistanceKm: 5, hours: 6 },
  { maxDistanceKm: CATCH_ALL_DISTANCE, hours: 8 },
]

const FALLBACK_MAX_WEEKLY_EXTRA_HOURS = 20

export function useCoordinationConfig(options: UseCoordinationConfigOptions) {
  const { notify, canManage } = options

  const loading = ref(false)
  const saving = ref(false)
  const loadFailed = ref(false)

  const distanceHourRules = ref<DistanceHourRule[]>(cloneRules(FALLBACK_RULES))
  const isMetropolitan = ref(true)
  const maxWeeklyExtraHours = ref(FALLBACK_MAX_WEEKLY_EXTRA_HOURS)

  /** Son kaydeden kullanıcı ve zamanı — hiç kaydedilmemişse null. */
  const lastUpdatedAt = ref<string | null>(null)
  const lastUpdatedBy = ref<string | null>(null)

  const draft = computed<CoordinationConfigDraft>(() => ({
    distanceHourRules: distanceHourRules.value,
    isMetropolitan: isMetropolitan.value,
    maxWeeklyExtraHours: maxWeeklyExtraHours.value,
  }))

  const validationErrors = computed(() => validateCoordinationConfig(draft.value))
  const isValid = computed(() => validationErrors.value.length === 0)

  const canSave = computed(
    () => canManage.value && isValid.value && !saving.value && !loading.value,
  )

  /** Kaydet düğmesi devre dışıysa nedenini Türkçe açıklar; etkinken boş metin döner. */
  const saveDisabledReason = computed(() => {
    if (!canManage.value) {
      return 'Kurum koordinasyon yapılandırmasını değiştirme yetkiniz yok. ' +
        'Tabloyu görüntüleyebilir, kaydedemezsiniz.'
    }
    if (loading.value) return 'Yapılandırma yükleniyor.'
    if (saving.value) return 'Kaydetme işlemi sürüyor.'
    if (!isValid.value) return 'Formda düzeltilmesi gereken hatalar var.'
    return ''
  })

  /** "0001-01-01" gibi hiç kaydedilmemiş damgaları eler. */
  function normalizeTimestamp(value: string | null | undefined): string | null {
    if (!value) return null
    const parsed = new Date(value)
    if (Number.isNaN(parsed.getTime())) return null
    if (parsed.getFullYear() < 2000) return null
    return value
  }

  async function load() {
    loading.value = true
    loadFailed.value = false
    try {
      const res = await coordinationApi.getConfig()
      const data = res.data
      if (!data) {
        loadFailed.value = true
        return
      }

      const rules = Array.isArray(data.distanceHourRules) ? data.distanceHourRules : []
      distanceHourRules.value = rules.length > 0
        ? sortRules(rules)
        : cloneRules(FALLBACK_RULES)
      isMetropolitan.value = data.isMetropolitan
      maxWeeklyExtraHours.value = data.maxWeeklyExtraHours
      lastUpdatedAt.value = normalizeTimestamp(data.updatedAt)
      // Ad backend'de saklanmaz; kimlikten UserNameView ile çözülür (#137).
      // Bilinmiyorsa null döner — silinmiş kullanıcı ya da backfill henüz koşmamış demektir.
      lastUpdatedBy.value = data.updatedByName?.trim() ? data.updatedByName : null
    } catch (e: unknown) {
      loadFailed.value = true
      notify.apiError(e, 'Koordinasyon yapılandırması yüklenemedi.')
    } finally {
      loading.value = false
    }
  }

  /** Yeni kural satırını "üzeri" satırının ÜSTÜNE ekler — catch-all her zaman en sonda kalır. */
  function addRule() {
    if (!canManage.value) return
    const next = cloneRules(distanceHourRules.value)
    const newRule: DistanceHourRule = {
      maxDistanceKm: NEW_RULE_DEFAULT_DISTANCE,
      hours: NEW_RULE_DEFAULT_HOURS,
    }
    const catchAllIndex = next.findIndex(isCatchAllRule)
    if (catchAllIndex < 0) {
      next.push(newRule)
    } else {
      next.splice(catchAllIndex, 0, newRule)
    }
    distanceHourRules.value = next
  }

  /** Catch-all satırı silinemez — mevzuat tablosunun tavanı odur. */
  function removeRule(index: number) {
    if (!canManage.value) return
    const rule = distanceHourRules.value[index]
    if (!rule || isCatchAllRule(rule)) return
    distanceHourRules.value = cloneRules(distanceHourRules.value).filter((_, i) => i !== index)
  }

  function canRemoveRule(rule: DistanceHourRule): boolean {
    return canManage.value && !isCatchAllRule(rule)
  }

  /**
   * Kaydeder. Doğrulama burada da tekrarlanır: düğme devre dışı olsa bile bu fonksiyon
   * programatik olarak çağrılabilir ve geçersiz veri sunucuya gitmemelidir.
   */
  async function save() {
    if (!canManage.value) {
      notify.warning('Kurum koordinasyon yapılandırmasını değiştirme yetkiniz yok.')
      return
    }
    if (!isValid.value) {
      notify.warning(validationErrors.value[0] ?? 'Yapılandırma geçersiz.')
      return
    }

    saving.value = true
    try {
      await coordinationApi.upsertConfig({
        distanceHourRules: cloneRules(distanceHourRules.value),
        isMetropolitan: isMetropolitan.value,
        maxWeeklyExtraHours: maxWeeklyExtraHours.value,
        // updatedBy gönderilmez — aktör token'dan damgalanır (#137)
      })
      notify.success('Koordinasyon yapılandırması kaydedildi.')
    } catch (e: unknown) {
      // 422 gövdesindeki Türkçe iş kuralı mesajı kullanıcıya aynen gösterilir.
      notify.apiError(e, 'Koordinasyon yapılandırması kaydedilemedi.')
      return
    } finally {
      saving.value = false
    }

    // Yeniden yükleme kaydetme try/catch'inin DIŞINDA: yükleme hatası "kaydedilemedi"
    // izlenimi vermemeli.
    await load()
  }

  return {
    loading,
    saving,
    loadFailed,
    distanceHourRules,
    isMetropolitan,
    maxWeeklyExtraHours,
    lastUpdatedAt,
    lastUpdatedBy,
    validationErrors,
    isValid,
    canSave,
    saveDisabledReason,
    load,
    save,
    addRule,
    removeRule,
    canRemoveRule,
  }
}
