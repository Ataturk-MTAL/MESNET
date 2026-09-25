import { computed } from 'vue'
import { useStudentOptions, useBusinessOptions } from 'src/composables/useEntityOptions'

/**
 * Tablo satırındaki öğrenci/işletme kimliğini ada çevirir (frontend lookup map deseni —
 * backend DTO'larına isim eklenmez). Seçenek listeleri önbellekli store'dan gelir.
 *
 * <p>Neden var: kimliği gösterilmeyen satır "kimin kaydı?" sorusunu cevapsız bırakır —
 * Beceri Sınavları ve Faaliyet Raporları listeleri yalnız tarih/puan/durum gösteriyordu.</p>
 */
export function useEntityNames() {
  const studentOpts = useStudentOptions()
  const businessOpts = useBusinessOptions()

  const studentNames = computed(
    () => new Map(studentOpts.allOptions.value.map((o) => [o.value, o.label])),
  )
  const businessNames = computed(
    () => new Map(businessOpts.allOptions.value.map((o) => [o.value, o.label])),
  )

  /** Ad bilinmiyorsa (liste henüz yüklenmedi / kayıt silindi) tire döner — kimlik gösterilmez. */
  function studentName(id: string | null | undefined): string {
    return (id && studentNames.value.get(id)) || '—'
  }

  function businessName(id: string | null | undefined): string {
    return (id && businessNames.value.get(id)) || '—'
  }

  async function load(): Promise<void> {
    await Promise.all([studentOpts.load(), businessOpts.load()])
  }

  return { studentName, businessName, load }
}
