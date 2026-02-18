<template>
  <q-page padding>
    <div class="row items-center q-mb-lg">
      <div class="col">
        <div class="text-h5 text-weight-bold">Öğrenciler</div>
      </div>
      <div class="col-auto q-gutter-sm">
        <PermissionGuard :permission="Permissions.Student.Manage">
          <q-btn color="primary" icon="person_add" label="Yeni Öğrenci" @click="openAddDialog" />
        </PermissionGuard>
      </div>
    </div>

    <!-- Filtreler -->
    <div class="row q-gutter-sm q-mb-md">
      <q-select
        v-model="branchFilter"
        :options="branchOptions"
        label="Alan"
        filled
        dense
        emit-value
        map-options
        clearable
        style="min-width: 200px"
        @update:model-value="load"
      />
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

    <AppTable :rows="students" :columns="columns" :loading="loading">
      <template #body-cell-statusSlug="{ row }">
        <q-td><StatusBadge :slug="row.statusSlug" /></q-td>
      </template>
      <template #body-cell-actions="{ row }">
        <q-td class="text-right">
          <q-btn flat round dense icon="visibility" @click="openDetail(row)" />
          <PermissionGuard :permission="Permissions.Internship.Approve">
            <q-btn
              v-if="row.status === 'Applied'"
              flat round dense icon="place"
              color="primary"
              title="Yerleştir"
              @click="openPlacement(row)"
            />
          </PermissionGuard>
          <PermissionGuard :permission="Permissions.Internship.Manage">
            <q-btn
              v-if="row.status === 'ActiveInternship' || row.status === 'Placed'"
              flat round dense icon="transfer_within_a_station"
              color="warning"
              title="Transfer Et"
              @click="openTransfer(row)"
            />
          </PermissionGuard>
        </q-td>
      </template>
    </AppTable>

    <!-- Detay Drawer -->
    <q-drawer v-model="detailOpen" side="right" bordered :width="400" overlay>
      <template v-if="selected">
        <q-toolbar>
          <q-toolbar-title class="text-subtitle1 text-weight-bold">{{ selected.fullName }}</q-toolbar-title>
          <q-btn flat round dense icon="close" @click="detailOpen = false" />
        </q-toolbar>
        <q-separator />
        <div class="q-pa-md q-gutter-sm">
          <q-item dense>
            <q-item-section avatar><q-icon name="school" /></q-item-section>
            <q-item-section>
              <q-item-label caption>Alan</q-item-label>
              <q-item-label>{{ selected.branchName }} ({{ selected.branchCode }})</q-item-label>
            </q-item-section>
          </q-item>
          <q-item dense>
            <q-item-section avatar><q-icon name="class" /></q-item-section>
            <q-item-section>
              <q-item-label caption>Sınıf / Şube</q-item-label>
              <q-item-label>{{ selected.classYear }}. Sınıf{{ selected.section ? ` / ${selected.section}` : '' }}</q-item-label>
            </q-item-section>
          </q-item>
          <q-item dense>
            <q-item-section avatar><q-icon name="badge" /></q-item-section>
            <q-item-section>
              <q-item-label caption>Durum</q-item-label>
              <q-item-label><StatusBadge :slug="selected.statusSlug" /></q-item-label>
            </q-item-section>
          </q-item>
          <q-item dense>
            <q-item-section avatar><q-icon name="event" /></q-item-section>
            <q-item-section>
              <q-item-label caption>Kayıt Tarihi</q-item-label>
              <q-item-label>{{ formatDate(selected.registeredAt) }}</q-item-label>
            </q-item-section>
          </q-item>
        </div>
      </template>
    </q-drawer>

    <!-- Yeni Öğrenci Dialog -->
    <q-dialog v-model="addDialog" persistent>
      <q-card style="min-width: 480px">
        <q-card-section class="row items-center">
          <div class="text-h6">Yeni Öğrenci Ekle</div>
          <q-space />
          <q-btn flat round dense icon="close" @click="addDialog = false" />
        </q-card-section>
        <q-card-section class="q-gutter-sm">
          <q-select
            v-model="addForm.keycloakUserId"
            :options="userOpts.options.value"
            :loading="userOpts.loading.value"
            label="Kullanıcı *"
            filled
            use-input
            input-debounce="0"
            emit-value
            map-options
            option-label="label"
            option-value="value"
            @filter="userOpts.filter"
            @update:model-value="onUserSelect"
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
          <q-input v-model="addForm.fullName" label="Ad Soyad *" filled readonly />
          <q-input v-model="addForm.branchCode" label="Alan Kodu *" filled />
          <q-input v-model.number="addForm.classYear" label="Sınıf (9-12)" filled type="number" min="9" max="12" />
          <q-input v-model="addForm.section" label="Şube (A, B, C...)" filled />
        </q-card-section>
        <q-card-actions align="right">
          <q-btn flat label="İptal" @click="addDialog = false" />
          <q-btn color="primary" label="Kaydet" :loading="saving" @click="registerStudent" />
        </q-card-actions>
      </q-card>
    </q-dialog>

    <!-- Yerleştirme Dialog -->
    <q-dialog v-model="placementDialog" persistent>
      <q-card style="min-width: 480px">
        <q-card-section class="row items-center">
          <div class="text-h6">Öğrenci Yerleştir</div>
          <q-space />
          <q-btn flat round dense icon="close" @click="placementDialog = false" />
        </q-card-section>
        <q-card-section class="q-gutter-sm">
          <div v-if="selected" class="text-subtitle2 q-mb-sm">Öğrenci: {{ selected.fullName }}</div>
          <q-select
            v-model="placementForm.businessId"
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
            v-model="placementForm.teacherId"
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
            <template #option="{ itemProps, opt }">
              <q-item v-bind="itemProps">
                <q-item-section>
                  <q-item-label>{{ opt.label }}</q-item-label>
                </q-item-section>
              </q-item>
            </template>
            <template #no-option>
              <q-item>
                <q-item-section class="text-grey">Sonuç bulunamadı</q-item-section>
              </q-item>
            </template>
          </q-select>
        </q-card-section>
        <q-card-actions align="right">
          <q-btn flat label="İptal" @click="placementDialog = false" />
          <q-btn color="primary" label="Yerleştir" :loading="saving" @click="placementStudent" />
        </q-card-actions>
      </q-card>
    </q-dialog>

    <!-- Transfer Dialog -->
    <q-dialog v-model="transferDialog" persistent>
      <q-card style="min-width: 480px">
        <q-card-section class="row items-center">
          <div class="text-h6">Öğrenci Transfer Et</div>
          <q-space />
          <q-btn flat round dense icon="close" @click="transferDialog = false" />
        </q-card-section>
        <q-card-section class="q-gutter-sm">
          <div v-if="selected" class="text-subtitle2 q-mb-sm">Öğrenci: {{ selected.fullName }}</div>
          <q-select
            v-model="transferForm.newBusinessId"
            :options="transferBusinessOpts.options.value"
            :loading="transferBusinessOpts.loading.value"
            label="Yeni İşletme *"
            filled
            use-input
            input-debounce="0"
            emit-value
            map-options
            option-label="label"
            option-value="value"
            @filter="transferBusinessOpts.filter"
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
          <q-input v-model="transferForm.reason" label="Transfer Gerekçesi" filled type="textarea" rows="2" />
        </q-card-section>
        <q-card-actions align="right">
          <q-btn flat label="İptal" @click="transferDialog = false" />
          <q-btn color="warning" label="Transfer Et" :loading="saving" @click="doTransfer" />
        </q-card-actions>
      </q-card>
    </q-dialog>
  </q-page>
