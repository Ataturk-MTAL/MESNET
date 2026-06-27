<template>
  <q-page padding>
    <PageHeader title="İşletmeler">
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
    </PageHeader>

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
      />
    </div>

    <!-- Tablo Görünümü -->
    <AppTable
      v-if="viewMode === 'table'"
      :rows="businesses"
      :columns="columns"
      :loading="loading"
      :pagination="pagination"
      show-search
      :search="search"
      @request="onRequest"
      @search="onSearch"
    >
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
          <q-btn flat round dense icon="visibility" aria-label="Detayları görüntüle" @click="openDetail(row)" />
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
    <DetailPanel v-model="detailOpen" :has-content="!!selected" :width="480">
      <template #title>{{ selected?.name }}</template>
      <template #toolbar-actions>
        <StatusBadge :slug="selected?.statusSlug ?? ''" class="q-mr-sm" />
        <PermissionGuard :permission="Permissions.Company.Manage">
          <q-btn flat round dense icon="edit" aria-label="Düzenle" @click="openEditDialog">
            <q-tooltip>Düzenle</q-tooltip>
          </q-btn>
        </PermissionGuard>
      </template>
      <template v-if="selected">
        <div class="q-gutter-sm">
            <InfoItem icon="location_on" label="Adres" :value="selected.address" />
            <div v-if="selected.location" class="q-px-md q-mt-sm">
              <MapPicker :model-value="selected.location" readonly height="200px" />
            </div>
            <InfoItem icon="phone" label="Telefon" :value="selected.phoneNumber" />
            <InfoItem icon="email" label="E-posta" :value="selected.email" />
            <InfoItem icon="groups" label="Personel Sayısı" :value="selected.personnelCount" />
            <InfoItem icon="category" label="Sektörler">
              <q-badge
                v-for="sec in selected.sectors"
                :key="sec.name"
                color="blue-grey-3"
                text-color="dark"
                class="q-mr-xs q-mb-xs"
                :label="sec.slug"
              />
              <span v-if="selected.sectors.length === 0" class="text-grey">Belirtilmemiş</span>
            </InfoItem>

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
                    <q-btn flat dense round icon="visibility" aria-label="Detayları görüntüle" color="primary" @click="previewDoc(doc.id)">
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
                      <q-btn flat dense round icon="delete" aria-label="Sil" color="negative" @click="confirmDeleteDoc(doc.id, doc.fileName)">
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
      </template>
    </DetailPanel>

    <!-- Form Dialogları -->
    <AddBusinessForm v-model="addDialog" :sector-options="sectorOptions" @saved="load" />
    <RejectBusinessForm v-model="rejectDialog" :business-id="selected?.id ?? ''" @saved="afterFormSaved" />
    <UploadBusinessDocForm v-model="docUploadDialog" :business-id="selected?.id ?? ''" @saved="afterFormSaved" />
    <EditBusinessForm v-model="editDialog" :business="selected" :sector-options="sectorOptions" @saved="afterFormSaved" />
  </q-page>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import type { QTableProps } from 'quasar'
import { useQuasar } from 'quasar'
import { businessApi, type BusinessDto, type SectorDto } from 'src/api/business'
import { useNotify } from 'src/composables/useNotify'
import { useServerPagination } from 'src/composables/useServerPagination'
import { useEntityOptionsStore } from 'stores/entityOptions'
import { Permissions } from 'utils/permissions'
import AppTable from 'components/AppTable.vue'
import StatusBadge from 'components/StatusBadge.vue'
import PermissionGuard from 'components/PermissionGuard.vue'
import MapPicker from 'components/MapPicker.vue'
import InfoItem from 'components/InfoItem.vue'
import PageHeader from 'components/PageHeader.vue'
import { useConfirmDialog } from 'src/composables/useConfirmDialog'
import DetailPanel from 'components/DetailPanel.vue'
import AddBusinessForm from 'components/forms/business/AddBusinessForm.vue'
import EditBusinessForm from 'components/forms/business/EditBusinessForm.vue'
import RejectBusinessForm from 'components/forms/business/RejectBusinessForm.vue'
import UploadBusinessDocForm from 'components/forms/business/UploadBusinessDocForm.vue'
import { LMap, LTileLayer, LMarker, LPopup } from '@vue-leaflet/vue-leaflet'
import 'leaflet/dist/leaflet.css'

