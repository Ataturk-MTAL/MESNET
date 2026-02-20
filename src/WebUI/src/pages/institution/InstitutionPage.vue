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

      <q-tabs v-model="tab" align="left" class="q-mb-md">
        <q-tab name="info" label="Genel Bilgi" icon="info" />
        <q-tab name="branches" label="Alanlar" icon="category" />
        <q-tab name="staff" label="Personel" icon="people" />
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
      </q-tab-panels>
    </template>

    <!-- Kurum Düzenleme Dialog -->
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

    <!-- Personel Ekleme Dialog -->
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
                  <q-item-label v-if="opt.caption" caption>{{ opt.caption }}</q-item-label>
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
            label="Rol *"
            filled
            emit-value
            map-options
          />
          <q-select
            v-model="staffForm.branchCode"
            :options="branchSelectOptions"
            label="Alan (opsiyonel)"
            filled
            emit-value
            map-options
            clearable
          />
        </q-card-section>
        <q-card-actions align="right">
          <q-btn flat label="İptal" @click="staffDialog = false" />
          <q-btn color="primary" label="Yetkilendir" :loading="saving" @click="addStaff" />
        </q-card-actions>
      </q-card>
    </q-dialog>

    <!-- Alan Ekleme Dialog -->
    <q-dialog v-model="branchDialog" persistent>
      <q-card style="min-width: 480px">
        <q-card-section class="row items-center">
          <div class="text-h6">Alan Ekle</div>
          <q-space />
          <q-btn flat round dense icon="close" @click="branchDialog = false" />
        </q-card-section>
        <q-card-section class="q-gutter-sm">
          <q-select
            v-model="branchForm.educationType"
            :options="educationTypeOptions"
            label="Eğitim Türü"
            filled
            emit-value
            map-options
            @update:model-value="onEducationTypeChange"
          />
          <q-select
            v-model="branchForm.fieldCode"
            :options="availableFields"
            :loading="loadingCatalog"
            label="Alan *"
            filled
            use-input
            input-debounce="0"
            emit-value
            map-options
            option-label="label"
            option-value="value"
            :disable="!branchForm.educationType"
            @filter="filterFields"
          >
            <template #option="{ itemProps, opt }">
              <q-item v-bind="itemProps">
                <q-item-section>
                  <q-item-label>{{ opt.label }}</q-item-label>
                  <q-item-label v-if="opt.caption" caption>{{ opt.caption }}</q-item-label>
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
          <q-btn flat label="İptal" @click="branchDialog = false" />
          <q-btn
            color="primary"
            label="Aktifleştir"
            :loading="saving"
            :disable="!branchForm.fieldCode"
            @click="activateBranch"
          />
        </q-card-actions>
      </q-card>
    </q-dialog>

    <!-- Uzmanlık Düzenleme Dialog -->
    <q-dialog v-model="specDialog" persistent>
      <q-card style="min-width: 480px">
        <q-card-section class="row items-center">
          <div class="text-h6">Uzmanlıkları Düzenle</div>
          <q-space />
          <q-btn flat round dense icon="close" @click="specDialog = false" />
        </q-card-section>
        <q-card-section>
          <div class="text-subtitle2 q-mb-sm">{{ specForm.fieldName }}</div>
          <div v-if="specForm.allSpecializations.length === 0" class="text-grey q-pa-md text-center">
            Bu alan için uzmanlık tanımlanmamış.
          </div>
          <q-list v-else bordered separator>
            <q-item v-for="spec in specForm.allSpecializations" :key="spec.code" tag="label">
              <q-item-section>
                <q-item-label>{{ spec.name }}</q-item-label>
                <q-item-label caption>{{ spec.code }}</q-item-label>
              </q-item-section>
              <q-item-section side>
                <q-checkbox v-model="specForm.selectedCodes" :val="spec.code" />
              </q-item-section>
            </q-item>
          </q-list>
        </q-card-section>
        <q-card-actions align="right">
          <q-btn flat label="İptal" @click="specDialog = false" />
          <q-btn color="primary" label="Kaydet" :loading="saving" @click="saveSpecializations" />
        </q-card-actions>
      </q-card>
    </q-dialog>

    <!-- Ders Programı Ayarları Dialog -->
    <q-dialog v-model="scheduleDialog" persistent>
      <q-card style="min-width: 400px">
        <q-card-section class="row items-center">
          <div class="text-h6">Ders Programı Ayarları</div>
          <q-space />
          <q-btn flat round dense icon="close" @click="scheduleDialog = false" />
        </q-card-section>
        <q-card-section>
          <div class="text-subtitle2 q-mb-md">Günlük Ders Sayısı</div>
          <q-slider
            v-model="scheduleForm.dailyPeriodCount"
            :min="1"
            :max="12"
            :step="1"
            label
            label-always
            color="primary"
            markers
          />
          <div class="text-center text-h5 text-primary q-mt-sm">
            {{ scheduleForm.dailyPeriodCount }} ders
          </div>
        </q-card-section>
        <q-card-actions align="right">
          <q-btn flat label="İptal" @click="scheduleDialog = false" />
          <q-btn color="primary" label="Kaydet" :loading="saving" @click="saveScheduleConfig" />
        </q-card-actions>
      </q-card>
    </q-dialog>
  </q-page>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, reactive } from 'vue'
