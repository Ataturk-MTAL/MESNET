<template>
  <q-page padding>
    <div class="row items-center q-mb-lg">
      <div class="col">
        <div class="text-h5 text-weight-bold">İşletmeler</div>
      </div>
      <div class="col-auto q-gutter-sm row items-center">
        <q-btn-toggle
          v-model="viewMode"
          toggle-color="primary"
          flat
          dense
          :options="[
            { value: 'table', slot: 'table' },
            { value: 'map', slot: 'map' },
          ]"
        >
          <template #table>
            <q-icon name="view_list" />
            <q-tooltip>Tablo Görünümü</q-tooltip>
          </template>
          <template #map>
            <q-icon name="map" />
            <q-tooltip>Harita Görünümü</q-tooltip>
          </template>
        </q-btn-toggle>
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
      <q-select
        v-model="sectorFilter"
        :options="sectorOptions"
        label="Sektör"
        filled
        dense
        emit-value
        map-options
        clearable
        style="min-width: 240px"
        @update:model-value="load"
      />
    </div>

    <!-- Tablo Görünümü -->
    <AppTable v-if="viewMode === 'table'" :rows="businesses" :columns="columns" :loading="loading">
      <template #body-cell-sectors="{ row }">
        <q-td>
          <q-badge
            v-for="sec in row.sectors"
            :key="sec.name"
            color="blue-grey-3"
            text-color="dark"
            class="q-mr-xs q-mb-xs"
            :label="sec.slug"
          />
          <span v-if="row.sectors.length === 0" class="text-grey text-caption">—</span>
        </q-td>
      </template>
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

    <!-- Harita Görünümü -->
    <div v-else class="business-map-container">
      <q-inner-loading :showing="loading" />
      <l-map
        ref="businessMapRef"
        :zoom="mapZoom"
        :center="mapCenter"
        :use-global-leaflet="false"
        style="height: 100%; width: 100%; border-radius: 8px"
      >
        <l-tile-layer
          url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png"
          attribution="&copy; <a href='https://www.openstreetmap.org/copyright'>OpenStreetMap</a> contributors"
          layer-type="base"
          name="OpenStreetMap"
        />
        <l-marker
          v-for="biz in businessesWithLocation"
          :key="biz.id"
          :lat-lng="getLatLng(biz)"
          @click="openDetail(biz)"
        >
          <l-popup>
            <div style="min-width: 200px">
              <div class="text-weight-bold">{{ biz.name }}</div>
              <div class="text-caption text-grey-8">{{ biz.address }}</div>
              <div class="text-caption q-mt-xs">
                <StatusBadge :slug="biz.statusSlug" />
              </div>
              <div v-if="biz.sectors.length > 0" class="q-mt-xs">
                <q-badge
                  v-for="sec in biz.sectors"
                  :key="sec.name"
                  color="blue-grey-3"
                  text-color="dark"
                  class="q-mr-xs q-mb-xs"
                  :label="sec.slug"
                  style="font-size: 10px"
                />
              </div>
              <div class="text-caption q-mt-xs">
                Kapasite: {{ biz.capacity.occupiedSlots }} / {{ biz.capacity.totalSlots }}
              </div>
              <q-btn
                flat dense size="sm" color="primary" icon="open_in_new" label="Detay"
                class="q-mt-xs"
                @click="openDetail(biz)"
              />
            </div>
          </l-popup>
        </l-marker>
      </l-map>
      <div v-if="businessesWithoutLocation.length > 0" class="text-caption text-grey q-mt-sm">
        {{ businessesWithoutLocation.length }} işletmenin konum bilgisi bulunmuyor.
      </div>
    </div>

    <!-- Detay Panel — sağdan overlay -->
    <transition name="slide-right">
      <div v-if="selected" class="detail-backdrop" @click.self="closeDetail" />
    </transition>
    <transition name="slide-right">
      <div v-if="selected" class="detail-panel">
        <q-toolbar>
          <q-toolbar-title class="text-subtitle1 text-weight-bold">{{ selected.name }}</q-toolbar-title>
          <StatusBadge :slug="selected.statusSlug" class="q-mr-sm" />
          <PermissionGuard :permission="Permissions.Company.Manage">
            <q-btn flat round dense icon="edit" @click="openEditDialog">
              <q-tooltip>Düzenle</q-tooltip>
            </q-btn>
          </PermissionGuard>
          <q-btn flat round dense icon="close" @click="closeDetail" />
        </q-toolbar>
        <q-separator />
        <div class="detail-panel-scroll">
          <div class="q-pa-md q-gutter-sm">
            <q-item dense>
              <q-item-section avatar><q-icon name="location_on" /></q-item-section>
              <q-item-section>
                <q-item-label caption>Adres</q-item-label>
                <q-item-label>{{ selected.address }}</q-item-label>
              </q-item-section>
            </q-item>
            <div v-if="selected.location" class="q-px-md q-mt-sm">
              <MapPicker :model-value="selected.location" readonly height="200px" />
            </div>
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
            <q-item dense>
              <q-item-section avatar><q-icon name="category" /></q-item-section>
              <q-item-section>
                <q-item-label caption>Sektörler</q-item-label>
                <q-item-label>
                  <q-badge
                    v-for="sec in selected.sectors"
                    :key="sec.name"
                    color="blue-grey-3"
                    text-color="dark"
                    class="q-mr-xs q-mb-xs"
                    :label="sec.slug"
                  />
                  <span v-if="selected.sectors.length === 0" class="text-grey">Belirtilmemiş</span>
                </q-item-label>
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
                  <div class="row no-wrap">
                    <q-btn flat dense round icon="visibility" color="primary" @click="previewDoc(doc.id)">
                      <q-tooltip>Görüntüle</q-tooltip>
                    </q-btn>
                    <PermissionGuard :permission="Permissions.Document.Approve">
                      <q-btn
                        v-if="doc.status === 'Uploaded'"
                        flat dense round icon="check"
                        color="positive"
                        @click="approveDoc(doc.id)"
                      >
                        <q-tooltip>Onayla</q-tooltip>
                      </q-btn>
                    </PermissionGuard>
                    <PermissionGuard :permission="Permissions.Company.Document">
                      <q-btn flat dense round icon="delete" color="negative" @click="confirmDeleteDoc(doc.id, doc.fileName)">
                        <q-tooltip>Sil</q-tooltip>
                      </q-btn>
                    </PermissionGuard>
                  </div>
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

            <!-- Durum İşlemleri -->
            <q-separator spaced />
            <div class="text-subtitle2 text-weight-medium">İşlemler</div>
            <PermissionGuard :permission="Permissions.Company.Manage">
              <div class="q-gutter-sm">
                <!-- PendingApproval → Onayla / Reddet -->
                <template v-if="selected.status === 'PendingApproval'">
                  <q-btn color="positive" icon="check_circle" label="Onayla" :loading="saving" @click="approveFromDrawer" class="full-width" />
                  <q-btn color="negative" icon="cancel" label="Reddet" :loading="saving" @click="openReject(selected)" class="full-width" />
                </template>
                <!-- Active → Pasife Al / Kapat -->
                <template v-if="selected.status === 'Active'">
                  <q-btn color="warning" text-color="white" icon="pause_circle" label="Pasife Al" :loading="saving" @click="deactivateBusiness" class="full-width" />
                  <q-btn outline color="negative" icon="block" label="Kapat" :loading="saving" @click="closeBusiness" class="full-width" />
                </template>
                <!-- Inactive → Aktifleştir / Kapat -->
                <template v-if="selected.status === 'Inactive'">
                  <q-btn color="positive" icon="play_circle" label="Aktifleştir" :loading="saving" @click="activateBusiness" class="full-width" />
                  <q-btn outline color="negative" icon="block" label="Kapat" :loading="saving" @click="closeBusiness" class="full-width" />
                </template>
              </div>
            </PermissionGuard>
          </div>
        </div>
      </div>
    </transition>

    <!-- İşletme Ekle Dialog -->
    <q-dialog v-model="addDialog" persistent :maximized="$q.screen.lt.sm" transition-show="slide-up" transition-hide="slide-down">
      <q-card :style="$q.screen.gt.xs ? 'width: 600px; max-width: 95vw' : ''">
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
          <q-select
            v-model="addForm.sectors"
            :options="sectorOptions"
            label="Sektörler"
            filled
            multiple
            emit-value
            map-options
            use-chips
          >
            <template #prepend>
              <q-icon name="category" />
            </template>
          </q-select>
          <div class="text-subtitle2 q-mt-md q-mb-xs">
            <q-icon name="map" class="q-mr-xs" />Konum
          </div>
          <MapPicker v-model="addForm.location" height="250px" />
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
          <q-select
            v-model="docForm.type"
            :options="docTypeOptions"
            label="Belge Tipi *"
            filled
            emit-value
            map-options
          >
            <template #prepend>
              <q-icon name="description" />
            </template>
          </q-select>
          <q-file
            v-model="docForm.file"
            label="Dosya Seç *"
            filled
            accept=".pdf,.jpg,.jpeg,.png"
          >
            <template #prepend>
              <q-icon name="attach_file" />
            </template>
          </q-file>
          <!-- Ön izleme -->
          <div v-if="docForm.file" class="q-mt-sm">
            <div class="text-caption text-grey q-mb-xs">Ön İzleme</div>
            <iframe
              v-if="docForm.file.type === 'application/pdf'"
              :src="filePreviewUrl ?? ''"
              style="width: 100%; height: 300px; border: 1px solid #ddd; border-radius: 4px"
            />
            <img
              v-else
              :src="filePreviewUrl ?? ''"
              style="max-width: 100%; max-height: 300px; border: 1px solid #ddd; border-radius: 4px"
            />
          </div>
        </q-card-section>
        <q-separator />
        <q-card-actions align="right" class="q-pa-md">
          <q-btn flat label="İptal" color="grey-7" v-close-popup />
          <q-btn unelevated color="secondary" label="Yükle" :loading="saving" :disable="!docForm.file || !docForm.type" @click="uploadDocument" />
        </q-card-actions>
      </q-card>
    </q-dialog>
    <!-- İşletme Düzenle Dialog -->
    <q-dialog v-model="editDialog" persistent :maximized="$q.screen.lt.sm" transition-show="slide-up" transition-hide="slide-down">
      <q-card :style="$q.screen.gt.xs ? 'width: 600px; max-width: 95vw' : ''">
        <q-toolbar class="bg-primary text-white">
          <q-icon name="edit" class="q-mr-sm" />
          <q-toolbar-title>İşletme Düzenle</q-toolbar-title>
          <q-btn flat round dense icon="close" color="white" v-close-popup />
        </q-toolbar>
        <q-card-section class="q-pt-lg q-gutter-md">
          <q-input v-model="editForm.name" label="İşletme Adı *" filled>
            <template #prepend>
              <q-icon name="business" />
            </template>
          </q-input>
          <q-input v-model="editForm.address" label="Adres *" filled>
            <template #prepend>
              <q-icon name="location_on" />
            </template>
          </q-input>
          <q-input v-model="editForm.phoneNumber" label="Telefon" filled>
            <template #prepend>
              <q-icon name="phone" />
            </template>
          </q-input>
          <q-input v-model="editForm.email" label="E-posta" filled type="email">
            <template #prepend>
              <q-icon name="email" />
            </template>
          </q-input>
          <q-input v-model="editForm.website" label="Web Sitesi" filled>
            <template #prepend>
              <q-icon name="language" />
            </template>
          </q-input>
          <q-input v-model.number="editForm.personnelCount" label="Personel Sayısı" filled type="number">
            <template #prepend>
              <q-icon name="groups" />
            </template>
          </q-input>
          <q-select
            v-model="editForm.sectors"
            :options="sectorOptions"
            label="Sektörler"
            filled
            multiple
            emit-value
            map-options
            use-chips
          >
            <template #prepend>
              <q-icon name="category" />
            </template>
          </q-select>
          <div class="text-subtitle2 q-mt-md q-mb-xs">
            <q-icon name="map" class="q-mr-xs" />Konum
          </div>
          <MapPicker v-model="editForm.location" height="250px" />
        </q-card-section>
        <q-separator />
        <q-card-actions align="right" class="q-pa-md">
          <q-btn flat label="İptal" color="grey-7" v-close-popup />
          <q-btn unelevated color="primary" label="Kaydet" :loading="saving" @click="saveEdit" />
        </q-card-actions>
      </q-card>
    </q-dialog>
  </q-page>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, reactive, watch } from 'vue'
