<template>
  <q-page padding>
    <div v-if="loading" class="flex flex-center q-pa-xl">
      <q-spinner-gears size="48px" color="primary" />
    </div>

    <q-banner v-else-if="error" type="negative" class="q-mb-md">
      {{ error }}
      <template #action>
        <q-btn flat label="Tekrar Dene" @click="load" />
      </template>
    </q-banner>

    <template v-else-if="institution">
      <div class="row items-center q-mb-lg">
        <div class="col">
          <div class="text-h5 text-weight-bold">{{ institution.fullName }}</div>
          <div class="text-caption text-grey">Kurum Kodu: {{ institution.institutionCode }}</div>
        </div>
        <div class="col-auto">
          <PermissionGuard :permission="Permissions.Institution.Manage">
            <q-btn color="primary" icon="edit" label="Düzenle" @click="editDialog = true" />
          </PermissionGuard>
        </div>
      </div>

      <q-card flat bordered class="q-mb-lg">
        <q-card-section>
          <div class="text-subtitle1 text-weight-medium q-mb-md">Kurum Bilgileri</div>
          <div class="row q-col-gutter-md">
            <div class="col-12 col-md-6">
              <q-item dense>
                <q-item-section avatar><q-icon name="location_on" color="grey-6" /></q-item-section>
                <q-item-section>
                  <q-item-label caption>Adres</q-item-label>
                  <q-item-label>{{ institution.address ?? '—' }}</q-item-label>
                </q-item-section>
              </q-item>
            </div>
            <div class="col-12 col-md-6">
              <q-item dense>
                <q-item-section avatar><q-icon name="phone" color="grey-6" /></q-item-section>
                <q-item-section>
                  <q-item-label caption>Telefon</q-item-label>
                  <q-item-label>{{ institution.phoneNumber ?? '—' }}</q-item-label>
                </q-item-section>
              </q-item>
            </div>
            <div class="col-12 col-md-6">
              <q-item dense>
                <q-item-section avatar><q-icon name="email" color="grey-6" /></q-item-section>
                <q-item-section>
                  <q-item-label caption>E-posta</q-item-label>
                  <q-item-label>{{ institution.email ?? '—' }}</q-item-label>
                </q-item-section>
              </q-item>
            </div>
            <div class="col-12 col-md-6">
              <q-item dense>
                <q-item-section avatar><q-icon name="language" color="grey-6" /></q-item-section>
                <q-item-section>
                  <q-item-label caption>Web Sitesi</q-item-label>
                  <q-item-label>
                    <a v-if="institution.webUrl" :href="institution.webUrl" target="_blank" class="text-primary">
                      {{ institution.webUrl }}
                    </a>
                    <span v-else>—</span>
                  </q-item-label>
                </q-item-section>
              </q-item>
            </div>
          </div>
        </q-card-section>
      </q-card>

      <q-card flat bordered class="q-mb-lg">
        <q-card-section>
          <div class="text-subtitle1 text-weight-medium q-mb-md">Alanlar / Şubeler</div>
          <AppTable :rows="institution?.branches ?? []" :columns="branchColumns">
            <template #body-cell-typeSlug="{ row }">
              <q-td><StatusBadge :slug="row.typeSlug" /></q-td>
            </template>
            <template #body-cell-capacity="{ row }">
              <q-td>
                <q-linear-progress
                  :value="row.totalCount > 0 ? row.atWorkCount / row.totalCount : 0"
                  color="primary"
                  class="q-my-xs"
                  style="height: 8px; border-radius: 4px"
                />
                <div class="text-caption text-grey">
                  {{ row.atWorkCount }} / {{ row.totalCount }} (Müsait: {{ row.availableCount }})
                </div>
              </q-td>
            </template>
            <template #body-cell-isActive="{ row }">
              <q-td>
                <q-badge :color="row.isActive ? 'positive' : 'grey'" :label="row.isActive ? 'Aktif' : 'Pasif'" />
              </q-td>
            </template>
          </AppTable>
        </q-card-section>
      </q-card>

      <q-card flat bordered>
        <q-card-section>
          <div class="row items-center q-mb-md">
            <div class="col text-subtitle1 text-weight-medium">Personel</div>
            <div class="col-auto">
              <PermissionGuard :permission="Permissions.Institution.Staff">
                <q-btn color="primary" icon="person_add" label="Personel Ekle" size="sm" @click="openStaffDialog" />
              </PermissionGuard>
            </div>
          </div>
          <AppTable :rows="institution?.staff ?? []" :columns="staffColumns">
            <template #body-cell-roleSlug="{ row }">
              <q-td><q-badge color="blue-grey" :label="row.roleSlug" /></q-td>
            </template>
            <template #body-cell-authorizedAt="{ row }">
              <q-td>{{ formatDate(row.authorizedAt) }}</q-td>
            </template>
          </AppTable>
        </q-card-section>
      </q-card>
    </template>

    <q-dialog v-model="editDialog" persistent>
      <q-card style="min-width: 480px">
        <q-card-section class="row items-center">
          <div class="text-h6">Kurum Bilgilerini Güncelle</div>
          <q-space />
          <q-btn flat round dense icon="close" @click="editDialog = false" />
        </q-card-section>
        <q-card-section class="q-gutter-sm">
          <q-input v-model="editForm.fullName" label="Kurum Adı" filled />
          <q-input v-model="editForm.address" label="Adres" filled />
          <q-input v-model="editForm.phoneNumber" label="Telefon" filled />
          <q-input v-model="editForm.email" label="E-posta" filled type="email" />
          <q-input v-model="editForm.webUrl" label="Web Sitesi" filled />
        </q-card-section>
        <q-card-actions align="right">
          <q-btn flat label="İptal" @click="editDialog = false" />
          <q-btn color="primary" label="Kaydet" :loading="saving" @click="saveInstitution" />
        </q-card-actions>
      </q-card>
    </q-dialog>

    <q-dialog v-model="staffDialog" persistent>
      <q-card style="min-width: 480px">
        <q-card-section class="row items-center">
          <div class="text-h6">Personel Yetkilendir</div>
          <q-space />
          <q-btn flat round dense icon="close" @click="staffDialog = false" />
        </q-card-section>
        <q-card-section class="q-gutter-sm">
          <q-select
            v-model="staffForm.keycloakUserId"
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
          <q-input v-model="staffForm.fullName" label="Ad Soyad" filled readonly />
          <q-select
            v-model="staffForm.role"
            :options="staffRoleOptions"
            label="Rol"
            filled
            emit-value
            map-options
          />
          <q-input v-model="staffForm.branchCode" label="Alan Kodu (opsiyonel)" filled />
        </q-card-section>
        <q-card-actions align="right">
          <q-btn flat label="İptal" @click="staffDialog = false" />
          <q-btn color="primary" label="Yetkilendir" :loading="saving" @click="addStaff" />
        </q-card-actions>
      </q-card>
    </q-dialog>
  </q-page>
