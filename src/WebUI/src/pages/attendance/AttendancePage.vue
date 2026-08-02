<template>
  <q-page padding>
    <PageHeader title="Devamsızlık">
      <PermissionGuard :permission="Permissions.Attendance.Manage">
        <q-btn
          :disable="periodStore.isReadOnly"
          color="primary"
          icon="add"
          label="Devamsızlık Ekle"
          @click="openAddDialog"
        />
      </PermissionGuard>
    </PageHeader>

    <AppNotice
      v-if="periodStore.isReadOnly"
      type="readonly"
      class="q-mb-md"
      message="Bu dönem kapatılmıştır — yalnızca görüntüleme yapılabilir."
    />

    <!-- Filtreler -->
    <div class="row q-gutter-sm q-mb-md">
      <q-select
        v-model="studentIdFilter"
        :options="filterStudentOpts.options.value"
        :loading="filterStudentOpts.loading.value"
        label="Öğrenci"
        outlined
        dense
        use-input
        input-debounce="0"
        emit-value
        map-options
        option-label="label"
        option-value="value"
        clearable
        style="min-width: 250px"
        @filter="filterStudentOpts.filter"
        @update:model-value="load"
      >
        <template #option="{ itemProps, opt }">
          <q-item v-bind="itemProps">
            <q-item-section>
              <q-item-label>{{ opt.label }}</q-item-label>
              <q-item-label
                v-if="opt.caption"
                caption
              >
                {{ opt.caption }}
              </q-item-label>
            </q-item-section>
          </q-item>
        </template>
        <template #no-option>
          <SelectEmptyOption />
        </template>
      </q-select>
      <q-select
        v-model="statusFilter"
        :options="statusOptions"
        label="Durum"
        outlined
        dense
        emit-value
        map-options
        clearable
        style="min-width: 150px"
        @update:model-value="load"
      />
      <q-select
        v-model="monthFilter"
        :options="monthOptions"
        label="Ay"
        outlined
        dense
        emit-value
        map-options
        clearable
        style="min-width: 130px"
        @update:model-value="load"
      />
      <q-select
        v-model="yearFilter"
        :options="yearOptions"
        label="Yıl"
        outlined
        dense
        emit-value
        map-options
        clearable
        style="min-width: 100px"
        @update:model-value="load"
      />
      <BranchSelector
        v-model="branchFilter"
        dense
        force-select
        style="min-width: 200px"
        @update:model-value="load"
      />
    </div>

    <AppTable
      :rows="records"
      :columns="columns"
      :loading="loading"
      :pagination="pagination"
      @request="onRequest"
    >
      <template #body-cell-student="{ row }">
        <q-td>
          <div class="text-weight-medium">
            {{ studentMap[row.studentId]?.fullName ?? '—' }}
          </div>
          <div
            v-if="studentMap[row.studentId]?.info"
            class="text-caption text-grey-6"
          >
            {{ studentMap[row.studentId].info }}
          </div>
        </q-td>
      </template>
      <template #body-cell-business="{ row }">
        <q-td>{{ businessMap[row.businessId] ?? '—' }}</q-td>
      </template>
      <template #body-cell-date="{ row }">
        <q-td>{{ formatDate(row.date) }}</q-td>
      </template>
      <template #body-cell-absenceTypeSlug="{ row }">
        <q-td><StatusBadge :slug="row.absenceTypeSlug" /></q-td>
      </template>
      <template #body-cell-statusSlug="{ row }">
        <q-td><StatusBadge :slug="row.statusSlug" /></q-td>
      </template>
      <template #body-cell-healthReport="{ row }">
        <q-td>
          <StatusBadge
            v-if="row.healthReportStatus !== 'None'"
            :slug="row.healthReportStatusSlug"
          />
          <span
            v-else
            class="text-grey-6"
          >—</span>
          <q-tooltip v-if="row.healthReportRejectionReason">
            {{ row.healthReportRejectionReason }}
          </q-tooltip>
        </q-td>
      </template>
      <template #body-cell-actions="{ row }">
        <q-td class="text-right">
          <PermissionGuard :permission="Permissions.Attendance.Upload">
            <q-btn
              v-if="row.healthReportStatus !== 'Pending'"
              :disable="periodStore.isReadOnly"
              flat
              round
              dense
              icon="medical_information"
              aria-label="Sağlık raporu yükle"
              @click="openHealthReportUpload(row)"
            >
              <q-tooltip>Sağlık raporu yükle</q-tooltip>
            </q-btn>
          </PermissionGuard>
          <PermissionGuard :permission="Permissions.Attendance.Approve">
            <q-btn
              v-if="row.healthReportStatus === 'Pending'"
              flat
              round
              dense
              icon="fact_check"
              color="positive"
              aria-label="Sağlık raporunu onayla"
              @click="approveHealthReport(row)"
            >
              <q-tooltip>Sağlık raporunu onayla</q-tooltip>
            </q-btn>
          </PermissionGuard>
          <PermissionGuard :permission="Permissions.Attendance.Approve">
            <q-btn
              v-if="row.healthReportStatus === 'Pending'"
              flat
              round
              dense
              icon="report_off"
              color="negative"
              aria-label="Sağlık raporunu reddet"
              @click="openHealthReportReject(row)"
            >
              <q-tooltip>Sağlık raporunu reddet</q-tooltip>
            </q-btn>
          </PermissionGuard>
          <PermissionGuard :permission="Permissions.Attendance.Approve">
            <q-btn
              v-if="row.status === 'Pending'"
              flat
              round
              dense
              icon="thumb_up"
              color="primary"
              aria-label="Onayla"
              @click="approve(row)"
            >
              <q-tooltip>Onayla</q-tooltip>
            </q-btn>
          </PermissionGuard>
          <PermissionGuard :permission="Permissions.Attendance.Approve">
            <q-btn
              v-if="row.status === 'Recorded'"
              flat
              round
              dense
              icon="check"
              color="positive"
              aria-label="Doğrula"
              @click="verify(row)"
            >
              <q-tooltip>Doğrula</q-tooltip>
            </q-btn>
          </PermissionGuard>
          <!-- Düzeltme türü değiştirir, yani kesintiyi kaldırabilir: okul tarafı izni (#172). -->
          <PermissionGuard :permission="Permissions.Attendance.DirectEntry">
            <q-btn
              flat
              round
              dense
              icon="edit"
              aria-label="Düzelt"
              @click="openCorrect(row)"
            >
              <q-tooltip>Düzelt</q-tooltip>
            </q-btn>
          </PermissionGuard>
          <PermissionGuard :permission="Permissions.Attendance.Delete">
            <q-btn
              v-if="isWithinDeleteWindow(row.date)"
              flat
              round
              dense
              icon="delete"
              color="negative"
              aria-label="Sil"
              @click="confirmDelete(row)"
            >
              <q-tooltip>Sil</q-tooltip>
            </q-btn>
          </PermissionGuard>
        </q-td>
      </template>
    </AppTable>

    <CorrectAttendanceForm
      v-model="correctDialog"
      :record-id="selected?.id ?? ''"
      :absence-type="selected?.absenceType ?? 'Unexcused'"
      :reason="selected?.reason ?? ''"
      @saved="load"
    />

    <HealthReportUploadForm
      v-model="healthReportDialog"
      :record-id="selected?.id ?? ''"
      :requires-approval="!canEnterHealthReportDirectly"
      @saved="load"
    />

    <HealthReportRejectForm
      v-model="healthReportRejectDialog"
      :record-id="selected?.id ?? ''"
      @saved="load"
    />
  </q-page>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, watch } from 'vue'