import type { QTableProps } from 'quasar'
import { useQuasar } from 'quasar'
import { businessApi, type BusinessDto, type SectorDto } from 'src/api/business'
import { useNotify } from 'src/composables/useNotify'
import { Permissions } from 'utils/permissions'
import AppTable from 'components/AppTable.vue'
import StatusBadge from 'components/StatusBadge.vue'
import PermissionGuard from 'components/PermissionGuard.vue'
import MapPicker from 'components/MapPicker.vue'
import { LMap, LTileLayer, LMarker, LPopup } from '@vue-leaflet/vue-leaflet'
import 'leaflet/dist/leaflet.css'

const $q = useQuasar()
const notify = useNotify()
const viewMode = ref<'table' | 'map'>('table')
const mapZoom = ref(7)
const mapCenter = ref<[number, number]>([39.0, 35.0])
const businessMapRef = ref<InstanceType<typeof LMap> | null>(null)

const businessesWithLocation = computed(() =>
  businesses.value.filter((b) => b.location !== null),
)

const businessesWithoutLocation = computed(() =>
  businesses.value.filter((b) => b.location === null),
)

function getLatLng(biz: BusinessDto): [number, number] {
  return [biz.location!.latitude, biz.location!.longitude]
}

const loading = ref(false)
const saving = ref(false)
const businesses = ref<BusinessDto[]>([])
const selected = ref<BusinessDto | null>(null)
const addDialog = ref(false)
const rejectDialog = ref(false)
const docUploadDialog = ref(false)
const editDialog = ref(false)
const statusFilter = ref<string | null>(null)
const sectorFilter = ref<string | null>(null)
const allSectors = ref<SectorDto[]>([])
const rejectReason = ref('')
const capacitySlots = ref(0)
const docForm = reactive({
  type: '',
  file: null as File | null,
})

