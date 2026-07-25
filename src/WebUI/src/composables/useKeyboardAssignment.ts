import { ref, computed } from 'vue'

/**
 * Sürükle-bırak yüzeylerinin klavyeyle erişilebilir alternatifi (#88).
 *
 * Akış "seç → hedefe git → bırak": Enter/Space ile işletme seçilir, Tab ile hedef hücreye
 * gidilir, Enter/Space ile bırakılır. Escape seçimi iptal eder. Ok tuşlarıyla ızgara
 * gezinmesi bilerek YAPILMADI — hücreler zaten doğal Tab sırasında ve ok tuşlarını ele
 * geçirmek ekran okuyucunun kendi gezinme kiplerini bozar.
 *
 * Her durum değişikliği `announcement` ile duyurulur; sayfa bunu aria-live bölgesine basar.
 */
export interface AssignmentSelection {
  businessId: string
  businessName: string
  /** Izgaradan alındıysa kaynak gün — atanmamış liste kartından alındıysa undefined. */
  fromDay?: string
  /** Izgaradan alındıysa kaynak ders saati. */
  fromPeriod?: number
}

export function useKeyboardAssignment() {
  const selected = ref<AssignmentSelection | null>(null)
  const announcement = ref('')

  const hasSelection = computed(() => selected.value !== null)

  function select(item: AssignmentSelection) {
    selected.value = item
    announcement.value =
      item.fromDay !== undefined
        ? `${item.businessName} seçildi (${dayLabel(item.fromDay)} ${item.fromPeriod}. ders). ` +
          'Boş bir hücreye gidip Enter ile taşıyın, iptal için Escape.'
        : `${item.businessName} seçildi. Boş bir hücreye gidip Enter ile atayın, iptal için Escape.`
  }

  /** Aynı öğeye tekrar basmak seçimi kaldırır. */
  function toggle(item: AssignmentSelection) {
    if (
      selected.value?.businessId === item.businessId &&
      selected.value?.fromDay === item.fromDay &&
      selected.value?.fromPeriod === item.fromPeriod
    ) {
      cancel()
      return
    }
    select(item)
  }

  function cancel() {
    if (!selected.value) return
    announcement.value = `${selected.value.businessName} seçimi iptal edildi.`
    selected.value = null
  }

  /** Bırakma yapıldığında çağrılır — seçimi temizler ve sonucu duyurur. */
  function completeDrop(day: string, periodNumber: number) {
    if (!selected.value) return
    announcement.value = `${selected.value.businessName}, ${dayLabel(day)} ${periodNumber}. derse atandı.`
    selected.value = null
  }

  return { selected, announcement, hasSelection, select, toggle, cancel, completeDrop }
}

const DAY_LABELS: Record<string, string> = {
  Monday: 'Pazartesi',
  Tuesday: 'Salı',
  Wednesday: 'Çarşamba',
  Thursday: 'Perşembe',
  Friday: 'Cuma',
}

export function dayLabel(day: string): string {
  return DAY_LABELS[day] ?? day
}
