<template>
  <q-page padding>
    <div class="form-page q-mx-auto">
      <div class="row items-center q-mb-lg">
        <q-btn
          flat
          round
          dense
          icon="arrow_back"
          aria-label="Faaliyet raporlarına dön"
          class="q-mr-sm"
          @click="goBack"
        >
          <q-tooltip>Faaliyet raporlarına dön</q-tooltip>
        </q-btn>
        <h1 class="text-h5 text-weight-bold col q-my-none">
          {{ isEdit ? 'Faaliyet Raporunu Düzenle' : 'Faaliyet Raporu Oluştur' }}
        </h1>
      </div>

      <AppNotice
        v-if="periodStore.isReadOnly"
        type="readonly"
        message="Seçili akademik dönem kapalı; bu dönemde rapor oluşturulamaz ve düzenlenemez."
        class="q-mb-md"
      />
      <AppNotice
        v-if="isLockedStatus"
        type="readonly"
        message="Bu rapor gönderildi; yalnız taslak raporlar düzenlenebilir."
        class="q-mb-md"
      />
      <AppNotice
        v-if="loadError"
        type="error"
        :message="loadError"
        class="q-mb-md"
      />

      <q-card
        flat
        bordered
      >
        <q-card-section>
          <SubjectHeader
            title="Aylık Faaliyet Raporu"
            :name="studentLabel"
            :context="subjectContext"
            placeholder="Öğrenci seçilmedi"
            :editing="isEdit"
          />

          <div class="form-grid">
            <q-select
              v-if="!isEdit"
              v-model="form.studentId"
              :options="placementOpts.options.value"
              :loading="placementOpts.loading.value"
              label="Öğrenci *"
              hint="Yalnız bir işletmeye yerleşmiş öğrenciler listelenir."
              outlined
              use-input
              hide-selected
              fill-input
              input-debounce="0"
              emit-value
              map-options
              class="form-grid__wide"
              @filter="placementOpts.filter"
              @update:model-value="onStudentChange"
            >
              <template #prepend>
                <q-icon name="person" />
              </template>
              <template #option="{ itemProps, opt }">
                <q-item v-bind="itemProps">
                  <q-item-section>
                    <q-item-label>{{ opt.label }}</q-item-label>
                    <q-item-label caption>
                      {{ opt.caption }}
                    </q-item-label>
                  </q-item-section>
                </q-item>
              </template>
              <template #no-option>
                <SelectEmptyOption />
              </template>
            </q-select>

            <CoordinatorTeacherField
              v-model="form.teacherId"
              :locked="isEdit"
              class="form-grid__wide"
            />

            <q-select
              v-model="form.month"
              :options="monthOptions"
              label="Ay *"
              outlined
              emit-value
              map-options
              :disable="isEdit"
            >
              <template #prepend>
                <q-icon name="calendar_month" />
              </template>
            </q-select>
            <q-input
              v-model.number="form.year"
              label="Yıl *"
              type="number"
              outlined
              :disable="isEdit"
            />
          </div>
        </q-card-section>

        <q-separator />

        <q-card-section>
          <div class="row items-center q-mb-sm">
            <div class="col text-subtitle1 text-weight-medium">
              Günlük Faaliyetler
            </div>
            <q-btn
              flat
              color="primary"
              icon="add"
              label="Gün ekle"
              :disable="isLockedStatus"
              @click="addDay"
            />
          </div>

          <div
            v-for="(activity, index) in form.activities"
            :key="index"
            class="row-editor"
          >
            <q-input
              v-model.number="activity.dayNumber"
              label="Gün"
              type="number"
              min="1"
              :max="lastDay"
              outlined
              dense
              class="row-editor__num"
              :disable="isLockedStatus"
            />
            <q-input
              v-model="activity.description"
              label="Yapılan iş"
              outlined
              dense
              autogrow
              class="row-editor__main"
              :disable="isLockedStatus"
            />
            <q-btn
              flat
              round
              dense
              icon="delete"
              color="negative"
              :aria-label="`${activity.dayNumber}. günü sil`"
              :disable="isLockedStatus"
              @click="form.activities.splice(index, 1)"
            >
              <q-tooltip>Günü sil</q-tooltip>
            </q-btn>
          </div>

          <div
            v-if="form.activities.length === 0"
            class="text-body2 text-grey-7"
          >
            Henüz gün girilmedi. Öğrencinin işletmede çalıştığı her gün için yapılan işi yazın.
          </div>
        </q-card-section>

        <q-separator />

        <q-card-section class="form-grid">
          <q-input
            v-model="form.instructorComment"
            label="Usta Öğretici Görüşü"
            type="textarea"
            rows="3"
            outlined
            :disable="isLockedStatus"
          />
          <q-input
            v-model="form.teacherComment"
            label="Koordinatör Öğretmen Görüşü"
            type="textarea"
            rows="3"
            outlined
            :disable="isLockedStatus"
          />
        </q-card-section>

        <q-separator />
        <q-card-actions
          align="right"
          class="q-pa-md"
        >
          <span
            v-if="problem && !isLockedStatus"
            class="text-caption text-negative q-mr-md"
            role="status"
          >{{ problem }}</span>
          <q-btn
            flat
            label="İptal"
            color="grey-7"
            @click="goBack"
          />
          <q-btn
            unelevated
            color="primary"
            label="Kaydet"
            :loading="saving"
            :disable="!!problem || periodStore.isReadOnly || isLockedStatus"
            @click="handleSave"
          />
        </q-card-actions>
      </q-card>
    </div>
  </q-page>
