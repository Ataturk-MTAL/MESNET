<template>
  <q-page padding>
    <PageHeader title="İşletmeler">
      <q-btn-toggle
        v-model="viewMode"
        toggle-color="primary"
        flat
        dense
        :options="[
          { value: 'table', slot: 'table', attrs: { 'aria-label': 'Tablo görünümü' } },
          { value: 'map', slot: 'map', attrs: { 'aria-label': 'Harita görünümü' } },
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
        <q-btn
          color="primary"
          icon="add_business"
          label="İşletme Ekle"
          unelevated
          @click="router.push('/companies/new')"
        />
      </PermissionGuard>
    </PageHeader>

    <!-- Filtre + arama (aynı satır: filtreler solda, arama sağda) -->
    <div class="row items-center q-gutter-sm q-mb-md">
      <q-select
        v-model="statusFilter"
        :options="statusOptions"
        label="Durum"
        outlined
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
        outlined
        dense
        emit-value
        map-options
        clearable
        style="min-width: 240px"
        @update:model-value="load"
      />
      <q-space />
      <SearchInput
        :model-value="search"
        placeholder="Ara..."
        aria-label="İşletmelerde ara"
        debounce="400"
        style="min-width: 250px"
        @update:model-value="(v) => onSearch(String(v ?? ''))"
      />
    </div>

    <!--
      Görünen satırların TAMAMI onay bekliyorsa (ör. "Onay Bekliyor" filtresi seçiliyse) satır
      rozeti hiçbir şeyi ayırt etmez, yalnız durum sütununu ikinci kez söyler. Yirmi rozet
      yerine tek cümle. Hiçbiri sırada değilse hiçbir şey gösterilmez.
    -->
    <AppNotice
      v-if="viewMode === 'table' && showTurnNotice"
      type="info"
      class="q-mb-md"
      :message="`Bu listedeki ${turnRows.length} işletmenin tamamı sizin onayınızı bekliyor.`"
    />

    <!-- Tablo Görünümü -->
    <AppTable
      v-if="viewMode === 'table'"
      :rows="businesses"
      :columns="columns"
      :loading="loading"
      :pagination="pagination"
      @request="onRequest"
    >
      <template #body-cell-sectors="{ row }">
        <q-td>
          <q-badge
            v-for="sec in row.sectors"
            :key="sec.name"
            color="neutral-soft"
            text-color="neutral-strong"
            class="q-mr-xs q-mb-xs"
            :label="sec.slug"
          />
          <span
            v-if="row.sectors.length === 0"
            class="text-grey-7 text-caption"
          >—</span>
        </q-td>
      </template>
      <template #body-cell-statusSlug="{ row }">
        <q-td>
          <StatusBadge :slug="row.statusSlug" />
          <!--
            "Sıra sizde" — koşul ve gerekçe için bkz. `isMyTurn` / `showRowSignal` (script).
            Yüzey olarak DURUM hücresi seçildi: aşama bilgisinin okunduğu yer burasıdır.
            Satır zemini boyamak ya da renkli sol kenarlık koymak yasak (craft-floor); dolu
            rozet metin etiketi taşır, yani renk ikincil sinyaldir.
            Rozet yalnız AYIRT ETTİĞİ sayfada basılır: görünen satırların hepsi sıradaysa
            yerini tablonun üstündeki tek cümlelik bildirim alır (bkz. `showTurnNotice`).
          -->
          <q-badge
            v-if="showRowSignal && isMyTurn(row)"
            color="accent-strong"
            class="text-body2 q-px-sm q-py-xs q-ml-xs"
            label="Sıra sizde"
          />
        </q-td>
      </template>
      <template #body-cell-capacity="{ row }">
        <q-td>
          <div class="text-caption">
            {{ row.capacity.occupiedSlots }} / {{ row.capacity.totalSlots }}
            <q-badge
              v-if="row.capacity.isFull"
              color="negative"
              label="Dolu"
              class="q-ml-xs"
            />
          </div>
        </q-td>
      </template>
      <template #body-cell-actions="{ row }">
        <q-td class="text-right">
          <q-btn
            flat
            round
            dense
            icon="visibility"
            aria-label="Detayları görüntüle"
            @click="openDetail(row)"
          >
            <q-tooltip>Detayları görüntüle</q-tooltip>
          </q-btn>
          <PermissionGuard :permission="Permissions.Company.Manage">
            <q-btn
              v-if="row.status === 'PendingApproval'"
              flat
              round
              dense
              icon="check_circle"
              color="positive"
              aria-label="İşletmeyi onayla"
              @click="approve(row)"
            >
              <q-tooltip>Onayla</q-tooltip>
            </q-btn>
            <q-btn
              v-if="row.status === 'PendingApproval'"
              flat
              round
              dense
              icon="cancel"
              color="negative"
              aria-label="İşletmeyi reddet"
              @click="openReject(row)"
            >
              <q-tooltip>Reddet</q-tooltip>
            </q-btn>
          </PermissionGuard>
        </q-td>
      </template>

      <template #empty-action>
        <PermissionGuard :permission="Permissions.Company.Manage">
          <q-btn
            color="primary"
            icon="add_business"
            label="İlk işletmeyi ekle"
            unelevated
            @click="router.push('/companies/new')"
          />
        </PermissionGuard>
      </template>
    </AppTable>

    <!-- Harita Görünümü -->
    <div
      v-else
      class="business-map-container"
    >
      <q-inner-loading :showing="loading" />
      <l-map
        ref="businessMapRef"
        :zoom="mapZoom"
        :center="mapCenter"
        :use-global-leaflet="false"
        style="height: 100%; width: 100%; border-radius: 8px"
        @ready="onBusinessMapReady"
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
              <div class="text-weight-bold">
                {{ biz.name }}
              </div>
              <div class="text-caption text-grey-8">
                {{ biz.address }}
              </div>
              <div class="text-caption q-mt-xs">
                <StatusBadge :slug="biz.statusSlug" />
              </div>
              <div
                v-if="biz.sectors.length > 0"
                class="q-mt-xs"
              >
                <q-badge
                  v-for="sec in biz.sectors"
                  :key="sec.name"
                  color="neutral-soft"
                  text-color="neutral-strong"
                  class="q-mr-xs q-mb-xs"
                  :label="sec.slug"
                />
              </div>
              <div class="text-caption q-mt-xs">
                Kapasite: {{ biz.capacity.occupiedSlots }} / {{ biz.capacity.totalSlots }}
              </div>
              <q-btn
                flat
                dense
                size="sm"
                color="primary"
                icon="open_in_new"
                label="Detay"
                class="q-mt-xs"
                @click="openDetail(biz)"
              />
            </div>
          </l-popup>
        </l-marker>
      </l-map>
      <div
        v-if="businessesWithoutLocation.length > 0"
        class="text-caption text-grey-7 q-mt-sm"
      >
        {{ businessesWithoutLocation.length }} işletmenin konum bilgisi bulunmuyor.
      </div>
    </div>

    <!-- Detay Panel — sağdan overlay -->
    <DetailPanel
      v-model="detailOpen"
      :has-content="!!selected"
      :width="480"
    >
      <template #title>
        {{ selected?.name }}
      </template>
      <template #toolbar-actions>
        <StatusBadge
          :slug="selected?.statusSlug ?? ''"
          class="q-mr-sm"
        />
        <PermissionGuard :permission="Permissions.Company.Manage">
          <q-btn
            flat
            round
            dense
            icon="edit"
            aria-label="Düzenle"
            @click="openEditDialog"
          >
            <q-tooltip>Düzenle</q-tooltip>
          </q-btn>
        </PermissionGuard>
      </template>
      <template v-if="selected">
        <div class="q-gutter-sm">
          <InfoItem
            icon="location_on"
            label="Adres"
            :value="selected.address"
          />
          <div
            v-if="selected.location"
            class="q-px-md q-mt-sm"
          >
            <MapPicker
              :model-value="selected.location"
              readonly
              height="200px"
            />
          </div>
          <InfoItem
            icon="phone"
            label="Telefon"
            :value="selected.phoneNumber"
          />
          <InfoItem
            icon="email"
            label="E-posta"
            :value="selected.email"
          />
          <InfoItem
            icon="groups"
            label="Personel Sayısı"
            :value="selected.personnelCount"
          />
          <InfoItem
            icon="category"
            label="Sektörler"
          >
            <q-badge
              v-for="sec in selected.sectors"
              :key="sec.name"
              color="neutral-soft"
              text-color="neutral-strong"
              class="q-mr-xs q-mb-xs"
              :label="sec.slug"
            />
            <span
              v-if="selected.sectors.length === 0"
              class="text-grey-7"
            >Belirtilmemiş</span>
          </InfoItem>

          <q-separator spaced />
          <div class="row items-center">
            <div class="text-subtitle2 text-weight-medium">
              Öğrenci Alabildiği Alanlar
            </div>
            <q-space />
            <PermissionGuard :permission="Permissions.Company.Manage">
              <q-btn
                flat
                round
                dense
                icon="verified"
                color="primary"
                aria-label="Alan yetkilerini düzenle"
                @click="branchAuthDialog = true"
              >
                <q-tooltip>Alan Yetkilerini Düzenle</q-tooltip>
              </q-btn>
            </PermissionGuard>
          </div>
          <!--
            Aşağıdaki v-else satırı bilerek grey-8'de: bu sayfadaki diğer ikincil
            metinler grey-7 (#757575, beyaz üzerinde 4,61:1) iken o satır bir SONUÇ
            bildiriyor (işletmeye hiçbir alandan yerleştirme yapılamaz), en soluk
            tonda kalmamalı → grey-8 (#616161, beyaz üzerinde 6,19:1).
            "Tutarlılık" gerekçesiyle grey-7'ye çekmeyin.
            (Yorum v-if'in ÜSTÜNDE duruyor: v-if ile v-else arasına konursa v-else
             dalı dev derlemesinde tek elemandan Fragment'e döner.)
          -->
          <div v-if="selected.activeBranchCodes.length > 0">
            <q-badge
              v-for="code in selected.activeBranchCodes"
              :key="code"
              color="primary"
              class="q-mr-xs q-mb-xs"
              :label="code"
            />
          </div>
          <div
            v-else
            class="text-grey-8 text-caption"
          >
            Yetkili alan yok — bu işletmeye hiçbir alandan öğrenci yerleştirilemez.
          </div>
          <div
            v-if="revokedBranches.length > 0"
            class="text-caption text-grey-7 q-mt-xs"
          >
            Kaldırılan yetkiler:
            <q-badge
              v-for="code in revokedBranches"
              :key="code"
              color="neutral-soft"
              text-color="neutral-strong"
              class="q-mr-xs"
              :label="code"
            />
          </div>

          <q-separator spaced />
          <div class="text-subtitle2 text-weight-medium">
            Kapasite
          </div>
          <PermissionGuard :permission="Permissions.Company.Manage">
            <div class="row items-center q-gutter-sm">
              <q-input
                v-model.number="capacitySlots"
                type="number"
                label="Toplam Kapasite"
                outlined
                dense
                class="col"
              />
              <q-btn
                color="primary"
                label="Güncelle"
                unelevated
                :loading="saving"
                @click="updateCapacity"
              />
            </div>
          </PermissionGuard>
          <div class="text-caption text-grey-7">
            Dolu: {{ selected.capacity.occupiedSlots }} / {{ selected.capacity.totalSlots }}
            — Müsait: {{ selected.capacity.availableSlots }}
          </div>

          <q-separator spaced />
          <div class="text-subtitle2 text-weight-medium">
            Belgeler
          </div>
          <div
            v-if="selected.documents.length === 0"
            class="text-grey-7 text-caption"
          >
            Belge yok
          </div>
          <q-list
            v-else
            dense
            bordered
            rounded
          >
            <q-item
              v-for="doc in selected.documents"
              :key="doc.id"
              dense
            >
              <q-item-section>
                <q-item-label>{{ doc.typeSlug }}</q-item-label>
                <q-item-label caption>
                  {{ doc.fileName }}
                </q-item-label>
              </q-item-section>
              <q-item-section side>
                <StatusBadge :slug="doc.statusSlug" />
              </q-item-section>
              <q-item-section side>
                <div class="row no-wrap">
                  <q-btn
                    flat
                    dense
                    round
                    icon="visibility"
                    aria-label="Detayları görüntüle"
                    color="primary"
                    @click="previewDoc(doc.id)"
                  >
                    <q-tooltip>Görüntüle</q-tooltip>
                  </q-btn>
                  <PermissionGuard :permission="Permissions.Document.Approve">
                    <q-btn
                      v-if="doc.status === 'Uploaded'"
                      flat
                      dense
                      round
                      icon="check"
                      color="positive"
                      aria-label="Belgeyi onayla"
                      @click="approveDoc(doc.id)"
                    >
                      <q-tooltip>Onayla</q-tooltip>
                    </q-btn>
                  </PermissionGuard>
                  <PermissionGuard :permission="Permissions.Company.Document">
                    <q-btn
                      flat
                      dense
                      round
                      icon="delete"
                      aria-label="Sil"
                      color="negative"
                      @click="confirmDeleteDoc(doc.id, doc.fileName)"
                    >
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
              unelevated
              @click="docUploadDialog = true"
            />
          </PermissionGuard>

          <!-- Durum İşlemleri -->
          <q-separator spaced />
          <div class="text-subtitle2 text-weight-medium">
            İşlemler
          </div>
          <PermissionGuard :permission="Permissions.Company.Manage">
            <div class="q-gutter-sm">
              <!-- PendingApproval → Onayla / Reddet -->
              <template v-if="selected.status === 'PendingApproval'">
                <q-btn
                  color="positive"
                  icon="check_circle"
                  label="Onayla"
                  unelevated
                  :loading="saving"
                  class="full-width"
                  @click="approveFromDrawer"
                />
                <q-btn
                  color="negative"
                  icon="cancel"
                  label="Reddet"
                  unelevated
                  :loading="saving"
                  class="full-width"
                  @click="openReject(selected)"
                />
              </template>
              <!-- Active → Pasife Al / Kapat -->
              <template v-if="selected.status === 'Active'">
                <q-btn
                  color="warning"
                  text-color="white"
                  icon="pause_circle"
                  label="Pasife Al"
                  unelevated
                  :loading="saving"
                  class="full-width"
                  @click="deactivateBusiness"
                />
                <q-btn
                  outline
                  color="negative"
                  icon="block"
                  label="Kapat"
                  :loading="saving"
                  class="full-width"
                  @click="closeBusiness"
                />
              </template>
              <!-- Inactive → Aktifleştir / Kapat -->
              <template v-if="selected.status === 'Inactive'">
                <q-btn
                  color="positive"
                  icon="play_circle"
                  label="Aktifleştir"
                  unelevated
                  :loading="saving"
                  class="full-width"
                  @click="activateBusiness"
                />
                <q-btn
                  outline
                  color="negative"
                  icon="block"
                  label="Kapat"
                  :loading="saving"
                  class="full-width"
                  @click="closeBusiness"
                />
              </template>
            </div>
          </PermissionGuard>
        </div>
      </template>
    </DetailPanel>

    <!-- Form Dialogları -->
    <RejectBusinessForm
      v-model="rejectDialog"
      :business-id="selected?.id ?? ''"
      @saved="afterFormSaved"
    />
    <UploadBusinessDocForm
      v-model="docUploadDialog"
      :business-id="selected?.id ?? ''"
      @saved="afterFormSaved"
    />
    <AuthorizeBranchesForm
      v-model="branchAuthDialog"
      :business="selected"
      @saved="afterBranchesAuthorized"
    />
  </q-page>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, watch } from 'vue'
import type { QTableProps } from 'quasar'
import { useQuasar } from 'quasar'
import { businessApi, type BusinessDto, type SectorDto } from 'src/api/business'
import { useNotify } from 'src/composables/useNotify'
import { useServerPagination } from 'src/composables/useServerPagination'
import { useEntityOptionsStore } from 'stores/entityOptions'
import { useAuthStore } from 'stores/auth'
import { Permissions } from 'utils/permissions'
import AppTable from 'components/AppTable.vue'
import AppNotice from 'components/AppNotice.vue'
import StatusBadge from 'components/StatusBadge.vue'
import PermissionGuard from 'components/PermissionGuard.vue'
import MapPicker from 'components/MapPicker.vue'
import InfoItem from 'components/InfoItem.vue'
import PageHeader from 'components/PageHeader.vue'
import SearchInput from 'components/SearchInput.vue'
import { useConfirmDialog } from 'src/composables/useConfirmDialog'
import DetailPanel from 'components/DetailPanel.vue'
import { useRouter } from 'vue-router'
import RejectBusinessForm from 'components/forms/business/RejectBusinessForm.vue'
import UploadBusinessDocForm from 'components/forms/business/UploadBusinessDocForm.vue'
import AuthorizeBranchesForm from 'components/forms/business/AuthorizeBranchesForm.vue'
import { LMap, LTileLayer, LMarker, LPopup } from '@vue-leaflet/vue-leaflet'
import 'leaflet/dist/leaflet.css'

const $q = useQuasar()
const router = useRouter()
const notify = useNotify()
const entityOptionsStore = useEntityOptionsStore()
const authStore = useAuthStore()
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
const rejectDialog = ref(false)
const docUploadDialog = ref(false)
const branchAuthDialog = ref(false)
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

// Harita: işletme marker'larına otomatik sığdır (fitBounds) — sabit zoom yerine veri extent'i
function fitMapToBusinesses() {
  const pts = businessesWithLocation.value.map(getLatLng)
  const map = (
    businessMapRef.value as {
      leafletObject?: { fitBounds: (b: [number, number][], o?: { padding: [number, number] }) => void }
    } | null
  )?.leafletObject
  if (map && pts.length) map.fitBounds(pts, { padding: [50, 50] })
}
function onBusinessMapReady() {
  fitMapToBusinesses()
}
watch(businessesWithLocation, () => fitMapToBusinesses())
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

/**
 * "SIRA SİZDE" — Tek Ses Kuralı: bu ekranda hardal TEK bağlamda görünür, o da işletme
 * onay kararıdır.
 *
 * Koşul VERİDEN türer, rol adından DEĞİL (ADR-0001):
 *  • `status === 'PendingApproval'` — kararı bekleyen tek aşama (bkz. #body-cell-actions).
 *  • `Permissions.Company.Manage` — kaydı İLERLETEN ucun istediği izin, listeyi AÇAN izin
 *    değil. Ölçüldü: `POST /api/businesses/{id}/approve` ve `.../reject` →
 *    `RequireAuthorization(Permissions.Company.Manage)` (BusinessEndpoints.cs:24-25);
 *    listeyi açan uç yalnız `company:view` ister (BusinessQueryEndpoints.cs:20).
 *  • `Permissions.Institution.View` — KAPSAM ayracı: kararı okul verir, işletme değil.
 *    Tek başına `company:manage` bu iki tarafı AYIRMAZ; ölçüldü (RolePermissionMap.cs):
 *    izni taşıyanlar InstitutionManager ("company:*", satır 24), DeputyDirector (satır 95)
 *    ve CompanyManager (satır 195). CompanyManager'da `company:view` de vardır, yani
 *    /companies rotasına girer ve GET /api/businesses sahiplik kapsamı uygulamaz — koşul
 *    daraltılmasaydı işletme yetkilisi KENDİ kaydında "Sıra sizde" okurdu, oysa o kararı
 *    okul verir. `institution:view` yalnız okul rollerindedir: InstitutionManager
 *    ("institution:*", satır 14), DeputyDirector (satır 60), InstitutionStaff (satır 106);
 *    CompanyManager'da YOKTUR. InstitutionStaff ise `company:manage` taşımaz, yani "VE"
 *    onu da eler. Geriye tam olarak karar mercii kalır.
 *
 *    Bu bir VEKİL ölçüttür, ucun izni değil: `institution:view` "okul tarafındayım" demektir.
 *    Asıl borç (bu partinin kapsamı dışı): onay/red uçları ve #body-cell-actions butonları
 *    `company:manage`ten ayrı bir onay iznine (`company:approve`) bağlanmalı — o gün bu satır
 *    o izinle değiştirilir. Vekil ölçüt yalnız DARALTIR; hiçbir erişim açmaz.
 *
 * NADİRLİK: rozet yalnız görünen satırların BİR KISMI sıradaysa basılır (`showRowSignal`).
 * "Onay Bekliyor" filtresi seçildiğinde sayfadaki her satır bu aşamadadır ve rozet durum
 * sütununun tekrarına dönerdi. Ölçüt artık filtre DEĞERİNE değil sayfadaki orana bakar —
 * aynı hâl filtresiz de doğabilir; o durumda tek cümlelik bildirim gösterilir.
 *
 * Dönem kapalılığı (`periodStore.isReadOnly`) BİLEREK sorgulanmadı: işletme onayı akademik
 * döneme bağlı değildir, bu sayfa dönem store'una hiç dokunmaz ve onay ucu dönem kapanınca
 * kapanmaz. Olmayan bir kapıyı taklit etmek yanlış olurdu.
 *
 * Detay panelindeki evrak onayı ikinci bir bağlam açardı — bilerek işaretlenmedi.
 */
function isMyTurn(row: BusinessDto): boolean {
  return (
    row.status === 'PendingApproval' &&
    authStore.hasPermission(Permissions.Company.Manage) &&
    authStore.hasPermission(Permissions.Institution.View)
  )
}

/** Görünen sayfadaki "sıra sizde" satırları — nadirlik ölçümünün tek kaynağı. */
const turnRows = computed(() => businesses.value.filter(isMyTurn))
/** Satır rozeti: bazı satırlar sırada, hepsi değil — sinyal burada ayırt ediyor. */
const showRowSignal = computed(
  () => turnRows.value.length > 0 && turnRows.value.length < businesses.value.length,
)
/** Hepsi sırada: yirmi rozet yerine tablonun üstünde tek cümle. */
const showTurnNotice = computed(
  () => businesses.value.length > 0 && turnRows.value.length === businesses.value.length,
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


/** İptal edilmiş (aktif olmayan) alan kodları — aktif yetkiye dönmüş olanlar hariç. */
const revokedBranches = computed(() => {
  const business = selected.value
  if (!business) return []
  const active = new Set(business.activeBranchCodes)
  return [
    ...new Set(
      business.authorizedBranches
        .filter((a) => !a.isActive && !active.has(a.branchCode))
        .map((a) => a.branchCode),
    ),
  ]
})

/**
 * Alan yetkisi değişince işletme seçim listesi cache'i bayatlar — yerleştirme ekranı
 * yetkisiz işletmeyi göstermeye devam ederdi (#119).
 */
async function afterBranchesAuthorized() {
  entityOptionsStore.invalidateBusinesses()
  await afterFormSaved()
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
  if (selected.value) router.push(`/companies/${selected.value.id}/edit`).catch(() => {})
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
