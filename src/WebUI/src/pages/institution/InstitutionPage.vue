<template>
  <q-page padding>
    <DataState
      :loading="loading"
      :error="!!error"
      :error-text="error ?? undefined"
      retryable
      gears
      spinner-size="48px"
      padding="q-pa-xl"
      @retry="load"
    >
      <template v-if="institution">
        <PageHeader
          :title="institution.fullName"
          :subtitle="`Kurum Kodu: ${institution.institutionCode}`"
        >
          <PermissionGuard :permission="Permissions.Institution.Manage">
            <q-btn
              unelevated
              color="primary"
              icon="edit"
              label="Düzenle"
              @click="router.push('/institution/edit')"
            />
          </PermissionGuard>
        </PageHeader>

        <q-tabs
          v-model="tab"
          align="left"
          class="q-mb-md"
        >
          <q-tab
            name="info"
            label="Genel Bilgi"
            icon="info"
          />
          <q-tab
            name="branches"
            label="Alanlar"
            icon="category"
          />
          <q-tab
            name="staff"
            label="Personel"
            icon="people"
          />
          <q-tab
            name="periods"
            label="Dönemler"
            icon="date_range"
          />
        </q-tabs>

        <q-tab-panels
          v-model="tab"
          animated
        >
          <!-- GENEL BİLGİ -->
          <q-tab-panel name="info">
            <q-card
              flat
              bordered
              class="q-mb-lg"
            >
              <q-card-section>
                <div class="text-subtitle1 text-weight-medium q-mb-md">
                  Kurum Bilgileri
                </div>
                <div class="row q-col-gutter-md info-items">
                  <div class="col-12 col-md-6">
                    <InfoItem
                      icon="location_on"
                      label="Adres"
                      :value="institution.address"
                    />
                  </div>
                  <div class="col-12 col-md-6">
                    <InfoItem
                      icon="map"
                      label="İl / İlçe"
                    >
                      <!-- Ad görüntü, kod yetkili (#147) — ikisi birlikte gösterilir ki
                           kaydın hangi il koduyla saklandığı ekrandan doğrulanabilsin. -->
                      <template v-if="institution.provinceName">
                        {{ institution.provinceName }} ({{ institution.provinceCode }})
                        <template v-if="institution.districtName">
                          / {{ institution.districtName }}
                        </template>
                      </template>
                      <span v-else>—</span>
                    </InfoItem>
                  </div>
                  <div class="col-12 col-md-6">
                    <InfoItem
                      icon="phone"
                      label="Telefon"
                      :value="institution.phoneNumber"
                    />
                  </div>
                  <div class="col-12 col-md-6">
                    <InfoItem
                      icon="email"
                      label="E-posta"
                      :value="institution.email"
                    />
                  </div>
                  <div class="col-12 col-md-6">
                    <InfoItem
                      icon="language"
                      label="Web Sitesi"
                    >
                      <!-- href yalnız süzülmüş http(s) URL'i alır: serbest metin alanına
                        yazılmış javascript:/data: adresi tıklayanın oturumunda çalışırdı.
                        rel="noopener noreferrer" ters sekme ele geçirmesini kapatır. -->
                      <a
                        v-if="safeWebUrl"
                        :href="safeWebUrl"
                        target="_blank"
                        rel="noopener noreferrer"
                        class="text-primary"
                      >
                        {{ institution.webUrl }}
                      </a>
                      <span v-else-if="institution.webUrl">{{ institution.webUrl }}</span>
                      <span v-else>—</span>
                    </InfoItem>
                  </div>
                  <div class="col-12 col-md-6">
                    <InfoItem
                      icon="my_location"
                      label="Konum"
                    >
                      <template v-if="institution.location">
                        {{ institution.location.latitude.toFixed(6) }}, {{ institution.location.longitude.toFixed(6) }}
                      </template>
                      <span
                        v-else
                        class="text-grey-7"
                      >Konum eklenmemiş</span>
                    </InfoItem>
                  </div>
                </div>
              </q-card-section>
            </q-card>

            <!--
              Kurum teması — kiracının marka paleti.

              Seçim küratörlüdür: serbest renk girilmez, sekiz ölçülmüş seçenekten biri
              seçilir. Renk kutusu tek başına yetmez, paletin Türkçe adı görünür metin
              olarak yanında durur (DESIGN.md "Renk Yalnız Kanıt Kuralı").
            -->
            <q-card
              flat
              bordered
              class="q-mb-lg"
            >
              <q-card-section>
                <div class="row items-center q-mb-md">
                  <div class="col text-subtitle1 text-weight-medium">
                    Kurum Teması
                  </div>
                  <div class="col-auto">
                    <PermissionGuard :permission="Permissions.Institution.Manage">
                      <q-btn
                        unelevated
                        color="primary"
                        icon="palette"
                        label="Değiştir"
                        @click="openBrandPaletteDialog"
                      />
                    </PermissionGuard>
                  </div>
                </div>
                <div class="row items-center no-wrap q-gutter-sm">
                  <BrandPaletteSwatch
                    :primary="institution.brandPrimary"
                    :secondary="institution.brandSecondary"
                  />
                  <div class="col">
                    <div class="text-body2 text-weight-medium">
                      {{ institution.brandPaletteSlug }}
                    </div>
                    <div class="text-caption text-grey-7">
                      Üst bar, birincil butonlar ve rozetler bu renkten türer. Durum renkleri
                      (onay, ret, uyarı, bilgi) kiracıya göre değişmez.
                    </div>
                  </div>
                </div>
              </q-card-section>
            </q-card>

            <q-card
              flat
              bordered
            >
              <q-card-section>
                <div class="row items-center q-mb-md">
                  <div class="col text-subtitle1 text-weight-medium">
                    Ders Programı Ayarları
                  </div>
                  <div class="col-auto">
                    <PermissionGuard :permission="Permissions.Institution.Manage">
                      <q-btn
                        unelevated
                        color="primary"
                        :icon="scheduleConfig?.configured ? 'edit' : 'settings'"
                        :label="scheduleConfig?.configured ? 'Düzenle' : 'Ayarla'"
                        @click="openScheduleDialog"
                      />
                    </PermissionGuard>
                  </div>
                </div>
                <div
                  v-if="!scheduleConfig || !scheduleConfig.configured"
                  class="text-grey-7 q-pa-sm"
                >
                  <q-icon
                    name="info"
                    class="q-mr-xs"
                  />
                  Henüz ayarlanmamış.
                </div>
                <div
                  v-else
                  class="info-items"
                >
                  <InfoItem
                    icon="schedule"
                    label="Günlük Ders Sayısı"
                  >
                    <span class="text-h6">{{ scheduleConfig.dailyPeriodCount }}</span>
                  </InfoItem>
                  <div class="text-caption text-grey-7 q-ml-lg q-mt-xs">
                    Son güncelleme: {{ formatDate(scheduleConfig.updatedAt ?? '') }}
                  </div>
                </div>
              </q-card-section>
            </q-card>
          </q-tab-panel>

          <!-- ALANLAR -->
          <q-tab-panel name="branches">
            <div class="row items-center q-mb-md">
              <div class="col text-subtitle1 text-weight-medium">
                Eğitim Alanları
              </div>
              <div class="col-auto">
                <PermissionGuard :permission="Permissions.Institution.Manage">
                  <q-btn
                    unelevated
                    color="primary"
                    icon="add"
                    label="Alan Ekle"
                    @click="openBranchDialog"
                  />
                </PermissionGuard>
              </div>
            </div>

            <DataState
              :empty="activeBranches.length === 0"
              padding="q-pa-xl"
            >
              <!-- Boş durum ölçeği aynı sayfadaki Dönemler sekmesiyle (AppTable) ortak
                   tutulur: 48px nötr ikon + 14px gövde metni. DataState'in varsayılanı
                   2em ikon + 12px caption'dır ve bu sayfada iki ayrı ölçek doğururdu. -->
              <template #empty>
                <q-icon
                  name="category"
                  size="48px"
                  class="q-mb-sm"
                />
                <div>Henüz alan eklenmemiş.</div>
              </template>
              <div class="q-gutter-md">
                <q-card
                  v-for="branch in activeBranches"
                  :key="branch.fieldCode"
                  flat
                  bordered
                >
                  <q-card-section>
                    <div class="row items-center">
                      <div class="col">
                        <div class="text-subtitle1 text-weight-medium">
                          {{ branch.fieldName }}
                        </div>
                        <StatusBadge
                          :slug="branch.typeSlug"
                          class="q-mt-xs"
                        />
                      </div>
                      <div class="col-auto q-gutter-sm">
                        <PermissionGuard :permission="Permissions.Institution.Manage">
                          <q-btn
                            flat
                            dense
                            size="sm"
                            icon="tune"
                            label="Uzmanlıklar"
                            color="primary"
                            @click="openSpecializationDialog(branch)"
                          />
                          <q-btn
                            flat
                            dense
                            size="sm"
                            icon="block"
                            label="Pasife Al"
                            color="negative"
                            @click="confirmDeactivateBranch(branch)"
                          />
                        </PermissionGuard>
                      </div>
                    </div>

                    <div class="q-mt-md">
                      <div class="row items-center q-mb-xs">
                        <div class="col text-caption text-grey-7">
                          Kapasite
                        </div>
                        <div class="col-auto text-caption">
                          {{ branch.atWorkCount }} / {{ branch.totalCount }}
                          <span class="text-grey-7">(Müsait: {{ branch.availableCount }})</span>
                        </div>
                      </div>
                      <q-linear-progress
                        :value="branch.totalCount > 0 ? branch.atWorkCount / branch.totalCount : 0"
                        color="primary"
                        style="height: 8px; border-radius: 4px"
                      />
                    </div>

                    <div
                      v-if="branch.activeSpecializations.length > 0"
                      class="q-mt-md"
                    >
                      <div class="text-caption text-grey-7 q-mb-xs">
                        Aktif Uzmanlıklar
                      </div>
                      <div class="q-gutter-xs">
                        <q-chip
                          v-for="spec in branch.activeSpecializations"
                          :key="spec"
                          dense
                          size="sm"
                          color="primary"
                          text-color="white"
                          :label="getSpecializationName(branch.fieldCode, spec)"
                        />
                      </div>
                    </div>
                    <div
                      v-else
                      class="q-mt-sm text-caption text-grey-7"
                    >
                      Uzmanlık alanı tanımlanmamış.
                    </div>
                  </q-card-section>
                </q-card>
              </div>
            </DataState>
          </q-tab-panel>

          <!-- PERSONEL -->
          <q-tab-panel name="staff">
            <div class="row items-center q-mb-md">
              <div class="col text-subtitle1 text-weight-medium">
                Personel
              </div>
              <div class="col-auto">
                <PermissionGuard :permission="Permissions.Institution.Staff">
                  <q-btn
                    unelevated
                    color="primary"
                    icon="person_add"
                    label="Personel Ekle"
                    @click="openStaffDialog"
                  />
                </PermissionGuard>
              </div>
            </div>
            <AppTable
              :rows="institution?.staff ?? []"
              :columns="staffColumns"
            >
              <template #body-cell-roleSlug="{ row }">
                <q-td>
                  <q-badge
                    color="neutral"
                    :label="row.roleSlug"
                  />
                </q-td>
              </template>
              <template #body-cell-branchName="{ row }">
                <q-td>{{ getBranchName(row.branchCode) }}</q-td>
              </template>
              <template #body-cell-authorizedAt="{ row }">
                <q-td>{{ formatDate(row.authorizedAt) }}</q-td>
              </template>
            </AppTable>
          </q-tab-panel>

          <!-- DÖNEMLER -->
          <q-tab-panel name="periods">
            <div class="row items-center q-mb-md">
              <div class="col text-subtitle1 text-weight-medium">
                Akademik Dönemler
              </div>
              <div class="col-auto">
                <PermissionGuard :permission="Permissions.Institution.Manage">
                  <q-btn
                    unelevated
                    color="primary"
                    icon="add"
                    label="Yeni Dönem"
                    @click="openPeriodDialog"
                  />
                </PermissionGuard>
              </div>
            </div>

            <AppTable
              :rows="periods"
              :columns="periodColumns"
              no-data-label="Henüz dönem oluşturulmamış."
            >
              <template #empty-action>
                <PermissionGuard :permission="Permissions.Institution.Manage">
                  <q-btn
                    unelevated
                    color="primary"
                    icon="add"
                    label="İlk dönemi oluştur"
                    @click="openPeriodDialog"
                  />
                </PermissionGuard>
              </template>
              <template #body-cell-status="{ row }">
                <q-td>
                  <StatusBadge :slug="row.statusSlug" />
                </q-td>
              </template>
              <template #body-cell-startDate="{ row }">
                <q-td>{{ formatDate(row.startDate) }}</q-td>
              </template>
              <template #body-cell-endDate="{ row }">
                <q-td>{{ formatDate(row.endDate) }}</q-td>
              </template>
              <template #body-cell-createdAt="{ row }">
                <q-td>{{ formatDate(row.createdAt) }}</q-td>
              </template>
              <template #body-cell-gradeWindow="{ row }">
                <q-td>
                  <q-badge
                    v-if="row.gradeEntryStartDate && row.gradeEntryEndDate"
                    color="positive"
                    :label="`${formatDate(row.gradeEntryStartDate)} – ${formatDate(row.gradeEntryEndDate)}`"
                  />
                  <span
                    v-else
                    class="text-grey-7"
                  >—</span>
                </q-td>
              </template>
              <template #body-cell-actions="{ row }">
                <q-td class="text-right">
                  <PermissionGuard :permission="Permissions.Institution.ManageGradeWindow">
                    <q-btn
                      v-if="row.status === 'Active'"
                      flat
                      dense
                      size="sm"
                      icon="event_available"
                      label="Not Girişi"
                      color="positive"
                      @click="openGradeWindowDialog(row)"
                    >
                      <q-tooltip>Dönem sonu not giriş penceresini aç/güncelle</q-tooltip>
                    </q-btn>
                  </PermissionGuard>
                  <PermissionGuard :permission="Permissions.Institution.Manage">
                    <q-btn
                      v-if="row.status === 'Active'"
                      flat
                      dense
                      size="sm"
                      icon="lock"
                      label="Kapat"
                      color="warning"
                      @click="confirmClosePeriod(row)"
                    />
                  </PermissionGuard>
                </q-td>
              </template>
            </AppTable>
          </q-tab-panel>
        </q-tab-panels>
      </template>
    </DataState>

    <AddStaffForm
      v-model="staffDialog"
      :institution-id="institutionId"
      :branch-options="branchSelectOptions"
      @saved="load"
    />
    <AddBranchForm
      v-model="branchDialog"
      :institution-id="institutionId"
      :active-branch-codes="activeBranches.map(b => b.fieldCode)"
      @saved="load"
    />
    <EditSpecializationsForm
      v-model="specDialog"
      :institution-id="institutionId"
      :field-code="specTarget?.fieldCode ?? ''"
      :field-name="specTarget?.fieldName ?? ''"
      :all-specializations="specTarget?.allSpecializations ?? []"
      :active-specializations="specTarget?.activeSpecializations ?? []"
      @saved="load"
    />
    <BrandPaletteForm
      v-model="brandPaletteDialog"
      :institution-id="institutionId"
      :current-palette-name="institution?.brandPaletteName ?? ''"
      @saved="load"
    />
    <ScheduleConfigForm
      v-model="scheduleDialog"
      :institution-id="institutionId"
      :current-count="scheduleConfig?.dailyPeriodCount ?? 8"
      @saved="loadSchedule"
    />
    <CreatePeriodForm
      v-model="periodDialog"
      :institution-id="institutionId"
      @saved="load"
    />
    <GradeEntryWindowForm
      v-model="gradeWindowDialog"
      :institution-id="institutionId"
      :period="gradeWindowTarget"
      @saved="load"
    />
  </q-page>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import type { QTableProps } from 'quasar'
import {
  institutionApi,
  type InstitutionDto,
  type InstitutionBranchDto,
  type FieldOfStudyDto,
  type ScheduleConfigDto,
  type SpecializationDto,
  type AcademicPeriodDto,
} from 'src/api/institution'
import { useNotify } from 'src/composables/useNotify'
import { Permissions } from 'utils/permissions'
import { toSafeUrl } from 'utils/safeUrl'
import AppTable from 'components/AppTable.vue'
import DataState from 'components/DataState.vue'
import InfoItem from 'components/InfoItem.vue'
import StatusBadge from 'components/StatusBadge.vue'
import PermissionGuard from 'components/PermissionGuard.vue'
import PageHeader from 'components/PageHeader.vue'
import { useConfirmDialog } from 'src/composables/useConfirmDialog'
import { useAcademicPeriodStore } from 'stores/academicPeriod'
import { useInstitutionStore } from 'stores/institution'
import { useAuthStore } from 'stores/auth'
import { resolveEditableInstitutionId } from 'utils/institutionScope'
import { useRoute, useRouter } from 'vue-router'
import AddStaffForm from 'components/forms/institution/AddStaffForm.vue'
import AddBranchForm from 'components/forms/institution/AddBranchForm.vue'
import EditSpecializationsForm from 'components/forms/institution/EditSpecializationsForm.vue'
import ScheduleConfigForm from 'components/forms/institution/ScheduleConfigForm.vue'
import BrandPaletteForm from 'components/forms/institution/BrandPaletteForm.vue'
import BrandPaletteSwatch from 'components/BrandPaletteSwatch.vue'
import CreatePeriodForm from 'components/forms/institution/CreatePeriodForm.vue'
import GradeEntryWindowForm from 'components/forms/institution/GradeEntryWindowForm.vue'

const periodStore = useAcademicPeriodStore()
const institutionStore = useInstitutionStore()
const authStore = useAuthStore()
const notify = useNotify()
const router = useRouter()
const route = useRoute()
const confirmDialog = useConfirmDialog()

// ── Core State ──
const loading = ref(false)
const saving = ref(false)
const error = ref<string | null>(null)
const institution = ref<InstitutionDto | null>(null)

// Bağlantı olarak SADECE http(s) adres verilir; güvenli değilse metin olarak gösterilir.
const safeWebUrl = computed(() => toSafeUrl(institution.value?.webUrl))
const scheduleConfig = ref<ScheduleConfigDto | null>(null)
const fieldCatalog = ref<FieldOfStudyDto[]>([])

const tab = ref('info')
const institutionId = ref<string>('')
const periods = ref<AcademicPeriodDto[]>([])

// ── Computed ──
const activeBranches = computed(() =>
  institution.value?.branches.filter((b) => b.isActive) ?? [],
)

const branchSelectOptions = computed(() =>
  activeBranches.value.map((b) => ({ label: b.fieldName, value: b.fieldCode })),
)

// ── Dialog Visibility ──
const staffDialog = ref(false)
const branchDialog = ref(false)
const specDialog = ref(false)
const scheduleDialog = ref(false)
const brandPaletteDialog = ref(false)
const periodDialog = ref(false)
const gradeWindowDialog = ref(false)
const gradeWindowTarget = ref<AcademicPeriodDto | null>(null)

// ── Specialization target (for passing to EditSpecializationsForm) ──
const specTarget = ref<{
  fieldCode: string
  fieldName: string
  allSpecializations: SpecializationDto[]
  activeSpecializations: string[]
} | null>(null)

// ── Table Columns ──
const staffColumns: QTableProps['columns'] = [
  { name: 'fullName', label: 'Ad Soyad', field: 'fullName', align: 'left', sortable: true },
  { name: 'roleSlug', label: 'Rol', field: 'roleSlug', align: 'left' },
  { name: 'branchName', label: 'Alan', field: 'branchCode', align: 'left' },
  { name: 'authorizedAt', label: 'Yetkilendirme Tarihi', field: 'authorizedAt', align: 'left' },
]

const periodColumns: QTableProps['columns'] = [
  { name: 'name', label: 'Dönem Adı', field: 'name', align: 'left', sortable: true },
  { name: 'startDate', label: 'Başlangıç', field: 'startDate', align: 'left' },
  { name: 'endDate', label: 'Bitiş', field: 'endDate', align: 'left' },
  { name: 'status', label: 'Durum', field: 'status', align: 'left' },
  { name: 'gradeWindow', label: 'Not Penceresi', field: 'id', align: 'left' },
  { name: 'createdAt', label: 'Oluşturulma', field: 'createdAt', align: 'left' },
  { name: 'actions', label: '', field: 'id', align: 'right' },
]

// ── Helpers ──
function formatDate(iso: string) {
  return new Date(iso).toLocaleDateString('tr-TR', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
  })
}

