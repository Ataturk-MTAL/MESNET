<template>
  <q-page padding>
    <div class="text-h5 text-weight-bold q-mb-lg">Raporlar</div>

    <div class="row q-col-gutter-md">
      <!-- Staj Raporu -->
      <div class="col-12 col-md-4">
        <q-card flat bordered>
          <q-card-section>
            <div class="row items-center q-mb-md">
              <q-icon name="work_history" size="32px" color="primary" class="q-mr-md" />
              <div>
                <div class="text-subtitle1 text-weight-medium">Staj Raporu</div>
                <div class="text-caption text-grey">Yerleştirme ve staj istatistikleri</div>
              </div>
            </div>
            <q-input v-model="internshipFilters.year" label="Yıl" filled dense type="number" class="q-mb-sm" />
            <q-input v-model="internshipFilters.branchCode" label="Alan Kodu (opsiyonel)" filled dense class="q-mb-md" />
            <q-btn
              color="primary"
              icon="download"
              label="PDF İndir"
              :loading="downloading.internship"
              class="full-width"
              @click="downloadInternship"
            />
          </q-card-section>
        </q-card>
      </div>

      <!-- Devamsızlık Raporu -->
      <div class="col-12 col-md-4">
        <q-card flat bordered>
          <q-card-section>
            <div class="row items-center q-mb-md">
              <q-icon name="event_busy" size="32px" color="orange" class="q-mr-md" />
              <div>
                <div class="text-subtitle1 text-weight-medium">Devamsızlık Raporu</div>
                <div class="text-caption text-grey">Devamsızlık özeti ve detayları</div>
              </div>
            </div>
            <q-input v-model="attendanceFilters.month" label="Ay (YYYY-MM)" filled dense class="q-mb-sm" />
            <q-select
              v-model="attendanceFilters.studentId"
              :options="studentOpts.options.value"
              :loading="studentOpts.loading.value"
              label="Öğrenci (opsiyonel)"
              filled
              dense
              use-input
              input-debounce="0"
              emit-value
              map-options
              option-label="label"
              option-value="value"
              clearable
              class="q-mb-md"
              @filter="studentOpts.filter"
            >
              <template #option="{ itemProps, opt }">
                <q-item v-bind="itemProps">
                  <q-item-section>
                    <q-item-label>{{ opt.label }}</q-item-label>
                    <q-item-label caption v-if="opt.caption">{{ opt.caption }}</q-item-label>
                  </q-item-section>
                </q-item>
              </template>
              <template #no-option>
                <q-item>
                  <q-item-section class="text-grey">Sonuç bulunamadı</q-item-section>
                </q-item>
              </template>
            </q-select>
            <q-btn
              color="orange"
              icon="download"
              label="PDF İndir"
              :loading="downloading.attendance"
              class="full-width"
              @click="downloadAttendance"
            />
          </q-card-section>
        </q-card>
      </div>

      <!-- Maaş Raporu -->
      <div class="col-12 col-md-4">
        <q-card flat bordered>
          <q-card-section>
            <div class="row items-center q-mb-md">
              <q-icon name="payments" size="32px" color="green" class="q-mr-md" />
              <div>
                <div class="text-subtitle1 text-weight-medium">Maaş Raporu</div>
                <div class="text-caption text-grey">Ödeme ve dekont özeti</div>
              </div>
            </div>
            <q-input v-model="paymentFilters.month" label="Ay (YYYY-MM)" filled dense class="q-mb-md" />
            <q-btn
              color="green"
              icon="download"
              label="PDF İndir"
              :loading="downloading.payment"
              class="full-width"
              @click="downloadPayment"
            />
          </q-card-section>
        </q-card>
      </div>
    </div>
  </q-page>
</template>

<script setup lang="ts">
import { reactive, onMounted } from 'vue'
import { reportingApi, downloadBlob } from 'src/api/reporting'
import { useNotify } from 'src/composables/useNotify'
import { useStudentOptions } from 'src/composables/useEntityOptions'
import { useAuthStore } from 'stores/auth'

const notify = useNotify()
const authStore = useAuthStore()
const institutionId = authStore.user?.institutionId ?? ''
const studentOpts = useStudentOptions()

onMounted(() => studentOpts.load())

const downloading = reactive({
  internship: false,
  attendance: false,
  payment: false,
})

const internshipFilters = reactive({ year: new Date().getFullYear(), branchCode: '' })
const attendanceFilters = reactive({ month: '', studentId: '' })
const paymentFilters = reactive({ month: '' })

async function downloadInternship() {
  downloading.internship = true
  try {
    const res = await reportingApi.getInternshipReport(institutionId, {
      year: internshipFilters.year,
      branchCode: internshipFilters.branchCode || undefined,
    })
    downloadBlob(res.data as Blob, `staj-raporu-${internshipFilters.year}.pdf`)
  } catch {
    notify.error('Rapor oluşturulurken bir hata oluştu.')
  } finally {
    downloading.internship = false
  }
}

async function downloadAttendance() {
  downloading.attendance = true
  try {
    const res = await reportingApi.getAttendanceReport(institutionId, {
      month: attendanceFilters.month || undefined,
      studentId: attendanceFilters.studentId || undefined,
    })
    downloadBlob(res.data as Blob, `devamsizlik-raporu-${attendanceFilters.month || 'tum'}.pdf`)
  } catch {
    notify.error('Rapor oluşturulurken bir hata oluştu.')
  } finally {
    downloading.attendance = false
  }
}

async function downloadPayment() {
  downloading.payment = true
  try {
    const res = await reportingApi.getPaymentReport(institutionId, {
      month: paymentFilters.month || undefined,
    })
    downloadBlob(res.data as Blob, `maas-raporu-${paymentFilters.month || 'tum'}.pdf`)
  } catch {
    notify.error('Rapor oluşturulurken bir hata oluştu.')
  } finally {
    downloading.payment = false
  }
}
</script>
