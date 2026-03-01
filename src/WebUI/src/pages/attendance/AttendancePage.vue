<template>
  <q-page padding>
    <div class="row items-center q-mb-lg">
      <div class="col">
        <div class="text-h5 text-weight-bold">Devamsızlık</div>
      </div>
      <div class="col-auto">
        <PermissionGuard :permission="Permissions.Attendance.Manage">
          <q-btn color="primary" icon="add" label="Devamsızlık Ekle" @click="openAddDialog" />
        </PermissionGuard>
      </div>
    </div>

    <!-- Filtreler -->
    <div class="row q-gutter-sm q-mb-md">
      <q-select
        v-model="studentIdFilter"
        :options="filterStudentOpts.options.value"
        :loading="filterStudentOpts.loading.value"
        label="Öğrenci"
        filled
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
              <q-item-label caption v-if="opt.caption">{{ opt.caption }}</q-item-label>
            </q-item-section>
          </q-item>
        </template>
        <template #no-option>
          <q-item>
            <q-item-section class="text-grey">Sonuç bulunamadı</q-item-section>
          </q-item>
        </template>
      </q-select>
      <q-select
        v-model="statusFilter"
        :options="statusOptions"
        label="Durum"
        filled dense emit-value map-options clearable
        style="min-width: 150px"
        @update:model-value="load"
      />
      <q-select
        v-model="monthFilter"
        :options="monthOptions"
        label="Ay"
        filled dense emit-value map-options clearable
        style="min-width: 130px"
        @update:model-value="load"
      />
      <q-select
        v-model="yearFilter"
        :options="yearOptions"
        label="Yıl"
        filled dense emit-value map-options clearable
        style="min-width: 100px"
        @update:model-value="load"
      />
      <q-select
        v-model="branchFilter"
        :options="branchOpts.options.value"
        :loading="branchOpts.loading.value"
        label="Alan"
        filled dense emit-value map-options clearable
        use-input
        input-debounce="0"
        option-label="label"
        option-value="value"
        style="min-width: 200px"
        @filter="branchOpts.filter"
      />
      <q-btn color="primary" icon="search" label="Ara" @click="load" />
    </div>

    <AppTable :rows="filteredRecords" :columns="columns" :loading="loading" :pagination="pagination" @request="onRequest">
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
      <template #body-cell-date="{ row }">
        <q-td>{{ formatDate(row.date) }}</q-td>
      </template>
      <template #body-cell-absenceTypeSlug="{ row }">
        <q-td><StatusBadge :slug="row.absenceTypeSlug" /></q-td>
      </template>
      <template #body-cell-statusSlug="{ row }">
        <q-td><StatusBadge :slug="row.statusSlug" /></q-td>
      </template>
      <template #body-cell-actions="{ row }">
        <q-td class="text-right">
          <PermissionGuard :permission="Permissions.Attendance.Approve">
            <q-btn
              v-if="row.status === 'Pending'"
              flat round dense icon="thumb_up"
              color="primary"
              title="Onayla"
              @click="approve(row)"
            />
          </PermissionGuard>
          <PermissionGuard :permission="Permissions.Attendance.Approve">
            <q-btn
              v-if="row.status === 'Recorded'"
              flat round dense icon="check"
              color="positive"
              title="Doğrula"
              @click="verify(row)"
            />
          </PermissionGuard>
          <PermissionGuard :permission="Permissions.Attendance.Manage">
            <q-btn
              flat round dense icon="edit"
              @click="openCorrect(row)"
            />
          </PermissionGuard>
          <PermissionGuard :permission="Permissions.Attendance.Delete">
            <q-btn
              v-if="isWithinDeleteWindow(row.date)"
              flat round dense icon="delete"
              color="negative"
              title="Sil"
              @click="confirmDelete(row)"
            />
          </PermissionGuard>
        </q-td>
      </template>
    </AppTable>

    <!-- Devamsızlık Ekle Dialog -->
    <q-dialog v-model="addDialog" persistent :maximized="$q.screen.lt.sm" transition-show="slide-up" transition-hide="slide-down">
      <q-card :style="$q.screen.gt.xs ? 'width: 480px; max-width: 95vw' : ''">
        <q-toolbar class="bg-primary text-white">
          <q-icon name="event_busy" class="q-mr-sm" />
          <q-toolbar-title>Devamsızlık Ekle</q-toolbar-title>
          <q-btn flat round dense icon="close" color="white" v-close-popup />
        </q-toolbar>
        <q-card-section class="q-pt-lg q-gutter-md">
          <q-select
            v-model="addForm.studentId"
            :options="placementOpts.options.value"
            :loading="placementOpts.loading.value"
            label="Öğrenci *"
            filled
            use-input
            input-debounce="0"
            emit-value
            map-options
            option-label="label"
            option-value="value"
            @filter="placementOpts.filter"
          >
            <template #prepend>
              <q-icon name="person" />
            </template>
            <template #option="{ itemProps, opt }">
              <q-item v-bind="itemProps">
                <q-item-section>
                  <q-item-label>{{ opt.label }}</q-item-label>
                  <q-item-label caption v-if="opt.caption">{{ opt.caption }}</q-item-label>
                </q-item-section>
              </q-item>
            </template>
            <template #no-option>
              <q-item>
                <q-item-section class="text-grey">Sonuç bulunamadı</q-item-section>
              </q-item>
            </template>
          </q-select>
          <q-input
            :model-value="addForm.businessName"
            label="İşletme"
            filled
            readonly
            :hint="addForm.businessId ? '' : 'Öğrenci seçildiğinde otomatik doldurulacaktır'"
          >
            <template #prepend>
              <q-icon name="business" />
            </template>
          </q-input>
          <q-input
            v-model="addForm.date" label="Tarih" filled type="date"
            :min="weekBounds.min" :max="weekBounds.max"
            hint="Sadece geçerli hafta içi tarih seçilebilir"
          >
            <template #prepend>
              <q-icon name="calendar_today" />
            </template>
          </q-input>
          <q-select
            v-model="addForm.absenceType"
            :options="absenceTypeOptions"
            label="Devamsızlık Türü"
            filled emit-value map-options
          >
            <template #prepend>
              <q-icon name="category" />
            </template>
          </q-select>
          <q-input v-model="addForm.reason" label="Gerekçe (opsiyonel)" filled>
            <template #prepend>
              <q-icon name="notes" />
            </template>
          </q-input>
        </q-card-section>
        <q-separator />
        <q-card-actions align="right" class="q-pa-md">
          <q-btn flat label="İptal" color="grey-7" v-close-popup />
          <q-btn unelevated color="primary" label="Kaydet" :loading="saving" @click="createRecord" />
        </q-card-actions>
      </q-card>
    </q-dialog>

    <!-- Düzeltme Dialog -->
    <q-dialog v-model="correctDialog" persistent :maximized="$q.screen.lt.sm" transition-show="slide-up" transition-hide="slide-down">
      <q-card :style="$q.screen.gt.xs ? 'width: 400px; max-width: 95vw' : ''">
        <q-toolbar class="bg-orange text-white">
          <q-icon name="edit_calendar" class="q-mr-sm" />
          <q-toolbar-title>Devamsızlık Düzelt</q-toolbar-title>
          <q-btn flat round dense icon="close" color="white" v-close-popup />
        </q-toolbar>
        <q-card-section class="q-pt-lg q-gutter-md">
          <q-select
            v-model="correctForm.absenceType"
            :options="absenceTypeOptions"
            label="Devamsızlık Türü"
            filled emit-value map-options
          >
            <template #prepend>
              <q-icon name="category" />
            </template>
          </q-select>
          <q-input v-model="correctForm.reason" label="Gerekçe" filled>
            <template #prepend>
              <q-icon name="notes" />
            </template>
          </q-input>
        </q-card-section>
        <q-separator />
        <q-card-actions align="right" class="q-pa-md">
          <q-btn flat label="İptal" color="grey-7" v-close-popup />
          <q-btn unelevated color="orange" label="Düzelt" :loading="saving" @click="correctRecord" />
        </q-card-actions>
      </q-card>
    </q-dialog>
  </q-page>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, reactive, watch } from 'vue'
