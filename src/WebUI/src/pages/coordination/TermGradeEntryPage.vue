<template>
  <q-page padding>
    <div class="row items-center q-mb-md">
      <div class="text-h5 col">
        Dönem Notu Girişi
      </div>
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
        :type="isWindowOpen ? 'info' : 'warning'"
        :message="windowMessage"
        class="q-mb-md"
      />

      <div
        v-if="!loading && rows.length === 0"
        class="text-center q-pa-xl text-grey-6"
      >
        <q-icon
          name="groups"
          size="48px"
          class="q-mb-sm"
        />
        <div>Bu dönemde işletmenize yerleştirilmiş öğrenci bulunamadı.</div>
      </div>

      <AppTable
        v-else
        :rows="rows"
        :columns="columns"
        :loading="loading"
      >
        <template #body-cell-status="{ row }">
          <q-td>
            <q-badge
              v-if="row.status"
              :color="row.status === 'Submitted' ? 'positive' : 'warning'"
              :label="row.statusSlug ?? row.status"
            />
            <span
              v-else
              class="text-grey-5"
            >Girilmedi</span>
          </q-td>
        </template>
        <template #body-cell-average="{ row }">
          <q-td>{{ row.termAverage ?? '—' }}</q-td>
        </template>
        <template #body-cell-actions="{ row }">
          <q-td class="text-right">
            <q-btn
              flat
              dense
              size="sm"
              icon="edit_note"
              label="Not Gir"
              color="primary"
              :disable="!isWindowOpen || row.status === 'Submitted'"
              @click="openEntry(row)"
            >
              <q-tooltip>{{ row.status === 'Submitted' ? 'Gönderilmiş not düzenlenemez' : 'Notları gir/düzenle' }}</q-tooltip>
            </q-btn>
            <q-btn
              v-if="row.gradeId && row.status === 'Draft'"
              flat
              dense
              size="sm"
              icon="send"
              label="Gönder"
              color="positive"
              :disable="!isWindowOpen"
              @click="confirmSubmit(row)"
            >
              <q-tooltip>Notu kesin gönder (sonra düzenlenemez)</q-tooltip>
            </q-btn>
          </q-td>
        </template>
      </AppTable>
    </template>

    <FormDialog
      v-model="entryDialog"
      title="Dönem Notu Gir"
      icon="edit_note"
      color="primary"
      save-label="Kaydet (Taslak)"
      :saving="saving"
      @save="handleSave"
    >
      <div class="text-subtitle2">
        {{ entryTarget?.studentName }}
      </div>
      <div class="text-caption text-grey-7 q-mb-sm">
        {{ entryTarget?.branchName }}
      </div>
      <q-input
        v-model="form.practice"
        label="Temrin notları"
        hint="Virgülle ayır: 85, 90, 88"
        outlined
      />
      <q-input
        v-model="form.service"
        label="İş-Hizmet notları"
        hint="Virgülle ayır"
        outlined
      />
      <q-input
        v-model="form.project"
        label="Proje notları"
        hint="Virgülle ayır"
        outlined
      />
      <q-input
        v-model="form.experiment"
        label="Deney notları"
        hint="Virgülle ayır"
        outlined
      />
      <q-input
        v-model="form.masterInstructor"
        label="Usta Öğretici Adı"
        outlined
      />
    </FormDialog>
  </q-page>
</template>

<script setup lang="ts">
import { ref, reactive, computed, onMounted } from 'vue'
import type { QTableProps } from 'quasar'
import { useQuasar } from 'quasar'
import { coordinationApi, type StudentGradeRow } from 'src/api/coordination'
import { useAcademicPeriodStore } from 'stores/academicPeriod'
import { useNotify } from 'src/composables/useNotify'
import AppTable from 'components/AppTable.vue'
import FormDialog from 'components/FormDialog.vue'
import AppNotice from 'components/AppNotice.vue'

const $q = useQuasar()
const periodStore = useAcademicPeriodStore()
const notify = useNotify()

const rows = ref<StudentGradeRow[]>([])
const loading = ref(false)
const saving = ref(false)
const entryDialog = ref(false)
const entryTarget = ref<StudentGradeRow | null>(null)

