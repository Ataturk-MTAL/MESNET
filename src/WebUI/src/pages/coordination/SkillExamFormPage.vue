<template>
  <q-page padding>
    <div class="form-page q-mx-auto">
      <div class="row items-center q-mb-lg">
        <q-btn
          flat
          round
          dense
          icon="arrow_back"
          aria-label="Beceri sınavlarına dön"
          class="q-mr-sm"
          @click="goBack"
        >
          <q-tooltip>Beceri sınavlarına dön</q-tooltip>
        </q-btn>
        <h1 class="text-h5 text-weight-bold col q-my-none">
          {{ isEdit ? 'Beceri Sınavını Düzenle' : 'Beceri Sınavı Ekle' }}
        </h1>
      </div>

      <AppNotice
        v-if="periodStore.isReadOnly"
        type="warning"
        message="Seçili akademik dönem kapalı; bu dönemde kayıt eklenemez ve düzenlenemez."
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
          <!-- Kimin sınavı — düzenlerken yanlış öğrencinin kaydını değiştirme riskine karşı
               sahip başlıkta durur (SubjectHeader deseni). -->
          <SubjectHeader
            title="Beceri Sınavı"
            :name="studentLabel"
            :context="businessLabel"
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

            <q-input
              v-model="form.examDate"
              label="Sınav Tarihi *"
              outlined
              type="date"
            >
              <template #prepend>
                <q-icon name="event" />
              </template>
            </q-input>

            <q-select
              v-model="form.semester"
              :options="semesterSelectOptions"
              label="Dönem *"
              outlined
              emit-value
              map-options
              :disable="isEdit"
            >
              <template #prepend>
                <q-icon name="date_range" />
              </template>
            </q-select>
          </div>
        </q-card-section>

        <q-separator />

        <q-card-section>
          <div class="row items-center q-mb-sm">
            <div class="col text-subtitle1 text-weight-medium">
              Değerlendirme Kriterleri
            </div>
            <q-btn
              flat
              color="primary"
              icon="add"
              label="Kriter ekle"
              @click="addCriterion"
            />
          </div>

          <div
            v-for="(criterion, index) in form.criteria"
            :key="index"
            class="row-editor"
          >
            <q-input
              v-model="criterion.name"
              label="Kriter"
              outlined
              dense
              class="row-editor__main"
            />
            <q-input
              v-model.number="criterion.maxScore"
              label="En yüksek"
              type="number"
              min="1"
              outlined
              dense
              class="row-editor__num"
            />
            <q-input
              v-model.number="criterion.score"
              label="Puan"
              type="number"
              min="0"
              :max="criterion.maxScore"
              outlined
              dense
              class="row-editor__num"
            />
            <q-btn
              flat
              round
              dense
              icon="delete"
              color="negative"
              :aria-label="`${criterion.name || 'Kriteri'} sil`"
              @click="form.criteria.splice(index, 1)"
            >
              <q-tooltip>Kriteri sil</q-tooltip>
            </q-btn>
          </div>

          <div
            v-if="form.criteria.length === 0"
            class="text-body2 text-grey-7"
          >
            Henüz kriter yok. Sınav puanı kriter puanlarından hesaplanır.
          </div>

          <div class="row items-center q-mt-md score-row">
            <div class="text-body1">
              Sınav puanı:
              <strong class="text-h6">{{ score }}</strong>
              <span class="text-caption text-grey-7"> / 100</span>
            </div>
            <q-select
              v-model="form.result"
              :options="resultOptions"
              label="Sonuç *"
              outlined
              dense
              emit-value
              map-options
              class="score-row__result"
            />
          </div>
        </q-card-section>

        <q-separator />

        <q-card-section>
          <div class="row items-center q-mb-sm">
            <div class="col text-subtitle1 text-weight-medium">
              Sınav Komisyonu
            </div>
            <q-btn
              flat
              color="primary"
              icon="add"
              label="Üye ekle"
              @click="form.committeeMembers.push({ fullName: '', title: '' })"
            />
          </div>

          <div
            v-for="(member, index) in form.committeeMembers"
            :key="index"
            class="row-editor"
          >
            <q-input
              v-model="member.fullName"
              label="Ad Soyad"
              outlined
              dense
              class="row-editor__main"
            />
            <q-input
              v-model="member.title"
              label="Unvan / Görev"
              outlined
              dense
              class="row-editor__main"
            />
            <q-btn
              flat
              round
              dense
              icon="delete"
              color="negative"
              :aria-label="`${member.fullName || 'Üyeyi'} sil`"
              @click="form.committeeMembers.splice(index, 1)"
            >
              <q-tooltip>Üyeyi sil</q-tooltip>
            </q-btn>
          </div>

          <div
            v-if="form.committeeMembers.length === 0"
            class="text-body2 text-grey-7"
          >
            En az bir komisyon üyesi girilmelidir.
          </div>
        </q-card-section>

        <q-separator />
        <q-card-actions
          align="right"
          class="q-pa-md"
        >
          <span
            v-if="problem"
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
            :disable="!!problem || periodStore.isReadOnly"
            @click="handleSave"
          />
        </q-card-actions>
      </q-card>
    </div>
  </q-page>
</template>