const docTypeOptions = [
  { label: 'Ustalık Belgesi', value: 'MasteryCertificate' },
  { label: 'Usta Öğreticilik Belgesi', value: 'MasterInstructorCertificate' },
]

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
  location: null as { latitude: number; longitude: number } | null,
  sectors: [] as string[],
})

const editForm = reactive({
  name: '',
  address: '',
  phoneNumber: '',
  email: '',
  website: '',
  personnelCount: 0,
  location: null as { latitude: number; longitude: number } | null,
  sectors: [] as string[],
})

const sectorOptions = computed(() =>
  allSectors.value.map((s) => ({ label: s.slug, value: s.name })),
)

const columns: QTableProps['columns'] = [
  { name: 'name', label: 'İşletme Adı', field: 'name', align: 'left', sortable: true },
  { name: 'sectors', label: 'Sektörler', field: 'sectors', align: 'left' },
  { name: 'address', label: 'Adres', field: 'address', align: 'left' },
  { name: 'statusSlug', label: 'Durum', field: 'statusSlug', align: 'left' },
  { name: 'capacity', label: 'Kapasite', field: 'capacity', align: 'left' },
  { name: 'actions', label: '', field: 'id', align: 'right' },
]

async function loadSectors() {
  try {
    const res = await businessApi.sectors()
    allSectors.value = res.data
  } catch {
    /* sektör listesi yüklenemezse sessizce devam et */
  }
}

