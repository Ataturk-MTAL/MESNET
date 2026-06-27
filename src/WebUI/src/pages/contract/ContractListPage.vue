<template>
  <q-page padding>
    <PageHeader title="Sözleşmeler">
      <PermissionGuard :permission="Permissions.Internship.Contract">
        <q-btn :disable="periodStore.isReadOnly" color="primary" icon="add" label="Yeni Sözleşme" unelevated @click="openCreateDialog" />
      </PermissionGuard>
    </PageHeader>

    <AppNotice v-if="periodStore.isReadOnly" type="readonly" class="q-mb-md"
      message="Bu dönem kapatılmıştır — yalnızca görüntüleme yapılabilir." />

    <!-- Filtreler -->
    <div class="row q-gutter-sm q-mb-md">
      <q-select
        v-model="statusFilter"
        :options="statusOptions"
        label="Durum"
        filled
        dense
        emit-value
        map-options
        style="min-width: 180px"
        @update:model-value="load"
      />
    </div>

    <AppTable :rows="contracts" :columns="columns" :loading="loading" :pagination="pagination" @request="onRequest">
      <template #body-cell-student="{ row }">
        <q-td>
          <div class="text-weight-medium">{{ studentMap[row.studentId]?.fullName ?? '—' }}</div>
          <div v-if="studentMap[row.studentId]?.info" class="text-caption text-grey-6">
            {{ studentMap[row.studentId].info }}
          </div>
        </q-td>
      </template>
      <template #body-cell-business="{ row }">
        <q-td>{{ businessMap[row.businessId] ?? '—' }}</q-td>
      </template>
      <template #body-cell-statusSlug="{ row }">
        <q-td><StatusBadge :slug="row.statusSlug" /></q-td>
      </template>
      <template #body-cell-startDate="{ row }">
        <q-td>{{ formatDate(row.startDate) }} – {{ formatDate(row.endDate) }}</q-td>
      </template>
      <template #body-cell-signatures="{ row }">
        <q-td>
          <q-icon
            :name="row.institutionSignature.isSigned ? 'check_circle' : 'radio_button_unchecked'"
            :color="row.institutionSignature.isSigned ? 'green-7' : 'grey-4'"
            size="xs"
          ><q-tooltip>Kurum{{ row.institutionSignature.signedBy ? ': ' + row.institutionSignature.signedBy : '' }}</q-tooltip></q-icon>
          <q-icon
            :name="row.businessSignature.isSigned ? 'check_circle' : 'radio_button_unchecked'"
            :color="row.businessSignature.isSigned ? 'green-7' : 'grey-4'"
            size="xs"
            class="q-ml-xs"
          ><q-tooltip>İşletme{{ row.businessSignature.signedBy ? ': ' + row.businessSignature.signedBy : '' }}</q-tooltip></q-icon>
          <q-icon
            :name="row.studentSignature.isSigned ? 'check_circle' : 'radio_button_unchecked'"
            :color="row.studentSignature.isSigned ? 'green-7' : 'grey-4'"
            size="xs"
            class="q-ml-xs"
          ><q-tooltip>Öğrenci{{ row.studentSignature.signedBy ? ': ' + row.studentSignature.signedBy : '' }}</q-tooltip></q-icon>
          <q-icon
            :name="row.parentSignature.isSigned ? 'check_circle' : 'radio_button_unchecked'"
            :color="row.parentSignature.isSigned ? 'green-7' : 'grey-4'"
            size="xs"
            class="q-ml-xs"
          ><q-tooltip>Veli{{ row.parentSignature.signedBy ? ': ' + row.parentSignature.signedBy : '' }}</q-tooltip></q-icon>
        </q-td>
      </template>
      <template #body-cell-actions="{ row }">
        <q-td class="text-right">
          <PermissionGuard :permission="Permissions.Document.Upload">
            <q-btn
              flat round dense
              icon="upload_file"
              color="secondary"
              title="Evrak Yükle"
              @click.stop="openUploadDialog(row)"
            />
          </PermissionGuard>
          <q-btn
            flat round dense
            icon="folder_open"
            color="grey-7"
            title="Evraklar"
            :badge="row.documents?.length > 0 ? String(row.documents.length) : undefined"
            badge-color="primary"
            @click.stop="openDocumentsDialog(row)"
          />
          <q-btn flat round dense icon="open_in_new" aria-label="Detayı aç" @click="openDetail(row)" />
        </q-td>
      </template>
    </AppTable>

    <!-- Detay Panel -->
    <DetailPanel v-model="detailOpen" title="Sözleşme Detayı" :has-content="!!selected" :width="520">
      <template v-if="selected">
            <!-- Durum & Tarih -->
            <div class="row items-center q-mb-md q-gutter-sm">
              <StatusBadge :slug="selected.statusSlug" />
              <q-chip icon="calendar_today" dense outline color="grey-7">
                {{ formatDate(selected.startDate) }}
                <span v-if="selected.endDate"> – {{ formatDate(selected.endDate) }}</span>
              </q-chip>
            </div>

            <!-- İmza durumu -->
            <q-card flat bordered class="q-mb-md">
              <q-card-section class="q-pb-sm">
                <div class="text-subtitle2 text-weight-medium q-mb-sm">İmza Durumu</div>
                <div class="row q-gutter-md justify-start">
                  <div v-for="sig in signatureList" :key="sig.label" class="text-center">
                    <q-icon
                      :name="sig.dto.isSigned ? 'check_circle' : 'pending'"
                      :color="sig.dto.isSigned ? 'green-7' : 'grey-4'"
                      size="36px"
                    />
                    <div class="text-caption text-weight-medium q-mt-xs">{{ sig.label }}</div>
                    <div v-if="sig.dto.signedBy" class="text-caption text-grey-7">{{ sig.dto.signedBy }}</div>
                    <div v-if="sig.dto.signedAt" class="text-caption text-grey-6">
                      {{ formatDate(sig.dto.signedAt) }}
                    </div>
                  </div>
                </div>
              </q-card-section>
            </q-card>

            <!-- Yüklü Evraklar -->
            <q-card v-if="selected.documents?.length" flat bordered class="q-mb-md">
              <q-card-section class="q-pb-sm">
                <div class="text-subtitle2 text-weight-medium q-mb-sm">Yüklü Evraklar</div>
                <q-list dense separator>
                  <q-item v-for="doc in selected.documents" :key="doc.documentId" class="q-px-none">
                    <q-item-section avatar>
                      <q-icon name="picture_as_pdf" color="red-7" />
                    </q-item-section>
                    <q-item-section>
                      <q-item-label class="text-weight-medium">{{ doc.documentTypeSlug }}</q-item-label>
                      <q-item-label v-if="doc.description" caption>{{ doc.description }}</q-item-label>
                      <q-item-label caption class="text-grey-6">
                        {{ doc.uploadedBy }} · {{ formatDate(doc.uploadedAt) }}
                      </q-item-label>
                    </q-item-section>
                  </q-item>
                </q-list>
              </q-card-section>
            </q-card>

            <!-- Fesih talebi bekliyor banner -->
            <q-banner
              v-if="selected.status === 'TerminationRequested'"
              class="q-mb-md text-white bg-deep-orange-9 rounded-borders"
              dense
            >
              <template #avatar><q-icon name="pending_actions" /></template>
              <div class="text-caption text-weight-bold">FESİH TALEBİ BEKLEMEDE</div>
              <div class="text-caption q-mt-xs">
                <span v-if="selected.terminationReasonTypeSlug">{{ selected.terminationReasonTypeSlug }}</span>
                <span v-if="selected.terminationReason"> — {{ selected.terminationReason }}</span>
              </div>
            </q-banner>

            <!-- Feshedildi bilgisi -->
            <q-banner
              v-if="selected.terminationReason && selected.status === 'Terminated'"
              class="q-mb-md text-white bg-deep-orange-7 rounded-borders"
              dense
            >
              <template #avatar>
                <q-icon name="gavel" />
              </template>
              <div class="text-caption text-weight-medium">{{ selected.terminationReasonTypeSlug }}</div>
              <div class="text-body2">{{ selected.terminationReason }}</div>
            </q-banner>

            <!-- Eylemler -->
            <div class="text-subtitle2 text-weight-medium q-mb-sm">İşlemler</div>
            <div class="column q-gutter-sm">
              <PermissionGuard :permission="Permissions.Internship.Contract">
                <q-btn
                  v-if="selected.status === 'Draft'"
                  color="primary"
                  icon="send"
                  label="İmzaya Gönder"
                  unelevated
                  :loading="saving"
                  @click="doSubmit"
                />
                <q-btn
                  v-if="selected.status === 'AwaitingSignature'"
                  color="teal"
                  icon="draw"
                  label="İmzala"
                  unelevated
                  :loading="saving"
                  @click="signDialog = true"
                />
                <q-btn
                  v-if="selected.status === 'AwaitingSignature'"
                  color="green"
                  icon="play_arrow"
                  label="Aktifleştir"
                  unelevated
                  :loading="saving"
                  @click="doActivate"
                />
              </PermissionGuard>

              <PermissionGuard :permission="Permissions.Internship.Manage">
                <q-btn
                  v-if="selected.status === 'Active'"
                  color="orange"
                  icon="pause"
                  label="Askıya Al"
                  unelevated
                  :loading="saving"
                  @click="suspendDialog = true"
                />
                <q-btn
                  v-if="selected.status === 'Suspended'"
                  color="green"
                  icon="play_arrow"
                  label="Devam Ettir"
                  unelevated
                  :loading="saving"
                  @click="doResume"
                />
                <q-btn
                  v-if="selected.status === 'Active' || selected.status === 'Suspended'"
                  color="negative"
                  icon="cancel"
                  label="Feshet"
                  unelevated
                  :loading="saving"
                  @click="terminateDialog = true"
                />
                <q-btn
                  v-if="selected.status === 'Active'"
                  color="purple"
                  icon="done_all"
                  label="Tamamla"
                  unelevated
                  :loading="saving"
                  @click="doComplete"
                />
              </PermissionGuard>

              <!-- Fesih talebi onay/red — Müdür / Müdür Yardımcısı -->
              <PermissionGuard :permission="Permissions.Internship.Approve">
                <template v-if="selected.status === 'TerminationRequested'">
                  <q-btn
                    color="negative"
                    icon="gavel"
                    label="Feshi Onayla"
                    unelevated
                    :loading="saving"
                    @click="terminateDialog = true"
                  />
                  <q-btn
                    color="teal"
                    icon="thumb_down"
                    label="Talebi Reddet"
                    unelevated
                    :loading="saving"
                    @click="rejectTerminateDialog = true"
                  />
                </template>
              </PermissionGuard>

              <!-- İşletme fesih talebi — CompanyManager -->
              <PermissionGuard :permission="Permissions.Company.Student">
                <q-btn
                  v-if="selected.status === 'Active' || selected.status === 'Suspended'"
                  color="deep-orange"
                  icon="report"
                  label="Fesih Talebi Oluştur"
                  outline
                  :loading="saving"
                  @click="requestTerminateDialog = true"
                />
              </PermissionGuard>

              <PermissionGuard :permission="Permissions.Document.Upload">
                <q-btn
                  color="secondary"
                  icon="upload_file"
                  label="Evrak Yükle"
                  outline
                  @click="openUploadDialog(selected)"
                />
              </PermissionGuard>
            </div>
      </template>
    </DetailPanel>

    <CreateContractForm v-model="createDialog" @saved="load" />
    <SignContractForm v-model="signDialog" :contract-id="selected?.id ?? ''" @saved="refreshSelected" />
    <SuspendContractForm v-model="suspendDialog" :contract-id="selected?.id ?? ''" @saved="refreshSelected" />
    <TerminateContractForm v-model="terminateDialog" :contract-id="selected?.id ?? ''" @saved="refreshSelected" />
    <RequestTerminationForm v-model="requestTerminateDialog" :contract-id="selected?.id ?? ''" @saved="refreshSelected" />
    <RejectTerminationForm v-model="rejectTerminateDialog" :contract-id="selected?.id ?? ''" @saved="refreshSelected" />
    <UploadContractDocForm v-model="uploadDialog" :contract-id="uploadTarget?.id ?? ''" @saved="afterUploadSaved" />

    <!-- ── Evraklar Dialog ── -->
    <q-dialog
      v-model="documentsDialog"
      :maximized="$q.screen.lt.sm"
      transition-show="slide-up"
      transition-hide="slide-down"
    >
      <q-card :style="$q.screen.gt.xs ? 'width: 520px; max-width: 95vw' : ''">
        <q-toolbar class="bg-grey-8 text-white">
          <q-icon name="folder_open" class="q-mr-sm" />
          <q-toolbar-title>Yüklü Evraklar</q-toolbar-title>
          <q-btn flat round dense icon="close" aria-label="Kapat" color="white" v-close-popup />
        </q-toolbar>

        <q-card-section>
          <div v-if="!documentsTarget?.documents?.length" class="text-center q-py-lg text-grey-6">
            <q-icon name="folder_off" size="48px" class="q-mb-sm" />
            <div>Henüz evrak yüklenmemiş.</div>
          </div>
          <q-list v-else separator>
            <q-item v-for="doc in documentsTarget?.documents" :key="doc.documentId">
              <q-item-section avatar>
                <q-avatar color="red-1" text-color="red-8" icon="picture_as_pdf" />
              </q-item-section>
              <q-item-section>
                <q-item-label class="text-weight-medium">{{ doc.documentTypeSlug }}</q-item-label>
                <q-item-label v-if="doc.description" caption>{{ doc.description }}</q-item-label>
                <q-item-label caption class="text-grey-6">
                  <q-icon name="person" size="12px" /> {{ doc.uploadedBy }}
                  &nbsp;·&nbsp;
                  <q-icon name="schedule" size="12px" /> {{ formatDate(doc.uploadedAt) }}
                </q-item-label>
              </q-item-section>
            </q-item>
          </q-list>
        </q-card-section>

        <q-separator />
        <q-card-actions align="right" class="q-pa-md">
          <PermissionGuard :permission="Permissions.Document.Upload">
            <q-btn
              unelevated
              color="secondary"
              icon="upload_file"
              label="Evrak Ekle"
              @click="() => { documentsDialog = false; if (documentsTarget) openUploadDialog(documentsTarget) }"
            />
          </PermissionGuard>
          <q-btn flat label="Kapat" color="grey-7" v-close-popup />
        </q-card-actions>
      </q-card>
    </q-dialog>
  </q-page>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, watch } from 'vue'
