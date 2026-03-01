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
        :options="branchOpts.allOptions.value"
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

    <AppTable :rows="students" :columns="columns" :loading="loading" :pagination="pagination" show-search :search="search" @request="onRequest" @search="onSearch">
      <template #body-cell-statusSlug="{ row }">
        <q-td><StatusBadge :slug="row.statusSlug" /></q-td>
      </template>
      <template #body-cell-actions="{ row }">
        <q-td class="text-right">
          <q-btn flat round dense icon="visibility" @click="openDetail(row)" />
          <PermissionGuard :permission="Permissions.Student.Manage">
            <q-btn flat round dense icon="edit" color="grey-7" @click="openEditDialog(row)">
              <q-tooltip>Düzenle</q-tooltip>
            </q-btn>
          </PermissionGuard>
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

    <!-- Detay Panel — sağdan overlay -->
    <transition name="slide-right">
      <div v-if="detailOpen && selected" class="detail-backdrop" @click.self="closeDetail" />
    </transition>
    <transition name="slide-right">
      <div v-if="detailOpen && selected" class="detail-panel">
        <q-toolbar>
          <q-toolbar-title class="text-subtitle1 text-weight-bold">{{ selected.fullName }}</q-toolbar-title>
          <PermissionGuard :permission="Permissions.Student.Manage">
            <q-btn flat round dense icon="edit" @click="openEditDialog(selected)">
              <q-tooltip>Düzenle</q-tooltip>
            </q-btn>
          </PermissionGuard>
          <q-btn flat round dense icon="close" @click="closeDetail" />
        </q-toolbar>
        <q-separator />
        <div class="detail-panel-scroll">
          <div class="q-pa-md q-gutter-sm">
            <q-item dense>
              <q-item-section avatar><q-icon name="school" /></q-item-section>
              <q-item-section>
                <q-item-label caption>Alan</q-item-label>
                <q-item-label>{{ selected.branchCode }} — {{ selected.branchName }}</q-item-label>
              </q-item-section>
            </q-item>
            <q-item v-if="selected.specializationName" dense>
              <q-item-section avatar><q-icon name="account_tree" /></q-item-section>
              <q-item-section>
                <q-item-label caption>Dal</q-item-label>
                <q-item-label>{{ selected.specializationName }}</q-item-label>
              </q-item-section>
            </q-item>
            <q-item dense>
              <q-item-section avatar><q-icon name="class" /></q-item-section>
              <q-item-section>
                <q-item-label caption>Sınıf / Şube</q-item-label>
                <q-item-label>{{ selected.classYear }}. Sınıf{{ selected.section ? ` / ${selected.section}` : '' }}</q-item-label>
              </q-item-section>
            </q-item>
            <q-item v-if="selected.studentNumber" dense>
              <q-item-section avatar><q-icon name="pin" /></q-item-section>
              <q-item-section>
                <q-item-label caption>Öğrenci No</q-item-label>
                <q-item-label>{{ selected.studentNumber }}</q-item-label>
              </q-item-section>
            </q-item>
            <q-item v-if="selected.tcKimlikNo" dense>
              <q-item-section avatar><q-icon name="fingerprint" /></q-item-section>
              <q-item-section>
                <q-item-label caption>T.C. Kimlik No</q-item-label>
                <q-item-label>{{ selected.tcKimlikNo }}</q-item-label>
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

            <q-separator v-if="selected.phoneNumber || selected.guardianName || selected.guardianPhone" spaced />
            <div v-if="selected.phoneNumber || selected.guardianName || selected.guardianPhone" class="text-subtitle2 text-grey-7 q-px-md">İletişim</div>
            <q-item v-if="selected.phoneNumber" dense>
              <q-item-section avatar><q-icon name="phone" /></q-item-section>
              <q-item-section>
                <q-item-label caption>Telefon</q-item-label>
                <q-item-label>{{ selected.phoneNumber }}</q-item-label>
              </q-item-section>
            </q-item>
            <q-item v-if="selected.guardianName" dense>
              <q-item-section avatar><q-icon name="person" /></q-item-section>
              <q-item-section>
                <q-item-label caption>Veli</q-item-label>
                <q-item-label>{{ selected.guardianName }}</q-item-label>
              </q-item-section>
            </q-item>
            <q-item v-if="selected.guardianPhone" dense>
              <q-item-section avatar><q-icon name="phone" /></q-item-section>
              <q-item-section>
                <q-item-label caption>Veli Telefon</q-item-label>
                <q-item-label>{{ selected.guardianPhone }}</q-item-label>
              </q-item-section>
            </q-item>
          </div>
        </div>
      </div>
    </transition>

    <!-- Yeni Öğrenci Dialog -->
    <q-dialog v-model="addDialog" persistent :maximized="$q.screen.lt.sm" transition-show="slide-up" transition-hide="slide-down">
      <q-card :style="$q.screen.gt.xs ? 'width: 480px; max-width: 95vw' : ''">
        <q-toolbar class="bg-primary text-white">
          <q-icon name="person_add" class="q-mr-sm" />
          <q-toolbar-title>Yeni Öğrenci Ekle</q-toolbar-title>
          <q-btn flat round dense icon="close" color="white" v-close-popup />
        </q-toolbar>
        <q-card-section class="q-pt-lg q-gutter-md">
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
            :error="!!addErrors.keycloakUserId"
            :error-message="addErrors.keycloakUserId"
            @filter="userOpts.filter"
            @update:model-value="onUserSelect"
          >
            <template #prepend>
              <q-icon name="person_search" />
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
          <q-input
            v-model="addForm.fullName" label="Ad Soyad *" filled
            :error="!!addErrors.fullName" :error-message="addErrors.fullName"
          >
            <template #prepend>
              <q-icon name="badge" />
            </template>
          </q-input>
          <q-input
            v-model="addForm.email" label="Kullanıcı Adı (E-posta)" filled readonly
          >
            <template #prepend>
              <q-icon name="email" />
            </template>
          </q-input>
          <q-select
            v-model="addForm.branchCode"
            :options="branchOpts.options.value"
            label="Alan *"
            filled
            use-input
            input-debounce="0"
            emit-value
            map-options
            option-label="label"
            option-value="value"
            :error="!!addErrors.branchCode"
            :error-message="addErrors.branchCode"
            @filter="branchOpts.filter"
            @update:model-value="onAddBranchChange"
          >
            <template #prepend>
              <q-icon name="category" />
            </template>
            <template #no-option>
              <q-item>
                <q-item-section class="text-grey">Sonuç bulunamadı</q-item-section>
              </q-item>
            </template>
          </q-select>
          <q-select
            v-if="addSpecOptions.length > 0"
            v-model="addForm.specializationCode"
            :options="addSpecOptions"
            label="Dal"
            filled
            emit-value
            map-options
            option-label="label"
            option-value="value"
          >
            <template #prepend>
              <q-icon name="account_tree" />
            </template>
          </q-select>
          <div class="row q-col-gutter-sm">
            <div class="col-6">
              <q-input
                v-model.number="addForm.classYear" label="Sınıf (9-12)" filled type="number" min="9" max="12"
                :error="!!addErrors.classYear" :error-message="addErrors.classYear"
              >
                <template #prepend>
                  <q-icon name="class" />
                </template>
              </q-input>
            </div>
            <div class="col-6">
              <q-input
                v-model="addForm.section" label="Şube" filled
                :error="!!addErrors.section" :error-message="addErrors.section"
              >
                <template #prepend>
                  <q-icon name="sort_by_alpha" />
                </template>
              </q-input>
            </div>
          </div>
          <q-input
            v-model="addForm.studentNumber" label="Öğrenci No" filled
            :error="!!addErrors.studentNumber" :error-message="addErrors.studentNumber"
          >
            <template #prepend>
              <q-icon name="pin" />
            </template>
          </q-input>
          <q-input
            v-model="addForm.tcKimlikNo" label="T.C. Kimlik No" filled maxlength="11"
            :error="!!addErrors.tcKimlikNo" :error-message="addErrors.tcKimlikNo"
          >
            <template #prepend>
              <q-icon name="fingerprint" />
            </template>
          </q-input>
          <q-input
            v-model="addForm.phoneNumber" label="Telefon" filled
            :error="!!addErrors.phoneNumber" :error-message="addErrors.phoneNumber"
          >
            <template #prepend>
              <q-icon name="phone" />
            </template>
          </q-input>
          <q-separator />
          <div class="text-subtitle2 text-grey-7">Veli Bilgileri</div>
          <q-input v-model="addForm.guardianName" label="Veli Adı" filled>
            <template #prepend>
              <q-icon name="person" />
            </template>
          </q-input>
          <q-input
            v-model="addForm.guardianPhone" label="Veli Telefon" filled
            :error="!!addErrors.guardianPhone" :error-message="addErrors.guardianPhone"
          >
            <template #prepend>
              <q-icon name="phone" />
            </template>
          </q-input>
        </q-card-section>
        <q-separator />
        <q-card-actions align="right" class="q-pa-md">
          <q-btn flat label="İptal" color="grey-7" v-close-popup />
          <q-btn unelevated color="primary" label="Kaydet" :loading="saving" @click="registerStudent" />
        </q-card-actions>
      </q-card>
    </q-dialog>

    <!-- Yerleştirme Dialog -->
    <q-dialog v-model="placementDialog" persistent :maximized="$q.screen.lt.sm" transition-show="slide-up" transition-hide="slide-down">
      <q-card :style="$q.screen.gt.xs ? 'width: 480px; max-width: 95vw' : ''">
        <q-toolbar class="bg-teal text-white">
          <q-icon name="place" class="q-mr-sm" />
          <q-toolbar-title>Öğrenci Yerleştir</q-toolbar-title>
          <q-btn flat round dense icon="close" color="white" v-close-popup />
        </q-toolbar>
        <q-card-section class="q-pt-lg q-gutter-md">
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
            :error="!!placementErrors.businessId"
            :error-message="placementErrors.businessId"
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
            <template #prepend>
              <q-icon name="person" />
            </template>
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
        <q-separator />
        <q-card-actions align="right" class="q-pa-md">
          <q-btn flat label="İptal" color="grey-7" v-close-popup />
          <q-btn unelevated color="teal" label="Yerleştir" :loading="saving" @click="placementStudent" />
        </q-card-actions>
      </q-card>
    </q-dialog>

    <!-- Transfer Dialog -->
    <q-dialog v-model="transferDialog" persistent :maximized="$q.screen.lt.sm" transition-show="slide-up" transition-hide="slide-down">
      <q-card :style="$q.screen.gt.xs ? 'width: 480px; max-width: 95vw' : ''">
        <q-toolbar class="bg-orange text-white">
          <q-icon name="transfer_within_a_station" class="q-mr-sm" />
          <q-toolbar-title>Öğrenci Transfer Et</q-toolbar-title>
          <q-btn flat round dense icon="close" color="white" v-close-popup />
        </q-toolbar>
        <q-card-section class="q-pt-lg q-gutter-md">
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
            :error="!!transferErrors.newBusinessId"
            :error-message="transferErrors.newBusinessId"
            @filter="transferBusinessOpts.filter"
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
          <q-input v-model="transferForm.reason" label="Transfer Gerekçesi" filled type="textarea" rows="2">
            <template #prepend>
              <q-icon name="notes" />
            </template>
          </q-input>
        </q-card-section>
        <q-separator />
        <q-card-actions align="right" class="q-pa-md">
          <q-btn flat label="İptal" color="grey-7" v-close-popup />
          <q-btn unelevated color="warning" label="Transfer Et" :loading="saving" @click="doTransfer" />
        </q-card-actions>
      </q-card>
    </q-dialog>

    <!-- Öğrenci Düzenle Dialog -->
    <q-dialog v-model="editDialog" persistent :maximized="$q.screen.lt.sm" transition-show="slide-up" transition-hide="slide-down">
      <q-card :style="$q.screen.gt.xs ? 'width: 480px; max-width: 95vw' : ''">
        <q-toolbar class="bg-primary text-white">
          <q-icon name="edit" class="q-mr-sm" />
          <q-toolbar-title>Öğrenci Düzenle</q-toolbar-title>
          <q-btn flat round dense icon="close" color="white" v-close-popup />
        </q-toolbar>
        <q-card-section class="q-pt-lg q-gutter-md">
          <q-input
            v-model="editForm.fullName" label="Ad Soyad *" filled
            :error="!!editErrors.fullName" :error-message="editErrors.fullName"
          >
            <template #prepend>
              <q-icon name="badge" />
            </template>
          </q-input>
          <q-input
            v-model="editForm.email" label="Kullanıcı Adı (E-posta)" filled readonly
          >
            <template #prepend>
              <q-icon name="email" />
            </template>
          </q-input>
          <q-select
            v-model="editForm.branchCode"
            :options="branchOpts.options.value"
            label="Alan *"
            filled
            use-input
            input-debounce="0"
            emit-value
            map-options
            option-label="label"
            option-value="value"
            :error="!!editErrors.branchCode"
            :error-message="editErrors.branchCode"
            @filter="branchOpts.filter"
            @update:model-value="onEditBranchChange"
          >
            <template #prepend>
              <q-icon name="category" />
            </template>
            <template #no-option>
              <q-item>
                <q-item-section class="text-grey">Sonuç bulunamadı</q-item-section>
              </q-item>
            </template>
          </q-select>
          <q-select
            v-if="editSpecOptions.length > 0"
            v-model="editForm.specializationCode"
            :options="editSpecOptions"
            label="Dal"
            filled
            emit-value
            map-options
            option-label="label"
            option-value="value"
          >
            <template #prepend>
              <q-icon name="account_tree" />
            </template>
          </q-select>
          <div class="row q-col-gutter-sm">
            <div class="col-6">
              <q-input
                v-model.number="editForm.classYear" label="Sınıf (9-12)" filled type="number" min="9" max="12"
                :error="!!editErrors.classYear" :error-message="editErrors.classYear"
              >
                <template #prepend>
                  <q-icon name="class" />
                </template>
              </q-input>
            </div>
            <div class="col-6">
              <q-input
                v-model="editForm.section" label="Şube" filled
                :error="!!editErrors.section" :error-message="editErrors.section"
              >
                <template #prepend>
                  <q-icon name="sort_by_alpha" />
                </template>
              </q-input>
            </div>
          </div>
          <q-input
            v-model="editForm.studentNumber" label="Öğrenci No" filled
            :error="!!editErrors.studentNumber" :error-message="editErrors.studentNumber"
          >
            <template #prepend>
              <q-icon name="pin" />
            </template>
          </q-input>
          <q-input
            v-model="editForm.tcKimlikNo" label="T.C. Kimlik No" filled maxlength="11"
            :error="!!editErrors.tcKimlikNo" :error-message="editErrors.tcKimlikNo"
          >
            <template #prepend>
              <q-icon name="fingerprint" />
            </template>
          </q-input>
          <q-input
            v-model="editForm.phoneNumber" label="Telefon" filled
            :error="!!editErrors.phoneNumber" :error-message="editErrors.phoneNumber"
          >
            <template #prepend>
              <q-icon name="phone" />
            </template>
          </q-input>
          <q-separator />
          <div class="text-subtitle2 text-grey-7">Veli Bilgileri</div>
          <q-input v-model="editForm.guardianName" label="Veli Adı" filled>
            <template #prepend>
              <q-icon name="person" />
            </template>
          </q-input>
          <q-input
            v-model="editForm.guardianPhone" label="Veli Telefon" filled
            :error="!!editErrors.guardianPhone" :error-message="editErrors.guardianPhone"
          >
            <template #prepend>
              <q-icon name="phone" />
            </template>
          </q-input>
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
import { ref, reactive, onMounted, watch, computed } from 'vue'
import type { QTableProps } from 'quasar'
import { useQuasar } from 'quasar'
import { enrollmentApi, type StudentProfileDto } from 'src/api/enrollment'
import { useNotify } from 'src/composables/useNotify'
import { useServerPagination } from 'src/composables/useServerPagination'
import { useKeycloakUserOptions, useBusinessOptions, useTeacherOptions, useBranchOptions, type SelectOption } from 'src/composables/useEntityOptions'
import { useAcademicPeriodStore } from 'stores/academicPeriod'
import { registerStudentSchema, editStudentSchema, placementSchema, transferSchema } from 'src/schemas/student'
import { Permissions } from 'utils/permissions'
import AppTable from 'components/AppTable.vue'
import StatusBadge from 'components/StatusBadge.vue'
import PermissionGuard from 'components/PermissionGuard.vue'