async function load() {
  loading.value = true
  try {
    const res = await businessApi.list(statusFilter.value ?? undefined, sectorFilter.value ?? undefined)
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
}

function closeDetail() {
  selected.value = null
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
      location: addForm.location ?? undefined,
      sectors: addForm.sectors.length > 0 ? addForm.sectors : undefined,
    })
    notify.success('İşletme başarıyla eklendi.')
    addDialog.value = false
    addForm.name = ''
    addForm.address = ''
    addForm.phoneNumber = ''
    addForm.email = ''
    addForm.personnelCount = 0
    addForm.location = null
    addForm.sectors = []
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

// Dosya ön izleme URL'i
const _previewBlobUrl = ref<string | null>(null)

const filePreviewUrl = computed(() => {
  if (!docForm.file) return null
  // Eski blob URL varsa temizle
  if (_previewBlobUrl.value) {
    URL.revokeObjectURL(_previewBlobUrl.value)
  }
  const url = URL.createObjectURL(docForm.file)
  _previewBlobUrl.value = url
  return url
})

// Dialog kapanınca veya dosya null olunca blob URL'i temizle
watch(() => docForm.file, (newFile) => {
  if (!newFile && _previewBlobUrl.value) {
    URL.revokeObjectURL(_previewBlobUrl.value)
    _previewBlobUrl.value = null
  }
})