import type { QTableProps } from 'quasar'
import { useQuasar } from 'quasar'
import {
  institutionApi,
  type InstitutionDto,
  type InstitutionBranchDto,
  type FieldOfStudyDto,
  type ScheduleConfigDto,
  type SpecializationDto,
} from 'src/api/institution'
import { useNotify } from 'src/composables/useNotify'
import { Permissions } from 'utils/permissions'
import AppTable from 'components/AppTable.vue'
import StatusBadge from 'components/StatusBadge.vue'
import PermissionGuard from 'components/PermissionGuard.vue'
import { useAuthStore } from 'stores/auth'
import { useKeycloakUserOptions } from 'src/composables/useEntityOptions'

const $q = useQuasar()
const authStore = useAuthStore()
const notify = useNotify()
const userOpts = useKeycloakUserOptions()

// ── Core State ──
const loading = ref(false)
const saving = ref(false)
const error = ref<string | null>(null)
const institution = ref<InstitutionDto | null>(null)
const scheduleConfig = ref<ScheduleConfigDto | null>(null)
const fieldCatalog = ref<FieldOfStudyDto[]>([])
const loadingCatalog = ref(false)

const tab = ref('info')
const institutionId = ref<string>('')

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

// ── Edit Form ──
const editForm = reactive({
  fullName: '',
  address: '',
  phoneNumber: '',
  email: '',
  webUrl: '',
})

// ── Staff Form ──
const staffForm = reactive({
  keycloakUserId: '',
  fullName: '',
  role: '',
  branchCode: null as string | null,
})

const staffRoleOptions = [
  { label: 'Müdür', value: 'Principal' },
  { label: 'Müdür Yardımcısı', value: 'VicePrincipal' },
  { label: 'Koordinatör', value: 'Coordinator' },
  { label: 'Alan Şefi', value: 'DepartmentHead' },
  { label: 'Personel', value: 'Staff' },
]

// ── Branch Form ──
const educationTypeOptions = [
  { label: 'MESEM', value: 'Mesem' },
  { label: 'Örgün', value: 'Formal' },
]
const branchForm = reactive({ educationType: '', fieldCode: '' })
const allFieldOptions = ref<Array<{ label: string; value: string; caption: string }>>([])
const availableFields = ref<Array<{ label: string; value: string; caption: string }>>([])

// ── Specialization Form ──
const specForm = reactive({
  fieldCode: '',
  fieldName: '',
  allSpecializations: [] as SpecializationDto[],
  selectedCodes: [] as string[],
})

// ── Schedule Form ──
const scheduleForm = reactive({ dailyPeriodCount: 8 })

// ── Table Columns ──
const staffColumns: QTableProps['columns'] = [
  { name: 'fullName', label: 'Ad Soyad', field: 'fullName', align: 'left', sortable: true },
  { name: 'roleSlug', label: 'Rol', field: 'roleSlug', align: 'left' },
  { name: 'branchName', label: 'Alan', field: 'branchCode', align: 'left' },
  { name: 'authorizedAt', label: 'Yetkilendirme Tarihi', field: 'authorizedAt', align: 'left' },
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
    // Önce kurum listesini al — token'daki ID'ye bağımlı olmadan
    if (!institutionId.value) {
      const listRes = await institutionApi.list()
      const institutions = listRes.data
      if (!institutions || institutions.length === 0) {
        error.value = 'Kayıtlı kurum bulunamadı.'
        return
      }
      institutionId.value = institutions[0].id
    }

    const [instRes, schedRes] = await Promise.all([
      institutionApi.get(institutionId.value),
      institutionApi.getScheduleConfig(institutionId.value),
    ])
    institution.value = instRes.data
    scheduleConfig.value = schedRes.data
    editForm.fullName = instRes.data.fullName
    editForm.address = instRes.data.address ?? ''
    editForm.phoneNumber = instRes.data.phoneNumber ?? ''
    editForm.email = instRes.data.email ?? ''
    editForm.webUrl = instRes.data.webUrl ?? ''
  } catch {
    error.value = 'Kurum bilgileri yüklenirken bir hata oluştu.'
  } finally {
    loading.value = false
  }
}

