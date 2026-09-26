<template>
  <q-page padding>
    <div class="form-page q-mx-auto">
      <div class="row items-center q-mb-lg">
        <q-btn
          flat
          round
          dense
          icon="arrow_back"
          aria-label="Rehberlik ziyaretlerine dön"
          class="q-mr-sm"
          @click="goBack"
        >
          <q-tooltip>Rehberlik ziyaretlerine dön</q-tooltip>
        </q-btn>
        <h1 class="text-h5 text-weight-bold col q-my-none">
          {{ isEdit ? 'Rehberlik Ziyaretini Düzenle' : 'Rehberlik Ziyareti Ekle' }}
        </h1>
      </div>

      <AppNotice
        v-if="periodStore.isReadOnly"
        type="readonly"
        message="Seçili akademik dönem kapalı; bu dönemde ziyaret eklenemez ve düzenlenemez."
        class="q-mb-md"
      />
      <AppNotice
        v-if="isLockedStatus"
        type="readonly"
        message="Bu ziyaret raporu gönderildi; yalnız taslak ziyaretler düzenlenebilir."
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
            title="Rehberlik Ziyareti"
            :name="businessLabel"
            :context="visitDateLabel"
            placeholder="İşletme seçilmedi"
            :editing="isEdit"
          />

          <div class="form-grid">
            <q-select
              v-if="!isEdit"
              v-model="form.businessId"
              :options="businessOptions"
              :loading="placementOpts.loading.value"
              label="İşletme *"
              hint="Yalnız öğrenci yerleşmiş işletmeler listelenir."
              outlined
              use-input
              hide-selected
              fill-input
              input-debounce="0"
              emit-value
              map-options
              class="form-grid__wide"
              @filter="filterBusinesses"
              @update:model-value="onBusinessChange"
            >
              <template #prepend>
                <q-icon name="business" />
              </template>
              <template #no-option>
                <SelectEmptyOption />
              </template>
            </q-select>

            <CoordinatorTeacherField
              v-model="form.teacherId"
              :locked="isEdit"
            />

            <q-input
              v-model="form.visitDate"
              label="Ziyaret Tarihi *"
              outlined
              type="date"
              :disable="isLockedStatus"
            >
              <template #prepend>
                <q-icon name="event" />
              </template>
            </q-input>
          </div>
        </q-card-section>

        <q-separator />

        <q-card-section>
          <div class="text-subtitle1 text-weight-medium q-mb-xs">
            Öğrenci Gözlemleri
          </div>
          <div class="text-caption text-grey-7 q-mb-md">
            Gördüğünüz her öğrencinin performansını yazın. Boş bırakılan öğrenci rapora girmez.
          </div>

          <div
            v-for="note in form.studentNotes"
            :key="note.studentId"
            class="q-mb-sm"
          >
            <q-input
              v-model="note.performanceNote"
              :label="names.studentName(note.studentId)"
              type="textarea"
              rows="2"
              autogrow
              outlined
              :disable="isLockedStatus"
            >
              <template #prepend>
                <q-icon name="person" />
              </template>
            </q-input>
          </div>

          <div
            v-if="form.studentNotes.length === 0"
            class="text-body2 text-grey-7"
          >
            {{ form.businessId ? 'Bu işletmede bu dönem yerleşik öğrenci bulunamadı.' : 'Önce işletme seçin.' }}
          </div>
        </q-card-section>

        <q-separator />

        <q-card-section class="form-grid">
          <q-input
            v-model="form.instructorMeetingNotes"
            label="Usta Öğreticiyle Görüşme"
            type="textarea"
            rows="3"
            outlined
            :disable="isLockedStatus"
          />
          <q-input
            v-model="form.issuesIdentified"
            label="Tespit Edilen Sorunlar"
            type="textarea"
            rows="3"
            outlined
            :disable="isLockedStatus"
          />
          <q-input
            v-model="form.actionsTaken"
            label="Alınan Önlemler"
            type="textarea"
            rows="3"
            outlined
            :disable="isLockedStatus"
          />
          <q-input
            v-model="form.generalAssessment"
            label="Genel Değerlendirme"
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
 * Rehberlik (işletme) ziyareti oluşturma / düzenleme formu (route:
 * `/coordination/guidance-visits/new`, `/coordination/guidance-visits/:id/edit`).
 *
 * Ziyaret işletmeye yapılır, gözlem öğrenci başınadır: işletme seçilince o işletmede
 * yerleşik öğrenciler not satırı olarak gelir. Ziyaret taslak doğar; gönderme ve onay
 * liste sayfasındaki satır eylemleridir. Backend yalnız taslağı günceller (`VisitNotDraft`).
 */