import type { QTableProps } from 'quasar'
import { useQuasar } from 'quasar'
import { attendanceApi, type AttendanceRecordDto, ABSENCE_TYPES } from 'src/api/attendance'
import { useNotify } from 'src/composables/useNotify'
import { useServerPagination } from 'src/composables/useServerPagination'
import { useStudentOptions, useBusinessOptions, usePlacementOptions, useBranchOptions } from 'src/composables/useEntityOptions'
import { useAcademicPeriodStore } from 'stores/academicPeriod'
import { Permissions } from 'utils/permissions'
import AppTable from 'components/AppTable.vue'
import StatusBadge from 'components/StatusBadge.vue'
import PermissionGuard from 'components/PermissionGuard.vue'
import { useAuthStore } from 'stores/auth'

const $q = useQuasar()
const notify = useNotify()
const authStore = useAuthStore()
const periodStore = useAcademicPeriodStore()
const placementOpts = usePlacementOptions()
const filterStudentOpts = useStudentOptions()
const businessOpts = useBusinessOptions()
const branchOpts = useBranchOptions()
const saving = ref(false)
const selected = ref<AttendanceRecordDto | null>(null)
const addDialog = ref(false)
const correctDialog = ref(false)
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

// Alan filtresi: frontend-side filtering
const filteredRecords = computed(() => {
  if (!branchFilter.value) return records.value
  return records.value.filter(r => {
    const student = studentMap.value[r.studentId]
    return student?.branchCode === branchFilter.value
  })
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

const absenceTypeOptions = ABSENCE_TYPES.map((t) => ({ label: t.label, value: t.value }))

// Geçerli hafta sınırları — MEB e-Okul kuralı: sadece bu hafta giriş yapılabilir
const weekBounds = computed(() => {
  const today = new Date()
  const day = today.getDay() // 0=Pazar, 1=Pazartesi ...
  const diffToMonday = day === 0 ? -6 : 1 - day
  const monday = new Date(today)
  monday.setDate(today.getDate() + diffToMonday)
  const sunday = new Date(monday)
  sunday.setDate(monday.getDate() + 6)
  const fmt = (d: Date) => d.toISOString().slice(0, 10)
  return { min: fmt(monday), max: fmt(sunday) }
})

const addForm = reactive({
  studentId: '', businessId: '', businessName: '',
  date: '', absenceType: 'Unexcused', reason: '',
})

const correctForm = reactive({ absenceType: 'Unexcused', reason: '' })

const columns: QTableProps['columns'] = [
  { name: 'date', label: 'Tarih', field: 'date', align: 'left', sortable: true },
  { name: 'student', label: 'Öğrenci', field: 'studentId', align: 'left' },
  { name: 'business', label: 'İşletme', field: 'businessId', align: 'left' },
  { name: 'absenceTypeSlug', label: 'Tür', field: 'absenceTypeSlug', align: 'left' },
  { name: 'statusSlug', label: 'Durum', field: 'statusSlug', align: 'left' },
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
  $q.dialog({
    title: 'Devamsızlık Kaydını Sil',
    message: `${formatDate(row.date)} tarihli devamsızlık kaydını silmek istediğinize emin misiniz?`,
    cancel: { label: 'İptal', flat: true },
    ok: { label: 'Sil', color: 'negative' },
    persistent: true,
  }).onOk(async () => {
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
  })
}


function openAddDialog() {
  addForm.studentId = ''
  addForm.businessId = ''
  addForm.businessName = ''
  addForm.date = ''
  addForm.absenceType = 'Unexcused'
  addForm.reason = ''
  placementOpts.reset()
  placementOpts.load({ academicPeriodId: periodStore.selectedPeriodId ?? undefined })
  addDialog.value = true
}

async function createRecord() {
  saving.value = true
  try {
    await attendanceApi.create({
      studentId: addForm.studentId,
      businessId: addForm.businessId,
      institutionId: authStore.user?.institutionId ?? '',
      academicPeriodId: periodStore.selectedPeriodId ?? '',
      date: new Date(addForm.date).toISOString(),
      absenceType: addForm.absenceType,
      reason: addForm.reason || undefined,
    })
    notify.success('Devamsızlık kaydedildi.')
    addDialog.value = false
    await load()
  } catch (e) {
    notify.apiError(e, 'Kayıt sırasında bir hata oluştu.')
  } finally {
    saving.value = false
  }
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
  correctForm.absenceType = row.absenceType
  correctForm.reason = row.reason ?? ''
  correctDialog.value = true
}

async function correctRecord() {
  if (!selected.value) return
  saving.value = true
  try {
    await attendanceApi.correct(selected.value.id, {
      absenceType: correctForm.absenceType,
      reason: correctForm.reason || undefined,
    })
    notify.success('Devamsızlık düzeltildi.')
    correctDialog.value = false
    await load()
  } catch (e) {
    notify.apiError(e, 'Düzeltme sırasında bir hata oluştu.')
  } finally {
    saving.value = false
  }
}

watch(() => addForm.studentId, (newId) => {
  if (newId) {
    const biz = placementOpts.getBusinessForStudent(newId)
    addForm.businessId = biz?.businessId ?? ''
    addForm.businessName = biz?.businessName ?? ''
  } else {
    addForm.businessId = ''
    addForm.businessName = ''
  }
})

watch(() => periodStore.selectedPeriodId, () => load())

onMounted(() => {
  filterStudentOpts.load()
  businessOpts.load()
  branchOpts.load()
  load()
})
</script>
