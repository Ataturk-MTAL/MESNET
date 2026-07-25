<template>
  <q-page padding>
    <div class="row items-center q-mb-lg">
      <div class="text-h5 text-weight-bold col">
        MEB Formları ve Dokümanlar
      </div>
      <div class="q-gutter-sm">
        <q-btn
          v-if="canGenerate"
          color="primary"
          icon="note_add"
          label="Belge Oluştur"
          @click="showGenerateDialog = true"
        />
        <q-btn
          v-if="canGenerate"
          color="orange"
          icon="sync"
          label="Form 3 Verilerini Yenile"
          :loading="resyncing"
          flat
          @click="resyncForm3"
        />
        <q-btn
          v-if="canGenerate && selected.length > 0"
          color="red"
          icon="delete_sweep"
          :label="`Seçilenleri Sil (${selected.length})`"
          :loading="deleting"
          flat
          @click="deleteSelected"
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
    <q-card
      flat
      bordered
      class="q-mb-md"
    >
      <q-card-section>
        <div class="row q-col-gutter-sm items-end">
          <div class="col-12 col-sm-6">
            <q-select
              v-model="filterState.formType"
              :options="formTypeOptions"
              label="Form Tipi"
              outlined
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
              outlined
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
    <q-card
      flat
      bordered
    >
      <q-table
        :rows="documents"
        :columns="columns"
        row-key="id"
        :loading="loading"
        flat
        bordered
        selection="multiple"
        :selected="selected"
        :rows-per-page-options="[10, 20, 50]"
        :pagination="pagination"
        no-data-label="Henüz doküman bulunmuyor"
        loading-label="Yükleniyor..."
        @update:selected="onSelectedUpdate"
        @request="onRequest"
      >
        <template #no-data>
          <div class="full-width column flex-center q-py-lg text-grey-7">
            <q-icon
              name="description"
              size="42px"
              class="q-mb-sm"
            />
            <div class="q-mb-md">
              Henüz doküman bulunmuyor
            </div>
            <q-btn
              v-if="canGenerate"
              color="primary"
              icon="note_add"
              label="İlk belgeyi oluştur"
              unelevated
              @click="showGenerateDialog = true"
            />
          </div>
        </template>
        <template #body-cell-formType="{ row }">
          <q-td>
            <q-badge
              color="blue-grey"
              :label="formTypeLabel(row.formType)"
            />
          </q-td>
        </template>

        <template #body-cell-status="{ row }">
          <q-td>
            <q-badge
              :color="statusColor(row.status)"
              :label="statusLabel(row.status)"
            />
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
              aria-label="PDF İndir"
              :loading="downloading === row.id"
              @click="downloadPdf(row)"
            >
              <q-tooltip>PDF İndir</q-tooltip>
            </q-btn>
            <q-btn
              v-if="row.status === 'Generated'"
              flat
              round
              dense
              icon="print"
              color="orange"
              aria-label="Yazdırıldı Olarak İşaretle"
              @click="markPrinted(row.id)"
            >
              <q-tooltip>Yazdırıldı Olarak İşaretle</q-tooltip>
            </q-btn>
            <q-btn
              v-if="row.status === 'Printed'"
              flat
              round
              dense
              icon="assignment_turned_in"
              color="green"
              aria-label="İmzalanıp Teslim Edildi"
              @click="markSignedReturned(row.id)"
            >
              <q-tooltip>İmzalanıp Teslim Edildi</q-tooltip>
            </q-btn>
            <q-btn
              v-if="row.status === 'SignedAndReturned'"
              flat
              round
              dense
              icon="archive"
              color="grey"
              aria-label="Arşivle"
              @click="archiveDoc(row.id)"
            >
              <q-tooltip>Arşivle</q-tooltip>
            </q-btn>
            <q-btn
              v-if="canGenerate"
              flat
              round
              dense
              icon="delete"
              color="red"
              aria-label="Sil"
              @click="deleteDoc(row.id)"
            >
              <q-tooltip>Sil</q-tooltip>
            </q-btn>
          </q-td>
        </template>
      </q-table>
    </q-card>

    <!-- Belge Oluştur Dialog -->
    <q-dialog
      v-model="showGenerateDialog"
      persistent
    >
      <q-card style="min-width: 420px">
        <q-card-section>
          <div class="text-h6">
            Toplu Belge Oluştur
          </div>
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
            outlined
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
                outlined
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
                outlined
                dense
                emit-value
                map-options
              />
            </div>
          </div>
        </q-card-section>

        <q-card-actions align="right">
          <q-btn
            flat
            label="İptal"
            color="grey"
            @click="showGenerateDialog = false"
          />
          <q-btn
            v-if="batchForm.formType === 'MonthlyAttendanceReport'"
            flat
            icon="preview"
            label="Önizle"
            color="blue"
            :loading="previewing"
            @click="previewBatchMonthlyAttendance"
          />
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
  type GeneratedDocumentSummaryDto,
} from 'src/api/reporting'
import { coordinationApi } from 'src/api/coordination'
import { useNotify } from 'src/composables/useNotify'
import { useConfirmDialog } from 'src/composables/useConfirmDialog'
import { useServerPagination } from 'src/composables/useServerPagination'
import { useReportingBatchGenerate } from 'src/composables/useReportingBatchGenerate'
import { useReportingFormatters } from 'src/composables/useReportingFormatters'
import { useAuthStore } from 'stores/auth'
import { useAcademicPeriodStore } from 'stores/academicPeriod'
import { useInstitutionStore } from 'stores/institution'

