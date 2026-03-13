<template>
  <q-page padding>
    <div class="row items-center q-mb-lg">
      <div class="text-h5 text-weight-bold col">MEB Formları ve Dokümanlar</div>
      <div class="q-gutter-sm">
        <q-btn
          v-if="canGenerate"
          color="teal"
          icon="note_add"
          label="Belge Oluştur"
          @click="showGenerateDialog = true"
        />
        <q-btn
          v-if="selected.length > 0"
          color="deep-purple"
          icon="archive"
          :label="`Seçilenleri ZIP İndir (${selected.length})`"
          :loading="zipping"
          @click="downloadSelectedZip"
        />
        <q-btn
          color="primary"
          icon="refresh"
          flat
          round
          :loading="loading"
          @click="load"
        />
      </div>
    </div>

    <!-- Filtreler -->
    <q-card flat bordered class="q-mb-md">
      <q-card-section>
        <div class="row q-col-gutter-sm items-end">
          <div class="col-12 col-sm-6">
            <q-select
              v-model="filterState.formType"
              :options="formTypeOptions"
              label="Form Tipi"
              filled
              dense
              clearable
              emit-value
              map-options
            />
          </div>
          <div class="col-12 col-sm-6">
            <q-select
              v-model="filterState.status"
              :options="statusOptions"
              label="Durum"
              filled
              dense
              clearable
              emit-value
              map-options
            />
          </div>
        </div>
      </q-card-section>
    </q-card>

    <!-- Doküman Tablosu -->
    <q-card flat bordered>
      <q-table
        :rows="documents"
        :columns="columns"
        row-key="id"
        :loading="loading"
        flat
        bordered
        selection="multiple"
        :selected="selected"
        @update:selected="onSelectedUpdate"
        :rows-per-page-options="[10, 20, 50]"
        :pagination="pagination"
        no-data-label="Henüz doküman bulunmuyor"
        loading-label="Yükleniyor..."
        @request="onRequest"
      >
        <template #body-cell-formType="{ row }">
          <q-td>
            <q-badge color="blue-grey" :label="formTypeLabel(row.formType)" />
          </q-td>
        </template>

        <template #body-cell-status="{ row }">
          <q-td>
            <q-badge :color="statusColor(row.status)" :label="statusLabel(row.status)" />
          </q-td>
        </template>

        <template #body-cell-generatedAt="{ row }">
          <q-td>{{ formatDate(row.generatedAt) }}</q-td>
        </template>

        <template #body-cell-actions="{ row }">
          <q-td class="q-gutter-xs">
            <q-btn
              flat
              round
              dense
              icon="download"
              color="primary"
              title="PDF İndir"
              :loading="downloading === row.id"
              @click="downloadPdf(row)"
            />
            <q-btn
              v-if="row.status === 'Generated'"
              flat
              round
              dense
              icon="print"
              color="orange"
              title="Yazdırıldı Olarak İşaretle"
              @click="markPrinted(row.id)"
            />
            <q-btn
              v-if="row.status === 'Printed'"
              flat
              round
              dense
              icon="assignment_turned_in"
              color="green"
              title="İmzalanıp Teslim Edildi"
              @click="markSignedReturned(row.id)"
            />
            <q-btn
              v-if="row.status === 'SignedAndReturned'"
              flat
              round
              dense
              icon="archive"
              color="grey"
              title="Arşivle"
              @click="archiveDoc(row.id)"
            />
          </q-td>
        </template>
      </q-table>
    </q-card>

    <!-- Belge Oluştur Dialog -->
    <q-dialog v-model="showGenerateDialog" persistent>
      <q-card style="min-width: 420px">
        <q-card-section>
          <div class="text-h6">Toplu Belge Oluştur</div>
          <div class="text-caption text-grey-7">
            Seçili form tipi ve dönem için eksik belgeler otomatik oluşturulur.
            Zaten oluşturulmuş belgeler tekrar oluşturulmaz.
          </div>
        </q-card-section>

        <q-card-section class="q-gutter-md">
          <q-select
            v-model="batchForm.formType"
            :options="batchFormTypeOptions"
            label="Form Tipi"
            filled
            dense
            emit-value
            map-options
          />

          <div class="row q-col-gutter-sm">
            <div class="col-6">
              <q-select
                v-model="batchForm.year"
                :options="yearOptions"
                label="Yıl"
                filled
                dense
                emit-value
                map-options
              />
            </div>
            <div class="col-6">
              <q-select
                v-model="batchForm.month"
                :options="monthOptions"
                label="Ay"
                filled
                dense
                emit-value
                map-options
              />
            </div>
          </div>
        </q-card-section>

        <q-card-actions align="right">
          <q-btn flat label="İptal" color="grey" @click="showGenerateDialog = false" />
          <q-btn
            label="Oluştur"
            color="teal"
            :loading="generating"
            :disable="!batchForm.formType"
            @click="generateBatch"
          />
        </q-card-actions>
      </q-card>
    </q-dialog>
  </q-page>
</template>