const periodStore = useAcademicPeriodStore()

const $q = useQuasar()
const notify = useNotify()
const userOpts = useKeycloakUserOptions()
const businessOpts = useBusinessOptions()
const teacherOpts = useTeacherOptions()
const transferBusinessOpts = useBusinessOptions()
const branchOpts = useBranchOptions()

const saving = ref(false)
const selected = ref<StudentProfileDto | null>(null)
const detailOpen = ref(false)
const addDialog = ref(false)
const editDialog = ref(false)
const placementDialog = ref(false)
const transferDialog = ref(false)
const branchFilter = ref<string | null>(null)
const statusFilter = ref<string | null>(null)

// ── Server-side pagination ──
const filters = computed(() => ({
  academicPeriodId: periodStore.selectedPeriodId ?? undefined,
  branchCode: branchFilter.value ?? undefined,
  status: statusFilter.value ?? undefined,
}))

const { rows: students, loading, pagination, search, onRequest, onSearch, load } =
  useServerPagination<StudentProfileDto>({
    fetchFn: (params) => enrollmentApi.listStudents(params),
    filters,
    defaultSortBy: 'fullName',
  })
// activePlacementId: transfer için mevcut yerleştirme ID'si
const activePlacementId = ref<string | null>(null)

// ── Zod validasyon helper ──
function zodValidate<T>(schema: { safeParse: (data: unknown) => { success: boolean; error?: { issues: { path: (string | number)[]; message: string }[] }; data?: T } }, data: unknown, errors: Record<string, string>): data is T {
  // clear previous errors
  for (const key of Object.keys(errors)) errors[key] = ''
  const result = schema.safeParse(data)
  if (result.success) return true
  for (const issue of result.error!.issues) {
    const field = issue.path[0]
    if (field && typeof field === 'string' && !errors[field]) {
      errors[field] = issue.message
    }
  }
  return false
}

