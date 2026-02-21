<template>
  <q-page padding>
    <div class="row items-center q-mb-lg">
      <div class="col">
        <div class="text-h5 text-weight-bold">Sözleşmeler</div>
      </div>
      <div class="col-auto">
        <PermissionGuard :permission="Permissions.Internship.Contract">
          <q-btn color="primary" icon="add" label="Yeni Sözleşme" unelevated @click="openCreateDialog" />
        </PermissionGuard>
      </div>
    </div>

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
        clearable
        style="min-width: 180px"
        @update:model-value="load"
      />
    </div>

    <AppTable :rows="contracts" :columns="columns" :loading="loading">
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
            :color="row.institutionSignature.isSigned ? 'positive' : 'grey-4'"
            size="xs"
            title="Kurum"
          />
          <q-icon
            :name="row.businessSignature.isSigned ? 'check_circle' : 'radio_button_unchecked'"
            :color="row.businessSignature.isSigned ? 'positive' : 'grey-4'"
            size="xs"
            title="İşletme"
            class="q-ml-xs"
          />
          <q-icon
            :name="row.studentSignature.isSigned ? 'check_circle' : 'radio_button_unchecked'"
            :color="row.studentSignature.isSigned ? 'positive' : 'grey-4'"
            size="xs"
            title="Öğrenci"
            class="q-ml-xs"
          />
        </q-td>
      </template>
      <template #body-cell-actions="{ row }">
        <q-td class="text-right">
          <q-btn flat round dense icon="open_in_new" @click="openDetail(row)" />
        </q-td>
      </template>
    </AppTable>

    <!-- Detay Drawer -->
    <q-drawer v-model="detailOpen" side="right" bordered :width="520" overlay>
      <template v-if="selected">
        <q-toolbar class="bg-primary text-white">
          <q-btn flat round dense icon="arrow_back" color="white" @click="detailOpen = false" />
          <q-toolbar-title class="text-subtitle1 text-weight-medium">
            Sözleşme Detayı
          </q-toolbar-title>
        </q-toolbar>
        <q-scroll-area class="fit">
          <div class="q-pa-md">
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
                      :color="sig.dto.isSigned ? 'positive' : 'grey-4'"
                      size="36px"
                    />
                    <div class="text-caption text-weight-medium q-mt-xs">{{ sig.label }}</div>
                    <div v-if="sig.dto.signedAt" class="text-caption text-grey-6">
                      {{ formatDate(sig.dto.signedAt) }}
                    </div>
                  </div>
                </div>
              </q-card-section>
            </q-card>

            <!-- Fesih bilgisi -->
            <q-banner
              v-if="selected.terminationReason"
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

              <PermissionGuard :permission="Permissions.Document.Upload">
                <q-btn
                  v-if="selected.status !== 'Draft'"
                  color="secondary"
                  icon="upload_file"
                  label="Islak İmzalı Belge Yükle"
                  outline
                  @click="uploadSignedDialog = true"
                />
                <q-btn
                  v-if="selected.status === 'Terminated'"
                  color="secondary"
                  icon="upload_file"
                  label="Fesih Belgesi Yükle"
                  outline
                  @click="uploadTermDialog = true"
                />
              </PermissionGuard>
            </div>
          </div>
        </q-scroll-area>
      </template>
    </q-drawer>

    <!-- ── Yeni Sözleşme Dialog ── -->
    <q-dialog
      v-model="createDialog"
      persistent
      :maximized="$q.screen.lt.sm"
      transition-show="slide-up"
      transition-hide="slide-down"
    >
      <q-card :style="$q.screen.gt.xs ? 'width: 520px; max-width: 95vw' : ''">
        <q-toolbar class="bg-primary text-white">
          <q-icon name="description" class="q-mr-sm" />
          <q-toolbar-title>Yeni Sözleşme Oluştur</q-toolbar-title>
          <q-btn flat round dense icon="close" color="white" v-close-popup />
        </q-toolbar>

        <q-card-section class="q-pt-lg q-gutter-md">
          <q-select
            v-model="createForm.studentId"
            :options="studentOpts.options.value"
            :loading="studentOpts.loading.value"
            label="Öğrenci *"
            filled
            use-input
            input-debounce="0"
            emit-value
            map-options
            option-label="label"
            option-value="value"
            @filter="studentOpts.filter"
          >
            <template #prepend><q-icon name="school" /></template>
            <template #option="{ itemProps, opt }">
              <q-item v-bind="itemProps">
                <q-item-section>
                  <q-item-label>{{ opt.label }}</q-item-label>
                  <q-item-label v-if="opt.caption" caption>{{ opt.caption }}</q-item-label>
                </q-item-section>
              </q-item>
            </template>
            <template #no-option>
              <q-item><q-item-section class="text-grey">Sonuç bulunamadı</q-item-section></q-item>
            </template>
          </q-select>

          <q-select
            v-model="createForm.businessId"
            :options="businessOpts.options.value"
            :loading="businessOpts.loading.value"
            label="İşletme *"
            filled
            use-input
            input-debounce="0"
            emit-value
            map-options
            option-label="label"
            option-value="value"
            @filter="businessOpts.filter"
          >
            <template #prepend><q-icon name="business" /></template>
            <template #option="{ itemProps, opt }">
              <q-item v-bind="itemProps">
                <q-item-section>
                  <q-item-label>{{ opt.label }}</q-item-label>
                  <q-item-label v-if="opt.caption" caption>{{ opt.caption }}</q-item-label>
                </q-item-section>
              </q-item>
            </template>
            <template #no-option>
              <q-item><q-item-section class="text-grey">Sonuç bulunamadı</q-item-section></q-item>
            </template>
          </q-select>

          <q-select
            v-model="createForm.teacherId"
            :options="teacherOpts.options.value"
            :loading="teacherOpts.loading.value"
            label="Koordinatör Öğretmen (opsiyonel)"
            filled
            use-input
            input-debounce="0"
            emit-value
            map-options
            option-label="label"
            option-value="value"
            clearable
            @filter="teacherOpts.filter"
          >
            <template #prepend><q-icon name="person" /></template>
            <template #option="{ itemProps, opt }">
              <q-item v-bind="itemProps">
                <q-item-section><q-item-label>{{ opt.label }}</q-item-label></q-item-section>
              </q-item>
            </template>
            <template #no-option>
              <q-item><q-item-section class="text-grey">Sonuç bulunamadı</q-item-section></q-item>
            </template>
          </q-select>

          <q-input v-model="createForm.startDate" label="Başlangıç Tarihi *" filled type="date">
            <template #prepend><q-icon name="calendar_today" /></template>
          </q-input>
        </q-card-section>

        <q-separator />
        <q-card-actions align="right" class="q-pa-md">
          <q-btn flat label="İptal" color="grey-7" v-close-popup />
          <q-btn unelevated color="primary" label="Oluştur" :loading="saving" @click="createContract" />
        </q-card-actions>
      </q-card>
    </q-dialog>

    <!-- ── İmzala Dialog ── -->
    <q-dialog
      v-model="signDialog"
      persistent
      :maximized="$q.screen.lt.sm"
      transition-show="slide-up"
      transition-hide="slide-down"
    >
      <q-card :style="$q.screen.gt.xs ? 'width: 420px; max-width: 95vw' : ''">
        <q-toolbar class="bg-teal text-white">
          <q-icon name="draw" class="q-mr-sm" />
          <q-toolbar-title>Sözleşme İmzala</q-toolbar-title>
          <q-btn flat round dense icon="close" color="white" v-close-popup />
        </q-toolbar>

        <q-card-section class="q-pt-lg q-gutter-md">
          <q-select
            v-model="signForm.party"
            :options="partyOptions"
            label="İmzacı Taraf"
            filled
            emit-value
            map-options
          >
            <template #prepend><q-icon name="group" /></template>
          </q-select>
          <q-input v-model="signForm.signedBy" label="İmzalayan Adı" filled>
            <template #prepend><q-icon name="badge" /></template>
          </q-input>
        </q-card-section>

        <q-separator />
        <q-card-actions align="right" class="q-pa-md">
          <q-btn flat label="İptal" color="grey-7" v-close-popup />
          <q-btn unelevated color="teal" label="İmzala" :loading="saving" @click="doSign" />
        </q-card-actions>
      </q-card>
    </q-dialog>

    <!-- ── Askıya Al Dialog ── -->
    <q-dialog
      v-model="suspendDialog"
      persistent
      :maximized="$q.screen.lt.sm"
      transition-show="slide-up"
      transition-hide="slide-down"
    >
      <q-card :style="$q.screen.gt.xs ? 'width: 420px; max-width: 95vw' : ''">
        <q-toolbar class="bg-orange text-white">
          <q-icon name="pause_circle" class="q-mr-sm" />
          <q-toolbar-title>Sözleşmeyi Askıya Al</q-toolbar-title>
          <q-btn flat round dense icon="close" color="white" v-close-popup />
        </q-toolbar>

        <q-card-section class="q-pt-lg">
          <q-input
            v-model="suspendReason"
            label="Askıya alma gerekçesi"
            filled
            type="textarea"
            :rows="3"
            autogrow
          >
            <template #prepend><q-icon name="notes" /></template>
          </q-input>
        </q-card-section>

        <q-separator />
        <q-card-actions align="right" class="q-pa-md">
          <q-btn flat label="İptal" color="grey-7" v-close-popup />
          <q-btn unelevated color="orange" label="Askıya Al" :loading="saving" @click="doSuspend" />
        </q-card-actions>
      </q-card>
    </q-dialog>

    <!-- ── Feshet Dialog ── -->
    <q-dialog
      v-model="terminateDialog"
      persistent
      :maximized="$q.screen.lt.sm"
      transition-show="slide-up"
      transition-hide="slide-down"
    >
      <q-card :style="$q.screen.gt.xs ? 'width: 460px; max-width: 95vw' : ''">
        <q-toolbar class="bg-negative text-white">
          <q-icon name="gavel" class="q-mr-sm" />
          <q-toolbar-title>Sözleşmeyi Feshet</q-toolbar-title>
          <q-btn flat round dense icon="close" color="white" v-close-popup />
        </q-toolbar>

        <q-card-section class="q-pt-lg q-gutter-md">
          <q-select
            v-model="terminateForm.reasonType"
            :options="terminationReasonOptions"
            label="Fesih Sebebi *"
            filled
            emit-value
            map-options
          >
            <template #prepend><q-icon name="category" /></template>
          </q-select>
          <q-input
            v-model="terminateForm.reason"
            label="Açıklama"
            filled
            type="textarea"
            :rows="3"
            autogrow
          >
            <template #prepend><q-icon name="notes" /></template>
          </q-input>
        </q-card-section>

        <q-separator />
        <q-card-actions align="right" class="q-pa-md">
          <q-btn flat label="İptal" color="grey-7" v-close-popup />
          <q-btn unelevated color="negative" label="Feshet" :loading="saving" @click="doTerminate" />
        </q-card-actions>
      </q-card>
    </q-dialog>

    <!-- ── Islak İmzalı Belge Yükleme ── -->
    <q-dialog
      v-model="uploadSignedDialog"
      persistent
      :maximized="$q.screen.lt.sm"
      transition-show="slide-up"
      transition-hide="slide-down"
    >
      <q-card :style="$q.screen.gt.xs ? 'width: 420px; max-width: 95vw' : ''">
        <q-toolbar class="bg-secondary text-white">
          <q-icon name="upload_file" class="q-mr-sm" />
          <q-toolbar-title>Islak İmzalı Belge Yükle</q-toolbar-title>
          <q-btn flat round dense icon="close" color="white" v-close-popup />
        </q-toolbar>

        <q-card-section class="q-pt-lg q-gutter-md">
          <q-file v-model="uploadFile" label="PDF veya Görsel Seç" filled accept=".pdf,.jpg,.jpeg,.png">
            <template #prepend><q-icon name="attach_file" /></template>
          </q-file>
          <q-input v-model="uploadedBy" label="Yükleyen" filled>
            <template #prepend><q-icon name="person" /></template>
          </q-input>
        </q-card-section>

        <q-separator />
        <q-card-actions align="right" class="q-pa-md">
          <q-btn flat label="İptal" color="grey-7" v-close-popup />
          <q-btn unelevated color="secondary" label="Yükle" :loading="saving" @click="doUploadSigned" />
        </q-card-actions>
      </q-card>
    </q-dialog>

    <!-- ── Fesih Belgesi Yükleme ── -->
    <q-dialog
      v-model="uploadTermDialog"
      persistent
      :maximized="$q.screen.lt.sm"
      transition-show="slide-up"
      transition-hide="slide-down"
    >
      <q-card :style="$q.screen.gt.xs ? 'width: 420px; max-width: 95vw' : ''">
        <q-toolbar class="bg-deep-orange text-white">
          <q-icon name="upload_file" class="q-mr-sm" />
          <q-toolbar-title>Fesih Belgesi Yükle</q-toolbar-title>
          <q-btn flat round dense icon="close" color="white" v-close-popup />
        </q-toolbar>

        <q-card-section class="q-pt-lg q-gutter-md">
          <q-file v-model="uploadFile" label="PDF veya Görsel Seç" filled accept=".pdf,.jpg,.jpeg,.png">
            <template #prepend><q-icon name="attach_file" /></template>
          </q-file>
          <q-input v-model="uploadedBy" label="Yükleyen" filled>
            <template #prepend><q-icon name="person" /></template>
          </q-input>
        </q-card-section>

        <q-separator />
        <q-card-actions align="right" class="q-pa-md">
          <q-btn flat label="İptal" color="grey-7" v-close-popup />
          <q-btn unelevated color="deep-orange" label="Yükle" :loading="saving" @click="doUploadTermination" />
        </q-card-actions>
      </q-card>
    </q-dialog>
  </q-page>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, reactive } from 'vue'