import type { QTableProps } from 'quasar'

import { attendanceApi, type AttendanceRecordDto } from 'src/api/attendance'
import { useNotify } from 'src/composables/useNotify'
import { useServerPagination } from 'src/composables/useServerPagination'
import { useStudentOptions, useBusinessOptions } from 'src/composables/useEntityOptions'
import { useAcademicPeriodStore } from 'stores/academicPeriod'
import { Permissions } from 'utils/permissions'
import AppTable from 'components/AppTable.vue'
import StatusBadge from 'components/StatusBadge.vue'
import PermissionGuard from 'components/PermissionGuard.vue'
import BranchSelector from 'components/BranchSelector.vue'
import PageHeader from 'components/PageHeader.vue'
import SelectEmptyOption from 'components/SelectEmptyOption.vue'
import { useConfirmDialog } from 'src/composables/useConfirmDialog'
import { useRouter } from 'vue-router'
import CorrectAttendanceForm from 'components/forms/attendance/CorrectAttendanceForm.vue'
import HealthReportUploadForm from 'components/forms/attendance/HealthReportUploadForm.vue'
import HealthReportRejectForm from 'components/forms/attendance/HealthReportRejectForm.vue'
import AppNotice from 'components/AppNotice.vue'
import { useAuthStore } from 'stores/auth'

