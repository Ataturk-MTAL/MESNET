<template>
  <q-page padding>
    <div class="row items-center q-mb-lg">
      <div class="col">
        <div class="text-h5 text-weight-bold">İşletmeler</div>
      </div>
      <div class="col-auto q-gutter-sm">
        <PermissionGuard :permission="Permissions.Company.Manage">
          <q-btn color="primary" icon="add_business" label="İşletme Ekle" @click="addDialog = true" />
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

    <AppTable :rows="businesses" :columns="columns" :loading="loading">
      <template #body-cell-statusSlug="{ row }">
        <q-td><StatusBadge :slug="row.statusSlug" /></q-td>
      </template>
      <template #body-cell-capacity="{ row }">
        <q-td>
          <div class="text-caption">
            {{ row.capacity.occupiedSlots }} / {{ row.capacity.totalSlots }}
            <q-badge v-if="row.capacity.isFull" color="negative" label="Dolu" class="q-ml-xs" />
          </div>
        </q-td>
      </template>
      <template #body-cell-actions="{ row }">
        <q-td class="text-right">
          <q-btn flat round dense icon="visibility" @click="openDetail(row)" />
          <PermissionGuard :permission="Permissions.Company.Manage">
            <q-btn
              v-if="row.status === 'PendingApproval'"
              flat round dense icon="check_circle"
              color="positive"
              @click="approve(row)"
            />
            <q-btn
              v-if="row.status === 'PendingApproval'"
              flat round dense icon="cancel"
              color="negative"
              @click="openReject(row)"
            />
          </PermissionGuard>
        </q-td>
      </template>
    </AppTable>

    <!-- Detay Drawer -->
    <q-drawer v-model="detailOpen" side="right" bordered :width="480" overlay>
      <template v-if="selected">
        <q-toolbar>
          <q-toolbar-title class="text-subtitle1 text-weight-bold">{{ selected.name }}</q-toolbar-title>
          <q-btn flat round dense icon="close" @click="detailOpen = false" />
        </q-toolbar>
        <q-separator />
        <q-scroll-area class="fit">
          <div class="q-pa-md q-gutter-sm">
            <q-item dense>
              <q-item-section avatar><q-icon name="location_on" /></q-item-section>
              <q-item-section>
                <q-item-label caption>Adres</q-item-label>
                <q-item-label>{{ selected.address }}</q-item-label>
              </q-item-section>
            </q-item>
            <q-item dense>
              <q-item-section avatar><q-icon name="phone" /></q-item-section>
              <q-item-section>
                <q-item-label caption>Telefon</q-item-label>
                <q-item-label>{{ selected.phoneNumber ?? '—' }}</q-item-label>
              </q-item-section>
            </q-item>
            <q-item dense>
              <q-item-section avatar><q-icon name="email" /></q-item-section>
              <q-item-section>
                <q-item-label caption>E-posta</q-item-label>
                <q-item-label>{{ selected.email ?? '—' }}</q-item-label>
              </q-item-section>
            </q-item>
            <q-item dense>
              <q-item-section avatar><q-icon name="groups" /></q-item-section>
              <q-item-section>
                <q-item-label caption>Personel Sayısı</q-item-label>
                <q-item-label>{{ selected.personnelCount }}</q-item-label>
              </q-item-section>
            </q-item>

            <q-separator spaced />
            <div class="text-subtitle2 text-weight-medium">Kapasite</div>
            <PermissionGuard :permission="Permissions.Company.Manage">
              <div class="row items-center q-gutter-sm">
                <q-input
                  v-model.number="capacitySlots"
                  type="number"
                  label="Toplam Kapasite"
                  filled
                  dense
                  class="col"
                />
                <q-btn color="primary" label="Güncelle" :loading="saving" @click="updateCapacity" />
              </div>
            </PermissionGuard>
            <div class="text-caption text-grey">
              Dolu: {{ selected.capacity.occupiedSlots }} / {{ selected.capacity.totalSlots }}
              — Müsait: {{ selected.capacity.availableSlots }}
            </div>

            <q-separator spaced />
            <div class="text-subtitle2 text-weight-medium">Belgeler</div>
            <div v-if="selected.documents.length === 0" class="text-grey text-caption">Belge yok</div>
            <q-list v-else dense bordered rounded>
              <q-item v-for="doc in selected.documents" :key="doc.id" dense>
                <q-item-section>
                  <q-item-label>{{ doc.typeSlug }}</q-item-label>
                  <q-item-label caption>{{ doc.fileName }}</q-item-label>
                </q-item-section>
                <q-item-section side>
                  <StatusBadge :slug="doc.statusSlug" />
                </q-item-section>
                <q-item-section side>
                  <PermissionGuard :permission="Permissions.Document.Approve">
                    <q-btn
                      v-if="doc.status === 'Uploaded'"
                      flat dense round icon="check"
                      color="positive"
                      @click="approveDoc(doc.id)"
                    />
                  </PermissionGuard>
                </q-item-section>
              </q-item>
            </q-list>

            <PermissionGuard :permission="Permissions.Company.Document">
              <q-btn
                color="secondary"
                icon="upload"
                label="Belge Yükle"
                class="q-mt-sm"
                @click="docUploadDialog = true"
              />
            </PermissionGuard>
          </div>
        </q-scroll-area>
      </template>
    </q-drawer>

    <!-- İşletme Ekle Dialog -->
    <q-dialog v-model="addDialog" persistent :maximized="$q.screen.lt.sm" transition-show="slide-up" transition-hide="slide-down">
      <q-card :style="$q.screen.gt.xs ? 'width: 480px; max-width: 95vw' : ''">
        <q-toolbar class="bg-primary text-white">
          <q-icon name="add_business" class="q-mr-sm" />
          <q-toolbar-title>Yeni İşletme Ekle</q-toolbar-title>
          <q-btn flat round dense icon="close" color="white" v-close-popup />
        </q-toolbar>
        <q-card-section class="q-pt-lg q-gutter-md">
          <q-input v-model="addForm.name" label="İşletme Adı *" filled>
            <template #prepend>
              <q-icon name="business" />
            </template>
          </q-input>
          <q-input v-model="addForm.address" label="Adres *" filled>
            <template #prepend>
              <q-icon name="location_on" />
            </template>
          </q-input>
          <q-input v-model="addForm.phoneNumber" label="Telefon" filled>
            <template #prepend>
              <q-icon name="phone" />
            </template>
          </q-input>
          <q-input v-model="addForm.email" label="E-posta" filled type="email">
            <template #prepend>
              <q-icon name="email" />
            </template>
          </q-input>
          <q-input v-model.number="addForm.personnelCount" label="Personel Sayısı" filled type="number">
            <template #prepend>
              <q-icon name="groups" />
            </template>
          </q-input>
        </q-card-section>
        <q-separator />
        <q-card-actions align="right" class="q-pa-md">
          <q-btn flat label="İptal" color="grey-7" v-close-popup />
          <q-btn unelevated color="primary" label="Kaydet" :loading="saving" @click="registerBusiness" />
        </q-card-actions>
      </q-card>
    </q-dialog>

    <!-- Red Dialog -->
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
          <q-btn unelevated color="negative" label="Reddet" :loading="saving" @click="rejectBusiness" />
        </q-card-actions>
      </q-card>
    </q-dialog>

    <!-- Belge Yükleme Dialog -->
    <q-dialog v-model="docUploadDialog" persistent :maximized="$q.screen.lt.sm" transition-show="slide-up" transition-hide="slide-down">
      <q-card :style="$q.screen.gt.xs ? 'width: 400px; max-width: 95vw' : ''">
        <q-toolbar class="bg-secondary text-white">
          <q-icon name="upload_file" class="q-mr-sm" />
          <q-toolbar-title>Belge Yükle</q-toolbar-title>
          <q-btn flat round dense icon="close" color="white" v-close-popup />
        </q-toolbar>
        <q-card-section class="q-pt-lg q-gutter-md">
          <q-file v-model="docFile" label="Dosya Seç" filled accept=".pdf,.jpg,.jpeg,.png">
            <template #prepend>
              <q-icon name="attach_file" />
            </template>
          </q-file>
        </q-card-section>
        <q-separator />
        <q-card-actions align="right" class="q-pa-md">
          <q-btn flat label="İptal" color="grey-7" v-close-popup />
          <q-btn unelevated color="secondary" label="Yükle" :loading="saving" @click="uploadDocument" />
        </q-card-actions>
      </q-card>
    </q-dialog>
  </q-page>