import { ref, reactive, computed, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { coordinationApi, type StudentVisitNote } from 'src/api/coordination'
import { useNotify } from 'src/composables/useNotify'
import { usePlacementOptions } from 'src/composables/useEntityOptions'
import { useEntityNames } from 'src/composables/useEntityNames'
import { useAuthStore } from 'stores/auth'
import { useAcademicPeriodStore } from 'stores/academicPeriod'
import { studentsAtBusiness } from 'utils/coordinationRecords'
import AppNotice from 'components/AppNotice.vue'
import CoordinatorTeacherField from 'components/CoordinatorTeacherField.vue'
import SelectEmptyOption from 'components/SelectEmptyOption.vue'
import SubjectHeader from 'components/SubjectHeader.vue'

const route = useRoute()
const router = useRouter()
const notify = useNotify()
const authStore = useAuthStore()
const periodStore = useAcademicPeriodStore()
const placementOpts = usePlacementOptions()
const names = useEntityNames()

const visitId = computed(() => (route.params.id as string | undefined) ?? null)
const isEdit = computed(() => visitId.value !== null)

const saving = ref(false)
const loadError = ref<string | null>(null)
const status = ref<string>('Draft')
const isLockedStatus = computed(() => status.value !== 'Draft')

const form = reactive({
  businessId: null as string | null,
  teacherId: null as string | null,
  visitDate: new Date().toISOString().slice(0, 10),
  studentNotes: [] as StudentVisitNote[],
  instructorMeetingNotes: '',
  issuesIdentified: '',
  actionsTaken: '',
  generalAssessment: '',
})

// Yerleştirmelerden türetilen işletme listesi — öğrencisi olmayan işletmeye rehberlik
// ziyareti anlamsızdır.
const allBusinessOptions = computed(() => {
  const seen = new Map<string, string>()
  for (const p of placementOpts.allOptions.value) seen.set(p.businessId, p.businessName)
  return [...seen].map(([value, label]) => ({ value, label }))
    .sort((a, b) => a.label.localeCompare(b.label, 'tr'))
})
const businessNeedle = ref('')
const businessOptions = computed(() => {
  const needle = businessNeedle.value.toLocaleLowerCase('tr')
  return needle
    ? allBusinessOptions.value.filter((o) => o.label.toLocaleLowerCase('tr').includes(needle))
    : allBusinessOptions.value
})

function filterBusinesses(val: string, update: (fn: () => void) => void) {
  update(() => {
    businessNeedle.value = val
  })
}

const businessLabel = computed(() => (form.businessId ? names.businessName(form.businessId) : null))
const visitDateLabel = computed(() =>
  form.visitDate
    ? new Date(form.visitDate).toLocaleDateString('tr-TR', { day: 'numeric', month: 'long', year: 'numeric' })
    : null,
)

const writtenNotes = computed(() => form.studentNotes
  .map((n) => ({ studentId: n.studentId, performanceNote: n.performanceNote.trim() }))
  .filter((n) => n.performanceNote))

const problem = computed(() => {
  if (!form.businessId) return 'İşletme seçilmelidir.'
  if (!form.teacherId) return 'Koordinatör öğretmen belirlenmelidir.'
  if (!form.visitDate) return 'Ziyaret tarihi girilmelidir.'
  if (writtenNotes.value.length === 0) return 'En az bir öğrenci için gözlem yazılmalıdır.'
  return null
})

/** Önceki notları koruyarak işletmedeki öğrencileri not satırı yapar. */
function noteRowsFor(businessId: string | null, existing: readonly StudentVisitNote[]): StudentVisitNote[] {
  const byStudent = new Map(existing.map((n) => [n.studentId, n.performanceNote]))
  const rows = studentsAtBusiness(placementOpts.allOptions.value, businessId)
    .map((s) => ({ studentId: s.studentId, performanceNote: byStudent.get(s.studentId) ?? '' }))
  // Kayıtlı notu olup artık yerleşik görünmeyen öğrenci (sözleşme bitti) kaybolmamalı.
  for (const n of existing) {
    if (!rows.some((r) => r.studentId === n.studentId)) rows.push({ ...n })
  }
  return rows
}

function onBusinessChange(businessId: string | null) {
  form.studentNotes = noteRowsFor(businessId, [])
}

function goBack() {
  router.push({ name: 'GuidanceVisits' }).catch(() => {})
}

function editablePart() {
  return {
    visitDate: new Date(form.visitDate).toISOString(),
    studentNotes: writtenNotes.value,
    instructorMeetingNotes: form.instructorMeetingNotes.trim() || undefined,
    issuesIdentified: form.issuesIdentified.trim() || undefined,
    actionsTaken: form.actionsTaken.trim() || undefined,
    generalAssessment: form.generalAssessment.trim() || undefined,
  }
}

async function handleSave() {
  if (problem.value) return
  saving.value = true
  try {
    if (visitId.value) {
      await coordinationApi.updateVisit(visitId.value, editablePart())
      notify.success('Rehberlik ziyareti güncellendi.')
    } else {
      await coordinationApi.createVisit({
        ...editablePart(),
        businessId: form.businessId!,
        teacherId: form.teacherId!,
        institutionId: authStore.currentInstitutionId ?? '',
        academicPeriodId: periodStore.selectedPeriodId ?? '',
      })
      notify.success('Rehberlik ziyareti taslak olarak kaydedildi.')
    }
    goBack()
  } catch (e) {
    notify.apiError(e, 'Rehberlik ziyareti kaydedilirken bir hata oluştu.')
  } finally {
    saving.value = false
  }
}

async function loadVisit(id: string) {
  try {
    const { data } = await coordinationApi.getVisit(id)
    form.businessId = data.businessId
    form.teacherId = data.teacherId
    form.visitDate = data.visitDate.slice(0, 10)
    form.instructorMeetingNotes = data.instructorMeetingNotes ?? ''
    form.issuesIdentified = data.issuesIdentified ?? ''
    form.actionsTaken = data.actionsTaken ?? ''
    form.generalAssessment = data.generalAssessment ?? ''
    form.studentNotes = noteRowsFor(data.businessId, data.studentNotes)
    status.value = data.status
  } catch (e) {
    loadError.value = 'Ziyaret kaydı yüklenemedi.'
    notify.apiError(e, 'Ziyaret kaydı yüklenemedi.')
  }
}

onMounted(async () => {
  names.load().catch(() => {})
  // Düzenlemede de gerekir: işletmedeki öğrenciler not satırı olarak listelenir.
  await placementOpts.load({ academicPeriodId: periodStore.selectedPeriodId ?? undefined })
    .catch((e: unknown) => notify.apiError(e, 'Yerleştirmeler yüklenemedi.'))
  if (visitId.value) await loadVisit(visitId.value)
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
</style>