import { useQuasar } from 'quasar'
import type { QTableProps } from 'quasar'
import { contractApi, type InternshipContractDto } from 'src/api/contract'
import { useNotify } from 'src/composables/useNotify'
import { useServerPagination } from 'src/composables/useServerPagination'
import { useStudentOptions, useBusinessOptions } from 'src/composables/useEntityOptions'
import { useAcademicPeriodStore } from 'stores/academicPeriod'
import { Permissions } from 'utils/permissions'
import AppTable from 'components/AppTable.vue'
import StatusBadge from 'components/StatusBadge.vue'
import PermissionGuard from 'components/PermissionGuard.vue'
import PageHeader from 'components/PageHeader.vue'
import DetailPanel from 'components/DetailPanel.vue'
import CreateContractForm from 'components/forms/contract/CreateContractForm.vue'
import SignContractForm from 'components/forms/contract/SignContractForm.vue'
import SuspendContractForm from 'components/forms/contract/SuspendContractForm.vue'
import TerminateContractForm from 'components/forms/contract/TerminateContractForm.vue'
import RequestTerminationForm from 'components/forms/contract/RequestTerminationForm.vue'
import RejectTerminationForm from 'components/forms/contract/RejectTerminationForm.vue'
import UploadContractDocForm from 'components/forms/contract/UploadContractDocForm.vue'
import AppNotice from 'components/AppNotice.vue'