function getBranchName(branchCode: string | null): string {
  if (!branchCode) return '\u2014'
  const branch = institution.value?.branches.find((b) => b.fieldCode === branchCode)
  return branch?.fieldName ?? branchCode
}

function getSpecializationName(fieldCode: string, specCode: string): string {
  const field = fieldCatalog.value.find((f) => f.code === fieldCode)
  if (!field) return specCode
  const spec = field.specializations.find((s) => s.code === specCode)
  return spec?.name ?? specCode
}

// ── Data Loading ──
async function load() {
  loading.value = true
  error.value = null
  try {
    if (!institutionId.value) {
      // Sıra: rota parametresi → aktörün kendi kurumu → liste. Liste yalnız kurumu olmayan
      // platform aktörü için yedektir. "Listenin ilk satırı" demek, sıralaması olmayan bir
      // sorguya güvenmekti ve platform aktöründe her yazmadan sonra başka bir okulu
      // düzenletiyordu — bkz. utils/institutionScope.ts.
      const routeId = typeof route.params.id === 'string' ? route.params.id : null
      const ownId = authStore.user?.institutionId ?? null
      const listRes = routeId || ownId ? null : await institutionApi.list({ pageSize: 100 })
      const resolved = resolveEditableInstitutionId(routeId, ownId, listRes?.data?.items ?? [])
      if (!resolved) {
        error.value = 'Kayıtlı kurum bulunamadı.'
        return
      }
      institutionId.value = resolved
    }

    const [instRes, schedRes, periodsRes] = await Promise.all([
      institutionApi.get(institutionId.value),
      institutionApi.getScheduleConfig(institutionId.value),
      institutionApi.listAcademicPeriods(institutionId.value, { pageSize: 100 }),
    ])
    institution.value = instRes.data
    scheduleConfig.value = schedRes.data
    periods.value = periodsRes.data?.items ?? []
    // Bu yönetim sayfası kurum/branş/program verisini değiştirir; paylaşılan store
    // cache'ini geçersiz kıl → diğer sayfalar bir sonraki erişimde taze veri çeker.
    institutionStore.clear()
  } catch {
    error.value = 'Kurum bilgileri yüklenirken bir hata oluştu.'
  } finally {
    loading.value = false
  }
}