</template>

<script setup lang="ts">
import { ref, onMounted, reactive } from 'vue'
import type { QTableProps } from 'quasar'
import { institutionApi, type InstitutionDto } from 'src/api/institution'
import { useNotify } from 'src/composables/useNotify'
import { Permissions } from 'utils/permissions'
import AppTable from 'components/AppTable.vue'
import StatusBadge from 'components/StatusBadge.vue'
import PermissionGuard from 'components/PermissionGuard.vue'
import { useAuthStore } from 'stores/auth'
import { useKeycloakUserOptions } from 'src/composables/useEntityOptions'

const authStore = useAuthStore()
const notify = useNotify()
const userOpts = useKeycloakUserOptions()

const loading = ref(false)
const saving = ref(false)
const error = ref<string | null>(null)
const institution = ref<InstitutionDto | null>(null)
const editDialog = ref(false)
const staffDialog = ref(false)

const institutionId = authStore.user?.institutionId ?? ''

const editForm = reactive({
  fullName: '',
  address: '',
  phoneNumber: '',
  email: '',
  webUrl: '',
})

const staffForm = reactive({
  keycloakUserId: '',
  fullName: '',
  role: '',
  branchCode: '',
})

const staffRoleOptions = [
  { label: 'Kurum Müdürü', value: 'InstitutionManager' },
  { label: 'Müdür Yardımcısı', value: 'DeputyDirector' },
  { label: 'Alan Şefi', value: 'DepartmentHead' },
  { label: 'Koordinatör Öğretmen', value: 'CoordinatorTeacher' },
]