const $q = useQuasar()
const notify = useNotify()
const entityOptionsStore = useEntityOptionsStore()
const confirmDialog = useConfirmDialog()
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

const saving = ref(false)
const selected = ref<BusinessDto | null>(null)
const detailOpen = ref(false)
const addDialog = ref(false)
const rejectDialog = ref(false)
const docUploadDialog = ref(false)
const editDialog = ref(false)
const statusFilter = ref<string | null>(null)
const sectorFilter = ref<string | null>(null)
const allSectors = ref<SectorDto[]>([])
const capacitySlots = ref(0)

// ── Server-side pagination ──
const filters = computed(() => ({
  ...(statusFilter.value ? { status: statusFilter.value } : {}),
  ...(sectorFilter.value ? { sector: sectorFilter.value } : {}),
}))

const { rows: businesses, loading, pagination, search, onRequest, onSearch, load } =
  useServerPagination<BusinessDto>({
    fetchFn: (params) => businessApi.list(params),
    filters,
    defaultSortBy: 'name',
  })
const statusOptions = [
  { label: 'Onay Bekliyor', value: 'PendingApproval' },
  { label: 'Aktif', value: 'Active' },
  { label: 'Reddedildi', value: 'Rejected' },
  { label: 'Pasif', value: 'Inactive' },
  { label: 'Kapatılmış', value: 'Closed' },
]

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


async function afterFormSaved() {
  await load()
  if (selected.value) {
    const updated = businesses.value.find((b) => b.id === selected.value?.id)
    if (updated) selected.value = updated
  }
}

function openDetail(row: BusinessDto) {
  selected.value = row
  detailOpen.value = true
  capacitySlots.value = row.capacity.totalSlots
}

async function approve(row: BusinessDto) {
  saving.value = true
  try {
    await businessApi.approve(row.id)
    entityOptionsStore.invalidateBusinesses()
    notify.success('İşletme onaylandı.')
    await load()
  } catch (e) {
    notify.apiError(e, 'Onaylama sırasında bir hata oluştu.')
  } finally {
    saving.value = false
  }
}

function openReject(row: BusinessDto) {
  selected.value = row
  rejectDialog.value = true
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
  } catch (e) {
    notify.apiError(e, 'Kapasite güncellenirken bir hata oluştu.')
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
  } catch (e) {
    notify.apiError(e, 'Belge onaylanırken bir hata oluştu.')
  } finally {
    saving.value = false
  }
}

async function previewDoc(documentId: string) {
  if (!selected.value) return
  try {
    const res = await businessApi.getDocumentUrl(selected.value.id, documentId)
    window.open(res.data.url, '_blank')
  } catch (e) {
    notify.apiError(e, 'Belge bağlantısı oluşturulamadı.')
  }
}

function confirmDeleteDoc(documentId: string, fileName: string) {
  confirmDialog.confirm({
    title: 'Belge Sil',
    message: `"${fileName}" belgesini silmek istediğinize emin misiniz? Bu işlem geri alınamaz.`,
    okLabel: 'Sil',
    onOk: () => deleteDoc(documentId),
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
  } catch (e) {
    notify.apiError(e, 'Belge silinirken bir hata oluştu.')
  } finally {
    saving.value = false
  }
}

function openEditDialog() {
  editDialog.value = true
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
    } catch (e) {
      notify.apiError(e, 'İşletme pasife alınırken bir hata oluştu.')
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
  } catch (e) {
    notify.apiError(e, 'İşletme aktifleştirilirken bir hata oluştu.')
  } finally {
    saving.value = false
  }
}

function closeBusiness() {
  if (!selected.value) return
  const id = selected.value.id
  confirmDialog.confirm({
    title: 'İşletmeyi Kapat',
    message: 'Bu işletmeyi kapatmak istediğinize emin misiniz? Bu işlem geri alınamaz.',
    okLabel: 'Kapat',
    onOk: async () => {
      saving.value = true
      try {
        await businessApi.close(id)
        notify.success('İşletme kapatıldı.')
        await load()
        const updated = businesses.value.find((b) => b.id === id)
        if (updated) selected.value = updated
      } catch (e) {
        notify.apiError(e, 'İşletme kapatılırken bir hata oluştu.')
      } finally {
        saving.value = false
      }
    },
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
</style>