const form = reactive({ practice: '', service: '', project: '', experiment: '', masterInstructor: '' })

const activePeriod = computed(() => periodStore.activePeriod)

const isWindowOpen = computed(() => {
  const p = activePeriod.value
  if (!p?.gradeEntryStartDate || !p?.gradeEntryEndDate) return false
  const today = new Date().toISOString().slice(0, 10)
  return today >= p.gradeEntryStartDate && today <= p.gradeEntryEndDate
})

const windowMessage = computed(() => {
  const p = activePeriod.value
  if (!p?.gradeEntryStartDate || !p?.gradeEntryEndDate)
    return 'Not giriş penceresi henüz açılmadı. Okul/kurum müdürlüğünün belirleyeceği tarihleri bekleyin.'
  const range = `${formatDate(p.gradeEntryStartDate)} – ${formatDate(p.gradeEntryEndDate)}`
  return isWindowOpen.value
    ? `Not giriş penceresi açık: ${range}`
    : `Not giriş penceresi kapalı (${range}).`
})

const columns: QTableProps['columns'] = [
  { name: 'studentName', label: 'Öğrenci', field: 'studentName', align: 'left', sortable: true },
  { name: 'branchName', label: 'Alan/Dal', field: 'branchName', align: 'left' },
  { name: 'status', label: 'Durum', field: 'status', align: 'left' },
  { name: 'average', label: 'Ortalama', field: 'termAverage', align: 'left' },
  { name: 'actions', label: '', field: 'studentId', align: 'right' },
]

function formatDate(iso: string) {
  return new Date(iso).toLocaleDateString('tr-TR')
}

function parseGrades(s: string): number[] {
  return s
    .split(/[,\s]+/)
    .map((x) => Number(x.trim()))
    .filter((n) => !Number.isNaN(n) && n >= 0 && n <= 100)
}

async function load() {
  const periodId = activePeriod.value?.id
  if (!periodId) return
  loading.value = true
  try {
    const { data } = await coordinationApi.getMyStudentsForGrading(periodId)
    rows.value = data?.students ?? []
  } catch (e) {
    notify.apiError(e, 'Öğrenci listesi yüklenemedi.')
  } finally {
    loading.value = false
  }
}

function openEntry(row: StudentGradeRow) {
  entryTarget.value = row
  form.practice = row.practiceGrades.join(', ')
  form.service = row.serviceGrades.join(', ')
  form.project = row.projectGrades.join(', ')
  form.experiment = row.experimentGrades.join(', ')
  form.masterInstructor = row.masterInstructorName ?? ''
  entryDialog.value = true
}

async function handleSave() {
  const periodId = activePeriod.value?.id
  if (!entryTarget.value || !periodId) return
  saving.value = true
  try {
    await coordinationApi.enterTermGrade({
      studentId: entryTarget.value.studentId,
      academicPeriodId: periodId,
      practiceGrades: parseGrades(form.practice),
      serviceGrades: parseGrades(form.service),
      projectGrades: parseGrades(form.project),
      experimentGrades: parseGrades(form.experiment),
      masterInstructorName: form.masterInstructor || null,
    })
    notify.success('Not taslağı kaydedildi.')
    entryDialog.value = false
    await load()
  } catch (e) {
    notify.apiError(e, 'Not kaydedilemedi.')
  } finally {
    saving.value = false
  }
}

function confirmSubmit(row: StudentGradeRow) {
  $q.dialog({
    title: 'Notu Gönder',
    message: `${row.studentName} için notları kesin göndermek istiyor musunuz? Gönderilen notlar düzenlenemez.`,
    cancel: { label: 'Vazgeç', flat: true },
    ok: { label: 'Gönder', color: 'positive', unelevated: true },
  }).onOk(() => {
    doSubmit(row).catch(() => {})
  })
}

async function doSubmit(row: StudentGradeRow) {
  if (!row.gradeId) return
  try {
    await coordinationApi.submitTermGrade(row.gradeId)
    notify.success('Not gönderildi.')
    await load()
  } catch (e) {
    notify.apiError(e, 'Not gönderilemedi.')
  }
}

onMounted(async () => {
  if (!periodStore.isLoaded) await periodStore.loadPeriods()
  await load()
})
</script>