import { useQuasar } from 'quasar'
import type { QTableProps } from 'quasar'
import { contractApi, type InternshipContractDto, TERMINATION_REASONS } from 'src/api/contract'
import { useNotify } from 'src/composables/useNotify'
import { useStudentOptions, useBusinessOptions, useTeacherOptions } from 'src/composables/useEntityOptions'
import { Permissions } from 'utils/permissions'
import AppTable from 'components/AppTable.vue'
import StatusBadge from 'components/StatusBadge.vue'
import PermissionGuard from 'components/PermissionGuard.vue'
import { useAuthStore } from 'stores/auth'

const $q = useQuasar()
const notify = useNotify()
const authStore = useAuthStore()
const studentOpts = useStudentOptions()
const businessOpts = useBusinessOptions()
const teacherOpts = useTeacherOptions()

const loading = ref(false)
const saving = ref(false)
const contracts = ref<InternshipContractDto[]>([])
const selected = ref<InternshipContractDto | null>(null)
const detailOpen = ref(false)
const createDialog = ref(false)
const signDialog = ref(false)
const suspendDialog = ref(false)
const terminateDialog = ref(false)
const uploadSignedDialog = ref(false)
const uploadTermDialog = ref(false)
const statusFilter = ref<string | null>(null)
const suspendReason = ref('')
const uploadFile = ref<File | null>(null)
const uploadedBy = ref('')