async function loadFieldCatalog() {
  try {
    const res = await institutionApi.getFieldCatalog()
    fieldCatalog.value = res.data
  } catch (e) {
    notify.apiError(e, 'Alan kataloğu yüklenirken hata oluştu.')
  }
}

async function loadSchedule() {
  try {
    const res = await institutionApi.getScheduleConfig(institutionId.value)
    scheduleConfig.value = res.data
    institutionStore.clear()
  } catch { /* sessiz */ }
}

// ── Dialog Openers ──
function openStaffDialog() {
  staffDialog.value = true
}

function openBranchDialog() {
  branchDialog.value = true
}

async function openSpecializationDialog(branch: InstitutionBranchDto) {
  if (fieldCatalog.value.length === 0) {
    await loadFieldCatalog()
  }
  const field = fieldCatalog.value.find((f) => f.code === branch.fieldCode)
  specTarget.value = {
    fieldCode: branch.fieldCode,
    fieldName: branch.fieldName,
    allSpecializations: field?.specializations.filter((s) => s.isActive) ?? [],
    activeSpecializations: [...branch.activeSpecializations],
  }
  specDialog.value = true
}

function openScheduleDialog() {
  scheduleDialog.value = true
}

function openBrandPaletteDialog() {
  brandPaletteDialog.value = true
}

