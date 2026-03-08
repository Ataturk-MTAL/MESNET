import { ref, computed, type Ref, type ComputedRef } from 'vue'
import {
  coordinationApi,
  type BranchWorkloadConfigDto,
} from 'src/api/coordination'
import { enrollmentApi } from 'src/api/enrollment'
import type { useNotify } from 'src/composables/useNotify'

export interface ClassLevelRow {
  classYear: number
  weeklyLessonHours: number
  studentCount: number
}

export const EDUCATION_TYPES = [
  { label: 'Örgün', value: 'Formal' },
  { label: 'MESEM', value: 'Mesem' },
] as const

/**
 * Norm Kadro Yönetmeliği Madde 22'ye göre grup sayısı hesapla.
 * Pure function — composable dışından da kullanılabilir.
 */
export function estimateGroupCount(educationType: string, classYear: number, studentCount: number): number {
  if (studentCount <= 0) return 0
  if (educationType === 'Mesem') {
    if (studentCount < 10) return 0
    if (studentCount < 41) return 1
    if (studentCount < 81) return 2
    if (studentCount < 121) return 3
    if (studentCount < 161) return 4
    if (studentCount < 201) return 5
    if (studentCount < 241) return 6
    if (studentCount < 281) return 7
    if (studentCount < 321) return 8
    if (studentCount < 361) return 9
    if (studentCount < 401) return 10
    if (studentCount < 441) return 11
    return 12
  }
  // Formal
  if (classYear === 9) {
    if (studentCount < 10) return 0
    if (studentCount < 21) return 1
    if (studentCount < 31) return 2
    return 3
  }
  if (classYear >= 10 && classYear <= 12) {
    if (studentCount < 8) return 0
    if (studentCount < 17) return 1
    if (studentCount < 25) return 2
    if (studentCount < 33) return 3
    return 4
  }
  return 0
}

export interface UseWorkloadConfigOptions {
  branchFilter: Ref<string | null>
  periodId: ComputedRef<string | null> | Ref<string | null>
  institutionId: ComputedRef<string | undefined> | Ref<string | undefined>
  notify: ReturnType<typeof useNotify>
}