const statusOptions = [
  { label: 'Taslak', value: 'Draft' },
  { label: 'İmza Bekliyor', value: 'AwaitingSignature' },
  { label: 'Aktif', value: 'Active' },
  { label: 'Askıda', value: 'Suspended' },
  { label: 'Feshedildi', value: 'Terminated' },
  { label: 'Tamamlandı', value: 'Completed' },
]

const partyOptions = [
  { label: 'Kurum', value: 'Institution' },
  { label: 'İşletme', value: 'Business' },
  { label: 'Öğrenci', value: 'Student' },
]

const terminationReasonOptions = TERMINATION_REASONS.map((r) => ({ label: r.label, value: r.value }))

const createForm = reactive({
  studentId: '',
  businessId: '',
  teacherId: '',
  startDate: '',
})

const signForm = reactive<{ party: 'Institution' | 'Business' | 'Student'; signedBy: string }>({
  party: 'Institution',
  signedBy: '',
})

const terminateForm = reactive({
  reasonType: '',
  reason: '',
})

const signatureList = computed(() =>
  selected.value
    ? [
        { label: 'Kurum', dto: selected.value.institutionSignature },
        { label: 'İşletme', dto: selected.value.businessSignature },
        { label: 'Öğrenci', dto: selected.value.studentSignature },
      ]
    : [],
)

