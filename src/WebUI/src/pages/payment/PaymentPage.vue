<template>
  <q-page padding>
    <div class="text-h5 text-weight-bold q-mb-lg">Maaş / Dekont</div>

    <!-- Filtreler -->
    <div class="row q-gutter-sm q-mb-md">
      <q-input v-model="studentIdFilter" label="Öğrenci ID" filled dense clearable style="min-width: 200px" />
      <q-select
        v-model="phaseFilter"
        :options="phaseOptions"
        label="Aşama"
        filled dense emit-value map-options clearable
        style="min-width: 200px"
      />
      <q-input v-model="monthFilter" label="Ay (YYYY-MM)" filled dense clearable style="min-width: 140px" />
      <q-btn color="primary" icon="search" label="Ara" @click="load" />
    </div>

    <AppTable :rows="payments" :columns="columns" :loading="loading" :pagination="pagination" @request="onRequest">
      <template #body-cell-phaseSlug="{ row }">
        <q-td><StatusBadge :slug="row.phaseSlug" /></q-td>
      </template>
      <template #body-cell-amounts="{ row }">
        <q-td>
          <div class="text-body2 text-weight-medium">{{ formatCurrency(row.netAmount) }}</div>
          <div class="text-caption text-grey">Brüt: {{ formatCurrency(row.baseWage) }}</div>
        </q-td>
      </template>
      <template #body-cell-actions="{ row }">
        <q-td class="text-right">
          <q-btn flat round dense icon="visibility" @click="openDetail(row)" />
        </q-td>
      </template>
    </AppTable>

    <!-- Detay Drawer -->
    <q-drawer v-model="detailOpen" side="right" bordered :width="480" overlay>
      <template v-if="selected">
        <q-toolbar>
          <q-toolbar-title class="text-subtitle1 text-weight-bold">Ödeme Detayı</q-toolbar-title>
          <q-btn flat round dense icon="close" @click="detailOpen = false" />
        </q-toolbar>
        <q-separator />
        <q-scroll-area class="fit">
          <div class="q-pa-md q-gutter-sm">
            <div class="row items-center q-mb-sm">
              <StatusBadge :slug="selected.phaseSlug" class="q-mr-sm" />
              <span class="text-caption">{{ selected.month }}</span>
            </div>

            <!-- Ödeme tablosu -->
            <q-card flat bordered class="q-mb-sm">
              <q-card-section>
                <div class="row justify-between q-mb-xs">
                  <span class="text-caption">Baz Ücret:</span>
                  <span class="text-weight-medium">{{ formatCurrency(selected.baseWage) }}</span>
                </div>
                <div class="row justify-between q-mb-xs">
                  <span class="text-caption">Kesinti:</span>
                  <span class="text-negative">-{{ formatCurrency(selected.deductionAmount) }}</span>
                </div>
                <q-separator class="q-my-xs" />
                <div class="row justify-between">
                  <span class="text-subtitle2">Net Ücret:</span>
                  <span class="text-subtitle2 text-weight-bold text-primary">{{ formatCurrency(selected.netAmount) }}</span>
                </div>
                <div class="row justify-between q-mt-xs">
                  <span class="text-caption">Devlet Katkısı:</span>
                  <span class="text-caption">{{ formatCurrency(selected.governmentContribution) }}</span>
                </div>
                <div class="row justify-between">
                  <span class="text-caption">İşveren Ödemesi:</span>
                  <span class="text-caption">{{ formatCurrency(selected.employerPayment) }}</span>
                </div>
              </q-card-section>
            </q-card>

            <!-- Onay zinciri -->
            <div class="text-subtitle2 q-mb-xs">Onay Zinciri</div>
            <q-timeline color="primary" layout="dense" class="q-mt-xs">
              <q-timeline-entry
                icon="upload"
                :color="selected.receiptObjectPath ? 'positive' : 'grey-4'"
                title="Dekont Yüklendi"
              />
              <q-timeline-entry
                icon="person"
                :color="selected.studentConfirmedAt ? 'positive' : 'grey-4'"
                title="Öğrenci Onayı"
              />
              <q-timeline-entry
                icon="school"
                :color="selected.teacherApprovedAt ? 'positive' : 'grey-4'"
                title="Öğretmen Onayı"
              />
              <q-timeline-entry
                icon="badge"
                :color="selected.deputyApprovedAt ? 'positive' : 'grey-4'"
                title="Müdür Yardımcısı Onayı"
              />
            </q-timeline>

            <!-- Eylemler -->
            <div class="q-gutter-sm q-mt-sm">
              <PermissionGuard :permission="Permissions.Company.UploadReceipt">
                <q-btn
                  v-if="selected.phase === 'AwaitingReceipt' || selected.phase === 'Calculated'"
                  color="secondary"
                  icon="upload"
                  label="Dekont Yükle (İşletme)"
                  @click="uploadReceiptDialog = true; uploadType = 'business'"
                />
              </PermissionGuard>
              <PermissionGuard :permission="Permissions.Salary.Receipt">
                <q-btn
                  v-if="selected.phase === 'AwaitingReceipt'"
                  color="secondary"
                  icon="upload"
                  label="Dekont Yükle (Öğrenci)"
                  @click="uploadReceiptDialog = true; uploadType = 'student'"
                />
              </PermissionGuard>
              <PermissionGuard :permission="Permissions.Salary.ViewOwn">
                <q-btn
                  v-if="selected.phase === 'ReceiptUploaded'"
                  color="primary"
                  icon="check"
                  label="Onayla (Öğrenci)"
                  :loading="saving"
                  @click="doConfirm"
                />
              </PermissionGuard>
              <PermissionGuard :permission="Permissions.Salary.Approve">
                <q-btn
                  v-if="selected.phase === 'StudentConfirmed'"
                  color="positive"
                  icon="check_circle"
                  label="Öğretmen Onayı"
                  :loading="saving"
                  @click="doApproveTeacher"
                />
                <q-btn
                  v-if="selected.phase === 'TeacherApproved'"
                  color="positive"
                  icon="verified"
                  label="Müd. Yrd. Onayı"
                  :loading="saving"
                  @click="doApproveDeputy"
                />
                <q-btn
                  v-if="['ReceiptUploaded','StudentConfirmed','TeacherApproved'].includes(selected.phase)"
                  color="negative"
                  icon="cancel"
                  label="Reddet"
                  @click="rejectDialog = true"
                />
              </PermissionGuard>
            </div>
          </div>
        </q-scroll-area>
      </template>
    </q-drawer>

    <!-- Dekont Yükleme Dialog -->
    <q-dialog v-model="uploadReceiptDialog" persistent :maximized="$q.screen.lt.sm" transition-show="slide-up" transition-hide="slide-down">
      <q-card :style="$q.screen.gt.xs ? 'width: 400px; max-width: 95vw' : ''">
        <q-toolbar class="bg-secondary text-white">
          <q-icon name="upload_file" class="q-mr-sm" />
          <q-toolbar-title>Dekont Yükle</q-toolbar-title>
          <q-btn flat round dense icon="close" color="white" v-close-popup />
        </q-toolbar>
        <q-card-section class="q-pt-lg q-gutter-md">
          <q-file v-model="uploadFile" label="Dosya Seç" filled accept=".pdf,.jpg,.jpeg,.png">
            <template #prepend>
              <q-icon name="attach_file" />
            </template>
          </q-file>
        </q-card-section>
        <q-separator />
        <q-card-actions align="right" class="q-pa-md">
          <q-btn flat label="İptal" color="grey-7" v-close-popup />
          <q-btn unelevated color="secondary" label="Yükle" :loading="saving" @click="doUploadReceipt" />
        </q-card-actions>
      </q-card>
    </q-dialog>

    <!-- Reddetme Dialog -->
    <q-dialog v-model="rejectDialog" persistent :maximized="$q.screen.lt.sm" transition-show="slide-up" transition-hide="slide-down">
      <q-card :style="$q.screen.gt.xs ? 'width: 400px; max-width: 95vw' : ''">
        <q-toolbar class="bg-negative text-white">
          <q-icon name="cancel" class="q-mr-sm" />
          <q-toolbar-title>Reddetme Gerekçesi</q-toolbar-title>
          <q-btn flat round dense icon="close" color="white" v-close-popup />
        </q-toolbar>
        <q-card-section class="q-pt-lg q-gutter-md">
          <q-input v-model="rejectReason" label="Gerekçe" filled type="textarea" rows="3">
            <template #prepend>
              <q-icon name="notes" />
            </template>
          </q-input>
        </q-card-section>
        <q-separator />
        <q-card-actions align="right" class="q-pa-md">
          <q-btn flat label="İptal" color="grey-7" v-close-popup />
          <q-btn unelevated color="negative" label="Reddet" :loading="saving" @click="doReject" />
        </q-card-actions>
      </q-card>
    </q-dialog>
  </q-page>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import type { QTableProps } from 'quasar'