</template>

<script setup lang="ts">
/**
 * Aylık faaliyet raporu oluşturma / düzenleme formu (route: `/coordination/activity-reports/new`,
 * `/coordination/activity-reports/:id/edit`).
 *
 * Rapor taslak olarak doğar; gönderme ve onay liste sayfasındaki satır eylemleridir.
 * Backend yalnız TASLAK raporu günceller (`ReportNotDraft`), bu yüzden gönderilmiş rapor
 * burada salt okunur açılır. Düzenlemede öğrenci, öğretmen, ay ve yıl değişmez.
 */
import { ref, reactive, computed, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { coordinationApi, type DailyActivity } from 'src/api/coordination'
import { useNotify } from 'src/composables/useNotify'
import { usePlacementOptions } from 'src/composables/useEntityOptions'
import { useEntityNames } from 'src/composables/useEntityNames'
import { useAuthStore } from 'stores/auth'
import { useAcademicPeriodStore } from 'stores/academicPeriod'
import { activitiesProblem, daysInMonth } from 'utils/coordinationRecords'
import AppNotice from 'components/AppNotice.vue'
import CoordinatorTeacherField from 'components/CoordinatorTeacherField.vue'
import SelectEmptyOption from 'components/SelectEmptyOption.vue'
import SubjectHeader from 'components/SubjectHeader.vue'

const MONTHS_IN_YEAR = 12

const route = useRoute()
const router = useRouter()
const notify = useNotify()
const authStore = useAuthStore()
const periodStore = useAcademicPeriodStore()
const placementOpts = usePlacementOptions()
const names = useEntityNames()

const reportId = computed(() => (route.params.id as string | undefined) ?? null)
const isEdit = computed(() => reportId.value !== null)

const saving = ref(false)
const loadError = ref<string | null>(null)
const status = ref<string>('Draft')
const isLockedStatus = computed(() => status.value !== 'Draft')

const monthFormat = new Intl.DateTimeFormat('tr-TR', { month: 'long' })
const monthOptions = Array.from({ length: MONTHS_IN_YEAR }, (_, i) => ({
  label: monthFormat.format(new Date(2000, i, 1)),
  value: i + 1,
}))

const today = new Date()
const form = reactive({
  studentId: null as string | null,
  businessId: null as string | null,
  teacherId: null as string | null,
  year: today.getFullYear(),
  month: today.getMonth() + 1,
  activities: [] as DailyActivity[],
  instructorComment: '',
  teacherComment: '',
})

const lastDay = computed(() => daysInMonth(form.year, form.month))

const studentLabel = computed(() => (form.studentId ? names.studentName(form.studentId) : null))
const subjectContext = computed(() => {
  const business = form.businessId ? names.businessName(form.businessId) : null
  const period = `${monthFormat.format(new Date(form.year, form.month - 1, 1))} ${form.year}`
  return business ? `${business} · ${period}` : period
})

const problem = computed(() => {
  if (!form.studentId || !form.businessId) return 'Öğrenci seçilmelidir.'
  if (!form.teacherId) return 'Koordinatör öğretmen belirlenmelidir.'
  return activitiesProblem(form.activities, form.year, form.month)
})

function onStudentChange(studentId: string | null) {
  form.businessId = studentId ? placementOpts.getBusinessForStudent(studentId)?.businessId ?? null : null
}

/** Sıradaki boş günü önerir; ay doluysa son günü. */
function addDay() {
  const used = new Set(form.activities.map((a) => a.dayNumber))
  let day = 1
  while (used.has(day) && day < lastDay.value) day++
  form.activities.push({ dayNumber: day, description: '' })
}

function goBack() {
  router.push({ name: 'ActivityReports' }).catch(() => {})
}

function editablePart() {
  return {
    activities: [...form.activities]
      .sort((a, b) => a.dayNumber - b.dayNumber)
      .map((a) => ({ dayNumber: a.dayNumber, description: a.description.trim() })),
    instructorComment: form.instructorComment.trim() || undefined,
    teacherComment: form.teacherComment.trim() || undefined,
  }
}

async function handleSave() {
  if (problem.value) return
  saving.value = true
  try {
    if (reportId.value) {
      await coordinationApi.updateActivityReport(reportId.value, editablePart())
      notify.success('Faaliyet raporu güncellendi.')
    } else {
      await coordinationApi.createActivityReport({
        ...editablePart(),
        studentId: form.studentId!,
        businessId: form.businessId!,
        teacherId: form.teacherId!,
        institutionId: authStore.currentInstitutionId ?? '',
        academicPeriodId: periodStore.selectedPeriodId ?? '',
        year: form.year,
        month: form.month,
      })
      notify.success('Faaliyet raporu taslak olarak oluşturuldu.')
    }
    goBack()
  } catch (e) {
    notify.apiError(e, 'Faaliyet raporu kaydedilirken bir hata oluştu.')
  } finally {
    saving.value = false
  }
}

async function loadReport(id: string) {
  try {
    const { data } = await coordinationApi.getActivityReport(id)
    form.studentId = data.studentId
    form.businessId = data.businessId
    form.teacherId = data.teacherId
    form.year = data.year
    form.month = data.month
    form.activities = data.activities.map((a) => ({ ...a }))
    form.instructorComment = data.instructorComment ?? ''
    form.teacherComment = data.teacherComment ?? ''
    status.value = data.status
  } catch (e) {
    loadError.value = 'Rapor yüklenemedi.'
    notify.apiError(e, 'Rapor yüklenemedi.')
  }
}

onMounted(() => {
  names.load().catch(() => {})
  if (reportId.value) {
    loadReport(reportId.value).catch(() => {})
  } else {
    placementOpts.load({ academicPeriodId: periodStore.selectedPeriodId ?? undefined }).catch(() => {})
  }
})
</script>

<style lang="scss" scoped>
.form-page {
  max-width: 800px;
}

.form-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(min(100%, 14rem), 1fr));
  gap: $space-base;
}

.form-grid__wide {
  grid-column: 1 / -1;
}

.row-editor {
  display: flex;
  flex-wrap: wrap;
  align-items: flex-start;
  gap: $space-x-base * 0.5;
  margin-bottom: $space-y-base * 0.5;
}

.row-editor__main {
  flex: 1 1 16rem;
}

.row-editor__num {
  flex: 0 1 6rem;
}
</style>