</template>

<script setup lang="ts">
import { ref, onMounted, reactive } from 'vue'
import type { QTableProps } from 'quasar'
import { useQuasar } from 'quasar'
import { businessApi, type BusinessDto } from 'src/api/business'
import { useNotify } from 'src/composables/useNotify'
import { Permissions } from 'utils/permissions'
import AppTable from 'components/AppTable.vue'
import StatusBadge from 'components/StatusBadge.vue'
import PermissionGuard from 'components/PermissionGuard.vue'

const $q = useQuasar()
const notify = useNotify()

const loading = ref(false)
const saving = ref(false)
const businesses = ref<BusinessDto[]>([])
const selected = ref<BusinessDto | null>(null)
const detailOpen = ref(false)
const addDialog = ref(false)
const rejectDialog = ref(false)
const docUploadDialog = ref(false)
const statusFilter = ref<string | null>(null)
const rejectReason = ref('')
const capacitySlots = ref(0)
const docFile = ref<File | null>(null)

const statusOptions = [
  { label: 'Onay Bekliyor', value: 'PendingApproval' },
  { label: 'Aktif', value: 'Active' },
  { label: 'Reddedildi', value: 'Rejected' },
  { label: 'Pasif', value: 'Inactive' },
  { label: 'Kapatılmış', value: 'Closed' },
]