const notify = useNotify()
const router = useRouter()
const confirmDialog = useConfirmDialog()
const periodStore = useAcademicPeriodStore()
const authStore = useAuthStore()
const filterStudentOpts = useStudentOptions()
const businessOpts = useBusinessOptions()
const saving = ref(false)
const selected = ref<AttendanceRecordDto | null>(null)
const correctDialog = ref(false)
const healthReportDialog = ref(false)
const healthReportRejectDialog = ref(false)

// Yükleyende bu izin yoksa rapor onaya düşer (#172) — kullanıcıya yükleme formunda söylenir.
const canEnterHealthReportDirectly = computed(() =>
  authStore.hasPermission(Permissions.Attendance.HealthReportDirect),
)
const studentIdFilter = ref('')
const statusFilter = ref<string | null>(null)
const monthFilter = ref<number | null>(null)
const yearFilter = ref<number | null>(null)
const branchFilter = ref<string | null>(null)

const filters = computed(() => ({
  academicPeriodId: periodStore.selectedPeriodId ?? undefined,
  studentId: studentIdFilter.value || undefined,
  status: statusFilter.value ?? undefined,
  year: yearFilter.value ?? undefined,
  month: monthFilter.value ?? undefined,
  branchCode: branchFilter.value || undefined,
}))

const { rows: records, loading, pagination, onRequest, load } = useServerPagination<AttendanceRecordDto>({
  fetchFn: (params) => attendanceApi.list(params),
  filters,
  defaultSortBy: 'date',
  defaultDescending: true,
})