<script setup lang="ts">
import { ref, reactive, computed, onMounted } from 'vue'
import {
  reportingApi,
  downloadBlob,
  MEB_FORM_LABELS,
  BATCH_FORM_TYPES,
  DOCUMENT_STATUS_LABELS,
  DOCUMENT_STATUS_COLORS,
  type GeneratedDocumentSummaryDto,
} from 'src/api/reporting'
import { useNotify } from 'src/composables/useNotify'
import { useServerPagination } from 'src/composables/useServerPagination'
import { useAuthStore } from 'stores/auth'
import { useAcademicPeriodStore } from 'stores/academicPeriod'

const notify = useNotify()
const authStore = useAuthStore()
const periodStore = useAcademicPeriodStore()

const downloading = ref<string | null>(null)
const zipping = ref(false)
const generating = ref(false)
const selected = ref<GeneratedDocumentSummaryDto[]>([])
const showGenerateDialog = ref(false)

// Müdür/müdür yardımcısı belge oluşturabilir
const canGenerate = computed(() => authStore.hasPermission('institution:manage'))

const filterState = reactive({
  formType: null as string | null,
  status: 'Generated' as string | null,
})

const filters = computed(() => ({
  ...(filterState.formType ? { formType: filterState.formType } : {}),
  ...(filterState.status ? { status: filterState.status } : {}),
  ...(authStore.user?.institutionId ? { institutionId: authStore.user.institutionId } : {}),
}))

const { rows: documents, loading, pagination, onRequest, load } =
  useServerPagination<GeneratedDocumentSummaryDto>({
    fetchFn: (params) => reportingApi.listDocuments(params),
    filters,
    defaultSortBy: 'generatedAt',
    defaultDescending: true,
  })

const formTypeOptions = Object.entries(MEB_FORM_LABELS).map(([value, label]) => ({ value, label }))
const statusOptions = Object.entries(DOCUMENT_STATUS_LABELS).map(([value, label]) => ({ value, label }))
const batchFormTypeOptions = Object.entries(BATCH_FORM_TYPES).map(([value, label]) => ({ value, label }))

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

const columns = [
  { name: 'formType', label: 'Form Tipi', field: 'formType', align: 'left' as const },
  { name: 'status', label: 'Durum', field: 'status', align: 'left' as const },
  { name: 'generatedByName', label: 'Oluşturan', field: 'generatedByName', align: 'left' as const },
  { name: 'academicYear', label: 'Eğitim Yılı', field: 'academicYear', align: 'left' as const },
  { name: 'generatedAt', label: 'Tarih', field: 'generatedAt', align: 'left' as const },
  { name: 'actions', label: 'İşlemler', field: 'id', align: 'center' as const },
]

function onSelectedUpdate(val: readonly any[]) {
  selected.value = val as GeneratedDocumentSummaryDto[]
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

async function downloadSelectedZip() {
  if (selected.value.length === 0) return

  zipping.value = true
  try {
    const ids = selected.value.map((d) => d.id)
    const res = await reportingApi.downloadDocumentsZip(ids)
    downloadBlob(res.data as Blob, 'evraklar.zip')
    notify.success(`${ids.length} doküman ZIP olarak indirildi.`)
    selected.value = []
  } catch (e) {
    notify.apiError(e, 'ZIP indirirken bir hata oluştu.')
  } finally {
    zipping.value = false
  }
}

async function downloadPdf(doc: GeneratedDocumentSummaryDto) {
  downloading.value = doc.id
  try {
    const res = await reportingApi.getDocumentPdfUrl(doc.id)
    const result = (res.data as any)?.data ?? res.data
    if (result?.url) {
      window.open(result.url, '_blank')
    } else {
      const blobRes = await reportingApi.getDocumentPdfBlob(doc.id)
      const label = formTypeLabel(doc.formType).replace(/\s+/g, '-').toLowerCase()
      downloadBlob(blobRes.data as Blob, `${label}-${doc.id.slice(0, 8)}.pdf`)
    }
  } catch (e) {
    notify.apiError(e, 'PDF indirirken bir hata oluştu.')
  } finally {
    downloading.value = null
  }
}

async function markPrinted(id: string) {
  try {
    await reportingApi.markAsPrinted(id)
    notify.success('Yazdırıldı olarak işaretlendi.')
    await load()
  } catch (e) {
    notify.apiError(e, 'İşlem başarısız.')
  }
}

async function markSignedReturned(id: string) {
  try {
    await reportingApi.markAsSignedAndReturned(id)
    notify.success('İmzalanıp teslim edildi olarak işaretlendi.')
    await load()
  } catch (e) {
    notify.apiError(e, 'İşlem başarısız.')
  }
}

async function archiveDoc(id: string) {
  try {
    await reportingApi.markAsArchived(id)
    notify.success('Doküman arşivlendi.')
    await load()
  } catch (e) {
    notify.apiError(e, 'İşlem başarısız.')
  }
}

function formTypeLabel(formType: string): string {
  return MEB_FORM_LABELS[formType] ?? formType
}

function statusLabel(status: string): string {
  return DOCUMENT_STATUS_LABELS[status] ?? status
}

function statusColor(status: string): string {
  return DOCUMENT_STATUS_COLORS[status] ?? 'grey'
}

function formatDate(dateStr: string): string {
  return new Date(dateStr).toLocaleDateString('tr-TR', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  })
}

onMounted(load)
</script>