<script setup lang="ts">
/**
 * Beceri sınavı oluşturma / düzenleme formu (route: `/coordination/skill-exams/new`,
 * `/coordination/skill-exams/:id/edit`).
 *
 * Sınav puanı elle girilmez, kriter puanlarından yüzde olarak hesaplanır — iki ayrı giriş
 * birbirini tutmayabilirdi. Sonuç (Başarılı/Başarısız) komisyonun kararıdır ve elle seçilir.
 *
 * Düzenlemede öğrenci, işletme ve dönem değişmez: `UpdateSkillExam` bu alanları almaz.
 */
import { ref, reactive, computed, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { coordinationApi, EXAM_RESULTS, type ExamCriterion, type ExamCommitteeMember } from 'src/api/coordination'
import { useNotify } from 'src/composables/useNotify'
import { usePlacementOptions } from 'src/composables/useEntityOptions'
import { useEntityNames } from 'src/composables/useEntityNames'
import { useAuthStore } from 'stores/auth'
import { useAcademicPeriodStore, semesterOptions } from 'stores/academicPeriod'
import { percentScore, criteriaProblem } from 'utils/coordinationRecords'
import AppNotice from 'components/AppNotice.vue'
import SelectEmptyOption from 'components/SelectEmptyOption.vue'
import SubjectHeader from 'components/SubjectHeader.vue'

const route = useRoute()
const router = useRouter()
const notify = useNotify()
const authStore = useAuthStore()
const periodStore = useAcademicPeriodStore()
const placementOpts = usePlacementOptions()
const names = useEntityNames()

const examId = computed(() => (route.params.id as string | undefined) ?? null)
const isEdit = computed(() => examId.value !== null)

const saving = ref(false)
const loadError = ref<string | null>(null)

const resultOptions = EXAM_RESULTS.map((r) => ({ label: r.label, value: r.value }))
const semesterSelectOptions = semesterOptions.map((s) => ({ label: s.label, value: s.value }))

const form = reactive({
  studentId: null as string | null,
  businessId: null as string | null,
  examDate: new Date().toISOString().slice(0, 10),
  semester: periodStore.selectedSemester,
  result: 'Passed',
  criteria: [] as ExamCriterion[],
  committeeMembers: [] as ExamCommitteeMember[],
})

const score = computed(() => percentScore(form.criteria))

const studentLabel = computed(() => (form.studentId ? names.studentName(form.studentId) : null))
const businessLabel = computed(() => (form.businessId ? names.businessName(form.businessId) : null))

const problem = computed(() => {
  if (!form.studentId || !form.businessId) return 'Öğrenci seçilmelidir.'
  if (!form.examDate) return 'Sınav tarihi girilmelidir.'
  const criteria = criteriaProblem(form.criteria)
  if (criteria) return criteria
  if (form.committeeMembers.length === 0) return 'En az bir komisyon üyesi girilmelidir.'
  if (form.committeeMembers.some((m) => !m.fullName.trim()))
    return 'Her komisyon üyesinin adı girilmelidir.'
  return null
})

function onStudentChange(studentId: string | null) {
  form.businessId = studentId ? placementOpts.getBusinessForStudent(studentId)?.businessId ?? null : null
}

function addCriterion() {
  form.criteria.push({ name: '', maxScore: 10, score: 0 })
}

function goBack() {
  router.push({ name: 'SkillExams' }).catch(() => {})
}

function payloadBase() {
  return {
    examDate: new Date(form.examDate).toISOString(),
    score: score.value,
    criteria: form.criteria.map((c) => ({ ...c, name: c.name.trim() })),
    committeeMembers: form.committeeMembers.map((m) => ({ fullName: m.fullName.trim(), title: m.title.trim() })),
    result: form.result,
  }
}

async function handleSave() {
  if (problem.value) return
  saving.value = true
  try {
    if (examId.value) {
      await coordinationApi.updateSkillExam(examId.value, payloadBase())
      notify.success('Beceri sınavı güncellendi.')
    } else {
      await coordinationApi.createSkillExam({
        ...payloadBase(),
        studentId: form.studentId!,
        businessId: form.businessId!,
        institutionId: authStore.currentInstitutionId ?? '',
        academicPeriodId: periodStore.selectedPeriodId ?? '',
        academicYear: periodStore.academicYear,
        semester: form.semester,
      })
      notify.success('Beceri sınavı eklendi.')
    }
    goBack()
  } catch (e) {
    notify.apiError(e, 'Beceri sınavı kaydedilirken bir hata oluştu.')
  } finally {
    saving.value = false
  }
}

async function loadExam(id: string) {
  try {
    const { data } = await coordinationApi.getSkillExam(id)
    form.studentId = data.studentId
    form.businessId = data.businessId
    form.examDate = data.examDate.slice(0, 10)
    form.semester = data.semester
    form.result = data.result
    form.criteria = data.criteria.map((c) => ({ ...c }))
    form.committeeMembers = data.committeeMembers.map((m) => ({ ...m }))
  } catch (e) {
    loadError.value = 'Sınav kaydı yüklenemedi.'
    notify.apiError(e, 'Sınav kaydı yüklenemedi.')
  }
}

onMounted(() => {
  names.load().catch(() => {})
  if (examId.value) {
    loadExam(examId.value).catch(() => {})
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
  align-items: center;
  gap: $space-x-base * 0.5;
  margin-bottom: $space-y-base * 0.5;
}

.row-editor__main {
  flex: 1 1 12rem;
}

.row-editor__num {
  flex: 0 1 7rem;
}

.score-row {
  gap: $space-base;
  flex-wrap: wrap;
  justify-content: space-between;
}

.score-row__result {
  min-width: 12rem;
}
</style>