// ID → metadata lookup map'leri (tablo satırlarında isim göstermek için)
const studentMap = computed<Record<string, { fullName: string; info: string; branchCode: string }>>(() => {
  const map: Record<string, { fullName: string; info: string; branchCode: string }> = {}
  for (const opt of filterStudentOpts.allOptions.value) {
    // caption format: "BranchCode · ClassYear/Section"
    const branchCode = opt.caption?.split(' · ')[0] ?? ''
    map[opt.value] = { fullName: opt.label, info: opt.caption ?? '', branchCode }
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

const statusOptions = [
  { label: 'Onay Bekliyor', value: 'Pending' },
  { label: 'Kaydedildi', value: 'Recorded' },
  { label: 'Doğrulandı', value: 'Verified' },
  { label: 'Düzeltildi', value: 'Corrected' },
]

const monthOptions = [
  { label: 'Ocak', value: 1 }, { label: 'Şubat', value: 2 },
  { label: 'Mart', value: 3 }, { label: 'Nisan', value: 4 },
  { label: 'Mayıs', value: 5 }, { label: 'Haziran', value: 6 },
  { label: 'Temmuz', value: 7 }, { label: 'Ağustos', value: 8 },
  { label: 'Eylül', value: 9 }, { label: 'Ekim', value: 10 },
  { label: 'Kasım', value: 11 }, { label: 'Aralık', value: 12 },
]

const currentYear = new Date().getFullYear()
const yearOptions = [
  { label: String(currentYear - 1), value: currentYear - 1 },
  { label: String(currentYear), value: currentYear },
  { label: String(currentYear + 1), value: currentYear + 1 },
]


const columns: QTableProps['columns'] = [
  { name: 'date', label: 'Tarih', field: 'date', align: 'left', sortable: true },
  { name: 'student', label: 'Öğrenci', field: 'studentId', align: 'left' },
  { name: 'business', label: 'İşletme', field: 'businessId', align: 'left' },
  { name: 'absenceTypeSlug', label: 'Tür', field: 'absenceTypeSlug', align: 'left' },
  { name: 'statusSlug', label: 'Durum', field: 'statusSlug', align: 'left' },
  { name: 'healthReport', label: 'Sağlık Raporu', field: 'healthReportStatusSlug', align: 'left' },
  { name: 'actions', label: '', field: 'id', align: 'right' },
]

function formatDate(iso: string) {
  return new Date(iso).toLocaleDateString('tr-TR', { day: '2-digit', month: '2-digit', year: 'numeric' })
}

function isWithinDeleteWindow(dateStr: string) {
  const recordDate = new Date(dateStr)
  const now = new Date()
  const diffDays = Math.floor((now.getTime() - recordDate.getTime()) / (1000 * 60 * 60 * 24))
  return diffDays <= 7
}

function confirmDelete(row: AttendanceRecordDto) {
  confirmDialog.confirm({
    title: 'Devamsızlık Kaydını Sil',
    message: `${formatDate(row.date)} tarihli devamsızlık kaydını silmek istediğinize emin misiniz?`,
    okLabel: 'Sil',
    onOk: async () => {
      saving.value = true
      try {
        await attendanceApi.remove(row.id)
        notify.success('Devamsızlık kaydı silindi.')
        await load()
      } catch (e) {
        notify.apiError(e, 'Silme sırasında bir hata oluştu.')
      } finally {
        saving.value = false
      }
    },
  })
}


function openAddDialog() {
  router.push('/attendance/new').catch(() => {})
}

async function approve(row: AttendanceRecordDto) {
  saving.value = true
  try {
    await attendanceApi.approve(row.id)
    notify.success('Devamsızlık onaylandı.')
    await load()
  } catch (e) {
    notify.apiError(e, 'Onaylama sırasında bir hata oluştu.')
  } finally {
    saving.value = false
  }
}

async function verify(row: AttendanceRecordDto) {
  saving.value = true
  try {
    await attendanceApi.verify(row.id)
    notify.success('Devamsızlık doğrulandı.')
    await load()
  } catch (e) {
    notify.apiError(e, 'Doğrulama sırasında bir hata oluştu.')
  } finally {
    saving.value = false
  }
}

function openCorrect(row: AttendanceRecordDto) {
  selected.value = row
  correctDialog.value = true
}

function openHealthReportUpload(row: AttendanceRecordDto) {
  selected.value = row
  healthReportDialog.value = true
}

function openHealthReportReject(row: AttendanceRecordDto) {
  selected.value = row
  healthReportRejectDialog.value = true
}

async function approveHealthReport(row: AttendanceRecordDto) {
  saving.value = true
  try {
    await attendanceApi.approveHealthReport(row.id)
    notify.success('Sağlık raporu onaylandı — bu gün için ücret kesintisi uygulanmayacak.')
    await load()
  } catch (e) {
    notify.apiError(e, 'Onaylama sırasında bir hata oluştu.')
  } finally {
    saving.value = false
  }
}

watch(() => periodStore.selectedPeriodId, () => load())

onMounted(() => {
  filterStudentOpts.load().catch(() => {})
  businessOpts.load().catch(() => {})
  // BranchSelector kendi onMounted'ında alan listesini yükler.
  load().catch(() => {})
})
</script>
