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
        style="min-width: 280px"
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
        style="min-width: 160px"
        @update:model-value="load"
      />
      <q-btn color="primary" icon="search" label="Ara" @click="load" />
    </div>

    <AppTable :rows="records" :columns="columns" :loading="loading">
      <template #body-cell-absenceTypeSlug="{ row }">
        <q-td><StatusBadge :slug="row.absenceTypeSlug" /></q-td>
      </template>
      <template #body-cell-statusSlug="{ row }">
        <q-td><StatusBadge :slug="row.statusSlug" /></q-td>
      </template>
      <template #body-cell-date="{ row }">
        <q-td>{{ formatDate(row.date) }}</q-td>
      </template>
      <template #body-cell-actions="{ row }">
        <q-td class="text-right">
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
          <q-select
            v-model="addForm.businessId"
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
            <template #prepend>
              <q-icon name="business" />
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
          <q-input v-model="addForm.date" label="Tarih" filled type="date">
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
import { ref, onMounted, reactive, watch } from 'vue'
import type { QTableProps } from 'quasar'
import { useQuasar } from 'quasar'
import { attendanceApi, type AttendanceRecordDto, ABSENCE_TYPES } from 'src/api/attendance'
import { useNotify } from 'src/composables/useNotify'
import { useStudentOptions, useBusinessOptions } from 'src/composables/useEntityOptions'
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
const studentOpts = useStudentOptions()
const businessOpts = useBusinessOptions()
const filterStudentOpts = useStudentOptions()
const loading = ref(false)
const saving = ref(false)
const records = ref<AttendanceRecordDto[]>([])
const selected = ref<AttendanceRecordDto | null>(null)
const addDialog = ref(false)
const correctDialog = ref(false)
const studentIdFilter = ref('')
const statusFilter = ref<string | null>(null)

const statusOptions = [
  { label: 'Kaydedildi', value: 'Recorded' },
  { label: 'Doğrulandı', value: 'Verified' },
  { label: 'Düzeltildi', value: 'Corrected' },
]

const absenceTypeOptions = ABSENCE_TYPES.map((t) => ({ label: t.label, value: t.value }))

const addForm = reactive({
  studentId: '', businessId: '',
  date: '', absenceType: 'Unexcused', reason: '',
})

const correctForm = reactive({ absenceType: 'Unexcused', reason: '' })

const columns: QTableProps['columns'] = [
  { name: 'date', label: 'Tarih', field: 'date', align: 'left', sortable: true },
  { name: 'studentId', label: 'Öğrenci ID', field: (row) => (row as AttendanceRecordDto).studentId.slice(0,8) + '…', align: 'left' },
  { name: 'absenceTypeSlug', label: 'Tür', field: 'absenceTypeSlug', align: 'left' },
  { name: 'statusSlug', label: 'Durum', field: 'statusSlug', align: 'left' },
  { name: 'markedBy', label: 'Kaydeden', field: 'markedBy', align: 'left' },
  { name: 'actions', label: '', field: 'id', align: 'right' },
]

function formatDate(iso: string) {
  return new Date(iso).toLocaleDateString('tr-TR', { day: '2-digit', month: '2-digit', year: 'numeric' })
}

async function load() {
  loading.value = true
  try {
    const res = await attendanceApi.list({
      academicPeriodId: periodStore.selectedPeriodId ?? undefined,
      studentId: studentIdFilter.value || undefined,
      status: statusFilter.value ?? undefined,
    })
    records.value = res.data
  } catch {
    notify.error('Devamsızlık kayıtları yüklenirken bir hata oluştu.')
  } finally {
    loading.value = false
  }
}

function openAddDialog() {
  addForm.studentId = ''
  addForm.businessId = ''
  addForm.date = ''
  addForm.absenceType = 'Unexcused'
  addForm.reason = ''
  studentOpts.reset()
  studentOpts.load()
  businessOpts.reset()
  businessOpts.load()
  addDialog.value = true
}

async function createRecord() {
  saving.value = true
  try {
    await attendanceApi.create({
      studentId: addForm.studentId,
      businessId: addForm.businessId,
      institutionId: authStore.user?.institutionId ?? '',
      date: new Date(addForm.date).toISOString(),
      absenceType: addForm.absenceType,
      reason: addForm.reason || undefined,
    })
    notify.success('Devamsızlık kaydedildi.')
    addDialog.value = false
    await load()
  } catch {
    notify.error('Kayıt sırasında bir hata oluştu.')
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
  } catch {
    notify.error('Doğrulama sırasında bir hata oluştu.')
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
  } catch {
    notify.error('Düzeltme sırasında bir hata oluştu.')
  } finally {
    saving.value = false
  }
}

watch(() => periodStore.selectedPeriodId, () => load())

onMounted(() => {
  load()
  filterStudentOpts.load()
})
</script>