const addForm = reactive({
  name: '',
  address: '',
  phoneNumber: '',
  email: '',
  personnelCount: 0,
})

const columns: QTableProps['columns'] = [
  { name: 'name', label: 'İşletme Adı', field: 'name', align: 'left', sortable: true },
  { name: 'address', label: 'Adres', field: 'address', align: 'left' },
  { name: 'statusSlug', label: 'Durum', field: 'statusSlug', align: 'left' },
  { name: 'capacity', label: 'Kapasite', field: 'capacity', align: 'left' },
  { name: 'actions', label: '', field: 'id', align: 'right' },
]

async function load() {
  loading.value = true
  try {
    const res = await businessApi.list(statusFilter.value ?? undefined)
    businesses.value = res.data
  } catch {
    notify.error('İşletmeler yüklenirken bir hata oluştu.')
  } finally {
    loading.value = false
  }
}

function openDetail(row: BusinessDto) {
  selected.value = row
  capacitySlots.value = row.capacity.totalSlots
  detailOpen.value = true
}

async function approve(row: BusinessDto) {
  saving.value = true
  try {
    await businessApi.approve(row.id)
    notify.success('İşletme onaylandı.')
    await load()
  } catch {
    notify.error('Onaylama sırasında bir hata oluştu.')
  } finally {
    saving.value = false
  }
}

function openReject(row: BusinessDto) {
  selected.value = row
  rejectReason.value = ''
  rejectDialog.value = true
}

async function rejectBusiness() {
  if (!selected.value) return
  saving.value = true
  try {
    await businessApi.reject(selected.value.id, rejectReason.value)
    notify.success('İşletme reddedildi.')
    rejectDialog.value = false
    await load()
  } catch {
    notify.error('Reddetme sırasında bir hata oluştu.')
  } finally {
    saving.value = false
  }
}

async function registerBusiness() {
  saving.value = true
  try {
    await businessApi.register({
      name: addForm.name,
      address: addForm.address,
      phoneNumber: addForm.phoneNumber || undefined,
      email: addForm.email || undefined,
      personnelCount: addForm.personnelCount || undefined,
    })
    notify.success('İşletme başarıyla eklendi.')
    addDialog.value = false
    addForm.name = ''
    addForm.address = ''
    addForm.phoneNumber = ''
    addForm.email = ''
    addForm.personnelCount = 0
    await load()
  } catch {
    notify.error('İşletme eklenirken bir hata oluştu.')
  } finally {
    saving.value = false
  }
}

async function updateCapacity() {
  if (!selected.value) return
  saving.value = true
  try {
    await businessApi.updateCapacity(selected.value.id, { totalSlots: capacitySlots.value })
    notify.success('Kapasite güncellendi.')
    await load()
    const updated = businesses.value.find((b) => b.id === selected.value?.id)
    if (updated) selected.value = updated
  } catch {
    notify.error('Kapasite güncellenirken bir hata oluştu.')
  } finally {
    saving.value = false
  }
}

async function approveDoc(documentId: string) {
  if (!selected.value) return
  saving.value = true
  try {
    await businessApi.approveDocument(selected.value.id, documentId)
    notify.success('Belge onaylandı.')
    await load()
  } catch {
    notify.error('Belge onaylanırken bir hata oluştu.')
  } finally {
    saving.value = false
  }
}

async function uploadDocument() {
  if (!selected.value || !docFile.value) return
  const formData = new FormData()
  formData.append('file', docFile.value)
  saving.value = true
  try {
    await businessApi.uploadDocument(selected.value.id, formData)
    notify.success('Belge yüklendi.')
    docUploadDialog.value = false
    docFile.value = null
    await load()
  } catch {
    notify.error('Belge yüklenirken bir hata oluştu.')
  } finally {
    saving.value = false
  }
}

onMounted(load)
</script>