// ── reactive form objects ──
const addForm = reactive({
  keycloakUserId: '', fullName: '', email: '', branchCode: '', specializationCode: '',
  classYear: 11, section: '', studentNumber: '', phoneNumber: '',
  tcKimlikNo: '', guardianName: '', guardianPhone: '',
})
const addErrors = reactive<Record<string, string>>({})

const editForm = reactive({
  fullName: '', email: '', branchCode: '', specializationCode: '',
  classYear: 11, section: '', studentNumber: '', phoneNumber: '',
  tcKimlikNo: '', guardianName: '', guardianPhone: '',
})
const editErrors = reactive<Record<string, string>>({})

const placementForm = reactive({ businessId: '', teacherId: '' })
const placementErrors = reactive<Record<string, string>>({})

const transferForm = reactive({ newBusinessId: '', reason: '' })
const transferErrors = reactive<Record<string, string>>({})

// branchName editForm'da validasyon dışı (UI helper)
const editBranchName = ref('')

const addSpecOptions = computed(() => branchOpts.getSpecializations(addForm.branchCode ?? ''))
const editSpecOptions = computed(() => branchOpts.getSpecializations(editForm.branchCode ?? ''))

const statusOptions = [
  { label: 'Kayıtlı', value: 'Registered' },
  { label: 'Başvurdu', value: 'Applied' },
  { label: 'Yerleştirildi', value: 'Placed' },
  { label: 'Aktif Staj', value: 'ActiveInternship' },
  { label: 'Tamamladı', value: 'Completed' },
]

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


