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
      <PageHeader :title="institution.fullName" :subtitle="`Kurum Kodu: ${institution.institutionCode}`">
        <PermissionGuard :permission="Permissions.Institution.Manage">
          <q-btn color="primary" icon="edit" label="Düzenle" @click="editDialog = true" />
        </PermissionGuard>
      </PageHeader>

      <q-tabs v-model="tab" align="left" class="q-mb-md">
        <q-tab name="info" label="Genel Bilgi" icon="info" />
        <q-tab name="branches" label="Alanlar" icon="category" />
        <q-tab name="staff" label="Personel" icon="people" />
        <q-tab name="periods" label="Dönemler" icon="date_range" />
      </q-tabs>

      <q-tab-panels v-model="tab" animated>
        <!-- GENEL BİLGİ -->
        <q-tab-panel name="info">
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
                <div class="col-12 col-md-6">
                  <q-item dense>
                    <q-item-section avatar><q-icon name="my_location" color="grey-6" /></q-item-section>
                    <q-item-section>
                      <q-item-label caption>Konum</q-item-label>
                      <q-item-label>
                        <template v-if="institution.location">
                          {{ institution.location.latitude.toFixed(6) }}, {{ institution.location.longitude.toFixed(6) }}
                        </template>
                        <span v-else class="text-grey">Konum eklenmemiş</span>
                      </q-item-label>
                    </q-item-section>
                  </q-item>
                </div>
              </div>
            </q-card-section>
          </q-card>

          <q-card flat bordered>
            <q-card-section>
              <div class="row items-center q-mb-md">
                <div class="col text-subtitle1 text-weight-medium">Ders Programı Ayarları</div>
                <div class="col-auto">
                  <PermissionGuard :permission="Permissions.Institution.Manage">
                    <q-btn
                      color="primary"
                      :icon="scheduleConfig?.configured ? 'edit' : 'settings'"
                      :label="scheduleConfig?.configured ? 'Düzenle' : 'Ayarla'"
                      size="sm"
                      @click="openScheduleDialog"
                    />
                  </PermissionGuard>
                </div>
              </div>
              <div v-if="!scheduleConfig || !scheduleConfig.configured" class="text-grey q-pa-sm">
                <q-icon name="info" class="q-mr-xs" />
                Henüz ayarlanmamış.
              </div>
              <div v-else>
                <q-item dense>
                  <q-item-section avatar><q-icon name="schedule" color="grey-6" /></q-item-section>
                  <q-item-section>
                    <q-item-label caption>Günlük Ders Sayısı</q-item-label>
                    <q-item-label class="text-h6">{{ scheduleConfig.dailyPeriodCount }}</q-item-label>
                  </q-item-section>
                </q-item>
                <div class="text-caption text-grey q-ml-lg q-mt-xs">
                  Son güncelleme: {{ formatDate(scheduleConfig.updatedAt ?? '') }}
                </div>
              </div>
            </q-card-section>
          </q-card>
        </q-tab-panel>

        <!-- ALANLAR -->
        <q-tab-panel name="branches">
          <div class="row items-center q-mb-md">
            <div class="col text-subtitle1 text-weight-medium">Eğitim Alanları</div>
            <div class="col-auto">
              <PermissionGuard :permission="Permissions.Institution.Manage">
                <q-btn color="primary" icon="add" label="Alan Ekle" @click="openBranchDialog" />
              </PermissionGuard>
            </div>
          </div>

          <div v-if="activeBranches.length === 0" class="text-center q-pa-xl text-grey-6">
            <q-icon name="category" size="48px" class="q-mb-sm" />
            <div>Henüz alan eklenmemiş.</div>
          </div>

          <div class="q-gutter-md">
            <q-card v-for="branch in activeBranches" :key="branch.fieldCode" flat bordered>
              <q-card-section>
                <div class="row items-center">
                  <div class="col">
                    <div class="text-subtitle1 text-weight-medium">{{ branch.fieldName }}</div>
                    <StatusBadge :slug="branch.typeSlug" class="q-mt-xs" />
                  </div>
                  <div class="col-auto q-gutter-sm">
                    <PermissionGuard :permission="Permissions.Institution.Manage">
                      <q-btn
                        flat dense size="sm"
                        icon="tune"
                        label="Uzmanlıklar"
                        color="primary"
                        @click="openSpecializationDialog(branch)"
                      />
                      <q-btn
                        flat dense size="sm"
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
                    <div class="col text-caption text-grey">Kapasite</div>
                    <div class="col-auto text-caption">
                      {{ branch.atWorkCount }} / {{ branch.totalCount }}
                      <span class="text-grey">(Müsait: {{ branch.availableCount }})</span>
                    </div>
                  </div>
                  <q-linear-progress
                    :value="branch.totalCount > 0 ? branch.atWorkCount / branch.totalCount : 0"
                    color="primary"
                    style="height: 8px; border-radius: 4px"
                  />
                </div>

                <div v-if="branch.activeSpecializations.length > 0" class="q-mt-md">
                  <div class="text-caption text-grey q-mb-xs">Aktif Uzmanlıklar</div>
                  <div class="q-gutter-xs">
                    <q-chip
                      v-for="spec in branch.activeSpecializations"
                      :key="spec"
                      dense size="sm"
                      color="primary"
                      text-color="white"
                      :label="getSpecializationName(branch.fieldCode, spec)"
                    />
                  </div>
                </div>
                <div v-else class="q-mt-sm text-caption text-grey">
                  Uzmanlık alanı tanımlanmamış.
                </div>
              </q-card-section>
            </q-card>
          </div>
        </q-tab-panel>

        <!-- PERSONEL -->
        <q-tab-panel name="staff">
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
            <div class="col text-subtitle1 text-weight-medium">Akademik Dönemler</div>
            <div class="col-auto">
              <PermissionGuard :permission="Permissions.Institution.Manage">
                <q-btn color="primary" icon="add" label="Yeni Dönem" size="sm" @click="openPeriodDialog" />
              </PermissionGuard>
            </div>
          </div>

          <div v-if="periods.length === 0" class="text-center q-pa-xl text-grey-6">
            <q-icon name="date_range" size="48px" class="q-mb-sm" />
            <div>Henüz dönem oluşturulmamış.</div>
          </div>

          <AppTable v-else :rows="periods" :columns="periodColumns">
            <template #body-cell-status="{ row }">
              <q-td>
                <q-badge
                  :color="row.status === 'Active' ? 'green-7' : 'grey-5'"
                  :label="row.statusSlug"
                />
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
            <template #body-cell-actions="{ row }">
              <q-td class="text-right">
                <PermissionGuard :permission="Permissions.Institution.Manage">
                  <q-btn
                    v-if="row.status === 'Active'"
                    flat dense size="sm"
                    icon="lock"
                    label="Kapat"
                    color="orange-8"
                    @click="confirmClosePeriod(row)"
                  />
                </PermissionGuard>
              </q-td>
            </template>
          </AppTable>
        </q-tab-panel>
      </q-tab-panels>
    </template>

    <EditInstitutionForm
      v-model="editDialog"
      :institution-id="institutionId"
      :institution="institution"
      @saved="load"
    />
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
import AppTable from 'components/AppTable.vue'
import StatusBadge from 'components/StatusBadge.vue'
import PermissionGuard from 'components/PermissionGuard.vue'
import PageHeader from 'components/PageHeader.vue'
import { useConfirmDialog } from 'src/composables/useConfirmDialog'
import { useAcademicPeriodStore } from 'stores/academicPeriod'
import { useInstitutionStore } from 'stores/institution'
import EditInstitutionForm from 'components/forms/institution/EditInstitutionForm.vue'
import AddStaffForm from 'components/forms/institution/AddStaffForm.vue'
import AddBranchForm from 'components/forms/institution/AddBranchForm.vue'
import EditSpecializationsForm from 'components/forms/institution/EditSpecializationsForm.vue'
import ScheduleConfigForm from 'components/forms/institution/ScheduleConfigForm.vue'
import CreatePeriodForm from 'components/forms/institution/CreatePeriodForm.vue'

const periodStore = useAcademicPeriodStore()
const institutionStore = useInstitutionStore()
const notify = useNotify()
const confirmDialog = useConfirmDialog()

// ── Core State ──
const loading = ref(false)
const saving = ref(false)
const error = ref<string | null>(null)
const institution = ref<InstitutionDto | null>(null)
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
const editDialog = ref(false)
const staffDialog = ref(false)
const branchDialog = ref(false)
const specDialog = ref(false)
const scheduleDialog = ref(false)
const periodDialog = ref(false)

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
      const listRes = await institutionApi.list()
      const institutions = listRes.data
      if (!institutions || institutions.length === 0) {
        error.value = 'Kayıtlı kurum bulunamadı.'
        return
      }
      institutionId.value = institutions[0].id
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

function openPeriodDialog() {
  periodDialog.value = true
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
    okColor: 'orange-8',
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