const $q = useQuasar()
const notify = useNotify()
const periodStore = useAcademicPeriodStore()
const studentOpts = useStudentOptions()
const businessOpts = useBusinessOptions()

// ID → metadata lookup map'leri (tablo satırlarında isim göstermek için)
const studentMap = computed<Record<string, { fullName: string; info: string }>>(() => {
  const map: Record<string, { fullName: string; info: string }> = {}
  for (const opt of studentOpts.allOptions.value) {
    map[opt.value] = { fullName: opt.label, info: opt.caption ?? '' }
  }
  return map
})

const businessMap = computed<Record<string, string>>(() => {
  const map: Record<string, string> = {}
  for (const opt of businessOpts.allOptions.value) {
    map[opt.value] = opt.label
  }
  return map
})

const saving = ref(false)
const selected = ref<InternshipContractDto | null>(null)
const detailOpen = ref(false)
const createDialog = ref(false)
const signDialog = ref(false)
const suspendDialog = ref(false)
const terminateDialog = ref(false)
const requestTerminateDialog = ref(false)
const rejectTerminateDialog = ref(false)
const uploadDialog = ref(false)
const documentsDialog = ref(false)
const statusFilter = ref<string | null>(null)

// Evrak yükleme/evraklar hedef sözleşme
const uploadTarget = ref<InternshipContractDto | null>(null)
const documentsTarget = ref<InternshipContractDto | null>(null)