</template>

<script setup lang="ts">
import { ref, onMounted, reactive } from 'vue'
import type { QTableProps } from 'quasar'
import { enrollmentApi, type StudentProfileDto } from 'src/api/enrollment'
import { useNotify } from 'src/composables/useNotify'
import { useKeycloakUserOptions, useBusinessOptions, useTeacherOptions } from 'src/composables/useEntityOptions'
import { Permissions } from 'utils/permissions'
import AppTable from 'components/AppTable.vue'
import StatusBadge from 'components/StatusBadge.vue'
import PermissionGuard from 'components/PermissionGuard.vue'

const notify = useNotify()
const userOpts = useKeycloakUserOptions()
const businessOpts = useBusinessOptions()
const teacherOpts = useTeacherOptions()
const transferBusinessOpts = useBusinessOptions()

const loading = ref(false)
const saving = ref(false)
const students = ref<StudentProfileDto[]>([])
const selected = ref<StudentProfileDto | null>(null)
const detailOpen = ref(false)
const addDialog = ref(false)
const placementDialog = ref(false)
const transferDialog = ref(false)
const branchFilter = ref<string | null>(null)
const statusFilter = ref<string | null>(null)
// activePlacementId: transfer için mevcut yerleştirme ID'si
const activePlacementId = ref<string | null>(null)

const branchOptions = [
  { label: 'Bilişim Teknolojileri', value: 'BT' },
  { label: 'Elektrik-Elektronik', value: 'EE' },
  { label: 'Makine', value: 'MK' },
]