export function useWorkloadConfig(options: UseWorkloadConfigOptions) {
  const { branchFilter, periodId, institutionId, notify } = options

  const workloadConfig = ref<BranchWorkloadConfigDto | null>(null)
  const workloadLoading = ref(false)
  const workloadSaving = ref(false)
  const syncingCounts = ref(false)

  // Editable form state
  const wlEducationType = ref('Formal')
  const wlDeptHeadCount = ref(1)
  const wlWorkshopHeadCount = ref(0)
  const wlDeptHeadHours = ref(10)
  const wlWorkshopHeadHours = ref(6)
  const wlClassLevels = ref<ClassLevelRow[]>([
    { classYear: 10, weeklyLessonHours: 8, studentCount: 0 },
    { classYear: 11, weeklyLessonHours: 8, studentCount: 0 },
    { classYear: 12, weeklyLessonHours: 8, studentCount: 0 },
  ])

  // Computed totals
  const wlSupervisorTotal = computed(() =>
    (wlDeptHeadCount.value * wlDeptHeadHours.value) + (wlWorkshopHeadCount.value * wlWorkshopHeadHours.value),
  )

  const wlTeachingTotal = computed(() =>
    wlClassLevels.value.reduce((sum, cl) => {
      const groups = estimateGroupCount(wlEducationType.value, cl.classYear, cl.studentCount)
      return sum + cl.weeklyLessonHours * groups
    }, 0),
  )

  const wlPoolTotal = computed(() => wlSupervisorTotal.value + wlTeachingTotal.value)

  function applySyncCounts(counts: Record<string, Record<string, number>>) {
    const key = `${branchFilter.value}:${wlEducationType.value}`
    const classCounts = counts[key]
    if (!classCounts) return

    wlClassLevels.value = wlClassLevels.value.map(cl => ({
      ...cl,
      studentCount: classCounts[String(cl.classYear)] ?? 0,
    }))
  }

  async function doAutoSync() {
    const instId = institutionId.value
    const pid = periodId.value
    if (!instId || !pid) return
    syncingCounts.value = true
    try {
      const res = await enrollmentApi.syncStudentCounts(instId, pid)
      applySyncCounts(res.data.counts)
    } catch {
      // Sessizce başarısız — kullanıcı manuel butonla deneyebilir
    } finally {
      syncingCounts.value = false
    }
  }

  async function loadWorkloadConfig() {
    if (!branchFilter.value || !periodId.value) return
    workloadLoading.value = true
    try {
      const res = await coordinationApi.getBranchWorkloadConfig(
        branchFilter.value,
        periodId.value,
        wlEducationType.value,
      )
      const data = res.data
      if (data && data.id) {
        workloadConfig.value = data
        wlEducationType.value = data.educationType
        wlDeptHeadCount.value = data.departmentHeadCount
        wlWorkshopHeadCount.value = data.workshopHeadCount
        wlDeptHeadHours.value = data.departmentHeadHours
        wlWorkshopHeadHours.value = data.workshopHeadHours
        wlClassLevels.value = data.classLevels.map(cl => ({
          classYear: cl.classYear,
          weeklyLessonHours: cl.weeklyLessonHours,
          studentCount: cl.studentCount,
        }))

        // Tüm sınıf sayıları 0 ise BranchStudentCountView henüz doldurulmamış — otomatik senkronize et
        const allZero = wlClassLevels.value.every(cl => cl.studentCount === 0)
        if (allZero && !syncingCounts.value) {
          await doAutoSync()
        }
      } else {
        workloadConfig.value = null
      }
    } catch {
      workloadConfig.value = null
    } finally {
      workloadLoading.value = false
    }
  }

  async function saveWorkloadConfig() {
    if (!branchFilter.value || !periodId.value) return
    workloadSaving.value = true
    try {
      await coordinationApi.upsertBranchWorkloadConfig(branchFilter.value, {
        academicPeriodId: periodId.value,
        educationType: wlEducationType.value,
        departmentHeadCount: wlDeptHeadCount.value,
        workshopHeadCount: wlWorkshopHeadCount.value,
        departmentHeadHours: wlDeptHeadHours.value,
        workshopHeadHours: wlWorkshopHeadHours.value,
        classLevels: wlClassLevels.value.map(cl => ({
          classYear: cl.classYear,
          weeklyLessonHours: cl.weeklyLessonHours,
        })),
      })
      notify.success('Alan ders yükü yapılandırması kaydedildi.')
      await loadWorkloadConfig()
    } catch (e: unknown) {
      const msg = e instanceof Error ? e.message : 'Bilinmeyen hata'
      notify.error(`Kaydetme hatası: ${msg}`)
    } finally {
      workloadSaving.value = false
    }
  }

  async function syncStudentCounts() {
    const instId = institutionId.value
    const pid = periodId.value
    if (!instId || !pid) return
    syncingCounts.value = true
    try {
      const res = await enrollmentApi.syncStudentCounts(instId, pid)
      applySyncCounts(res.data.counts)
      notify.success('Öğrenci sayıları senkronize edildi.')
    } catch (e: unknown) {
      const msg = e instanceof Error ? e.message : 'Bilinmeyen hata'
      notify.error(`Senkronizasyon hatası: ${msg}`)
    } finally {
      syncingCounts.value = false
    }
  }

  return {
    workloadConfig,
    workloadLoading,
    workloadSaving,
    syncingCounts,
    wlEducationType,
    wlDeptHeadCount,
    wlWorkshopHeadCount,
    wlDeptHeadHours,
    wlWorkshopHeadHours,
    wlClassLevels,
    wlSupervisorTotal,
    wlTeachingTotal,
    wlPoolTotal,
    loadWorkloadConfig,
    saveWorkloadConfig,
    syncStudentCounts,
  }
}
