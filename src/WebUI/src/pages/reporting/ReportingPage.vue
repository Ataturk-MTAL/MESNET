<template>
  <q-page padding>
    <div class="row items-center q-mb-lg">
      <div class="text-h5 text-weight-bold col">MEB Formları ve Dokümanlar</div>
      <q-btn
        color="primary"
        icon="refresh"
        flat
        round
        :loading="loading"
        @click="loadDocuments"
      />
    </div>

    <!-- Filtreler -->
    <q-card flat bordered class="q-mb-md">
      <q-card-section>
        <div class="row q-col-gutter-sm items-end">
          <div class="col-12 col-sm-4">
            <q-select
              v-model="filters.formType"
              :options="formTypeOptions"
              label="Form Tipi"
              filled
              dense
              clearable
              emit-value
              map-options
            />
          </div>
          <div class="col-12 col-sm-4">
            <q-select
              v-model="filters.status"
              :options="statusOptions"
              label="Durum"
              filled
              dense
              clearable
              emit-value
              map-options
            />
          </div>
          <div class="col-12 col-sm-4">
            <q-btn
              color="primary"
              label="Filtrele"
              icon="filter_alt"
              unelevated
              class="full-width"
              @click="loadDocuments"
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
        :rows-per-page-options="[15, 30, 50]"
        no-data-label="Henüz doküman bulunmuyor"
        loading-label="Yükleniyor..."
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
  </q-page>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted } from 'vue'
import {
  reportingApi,
  downloadBlob,
  MEB_FORM_LABELS,
  DOCUMENT_STATUS_LABELS,
  DOCUMENT_STATUS_COLORS,
  type GeneratedDocumentSummaryDto,
} from 'src/api/reporting'
import { useNotify } from 'src/composables/useNotify'
import { useAuthStore } from 'stores/auth'

const notify = useNotify()
const authStore = useAuthStore()

const loading = ref(false)
const downloading = ref<string | null>(null)
const documents = ref<GeneratedDocumentSummaryDto[]>([])

const filters = reactive({
  formType: null as string | null,
  status: null as string | null,
})

const formTypeOptions = Object.entries(MEB_FORM_LABELS).map(([value, label]) => ({ value, label }))
const statusOptions = Object.entries(DOCUMENT_STATUS_LABELS).map(([value, label]) => ({ value, label }))

const columns = [
  { name: 'formType', label: 'Form Tipi', field: 'formType', align: 'left' as const },
  { name: 'status', label: 'Durum', field: 'status', align: 'left' as const },
  { name: 'generatedByName', label: 'Oluşturan', field: 'generatedByName', align: 'left' as const },
  { name: 'academicYear', label: 'Eğitim Yılı', field: 'academicYear', align: 'left' as const },
  { name: 'generatedAt', label: 'Tarih', field: 'generatedAt', align: 'left' as const },
  { name: 'actions', label: 'İşlemler', field: 'id', align: 'center' as const },
]

async function loadDocuments() {
  loading.value = true
  try {
    const institutionId = authStore.user?.institutionId
    const params = {
      formType: filters.formType ?? undefined,
      status: filters.status ?? undefined,
      institutionId: institutionId ?? undefined,
    }
    const res = await reportingApi.listDocuments(params)
    documents.value = Array.isArray(res.data) ? res.data : (res.data as any)?.data ?? []
  } catch {
    notify.error('Dokümanlar yüklenirken bir hata oluştu.')
  } finally {
    loading.value = false
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
      // Fallback: blob olarak indir
      const blobRes = await reportingApi.getDocumentPdfBlob(doc.id)
      const label = formTypeLabel(doc.formType).replace(/\s+/g, '-').toLowerCase()
      downloadBlob(blobRes.data as Blob, `${label}-${doc.id.slice(0, 8)}.pdf`)
    }
  } catch {
    notify.error('PDF indirirken bir hata oluştu.')
  } finally {
    downloading.value = null
  }
}

async function markPrinted(id: string) {
  try {
    await reportingApi.markAsPrinted(id)
    notify.success('Yazdırıldı olarak işaretlendi.')
    await loadDocuments()
  } catch {
    notify.error('İşlem başarısız.')
  }
}

async function markSignedReturned(id: string) {
  try {
    await reportingApi.markAsSignedAndReturned(id)
    notify.success('İmzalanıp teslim edildi olarak işaretlendi.')
    await loadDocuments()
  } catch {
    notify.error('İşlem başarısız.')
  }
}

async function archiveDoc(id: string) {
  try {
    await reportingApi.markAsArchived(id)
    notify.success('Doküman arşivlendi.')
    await loadDocuments()
  } catch {
    notify.error('İşlem başarısız.')
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

onMounted(loadDocuments)
</script>
