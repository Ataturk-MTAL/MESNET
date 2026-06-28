import { ref, type Ref } from 'vue'
import { coordinationApi, type DailyScheduleDto } from 'src/api/coordination'

export interface UseTeacherScheduleLoaderOptions {
  // Seçili dönem (null ise yükleme yapılmaz)
  periodId: Ref<string | null>
  // Seçili yarıyıl (API çağrısına geçilir)
  semester: Ref<string>
  // Config eksik/hata durumunda boş program üretir (useScheduleConfig'ten gelir)
  createEmptySchedule: () => DailyScheduleDto[]
}

/**
 * Bir öğretmenin mevcut haftalık ders programını yükler.
 * Öğretmen-programı state'ini (rawSchedule) ve yükleme bayrağını (scheduleLoading)
 * sahiplenir. Hata durumunda davranış korunur: boş program'a düşülür.
 */
export function useTeacherScheduleLoader(options: UseTeacherScheduleLoaderOptions) {
  const { periodId, semester, createEmptySchedule } = options

  // ── State ──
  const scheduleLoading = ref(false)
  const rawSchedule = ref<DailyScheduleDto[]>([])

  // ── Action: öğretmenin güncel programını çek ──
  async function loadTeacherSchedule(teacherId: string) {
    if (!periodId.value) return
    scheduleLoading.value = true
    try {
      const { data } = await coordinationApi.getCurrentSchedule(
        teacherId,
        periodId.value,
        semester.value,
      )
      rawSchedule.value = data.weeklySchedule
    } catch {
      rawSchedule.value = createEmptySchedule()
    } finally {
      scheduleLoading.value = false
    }
  }

  return {
    scheduleLoading,
    rawSchedule,
    loadTeacherSchedule,
  }
}