const notify = useNotify()
const confirmDialog = useConfirmDialog()
const authStore = useAuthStore()
const periodStore = useAcademicPeriodStore()
const institutionStore = useInstitutionStore()

const institutionName = computed<string | undefined>(() => institutionStore.institution?.fullName)


const downloading = ref<string | null>(null)
const zipping = ref(false)
const deleting = ref(false)
const resyncing = ref(false)
const selected = ref<GeneratedDocumentSummaryDto[]>([])

// Müdür/müdür yardımcısı belge oluşturabilir
const canGenerate = computed(() => authStore.hasPermission('institution:manage'))

const filterState = reactive({
  formType: null as string | null,
  status: null as string | null,
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

const {
  formTypeOptions,
  statusOptions,
  batchFormTypeOptions,
  formTypeLabel,
  statusLabel,
  statusColor,
  formatDate,
} = useReportingFormatters()

const {
  generating,
  previewing,
  showGenerateDialog,
  batchForm,
  yearOptions,
  monthOptions,
  previewBatchMonthlyAttendance,
  generateBatch,
} = useReportingBatchGenerate({
  institutionName,
  authStore,
  periodStore,
  notify,
  load,
})

const columns = [
  { name: 'formType', label: 'Form Tipi', field: 'formType', align: 'left' as const },
  { name: 'status', label: 'Durum', field: 'status', align: 'left' as const },
  { name: 'generatedByName', label: 'Oluşturan', field: 'generatedByName', align: 'left' as const },
  { name: 'academicYear', label: 'Eğitim Yılı', field: 'academicYear', align: 'left' as const },
  { name: 'generatedAt', label: 'Tarih', field: 'generatedAt', align: 'left' as const },
  { name: 'actions', label: 'İşlemler', field: 'id', align: 'center' as const },
]

function onSelectedUpdate(val: readonly unknown[]) {
  selected.value = val as GeneratedDocumentSummaryDto[]
}

function deleteSelected() {
  if (selected.value.length === 0) return
  const count = selected.value.length
  confirmDialog.confirm({
    title: 'Belgeleri Sil',
    message: `${count} belgeyi silmek istediğinize emin misiniz? Bu işlem geri alınamaz.`,
    okLabel: 'Sil',
    onOk: async () => {
      deleting.value = true
      try {
        const ids = selected.value.map((d) => d.id)
        await reportingApi.deleteDocumentsBatch(ids)
        notify.success(`${ids.length} belge silindi.`)
        selected.value = []
        await load()
      } catch (e) {
        notify.apiError(e, 'Belgeler silinirken bir hata oluştu.')
      } finally {
        deleting.value = false
      }
    },
  })
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
    const result = (res.data as { data?: { url?: string } })?.data ?? res.data
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

function deleteDoc(id: string) {
  confirmDialog.confirm({
    title: 'Belgeyi Sil',
    message: 'Bu belgeyi silmek istediğinize emin misiniz? Bu işlem geri alınamaz.',
    okLabel: 'Sil',
    onOk: async () => {
      try {
        await reportingApi.deleteDocument(id)
        notify.success('Belge silindi.')
        await load()
      } catch (e) {
        notify.apiError(e, 'Silme işlemi başarısız.')
      }
    },
  })
}

async function resyncForm3() {
  const institutionId = authStore.user?.institutionId
  const periodId = periodStore.selectedPeriodId
  if (!institutionId || !periodId) {
    notify.error('Kurum veya akademik dönem bilgisi bulunamadı.')
    return
  }
  resyncing.value = true
  try {
    const res = await coordinationApi.resyncWeeklyVisitEvents({ institutionId, academicPeriodId: periodId })
    type ResyncResult = { resyncedAssignments: number }
    const result = ((res.data as { data?: ResyncResult })?.data ?? res.data) as ResyncResult
    notify.success(`${result.resyncedAssignments} ziyaret ataması yeniden işlendi. Form 3 verileri güncellendi.`)
  } catch (e) {
    notify.apiError(e, 'Yenileme sırasında bir hata oluştu.')
  } finally {
    resyncing.value = false
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

onMounted(async () => {
  await load()
  // Kurum adı (batch belge üretiminde kullanılır) — kurum adı olmadan da devam edilebilir
  await institutionStore.loadInstitution()
})
</script>
