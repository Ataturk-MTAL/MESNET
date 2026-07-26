<template>
  <q-page padding>
    <div class="row items-center q-mb-md">
      <div class="text-h5 col">
        Dönem Not Fişleri
      </div>
      <q-btn
        flat
        dense
        icon="folder_open"
        label="Belgeler"
        :to="{ name: 'Documents' }"
        class="q-mr-sm"
      >
        <q-tooltip>Üretilen fişler Belgeler'de görünür (yazdır / imzala / arşivle)</q-tooltip>
      </q-btn>
      <q-btn
        flat
        round
        dense
        icon="refresh"
        aria-label="Yenile"
        @click="load"
      >
        <q-tooltip>Yenile</q-tooltip>
      </q-btn>
    </div>

    <AppNotice
      v-if="!activePeriod"
      type="info"
      message="Aktif akademik dönem bulunamadı."
    />

    <template v-else>
      <AppNotice
        type="info"
        message="İşletmelerin gönderdiği dönem notları aşağıdadır. Okul-payı (Telafi/Beceri Yarışması) puanlarını girip fişi üretebilirsiniz."
        class="q-mb-md"
      />

      <div
        v-if="!loading && rows.length === 0"
        class="text-center q-pa-xl text-grey-6"
      >
        <q-icon
          name="grading"
          size="48px"
          class="q-mb-sm"
        />
        <div>Bu dönemde işletmelerce gönderilmiş not bulunmuyor.</div>
      </div>

      <AppTable
        v-else
        :rows="rows"
        :columns="columns"
        :loading="loading"
      >
        <template #body-cell-average="{ row }">
          <q-td>{{ row.termAverage ?? '—' }}</q-td>
        </template>
        <template #body-cell-actions="{ row }">
          <q-td class="text-right">
            <q-btn
              flat
              dense
              size="sm"
              icon="grading"
              label="Fiş Üret"
              color="primary"
              @click="openGenerate(row)"
            >
              <q-tooltip>Dönem not fişini üret (okul-payı puanları + imzalar)</q-tooltip>
            </q-btn>
          </q-td>
        </template>
      </AppTable>
    </template>

    <FormDialog
      v-model="generateDialog"
      title="Dönem Not Fişi Üret"
      icon="grading"
      color="primary"
      save-label="Üret"
      :saving="generating"
      :save-disabled="!form.institutionName || !form.academicYear || !form.semester"
      @save="handleGenerate"
    >
      <div class="text-subtitle2">
        {{ generateTarget?.studentName }}
      </div>
      <div class="text-caption text-grey-7 q-mb-sm">
        {{ generateTarget?.branchName }}
      </div>

      <q-input
        v-model="form.institutionName"
        label="Okul/Kurum Adı *"
        outlined
      />
      <div class="row q-col-gutter-md">
        <div class="col-6">
          <!-- Aktif akademik dönemden türetilir; elle düzenlenemez (#112) -->
          <q-input
            v-model="form.academicYear"
            label="Öğretim Yılı *"
            outlined
            readonly
            hint="Aktif akademik dönemden alınır"
          />
        </div>
        <div class="col-6">
          <q-input
            v-model="form.semester"
            label="Dönem *"
            outlined
          />
        </div>
      </div>

      <div class="text-caption text-grey-7 q-mt-sm">
        Okulda verilen puanlar (*) — opsiyonel
      </div>
      <div class="row q-col-gutter-md">
        <div class="col-6">
          <q-input
            v-model.number="form.makeupTrainingScore"
            label="Telafi Eğitim Puanı"
            type="number"
            outlined
          />
        </div>
        <div class="col-6">
          <q-input
            v-model.number="form.skillCompetitionScore"
            label="Beceri Yarışması Puanı"
            type="number"
            outlined
          />
        </div>
      </div>

      <div class="row q-col-gutter-md">
        <div class="col-6">
          <q-input
            v-model="form.vicePrincipalName"
            label="Koor. Müdür Yardımcısı"
            outlined
          />
        </div>
        <div class="col-6">
          <q-input
            v-model="form.principalName"
            label="Okul/Kurum Müdürü"
            outlined
          />
        </div>
      </div>
    </FormDialog>
  </q-page>
</template>

<script setup lang="ts">
import { ref, reactive, computed, onMounted } from 'vue'
import type { QTableProps } from 'quasar'
import { coordinationApi, type StudentGradeRow } from 'src/api/coordination'
import { reportingApi } from 'src/api/reporting'
import { useAcademicPeriodStore } from 'stores/academicPeriod'
import { useNotify } from 'src/composables/useNotify'
import AppTable from 'components/AppTable.vue'
import FormDialog from 'components/FormDialog.vue'
import AppNotice from 'components/AppNotice.vue'

const periodStore = useAcademicPeriodStore()
const notify = useNotify()

const rows = ref<StudentGradeRow[]>([])
const loading = ref(false)
const generating = ref(false)
const generateDialog = ref(false)
const generateTarget = ref<StudentGradeRow | null>(null)

// Okul bilgileri oturum boyunca hatırlanır (her fişte tekrar yazmamak için)
const form = reactive({
  institutionName: '',
  academicYear: '',
  semester: '',
  makeupTrainingScore: null as number | null,
  skillCompetitionScore: null as number | null,
  vicePrincipalName: '',
  principalName: '',
})

const activePeriod = computed(() => periodStore.activePeriod)

// Eğitim yılı tek kanonik biçimde üretilir: "2025-2026" (#112).
// Serbest metin girişi kapalı — kullanıcı elle "2025 / 2026" veya "2025" yazamasın.
const academicYearLabel = computed(() =>
  activePeriod.value ? `${activePeriod.value.startYear}-${activePeriod.value.endYear}` : '',
)

const columns: QTableProps['columns'] = [
  { name: 'studentName', label: 'Öğrenci', field: 'studentName', align: 'left', sortable: true },
  { name: 'branchName', label: 'Alan/Dal', field: 'branchName', align: 'left' },
  { name: 'average', label: 'İşletme Ortalaması', field: 'termAverage', align: 'left' },
  { name: 'actions', label: '', field: 'studentId', align: 'right' },
]

async function load() {
  const periodId = activePeriod.value?.id
  if (!periodId) return
  loading.value = true
  try {
    const { data } = await coordinationApi.getSubmittedTermGrades(periodId)
    rows.value = data?.students ?? []
  } catch (e) {
    notify.apiError(e, 'Gönderilmiş notlar yüklenemedi.')
  } finally {
    loading.value = false
  }
}

function openGenerate(row: StudentGradeRow) {
  generateTarget.value = row
  // okul-payı puanlar her öğrenciye özel — sıfırla; okul/imza bilgileri korunur
  form.makeupTrainingScore = null
  form.skillCompetitionScore = null
  form.academicYear = academicYearLabel.value
  if (!form.semester) form.semester = String(periodStore.selectedSemesterLabel ?? '')
  generateDialog.value = true
}

async function handleGenerate() {
  const periodId = activePeriod.value?.id
  if (!generateTarget.value || !periodId) return
  generating.value = true
  try {
    await reportingApi.generateTermGradeSlipFromGrades({
      studentId: generateTarget.value.studentId,
      academicPeriodId: periodId,
      institutionName: form.institutionName,
      academicYear: form.academicYear,
      semester: form.semester,
      makeupTrainingScore: form.makeupTrainingScore,
      skillCompetitionScore: form.skillCompetitionScore,
      vicePrincipalName: form.vicePrincipalName || null,
      principalName: form.principalName || null,
    })
    notify.success('Dönem not fişi üretildi. Belgeler ekranından yazdırıp arşivleyebilirsiniz.')
    generateDialog.value = false
  } catch (e) {
    notify.apiError(e, 'Fiş üretilemedi.')
  } finally {
    generating.value = false
  }
}

onMounted(async () => {
  if (!periodStore.isLoaded) await periodStore.loadPeriods()
  await load()
})
</script>