const statusOptions = [
  { label: 'Kayıtlı', value: 'Registered' },
  { label: 'Başvurdu', value: 'Applied' },
  { label: 'Yerleştirildi', value: 'Placed' },
  { label: 'Aktif Staj', value: 'ActiveInternship' },
  { label: 'Tamamladı', value: 'Completed' },
]

const addForm = reactive({
  keycloakUserId: '',
  fullName: '',
  branchCode: '',
  classYear: 11,
  section: '',
})

const placementForm = reactive({
  businessId: '',
  teacherId: '',
})

const transferForm = reactive({
  newBusinessId: '',
  reason: '',
})

const columns: QTableProps['columns'] = [
  { name: 'fullName', label: 'Ad Soyad', field: 'fullName', align: 'left', sortable: true },
  { name: 'branchName', label: 'Alan', field: 'branchName', align: 'left' },
  { name: 'classYear', label: 'Sınıf/Şube', field: (row) => { const s = row as StudentProfileDto; return `${s.classYear}. Sınıf${s.section ? ` / ${s.section}` : ''}`; }, align: 'left' },
  { name: 'statusSlug', label: 'Durum', field: 'statusSlug', align: 'left' },
  { name: 'actions', label: '', field: 'id', align: 'right' },
]

function formatDate(iso: string) {
  return new Date(iso).toLocaleDateString('tr-TR', { day: '2-digit', month: '2-digit', year: 'numeric' })
}

async function load() {
  loading.value = true
  try {
    const res = await enrollmentApi.listStudents({
      branchCode: branchFilter.value ?? undefined,
      status: statusFilter.value ?? undefined,
    })
    students.value = res.data
  } catch {
    notify.error('Öğrenciler yüklenirken bir hata oluştu.')
  } finally {
    loading.value = false
  }
}

function openDetail(row: StudentProfileDto) {
  selected.value = row
  detailOpen.value = true
}

function onUserSelect(val: string | null) {
  if (val) {
    const found = userOpts.allOptions.value.find((o) => o.value === val)
    if (found) addForm.fullName = found.label
  } else {
    addForm.fullName = ''
  }
}

function openAddDialog() {
  addForm.keycloakUserId = ''
  addForm.fullName = ''
  addForm.branchCode = ''
  addForm.classYear = 11
  addForm.section = ''
  userOpts.reset()
  userOpts.load()
  addDialog.value = true
}

function openPlacement(row: StudentProfileDto) {
  selected.value = row
  placementForm.businessId = ''
  placementForm.teacherId = ''
  businessOpts.reset()
  businessOpts.load()
  teacherOpts.reset()
  teacherOpts.load()
  placementDialog.value = true
}

async function openTransfer(row: StudentProfileDto) {
  selected.value = row
  transferForm.newBusinessId = ''
  transferForm.reason = ''
  transferBusinessOpts.reset()
  transferBusinessOpts.load()
  // Mevcut aktif yerleştirmeyi bul
  try {
    const res = await enrollmentApi.listPlacements({ studentId: row.id, status: 'Active' })
    activePlacementId.value = res.data[0]?.id ?? null
  } catch {
    activePlacementId.value = null
  }
  transferDialog.value = true
}

async function registerStudent() {
  saving.value = true
  try {
    await enrollmentApi.registerStudent({
      keycloakUserId: addForm.keycloakUserId,
      fullName: addForm.fullName,
      branchCode: addForm.branchCode,
      classYear: addForm.classYear,
      section: addForm.section || undefined,
    })
    notify.success('Öğrenci başarıyla kaydedildi.')
    addDialog.value = false
    await load()
  } catch {
    notify.error('Öğrenci eklenirken bir hata oluştu.')
  } finally {
    saving.value = false
  }
}

async function placementStudent() {
  if (!selected.value) return
  saving.value = true
  try {
    await enrollmentApi.createPlacement({
      studentId: selected.value.id,
      businessId: placementForm.businessId,
      teacherId: placementForm.teacherId || undefined,
    })
    notify.success('Öğrenci yerleştirildi.')
    placementDialog.value = false
    await load()
  } catch {
    notify.error('Yerleştirme sırasında bir hata oluştu.')
  } finally {
    saving.value = false
  }
}

async function doTransfer() {
  if (!activePlacementId.value) {
    notify.error('Aktif yerleştirme bulunamadı.')
    return
  }
  saving.value = true
  try {
    await enrollmentApi.transferStudent(activePlacementId.value, {
      newBusinessId: transferForm.newBusinessId,
      reason: transferForm.reason,
    })
    notify.success('Öğrenci transfer edildi.')
    transferDialog.value = false
    await load()
  } catch {
    notify.error('Transfer sırasında bir hata oluştu.')
  } finally {
    saving.value = false
  }
}

onMounted(load)
</script>
