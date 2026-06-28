import { ref, reactive, computed, type Ref } from 'vue'
import { reportingApi } from 'src/api/reporting'
import type { useNotify } from 'src/composables/useNotify'
import type { useAuthStore } from 'stores/auth'
import type { useAcademicPeriodStore } from 'stores/academicPeriod'

export interface UseReportingBatchGenerateOptions {
  institutionName: Ref<string | undefined>
  authStore: ReturnType<typeof useAuthStore>
  periodStore: ReturnType<typeof useAcademicPeriodStore>
  notify: ReturnType<typeof useNotify>
  load: () => Promise<void>
}

export function useReportingBatchGenerate(options: UseReportingBatchGenerateOptions) {
  const { institutionName, authStore, periodStore, notify, load } = options

  const generating = ref(false)
  const previewing = ref(false)
  const showGenerateDialog = ref(false)

  // Belge oluşturma dialog formu
  const now = new Date()
  const batchForm = reactive({
    formType: 'MonthlyAttendanceReport' as string,
    year: now.getFullYear(),
    month: now.getMonth() + 1,
  })

  const yearOptions = computed(() => {
    const currentYear = now.getFullYear()
    return [currentYear - 1, currentYear, currentYear + 1].map((y) => ({
      value: y,
      label: String(y),
    }))
  })

  const monthOptions = [
    { value: 1, label: 'Ocak' },
    { value: 2, label: 'Şubat' },
    { value: 3, label: 'Mart' },
    { value: 4, label: 'Nisan' },
    { value: 5, label: 'Mayıs' },
    { value: 6, label: 'Haziran' },
    { value: 7, label: 'Temmuz' },
    { value: 8, label: 'Ağustos' },
    { value: 9, label: 'Eylül' },
    { value: 10, label: 'Ekim' },
    { value: 11, label: 'Kasım' },
    { value: 12, label: 'Aralık' },
  ]

  async function previewBatchMonthlyAttendance() {
    const institutionId = authStore.user?.institutionId
    const periodId = periodStore.selectedPeriodId
    const period = periodStore.selectedPeriod

    if (!institutionId || !periodId || !period) {
      notify.error('Kurum veya akademik dönem bilgisi bulunamadı.')
      return
    }

    previewing.value = true
    try {
      const res = await reportingApi.previewBatchMonthlyAttendance({
        institutionId,
        academicPeriodId: periodId,
        year: batchForm.year,
        month: batchForm.month,
        institutionName: institutionName.value ?? '',
        academicYear: `${period.startYear} / ${period.endYear}`,
      })
      const blob = new Blob([res.data as BlobPart], { type: 'application/pdf' })
      const url = URL.createObjectURL(blob)
      window.open(url, '_blank')
      setTimeout(() => URL.revokeObjectURL(url), 60000)
    } catch (e) {
      notify.apiError(e, 'Önizleme oluşturulurken bir hata oluştu.')
    } finally {
      previewing.value = false
    }
  }

  async function generateBatch() {
    const institutionId = authStore.user?.institutionId
    const periodId = periodStore.selectedPeriodId
    const period = periodStore.selectedPeriod

    if (!institutionId || !periodId || !period) {
      notify.error('Kurum veya akademik dönem bilgisi bulunamadı.')
      return
    }

    generating.value = true
    try {
      const res = await reportingApi.generateBatch({
        formType: batchForm.formType,
        year: batchForm.year,
        month: batchForm.month,
        institutionId,
        academicPeriodId: periodId,
        academicYear: `${period.startYear} / ${period.endYear}`,
        institutionName: institutionName.value,
      })
      const result = (res.data as any)?.data ?? res.data
      if (result.generated > 0) {
        notify.success(`${result.generated} yeni belge oluşturuldu, ${result.skipped} belge zaten mevcuttu.`)
      } else {
        notify.info('Tüm belgeler zaten oluşturulmuş — yeni belge üretilmedi.')
      }
      showGenerateDialog.value = false
      await load()
    } catch (e) {
      notify.apiError(e, 'Belge oluşturulurken bir hata oluştu.')
    } finally {
      generating.value = false
    }
  }

  return {
    generating,
    previewing,
    showGenerateDialog,
    batchForm,
    yearOptions,
    monthOptions,
    previewBatchMonthlyAttendance,
    generateBatch,
  }
}