import { useQuasar } from 'quasar'
import { paymentApi, type PaymentSummaryDto, PAYMENT_PHASES } from 'src/api/payment'
import { useNotify } from 'src/composables/useNotify'
import { useServerPagination } from 'src/composables/useServerPagination'
import { Permissions } from 'utils/permissions'
import AppTable from 'components/AppTable.vue'
import StatusBadge from 'components/StatusBadge.vue'
import PermissionGuard from 'components/PermissionGuard.vue'

const $q = useQuasar()
const notify = useNotify()
const saving = ref(false)
const selected = ref<PaymentSummaryDto | null>(null)
const detailOpen = ref(false)
const uploadReceiptDialog = ref(false)
const rejectDialog = ref(false)
const studentIdFilter = ref('')
const phaseFilter = ref<string | null>(null)
const monthFilter = ref('')
const uploadFile = ref<File | null>(null)
const uploadType = ref<'business' | 'student'>('business')
const rejectReason = ref('')

const phaseOptions = PAYMENT_PHASES.map((p) => ({ label: p.label, value: p.value }))

const filters = computed(() => ({
  studentId: studentIdFilter.value || undefined,
  phase: phaseFilter.value ?? undefined,
  month: monthFilter.value || undefined,
}))

const { rows: payments, loading, pagination, onRequest, load } = useServerPagination<PaymentSummaryDto>({
  fetchFn: (params) => paymentApi.list(params),
  filters,
  defaultSortBy: 'month',
  defaultDescending: true,
})