async function uploadDocument() {
  if (!selected.value || !docForm.type || !docForm.file) return
  saving.value = true
  try {
    await businessApi.uploadDocument(selected.value.id, docForm.file, docForm.type)
    notify.success('Belge yüklendi.')
    docUploadDialog.value = false
    docForm.type = ''
    docForm.file = null
    await load()
    // Drawer'daki detayı güncelle
    const updated = businesses.value.find((b) => b.id === selected.value?.id)
    if (updated) selected.value = updated
  } catch {
    notify.error('Belge yüklenirken bir hata oluştu.')
  } finally {
    saving.value = false
  }
}

async function previewDoc(documentId: string) {
  if (!selected.value) return
  try {
    const res = await businessApi.getDocumentUrl(selected.value.id, documentId)
    window.open(res.data.url, '_blank')
  } catch {
    notify.error('Belge bağlantısı oluşturulamadı.')
  }
}

function confirmDeleteDoc(documentId: string, fileName: string) {
  $q.dialog({
    title: 'Belge Sil',
    message: `"${fileName}" belgesini silmek istediğinize emin misiniz? Bu işlem geri alınamaz.`,
    cancel: { flat: true, label: 'İptal' },
    ok: { color: 'negative', label: 'Sil' },
    persistent: true,
  }).onOk(async () => {
    await deleteDoc(documentId)
  })
}

async function deleteDoc(documentId: string) {
  if (!selected.value) return
  saving.value = true
  try {
    await businessApi.deleteDocument(selected.value.id, documentId)
    notify.success('Belge silindi.')
    await load()
    const updated = businesses.value.find((b) => b.id === selected.value?.id)
    if (updated) selected.value = updated
  } catch {
    notify.error('Belge silinirken bir hata oluştu.')
  } finally {
    saving.value = false
  }
}

// ── İşletme Düzenle ──
function openEditDialog() {
  if (!selected.value) return
  editForm.name = selected.value.name
  editForm.address = selected.value.address
  editForm.phoneNumber = selected.value.phoneNumber ?? ''
  editForm.email = selected.value.email ?? ''
  editForm.website = selected.value.website ?? ''
  editForm.personnelCount = selected.value.personnelCount
  editForm.location = selected.value.location ? { ...selected.value.location } : null
  editForm.sectors = selected.value.sectors.map((s) => s.name)
  editDialog.value = true
}