function openPeriodDialog() {
  periodDialog.value = true
}

function openGradeWindowDialog(period: AcademicPeriodDto) {
  gradeWindowTarget.value = period
  gradeWindowDialog.value = true
}

// ── Direct Actions (no form needed) ──
function confirmDeactivateBranch(branch: InstitutionBranchDto) {
  confirmDialog.confirm({
    title: 'Pasife Al',
    message: `"${branch.fieldName}" alanını pasife almak istediğinizden emin misiniz?`,
    okLabel: 'Pasife Al',
    onOk: async () => {
      saving.value = true
      try {
        await institutionApi.deactivateBranch(institutionId.value, branch.fieldCode)
        notify.success('Alan pasife alındı.')
        await load()
      } catch (e) {
        notify.apiError(e, 'İşlem sırasında hata oluştu.')
      } finally {
        saving.value = false
      }
    },
  })
}

function confirmClosePeriod(period: AcademicPeriodDto) {
  confirmDialog.confirm({
    title: 'Dönemi Kapat',
    message: `"${period.name}" dönemini kapatmak istediğinizden emin misiniz? Bu işlem geri alınamaz.`,
    okLabel: 'Kapat',
    okColor: 'warning',
    onOk: async () => {
      saving.value = true
      try {
        await institutionApi.closeAcademicPeriod(institutionId.value, period.id)
        notify.success('Dönem kapatıldı.')
        await load()
        await periodStore.loadPeriods()
      } catch (e) {
        notify.apiError(e, 'Dönem kapatılırken bir hata oluştu.')
      } finally {
        saving.value = false
      }
    },
  })
}