// ── Server-side pagination ──
const filters = computed(() => ({
  academicPeriodId: periodStore.selectedPeriodId ?? undefined,
  status: statusFilter.value ?? undefined,
}))

const { rows: contracts, loading, pagination, onRequest, load } = useServerPagination<InternshipContractDto>({
  fetchFn: (params) => contractApi.list(params),
  filters,
  defaultSortBy: 'createdAt',
  defaultDescending: true,
})

const statusOptions = [
  { label: 'Tüm Durumlar', value: null },
  { label: 'Taslak', value: 'Draft' },
  { label: 'İmza Bekliyor', value: 'AwaitingSignature' },
  { label: 'Aktif', value: 'Active' },
  { label: 'Askıda', value: 'Suspended' },
  { label: 'Fesih Talep Edildi', value: 'TerminationRequested' },
  { label: 'Feshedildi', value: 'Terminated' },
  { label: 'Tamamlandı', value: 'Completed' },
]

const signatureList = computed(() =>
  selected.value
    ? [
        { label: 'Kurum',    dto: selected.value.institutionSignature },
        { label: 'İşletme',  dto: selected.value.businessSignature },
        { label: 'Öğrenci',  dto: selected.value.studentSignature },
        { label: 'Veli',     dto: selected.value.parentSignature },
      ]
    : [],
)