const columns: QTableProps['columns'] = [
  { name: 'id', label: 'ID', field: (row) => (row as InternshipContractDto).id.slice(0, 8) + '…', align: 'left' },
  { name: 'statusSlug', label: 'Durum', field: 'statusSlug', align: 'left' },
  { name: 'startDate', label: 'Dönem', field: 'startDate', align: 'left' },
  { name: 'signatures', label: 'İmzalar', field: 'institutionSignature', align: 'center' },
  { name: 'actions', label: '', field: 'id', align: 'right' },
]

function formatDate(iso: string | null | undefined) {
  if (!iso) return '—'
  return new Date(iso).toLocaleDateString('tr-TR', { day: '2-digit', month: '2-digit', year: 'numeric' })
}

async function load() {
  loading.value = true
  try {
    const res = await contractApi.list({ status: statusFilter.value ?? undefined })
    contracts.value = res.data
  } catch {
    notify.error('Sözleşmeler yüklenirken bir hata oluştu.')
  } finally {
    loading.value = false
  }
}

function openCreateDialog() {
  createForm.studentId = ''
  createForm.businessId = ''
  createForm.teacherId = ''
  createForm.startDate = ''
  studentOpts.reset()
  studentOpts.load()
  businessOpts.reset()
  businessOpts.load()
  teacherOpts.reset()
  teacherOpts.load()
  createDialog.value = true
}