function openDetail(row: StudentProfileDto) {
  selected.value = row
  detailOpen.value = true
}

function closeDetail() {
  detailOpen.value = false
}

function onEditBranchChange(code: string | null) {
  editBranchName.value = code ? branchOpts.getFieldName(code) : ''
  editForm.specializationCode = ''
}

function onAddBranchChange() {
  addForm.specializationCode = ''
}

async function openEditDialog(row: StudentProfileDto) {
  selected.value = row
  editBranchName.value = row.branchName
  // Keycloak'tan email bilgisini çek
  let email = ''
  if (row.keycloakUserId) {
    await userOpts.load()
    const found = userOpts.allOptions.value.find((o: SelectOption) => o.value === row.keycloakUserId)
    if (found?.caption) {
      const emailMatch = found.caption.match(/^(.+?)\s*\(/)
      email = emailMatch ? emailMatch[1] : found.caption
    }
  }
  Object.assign(editForm, {
    fullName: row.fullName,
    email,
    branchCode: row.branchCode,
    specializationCode: row.specializationCode ?? '',
    classYear: row.classYear,
    section: row.section ?? '',
    studentNumber: row.studentNumber ?? '',
    phoneNumber: row.phoneNumber ?? '',
    tcKimlikNo: row.tcKimlikNo ?? '',
    guardianName: row.guardianName ?? '',
    guardianPhone: row.guardianPhone ?? '',
  })
  // clear previous errors
  for (const key of Object.keys(editErrors)) editErrors[key] = ''
  editDialog.value = true
}

async function saveEdit() {
  if (!selected.value) return
  if (!zodValidate(editStudentSchema, editForm, editErrors)) return
  saving.value = true
  try {
    await enrollmentApi.updateStudent(selected.value.id, {
      fullName: editForm.fullName || undefined,
      branchCode: editForm.branchCode || undefined,
      branchName: editBranchName.value || undefined,
      specializationCode: editForm.specializationCode || undefined,
      specializationName: editSpecOptions.value.find((o) => o.value === editForm.specializationCode)?.label || undefined,
      classYear: editForm.classYear || undefined,
      section: editForm.section || undefined,
      studentNumber: editForm.studentNumber || undefined,
      phoneNumber: editForm.phoneNumber || undefined,
      tcKimlikNo: editForm.tcKimlikNo || undefined,
      guardianName: editForm.guardianName || undefined,
      guardianPhone: editForm.guardianPhone || undefined,
    })
    notify.success('Öğrenci bilgileri güncellendi.')
    editDialog.value = false
    await load()
    const updated = students.value.find((s) => s.id === selected.value?.id)
    if (updated) selected.value = updated
  } catch (e) {
    notify.apiError(e, 'Öğrenci güncellenirken bir hata oluştu.')
  } finally {
    saving.value = false
  }
}

function onUserSelect(val: string | null) {
  if (val) {
    const found = userOpts.allOptions.value.find((o: SelectOption) => o.value === val)
    addForm.fullName = found?.label ?? ''
    // caption format: "email (username)" — email kısmını çıkar
    const caption = found?.caption ?? ''
    const emailMatch = caption.match(/^(.+?)\s*\(/)
    addForm.email = emailMatch ? emailMatch[1] : caption
  } else {
    addForm.fullName = ''
    addForm.email = ''
  }
}

function openAddDialog() {
  Object.assign(addForm, {
    keycloakUserId: '', fullName: '', email: '', branchCode: '', specializationCode: '',
    classYear: 11, section: '', studentNumber: '', phoneNumber: '',
    tcKimlikNo: '', guardianName: '', guardianPhone: '',
  })
  for (const key of Object.keys(addErrors)) addErrors[key] = ''
  userOpts.reset()
  userOpts.load()
  branchOpts.reset()
  branchOpts.load()
  addDialog.value = true
}

function openPlacement(row: StudentProfileDto) {
  selected.value = row
  Object.assign(placementForm, { businessId: '', teacherId: '' })
  for (const key of Object.keys(placementErrors)) placementErrors[key] = ''
  businessOpts.reset()
  businessOpts.load()
  teacherOpts.reset()
  teacherOpts.load()
  placementDialog.value = true
}

async function openTransfer(row: StudentProfileDto) {
  selected.value = row
  Object.assign(transferForm, { newBusinessId: '', reason: '' })
  for (const key of Object.keys(transferErrors)) transferErrors[key] = ''
  transferBusinessOpts.reset()
  transferBusinessOpts.load()
  // Mevcut aktif yerleştirmeyi bul
  try {
    const res = await enrollmentApi.listPlacements({ studentId: row.id, status: 'Active' })
    activePlacementId.value = res.data?.items?.[0]?.id ?? null
  } catch {
    activePlacementId.value = null
  }
  transferDialog.value = true
}

async function registerStudent() {
  if (!zodValidate(registerStudentSchema, addForm, addErrors)) return
  saving.value = true
  try {
    const selectedSpec = addSpecOptions.value.find((o) => o.value === addForm.specializationCode)
    await enrollmentApi.registerStudent({
      keycloakUserId: addForm.keycloakUserId,
      fullName: addForm.fullName,
      branchCode: addForm.branchCode,
      branchName: branchOpts.getFieldName(addForm.branchCode) || undefined,
      academicPeriodId: periodStore.selectedPeriodId ?? undefined,
      specializationCode: addForm.specializationCode || undefined,
      specializationName: selectedSpec?.label || undefined,
      classYear: addForm.classYear,
      section: addForm.section || undefined,
      studentNumber: addForm.studentNumber || undefined,
      phoneNumber: addForm.phoneNumber || undefined,
      tcKimlikNo: addForm.tcKimlikNo || undefined,
      guardianName: addForm.guardianName || undefined,
      guardianPhone: addForm.guardianPhone || undefined,
    })
    notify.success('Öğrenci başarıyla kaydedildi.')
    addDialog.value = false
    await load()
  } catch (e) {
    notify.apiError(e, 'Öğrenci eklenirken bir hata oluştu.')
  } finally {
    saving.value = false
  }
}

async function placementStudent() {
  if (!selected.value) return
  if (!zodValidate(placementSchema, placementForm, placementErrors)) return
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
  } catch (e) {
    notify.apiError(e, 'Yerleştirme sırasında bir hata oluştu.')
  } finally {
    saving.value = false
  }
}

async function doTransfer() {
  if (!activePlacementId.value) {
    notify.error('Aktif yerleştirme bulunamadı.')
    return
  }
  if (!zodValidate(transferSchema, transferForm, transferErrors)) return
  saving.value = true
  try {
    await enrollmentApi.transferStudent(activePlacementId.value, {
      newBusinessId: transferForm.newBusinessId,
      reason: transferForm.reason ?? '',
    })
    notify.success('Öğrenci transfer edildi.')
    transferDialog.value = false
    await load()
  } catch (e) {
    notify.apiError(e, 'Transfer sırasında bir hata oluştu.')
  } finally {
    saving.value = false
  }
}

watch(() => periodStore.selectedPeriodId, () => load())
onMounted(() => {
  load()
  branchOpts.load()
})
</script>

<style scoped>
.detail-backdrop {
  position: fixed;
  inset: 0;
  background: rgba(0, 0, 0, 0.3);
  z-index: 2000;
}

.detail-panel {
  position: fixed;
  top: 50px;
  right: 0;
  bottom: 0;
  width: 400px;
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

.slide-right-enter-active,
.slide-right-leave-active {
  transition: all 0.3s ease;
}

.slide-right-enter-from,
.slide-right-leave-to {
  opacity: 0;
}
</style>