const columns: QTableProps['columns'] = [
  { name: 'student',    label: 'Öğrenci',   field: 'studentId',            align: 'left' },
  { name: 'business',   label: 'İşletme',   field: 'businessId',           align: 'left' },
  { name: 'statusSlug', label: 'Durum',     field: 'statusSlug',           align: 'left' },
  { name: 'startDate',  label: 'Dönem',     field: 'startDate',            align: 'left' },
  { name: 'signatures', label: 'İmzalar',   field: 'institutionSignature', align: 'center' },
  { name: 'actions',    label: '',          field: 'id',                   align: 'right' },
]

function formatDate(iso: string | null | undefined) {
  if (!iso) return '—'
  return new Date(iso).toLocaleDateString('tr-TR', { day: '2-digit', month: '2-digit', year: 'numeric' })
}

function openCreateDialog() {
  createDialog.value = true
}

function openDetail(row: InternshipContractDto) {
  selected.value = row
  detailOpen.value = true
}

function openUploadDialog(contract: InternshipContractDto) {
  uploadTarget.value = contract
  uploadDialog.value = true
}

function openDocumentsDialog(contract: InternshipContractDto) {
  documentsTarget.value = contract
  documentsDialog.value = true
}

// Doğrudan eylemler (form gerektirmeyen)
async function doSubmit() {
  if (!selected.value) return
  saving.value = true
  try {
    await contractApi.submit(selected.value.id)
    notify.success('Sözleşme imzaya gönderildi.')
    await refreshSelected()
  } catch (e) {
    notify.apiError(e, 'İşlem sırasında bir hata oluştu.')
  } finally {
    saving.value = false
  }
}

async function doActivate() {
  if (!selected.value) return
  saving.value = true
  try {
    await contractApi.activate(selected.value.id)
    notify.success('Sözleşme aktifleştirildi.')
    await refreshSelected()
  } catch (e) {
    notify.apiError(e, 'İşlem sırasında bir hata oluştu.')
  } finally {
    saving.value = false
  }
}

async function doResume() {
  if (!selected.value) return
  saving.value = true
  try {
    await contractApi.resume(selected.value.id)
    notify.success('Sözleşme devam ettirildi.')
    await refreshSelected()
  } catch (e) {
    notify.apiError(e, 'İşlem sırasında bir hata oluştu.')
  } finally {
    saving.value = false
  }
}

async function doComplete() {
  if (!selected.value) return
  saving.value = true
  try {
    await contractApi.complete(selected.value.id)
    notify.success('Sözleşme tamamlandı.')
    await refreshSelected()
  } catch (e) {
    notify.apiError(e, 'İşlem sırasında bir hata oluştu.')
  } finally {
    saving.value = false
  }
}

async function refreshSelected() {
  if (!selected.value) return
  try {
    const res = await contractApi.get(selected.value.id)
    selected.value = res.data
    const idx = contracts.value.findIndex((c) => c.id === res.data.id)
    if (idx !== -1) contracts.value[idx] = res.data
  } catch { /* sessiz */ }
}

async function afterUploadSaved() {
  await load()
  if (uploadTarget.value && selected.value?.id === uploadTarget.value.id) {
    await refreshSelected()
  }
}

watch(() => periodStore.selectedPeriodId, () => load())

onMounted(async () => {
  studentOpts.load().catch(() => {})
  businessOpts.load().catch(() => {})
  await load()
})
</script>