function openDetail(row: InternshipContractDto) {
  selected.value = row
  detailOpen.value = true
}

async function createContract() {
  saving.value = true
  try {
    await contractApi.create({
      studentId: createForm.studentId,
      businessId: createForm.businessId,
      institutionId: authStore.user?.institutionId ?? '',
      teacherId: createForm.teacherId || undefined,
      startDate: new Date(createForm.startDate).toISOString(),
    })
    notify.success('Sözleşme oluşturuldu.')
    createDialog.value = false
    await load()
  } catch {
    notify.error('Sözleşme oluşturulurken bir hata oluştu.')
  } finally {
    saving.value = false
  }
}

async function doSubmit() {
  if (!selected.value) return
  saving.value = true
  try {
    await contractApi.submit(selected.value.id)
    notify.success('Sözleşme imzaya gönderildi.')
    await refreshSelected()
  } catch {
    notify.error('İşlem sırasında bir hata oluştu.')
  } finally {
    saving.value = false
  }
}

async function doSign() {
  if (!selected.value) return
  saving.value = true
  try {
    await contractApi.sign(selected.value.id, { party: signForm.party, signedBy: signForm.signedBy })
    notify.success('Sözleşme imzalandı.')
    signDialog.value = false
    await refreshSelected()
  } catch {
    notify.error('İmzalama sırasında bir hata oluştu.')
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
  } catch {
    notify.error('İşlem sırasında bir hata oluştu.')
  } finally {
    saving.value = false
  }
}

async function doSuspend() {
  if (!selected.value) return
  saving.value = true
  try {
    await contractApi.suspend(selected.value.id, { reason: suspendReason.value })
    notify.success('Sözleşme askıya alındı.')
    suspendDialog.value = false
    await refreshSelected()
  } catch {
    notify.error('İşlem sırasında bir hata oluştu.')
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
  } catch {
    notify.error('İşlem sırasında bir hata oluştu.')
  } finally {
    saving.value = false
  }
}

async function doTerminate() {
  if (!selected.value) return
  saving.value = true
  try {
    await contractApi.terminate(selected.value.id, {
      reason: terminateForm.reason,
      reasonType: terminateForm.reasonType,
    })
    notify.success('Sözleşme feshedildi.')
    terminateDialog.value = false
    await refreshSelected()
  } catch {
    notify.error('Fesih sırasında bir hata oluştu.')
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
  } catch {
    notify.error('İşlem sırasında bir hata oluştu.')
  } finally {
    saving.value = false
  }
}

async function doUploadSigned() {
  if (!selected.value || !uploadFile.value) return
  saving.value = true
  try {
    await contractApi.uploadSigned(selected.value.id, uploadFile.value, uploadedBy.value)
    notify.success('Islak imzalı belge yüklendi.')
    uploadSignedDialog.value = false
    uploadFile.value = null
  } catch {
    notify.error('Belge yüklenirken bir hata oluştu.')
  } finally {
    saving.value = false
  }
}

async function doUploadTermination() {
  if (!selected.value || !uploadFile.value) return
  saving.value = true
  try {
    await contractApi.uploadTermination(selected.value.id, uploadFile.value, uploadedBy.value)
    notify.success('Fesih belgesi yüklendi.')
    uploadTermDialog.value = false
    uploadFile.value = null
  } catch {
    notify.error('Belge yüklenirken bir hata oluştu.')
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

onMounted(load)
</script>