const branchColumns: QTableProps['columns'] = [
  { name: 'fieldCode', label: 'Alan Kodu', field: 'fieldCode', align: 'left', sortable: true },
  { name: 'fieldName', label: 'Alan Adı', field: 'fieldName', align: 'left', sortable: true },
  { name: 'typeSlug', label: 'Tür', field: 'typeSlug', align: 'left' },
  { name: 'capacity', label: 'Kapasite', field: 'totalCount', align: 'left' },
  { name: 'isActive', label: 'Durum', field: 'isActive', align: 'center' },
]

const staffColumns: QTableProps['columns'] = [
  { name: 'fullName', label: 'Ad Soyad', field: 'fullName', align: 'left', sortable: true },
  { name: 'roleSlug', label: 'Rol', field: 'roleSlug', align: 'left' },
  {
    name: 'branchCode',
    label: 'Alan',
    field: (row) => (row as { branchCode: string | null }).branchCode ?? '—',
    align: 'left',
  },
  { name: 'authorizedAt', label: 'Yetkilendirme Tarihi', field: 'authorizedAt', align: 'left' },
]

function formatDate(iso: string) {
  return new Date(iso).toLocaleDateString('tr-TR', { day: '2-digit', month: '2-digit', year: 'numeric' })
}

async function load() {
  if (!institutionId) { error.value = 'Kurum bilgisi bulunamadı.'; return }
  loading.value = true
  error.value = null
  try {
    const res = await institutionApi.get(institutionId)
    institution.value = res.data
    editForm.fullName = res.data.fullName
    editForm.address = res.data.address ?? ''
    editForm.phoneNumber = res.data.phoneNumber ?? ''
    editForm.email = res.data.email ?? ''
    editForm.webUrl = res.data.webUrl ?? ''
  } catch {
    error.value = 'Kurum bilgileri yüklenirken bir hata oluştu.'
  } finally {
    loading.value = false
  }
}

async function saveInstitution() {
  saving.value = true
  try {
    await institutionApi.update(institutionId, {
      fullName: editForm.fullName,
      address: editForm.address || undefined,
      phoneNumber: editForm.phoneNumber || undefined,
      email: editForm.email || undefined,
      webUrl: editForm.webUrl || undefined,
    })
    notify.success('Kurum bilgileri güncellendi.')
    editDialog.value = false
    await load()
  } catch {
    notify.error('Güncelleme sırasında bir hata oluştu.')
  } finally {
    saving.value = false
  }
}

function openStaffDialog() {
  staffForm.keycloakUserId = ''
  staffForm.fullName = ''
  staffForm.role = ''
  staffForm.branchCode = ''
  userOpts.reset()
  userOpts.load({ institutionId })
  staffDialog.value = true
}

function onUserSelect(val: string | null) {
  if (val) {
    const selected = userOpts.allOptions.value.find((o) => o.value === val)
    if (selected) staffForm.fullName = selected.label
  } else {
    staffForm.fullName = ''
  }
}

async function addStaff() {
  saving.value = true
  try {
    await institutionApi.authorizeStaff(institutionId, {
      keycloakUserId: staffForm.keycloakUserId,
      fullName: staffForm.fullName,
      role: staffForm.role,
      branchCode: staffForm.branchCode || undefined,
    })
    notify.success('Personel başarıyla yetkilendirildi.')
    staffDialog.value = false
    await load()
  } catch {
    notify.error('Personel eklenirken bir hata oluştu.')
  } finally {
    saving.value = false
  }
}

onMounted(load)
</script>
