<template>
  <q-page padding>
    <PageHeader title="Öğrenciler">
      <PermissionGuard :permission="Permissions.Student.Manage">
        <q-btn
          color="primary"
          icon="person_add"
          label="Yeni Öğrenci"
          @click="openAddDialog"
        />
      </PermissionGuard>
    </PageHeader>

    <AppTable
      :rows="students"
      :columns="columns"
      :loading="loading"
      :pagination="pagination"
      show-search
      :search="search"
      @request="onRequest"
      @search="onSearch"
    >
      <template #filters>
        <BranchSelector
          v-model="branchFilter"
          dense
          force-select
          style="min-width: 200px"
          @update:model-value="load"
        />
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
      </template>
      <template #body-cell-statusSlug="{ row }">
        <q-td><StatusBadge :slug="row.statusSlug" /></q-td>
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
          />
          <PermissionGuard :permission="Permissions.Student.Manage">
            <q-btn
              flat
              round
              dense
              icon="edit"
              aria-label="Düzenle"
              color="grey-7"
              @click="openEditDialog(row)"
            >
              <q-tooltip>Düzenle</q-tooltip>
            </q-btn>
          </PermissionGuard>
          <PermissionGuard :permission="Permissions.Internship.Approve">
            <q-btn
              v-if="row.status === 'Applied'"
              flat
              round
              dense
              icon="place"
              color="primary"
              aria-label="Yerleştir"
              @click="openPlacement(row)"
            >
              <q-tooltip>Yerleştir</q-tooltip>
            </q-btn>
          </PermissionGuard>
          <PermissionGuard :permission="Permissions.Student.Manage">
            <q-btn
              v-if="row.status !== 'ActiveInternship' && row.status !== 'Completed' && row.status !== 'Deregistered'"
              flat
              round
              dense
              icon="person_remove"
              color="negative"
              aria-label="Kayıt sil"
              @click="openDeregister(row)"
            >
              <q-tooltip>Kayıt sil</q-tooltip>
            </q-btn>
          </PermissionGuard>
        </q-td>
      </template>

      <template #empty-action>
        <PermissionGuard :permission="Permissions.Student.Manage">
          <q-btn
            color="primary"
            icon="person_add"
            label="İlk öğrenciyi ekle"
            unelevated
            @click="openAddDialog"
          />
        </PermissionGuard>
      </template>
    </AppTable>

    <!-- Detay Panel — sağdan overlay -->
    <DetailPanel
      v-model="detailOpen"
      :has-content="!!selected"
      :width="400"
    >
      <template #title>
        {{ selected?.fullName }}
      </template>
      <template #toolbar-actions>
        <PermissionGuard :permission="Permissions.Student.Manage">
          <q-btn
            flat
            round
            dense
            icon="edit"
            aria-label="Düzenle"
            @click="selected && openEditDialog(selected)"
          >
            <q-tooltip>Düzenle</q-tooltip>
          </q-btn>
        </PermissionGuard>
      </template>
      <template v-if="selected">
        <div class="q-gutter-sm">
          <InfoItem
            icon="school"
            label="Alan"
            :value="`${selected.branchCode} — ${selected.branchName}`"
          />
          <InfoItem
            v-if="selected.specializationName"
            icon="account_tree"
            label="Dal"
            :value="selected.specializationName"
          />
          <InfoItem
            icon="class"
            label="Sınıf / Şube"
            :value="`${selected.classYear}. Sınıf${selected.section ? ` / ${selected.section}` : ''}`"
          />
          <InfoItem
            v-if="selected.studentNumber"
            icon="pin"
            label="Öğrenci No"
            :value="selected.studentNumber"
          />
          <InfoItem
            v-if="selected.tcKimlikNo"
            icon="fingerprint"
            label="T.C. Kimlik No"
            :value="selected.tcKimlikNo"
          />
          <InfoItem
            icon="badge"
            label="Durum"
          >
            <StatusBadge :slug="selected.statusSlug" />
          </InfoItem>
          <InfoItem
            icon="event"
            label="Kayıt Tarihi"
            :value="formatDate(selected.registeredAt)"
          />

          <q-separator
            v-if="selected.phoneNumber || selected.guardianName || selected.guardianPhone"
            spaced
          />
          <div
            v-if="selected.phoneNumber || selected.guardianName || selected.guardianPhone"
            class="text-subtitle2 text-grey-7 q-px-md"
          >
            İletişim
          </div>
          <InfoItem
            v-if="selected.phoneNumber"
            icon="phone"
            label="Telefon"
            :value="selected.phoneNumber"
          />
          <InfoItem
            v-if="selected.guardianName"
            icon="person"
            label="Veli"
            :value="selected.guardianName"
          />
          <InfoItem
            v-if="selected.guardianPhone"
            icon="phone"
            label="Veli Telefon"
            :value="selected.guardianPhone"
          />
        </div>
      </template>
    </DetailPanel>

    <PlaceStudentForm
      v-model="placementDialog"
      :student-id="selected?.id ?? ''"
      :student-name="selected?.fullName ?? ''"
      @saved="afterFormSaved"
    />
    <DeregisterStudentForm
      v-model="deregisterDialog"
      :student-id="selected?.id ?? ''"
      :student-name="selected?.fullName ?? ''"
      @saved="afterFormSaved"
    />
  </q-page>
</template>

<script setup lang="ts">
import { ref, onMounted, watch, computed } from 'vue'
import type { QTableProps } from 'quasar'
import { enrollmentApi, type StudentProfileDto } from 'src/api/enrollment'
import { useServerPagination } from 'src/composables/useServerPagination'
import { useAcademicPeriodStore } from 'stores/academicPeriod'
import { Permissions } from 'utils/permissions'
import AppTable from 'components/AppTable.vue'
import StatusBadge from 'components/StatusBadge.vue'
import PermissionGuard from 'components/PermissionGuard.vue'
import BranchSelector from 'components/BranchSelector.vue'
import InfoItem from 'components/InfoItem.vue'
import PageHeader from 'components/PageHeader.vue'
import DetailPanel from 'components/DetailPanel.vue'
import { useRouter } from 'vue-router'
import PlaceStudentForm from 'components/forms/student/PlaceStudentForm.vue'
import DeregisterStudentForm from 'components/forms/student/DeregisterStudentForm.vue'

const periodStore = useAcademicPeriodStore()
const router = useRouter()

const selected = ref<StudentProfileDto | null>(null)
const detailOpen = ref(false)
const placementDialog = ref(false)
const deregisterDialog = ref(false)
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
const statusOptions = [
  { label: 'Kayıtlı', value: 'Registered' },
  { label: 'Başvurdu', value: 'Applied' },
  { label: 'Yerleştirildi', value: 'Placed' },
  { label: 'Aktif Staj', value: 'ActiveInternship' },
  { label: 'Tamamladı', value: 'Completed' },
  { label: 'Kayıt Silindi', value: 'Deregistered' },
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

function openEditDialog(row: StudentProfileDto) {
  router.push(`/enrollment/students/${row.id}/edit`).catch(() => {})
}

function openAddDialog() {
  router.push('/enrollment/students/new').catch(() => {})
}

function openPlacement(row: StudentProfileDto) {
  selected.value = row
  placementDialog.value = true
}

function openDeregister(row: StudentProfileDto) {
  selected.value = row
  deregisterDialog.value = true
}

async function afterFormSaved() {
  await load()
  if (selected.value) {
    const updated = students.value.find((s) => s.id === selected.value?.id)
    if (updated) selected.value = updated
  }
}

watch(() => periodStore.selectedPeriodId, () => load())
onMounted(() => {
  load()
})
</script>