async function saveEdit() {
  if (!selected.value) return
  saving.value = true
  try {
    await businessApi.update(selected.value.id, {
      name: editForm.name || undefined,
      address: editForm.address || undefined,
      phoneNumber: editForm.phoneNumber || undefined,
      email: editForm.email || undefined,
      website: editForm.website || undefined,
      personnelCount: editForm.personnelCount || undefined,
      location: editForm.location ?? undefined,
      sectors: editForm.sectors,
    })
    notify.success('İşletme bilgileri güncellendi.')
    editDialog.value = false
    await load()
    const updated = businesses.value.find((b) => b.id === selected.value?.id)
    if (updated) selected.value = updated
  } catch {
    notify.error('İşletme güncellenirken bir hata oluştu.')
  } finally {
    saving.value = false
  }
}

// ── Durum Aksiyonları ──
async function approveFromDrawer() {
  if (!selected.value) return
  await approve(selected.value)
  const updated = businesses.value.find((b) => b.id === selected.value?.id)
  if (updated) selected.value = updated
}

function deactivateBusiness() {
  if (!selected.value) return
  const id = selected.value.id
  $q.dialog({
    title: 'Pasife Al',
    message: 'İşletmeyi pasife almak için gerekçe giriniz:',
    prompt: { model: '', type: 'textarea' },
    cancel: { flat: true, label: 'İptal' },
    ok: { color: 'warning', label: 'Pasife Al' },
    persistent: true,
  }).onOk(async (reason: string) => {
    saving.value = true
    try {
      await businessApi.deactivate(id, reason)
      notify.success('İşletme pasife alındı.')
      await load()
      const updated = businesses.value.find((b) => b.id === id)
      if (updated) selected.value = updated
    } catch {
      notify.error('İşletme pasife alınırken bir hata oluştu.')
    } finally {
      saving.value = false
    }
  })
}

async function activateBusiness() {
  if (!selected.value) return
  saving.value = true
  try {
    await businessApi.activate(selected.value.id)
    notify.success('İşletme aktifleştirildi.')
    await load()
    const updated = businesses.value.find((b) => b.id === selected.value?.id)
    if (updated) selected.value = updated
  } catch {
    notify.error('İşletme aktifleştirilirken bir hata oluştu.')
  } finally {
    saving.value = false
  }
}

function closeBusiness() {
  if (!selected.value) return
  const id = selected.value.id
  $q.dialog({
    title: 'İşletmeyi Kapat',
    message: 'Bu işletmeyi kapatmak istediğinize emin misiniz? Bu işlem geri alınamaz.',
    cancel: { flat: true, label: 'İptal' },
    ok: { color: 'negative', label: 'Kapat' },
    persistent: true,
  }).onOk(async () => {
    saving.value = true
    try {
      await businessApi.close(id)
      notify.success('İşletme kapatıldı.')
      await load()
      const updated = businesses.value.find((b) => b.id === id)
      if (updated) selected.value = updated
    } catch {
      notify.error('İşletme kapatılırken bir hata oluştu.')
    } finally {
      saving.value = false
    }
  })
}

onMounted(async () => {
  await loadSectors()
  await load()
})

</script>

<style scoped>
.business-map-container {
  position: relative;
  height: calc(100vh - 220px);
  min-height: 400px;
}

/* Detay paneli — sağdan overlay */
.detail-backdrop {
  position: fixed;
  inset: 0;
  background: rgba(0, 0, 0, 0.3);
  z-index: 2000;
}

.detail-panel {
  position: fixed;
  top: 50px; /* header yüksekliği */
  right: 0;
  bottom: 0;
  width: 480px;
  max-width: 100vw;
  background: white;
  z-index: 2001;
  box-shadow: -2px 0 12px rgba(0, 0, 0, 0.15);
  display: flex;
  flex-direction: column;
}

.detail-panel-scroll {
  flex: 1;
  overflow-y: auto;
}

/* Slide-right transition */
.slide-right-enter-active,
.slide-right-leave-active {
  transition: all 0.3s ease;
}

.slide-right-enter-from,
.slide-right-leave-to {
  opacity: 0;
}

.slide-right-enter-from .detail-panel,
.slide-right-leave-to .detail-panel {
  transform: translateX(100%);
}
</style>
