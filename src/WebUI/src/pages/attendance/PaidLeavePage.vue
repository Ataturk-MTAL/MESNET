<template>
  <q-page padding>
    <PageHeader title="Ücretli İzin Başvuruları">
      <PermissionGuard :permission="Permissions.Attendance.LeaveRequest">
        <q-btn
          :disable="periodStore.isReadOnly"
          color="primary"
          icon="add"
          label="İzin Başvurusu"
          @click="openForm"
        />
      </PermissionGuard>
    </PageHeader>

    <AppNotice
      v-if="periodStore.isReadOnly"
      type="readonly"
      class="q-mb-md"
      message="Bu dönem kapatılmıştır — yalnızca görüntüleme yapılabilir."
    />

    <div class="row q-gutter-sm q-mb-md">
      <q-select
        v-model="statusFilter"
        :options="PAID_LEAVE_STATUSES"
        label="Durum"
        outlined
        dense
        emit-value
        map-options
        clearable
        style="min-width: 220px"
        @update:model-value="load"
      />
    </div>

    <AppTable
      :rows="requests"
      :columns="columns"
      :loading="loading"
      :pagination="pagination"
      @request="onRequest"
    >
      <template #body-cell-student="{ row }">
        <q-td>{{ studentMap[row.studentId] ?? '—' }}</q-td>
      </template>
      <template #body-cell-range="{ row }">
        <q-td>
          <div>{{ formatDate(row.startDate) }} – {{ formatDate(row.endDate) }}</div>
          <div class="text-caption text-grey-6">
            {{ row.dayCount }} gün
          </div>
        </q-td>
      </template>
      <template #body-cell-statusSlug="{ row }">
        <q-td>
          <StatusBadge :slug="row.statusSlug" />
          <q-tooltip v-if="row.rejectionReason">
            {{ row.rejectionReason }}
          </q-tooltip>
        </q-td>
      </template>
      <template #body-cell-actions="{ row }">
        <q-td class="text-right">
          <!-- 1. adım: işletme. Buton izne bakar; sunucu ayrıca business_id kapsamını arar. -->
          <PermissionGuard :permission="Permissions.Attendance.LeaveBusinessApprove">
            <template v-if="row.status === 'PendingBusiness'">
              <q-btn
                :disable="periodStore.isReadOnly"
                flat
                round
                dense
                icon="thumb_up"
                color="primary"
                aria-label="İşletme adına onayla"
                @click="businessApprove(row)"
              >
                <q-tooltip>İşletme adına onayla</q-tooltip>
              </q-btn>
              <q-btn
                :disable="periodStore.isReadOnly"
                flat
                round
                dense
                icon="event_busy"
                color="negative"
                aria-label="İşletme adına reddet"
                @click="openReject(row, 'business')"
              >
                <q-tooltip>İşletme adına reddet</q-tooltip>
              </q-btn>
            </template>
          </PermissionGuard>
          <!-- 2. adım: okul. İzin bu adımla resmileşir. -->
          <PermissionGuard :permission="Permissions.Attendance.LeaveApprove">
            <template v-if="row.status === 'PendingSchool'">
              <q-btn
                :disable="periodStore.isReadOnly"
                flat
                round
                dense
                icon="fact_check"
                color="positive"
                aria-label="Okul adına onayla"
                @click="approve(row)"
              >
                <q-tooltip>Okul adına onayla — izin resmileşir</q-tooltip>
              </q-btn>
              <q-btn
                :disable="periodStore.isReadOnly"
                flat
                round
                dense
                icon="event_busy"
                color="negative"
                aria-label="Okul adına reddet"
                @click="openReject(row, 'school')"
              >
                <q-tooltip>Okul adına reddet</q-tooltip>
              </q-btn>
            </template>
          </PermissionGuard>
        </q-td>
      </template>
    </AppTable>

    <PaidLeaveRejectForm
      v-model="rejectDialog"
      :request-id="selected?.id ?? ''"
      :stage="rejectStage"
      @saved="load"
    />
  </q-page>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, watch } from 'vue'