async function loadFieldCatalog(educationType?: string) {
  loadingCatalog.value = true
  try {
    const res = await institutionApi.getFieldCatalog(educationType)
    fieldCatalog.value = res.data
    const activeCodes = new Set(activeBranches.value.map((b) => b.fieldCode))
    allFieldOptions.value = res.data
      .filter((f) => !activeCodes.has(f.code))
      .map((f) => ({ label: f.name, value: f.code, caption: `${f.code} — ${f.typeSlug}` }))
    availableFields.value = allFieldOptions.value
  } catch {
    notify.error('Alan kataloğu yüklenirken hata oluştu.')
  } finally {
    loadingCatalog.value = false
  }
}

// ── Edit Institution ──
async function saveInstitution() {
  saving.value = true
  try {
    await institutionApi.update(institutionId.value, {
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

// ── Staff ──
function openStaffDialog() {
  staffForm.keycloakUserId = ''
  staffForm.fullName = ''
  staffForm.role = ''
  staffForm.branchCode = null
  userOpts.reset()
  userOpts.load({ institutionId: institutionId.value })
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
    await institutionApi.authorizeStaff(institutionId.value, {
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

// ── Branch Management ──
function openBranchDialog() {
  branchForm.educationType = ''
  branchForm.fieldCode = ''
  availableFields.value = []
  allFieldOptions.value = []
  branchDialog.value = true
}

async function onEducationTypeChange(val: string) {
  branchForm.fieldCode = ''
  await loadFieldCatalog(val || undefined)
}

function filterFields(val: string, update: (fn: () => void) => void) {
  update(() => {
    const needle = val.toLowerCase()
    availableFields.value = needle
      ? allFieldOptions.value.filter(
          (o) => o.label.toLowerCase().includes(needle) || o.caption.toLowerCase().includes(needle),
        )
      : allFieldOptions.value
  })
}

async function activateBranch() {
  saving.value = true
  try {
    await institutionApi.activateBranch(institutionId.value, branchForm.fieldCode)
    notify.success('Alan aktifleştirildi.')
    branchDialog.value = false
    await load()
  } catch {
    notify.error('Alan aktifleştirilirken hata oluştu.')
  } finally {
    saving.value = false
  }
}

function confirmDeactivateBranch(branch: InstitutionBranchDto) {
  $q.dialog({
    title: 'Pasife Al',
    message: `"${branch.fieldName}" alanını pasife almak istediğinizden emin misiniz?`,
    cancel: { flat: true, label: 'İptal' },
    ok: { color: 'negative', label: 'Pasife Al' },
    persistent: true,
  }).onOk(async () => {
    saving.value = true
    try {
      await institutionApi.deactivateBranch(institutionId.value, branch.fieldCode)
      notify.success('Alan pasife alındı.')
      await load()
    } catch {
      notify.error('İşlem sırasında hata oluştu.')
    } finally {
      saving.value = false
    }
  })
}

// ── Specialization Management ──
async function openSpecializationDialog(branch: InstitutionBranchDto) {
  if (fieldCatalog.value.length === 0) {
    await loadFieldCatalog()
  }
  const field = fieldCatalog.value.find((f) => f.code === branch.fieldCode)
  specForm.fieldCode = branch.fieldCode
  specForm.fieldName = branch.fieldName
  specForm.allSpecializations = field?.specializations.filter((s) => s.isActive) ?? []
  specForm.selectedCodes = [...branch.activeSpecializations]
  specDialog.value = true
}

async function saveSpecializations() {
  saving.value = true
  try {
    await institutionApi.updateSpecializations(institutionId.value, specForm.fieldCode, {
      activeSpecializations: specForm.selectedCodes,
    })
    notify.success('Uzmanlık alanları güncellendi.')
    specDialog.value = false
    await load()
  } catch {
    notify.error('Güncelleme sırasında hata oluştu.')
  } finally {
    saving.value = false
  }
}

// ── Schedule Config ──
function openScheduleDialog() {
  scheduleForm.dailyPeriodCount = scheduleConfig.value?.dailyPeriodCount ?? 8
  scheduleDialog.value = true
}

async function saveScheduleConfig() {
  saving.value = true
  try {
    await institutionApi.updateScheduleConfig(institutionId.value, {
      dailyPeriodCount: scheduleForm.dailyPeriodCount,
      updatedBy: authStore.user?.fullName ?? '',
    })
    notify.success('Ders programı ayarları güncellendi.')
    scheduleDialog.value = false
    const res = await institutionApi.getScheduleConfig(institutionId.value)
    scheduleConfig.value = res.data
  } catch {
    notify.error('Ayarlar kaydedilirken hata oluştu.')
  } finally {
    saving.value = false
  }
}

onMounted(load)
</script>