onMounted(load)
</script>

<style scoped>
/*
 * InfoItem ikonu ikincil tondadır — değer metni birincil kalır.
 *
 * Neden bir kural gerekiyor: Quasar `.q-item__section--side` için $grey-7 atar,
 * ama `.q-item__section--avatar` bunu `color: inherit` ile geri alır
 * (quasar/src/components/item/QItem.sass) — InfoItem ikonu gövde metni rengine
 * yükseliyordu.
 *
 * Ton ESKİDEN (çağrı yerindeki `color="grey-6"`) #9e9e9e idi ve InfoItem'a
 * inerken düştü. Birebir geri GETİRİLMEDİ, bilerek bir kademe koyulaştırıldı:
 * ikon DEKORATİFTİR — InfoItem şablonu (components/InfoItem.vue) ikonun hemen
 * yanına koşulsuz bir `q-item-label caption` basar (Adres, Telefon, E-posta,
 * Konum …) ve aynı bilgiyi görünür veriyor. Bu yüzden WCAG 1.4.11 muafiyeti
 * geçerlidir ve 3:1 grafik nesnesi eşiği YÜRÜRLÜKTE DEĞİLDİR.
 * #757575 bir erişilebilirlik zorunluluğu değil, bilinçli hiyerarşi kararıdır.
 *
 * Ölçüm (WCAG 2.x, sRGB relative luminance; beyaz zemin #FFFFFF):
 *   grey-6 #9e9e9e → 2,68:1  — muafiyet olmasaydı 3:1'i geçemezdi
 *   grey-7 #757575 → 4,61:1  — muafiyete güvenmeden de her iki eşiği geçer
 * Muafiyet varken bile koyu ton seçildi: bu ikonlar bir gün etiketsiz bir
 * bağlama taşınırsa (etiket InfoItem'dan kalkarsa) muafiyet düşer ve ton
 * sessizce eşik altına inerdi.
 *
 * Hex Quasar'ın $grey-7 token'ıdır (quasar/src/css/variables.sass:360) — aynı
 * zamanda Quasar'ın kendi `.q-item__section--side` rengi, uydurma bir ton değil.
 * Değeri değiştirmeden önce kontrastı yeniden ölç.
 */
.info-items :deep(.q-item__section--avatar .q-icon) {
  color: #757575;
}
</style>