import type { QTableProps } from 'quasar'
import { useRouter } from 'vue-router'

import {
  paidLeaveApi,
  PAID_LEAVE_STATUSES,
  type PaidLeaveRequestDto,
} from 'src/api/paidLeave'
import { useNotify } from 'src/composables/useNotify'
import { useServerPagination } from 'src/composables/useServerPagination'
import { useStudentOptions } from 'src/composables/useEntityOptions'
import { useAcademicPeriodStore } from 'stores/academicPeriod'
import { Permissions } from 'utils/permissions'
import AppTable from 'components/AppTable.vue'
import AppNotice from 'components/AppNotice.vue'
import StatusBadge from 'components/StatusBadge.vue'
import PermissionGuard from 'components/PermissionGuard.vue'
import PageHeader from 'components/PageHeader.vue'
import PaidLeaveRejectForm from 'components/forms/attendance/PaidLeaveRejectForm.vue'

const router = useRouter()
const notify = useNotify()
const periodStore = useAcademicPeriodStore()
const studentOpts = useStudentOptions()

const saving = ref(false)
const selected = ref<PaidLeaveRequestDto | null>(null)
const rejectDialog = ref(false)
const rejectStage = ref<'business' | 'school'>('business')
const statusFilter = ref<string | null>(null)

const filters = computed(() => ({
  academicPeriodId: periodStore.selectedPeriodId ?? undefined,
  status: statusFilter.value ?? undefined,
}))

const { rows: requests, loading, pagination, onRequest, load } =
  useServerPagination<PaidLeaveRequestDto>({
    fetchFn: (params) => paidLeaveApi.list(params),
    filters,
    defaultSortBy: 'startDate',
    defaultDescending: true,
  })

// Öğrenci adı backend DTO'sunda taşınmaz, frontend'de çözülür (proje deseni).
const studentMap = computed<Record<string, string>>(() => {
  const map: Record<string, string> = {}
  for (const opt of studentOpts.allOptions.value) {
    map[opt.value] = opt.label
  }
  return map
})

const columns: QTableProps['columns'] = [
  { name: 'range', label: 'Tarih Aralığı', field: 'startDate', align: 'left', sortable: true },
  { name: 'student', label: 'Öğrenci', field: 'studentId', align: 'left' },
  { name: 'reason', label: 'Gerekçe', field: 'reason', align: 'left' },
  { name: 'statusSlug', label: 'Durum', field: 'statusSlug', align: 'left' },
  { name: 'actions', label: '', field: 'id', align: 'right' },
]

function formatDate(iso: string) {
  return new Date(iso).toLocaleDateString('tr-TR', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
  })
}

function openForm() {
  router.push('/attendance/paid-leave/new').catch(() => {})
}

function openReject(row: PaidLeaveRequestDto, stage: 'business' | 'school') {
  selected.value = row
  rejectStage.value = stage
  rejectDialog.value = true
}

async function businessApprove(row: PaidLeaveRequestDto) {
  saving.value = true
  try {
    await paidLeaveApi.businessApprove(row.id)
    notify.success('Başvuru işletme adına onaylandı. Okul onayı bekleniyor.')
    await load()
  } catch (e) {
    notify.apiError(e, 'Onaylama sırasında bir hata oluştu.')
  } finally {
    saving.value = false
  }
}

async function approve(row: PaidLeaveRequestDto) {
  saving.value = true
  try {
    await paidLeaveApi.approve(row.id)
    notify.success('Ücretli izin onaylandı — izin günleri devamsızlık kaydına işlenecek.')
    await load()
  } catch (e) {
    notify.apiError(e, 'Onaylama sırasında bir hata oluştu.')
  } finally {
    saving.value = false
  }
}

watch(() => periodStore.selectedPeriodId, () => load())

onMounted(() => {
  studentOpts.load().catch(() => {})
  load().catch(() => {})
})
</script>