const columns: QTableProps['columns'] = [
  { name: 'month', label: 'Ay', field: 'month', align: 'left', sortable: true },
  { name: 'amounts', label: 'Net / Brüt', field: 'netAmount', align: 'left' },
  { name: 'phaseSlug', label: 'Aşama', field: 'phaseSlug', align: 'left' },
  { name: 'actions', label: '', field: 'id', align: 'right' },
]

function formatCurrency(amount: number) {
  return amount.toLocaleString('tr-TR', { style: 'currency', currency: 'TRY' })
}

function openDetail(row: PaymentSummaryDto) {
  selected.value = row
  detailOpen.value = true
}

async function doConfirm() {
  if (!selected.value) return
  saving.value = true
  try {
    await paymentApi.confirm(selected.value.id)
    notify.success('Ödeme onaylandı.')
    await refreshSelected()
  } catch (e) {
    notify.apiError(e, 'Onaylama sırasında bir hata oluştu.')
  } finally {
    saving.value = false
  }
}

async function doApproveTeacher() {
  if (!selected.value) return
  saving.value = true
  try {
    await paymentApi.approveTeacher(selected.value.id)
    notify.success('Öğretmen onayı verildi.')
    await refreshSelected()
  } catch (e) {
    notify.apiError(e, 'Onaylama sırasında bir hata oluştu.')
  } finally {
    saving.value = false
  }
}

async function doApproveDeputy() {
  if (!selected.value) return
  saving.value = true
  try {
    await paymentApi.approveDeputy(selected.value.id)
    notify.success('Müdür Yardımcısı onayı verildi.')
    await refreshSelected()
  } catch (e) {
    notify.apiError(e, 'Onaylama sırasında bir hata oluştu.')
  } finally {
    saving.value = false
  }
}

async function doUploadReceipt() {
  if (!selected.value || !uploadFile.value) return
  saving.value = true
  try {
    if (uploadType.value === 'business') {
      await paymentApi.uploadReceiptBusiness(selected.value.id, uploadFile.value)
    } else {
      await paymentApi.uploadReceiptStudent(selected.value.id, uploadFile.value)
    }
    notify.success('Dekont yüklendi.')
    uploadReceiptDialog.value = false
    uploadFile.value = null
    await refreshSelected()
  } catch (e) {
    notify.apiError(e, 'Dekont yüklenirken bir hata oluştu.')
  } finally {
    saving.value = false
  }
}

async function doReject() {
  if (!selected.value) return
  saving.value = true
  try {
    await paymentApi.reject(selected.value.id, rejectReason.value)
    notify.success('Reddedildi.')
    rejectDialog.value = false
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
    const res = await paymentApi.get(selected.value.id)
    selected.value = res.data
  } catch { /* sessiz */ }
  await load()
}

onMounted(load)
</script>
