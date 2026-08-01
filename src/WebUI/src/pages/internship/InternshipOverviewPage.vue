<template>
  <q-page padding>
    <PageHeader title="Staj Takibi" />

    <AppNotice
      v-if="periodStore.isReadOnly"
      type="readonly"
      class="q-mb-md"
      message="Bu dönem kapatılmıştır — yalnızca görüntüleme yapılabilir."
    />

    <!-- Özet Kartlar -->
    <div class="row q-col-gutter-md q-mb-lg">
      <div class="col-12 col-sm-6 col-md">
        <StatCard
          orientation="vertical"
          icon="school"
          :value="stats.placed"
          label="Yerleştirildi"
          color="primary"
        />
      </div>
      <div class="col-12 col-sm-6 col-md">
        <StatCard
          orientation="vertical"
          icon="work"
          :value="stats.active"
          label="Aktif Staj"
          color="positive"
        />
      </div>
      <div class="col-12 col-sm-6 col-md">
        <StatCard
          orientation="vertical"
          icon="done_all"
          :value="stats.completed"
          label="Tamamlandı"
          color="secondary"
        />
      </div>
      <div class="col-12 col-sm-6 col-md">
        <StatCard
          orientation="vertical"
          icon="cancel"
          :value="stats.cancelled"
          label="Fesih Yapıldı"
          color="negative"
        />
      </div>
      <div class="col-12 col-sm-6 col-md">
        <StatCard
          orientation="vertical"
          icon="event_busy"
          :value="stats.failedToComplete"
          label="Tamamlayamadı"
          color="warning"
        />
      </div>
    </div>

    <!-- Yerleştirme Listesi -->
    <q-card
      flat
      bordered
    >
      <q-card-section>
        <div class="text-subtitle1 text-weight-medium q-mb-md">
          Yerleştirmeler
        </div>

        <div class="row q-col-gutter-sm q-mb-md items-end">
          <div class="col-12 col-sm-4">
            <BranchSelector
              v-model="branchFilter"
              dense
              force-select
            />
          </div>
          <div class="col-12 col-sm-4">
            <q-select
              v-model="statusFilter"
              :options="statusOptions"
              label="Durum"
              outlined
              dense
              emit-value
              map-options
              clearable
            />
          </div>
        </div>

        <AppTable
          :rows="placements"
          :columns="columns"
          :loading="loading"
          :pagination="pagination"
          show-search
          :search="search"
          @request="onRequest"
          @search="onSearch"
        >
          <template #body-cell-statusSlug="{ row }">
            <q-td><StatusBadge :slug="row.statusSlug" /></q-td>
          </template>
          <template #body-cell-placedAt="{ row }">
            <q-td>{{ formatDate(row.placedAt) }}</q-td>
          </template>
          <template #body-cell-sourceSlug="{ row }">
            <q-td>
              <q-badge
                color="neutral"
                :label="row.sourceSlug"
              />
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
              />
            </q-td>
          </template>
        </AppTable>
      </q-card-section>
    </q-card>

    <!-- Devamsızlık Durumu -->
    <q-card
      flat
      bordered
      class="q-mt-md"
    >
      <q-card-section>
        <div class="text-subtitle1 text-weight-medium q-mb-md">
          Devamsızlık Durumu
        </div>
        <div class="text-caption text-grey q-mb-sm">
          25 gün ve üzeri devamsız öğrenciler listelenir.
          30 günü aşan öğrenciler <span class="text-negative text-weight-medium">kırmızı</span> ile gösterilir.
        </div>

        <q-table
          flat
          :rows="highAbsenceRows"
          :columns="absenceColumns"
          :loading="absenceLoading"
          row-key="id"
          :rows-per-page-options="[0]"
          hide-bottom
        >
          <template #body="{ row }">
            <tr :class="row.totalAbsenceDays >= 30 ? 'bg-negative-soft' : 'bg-warning-soft'">
              <td class="text-left">
                {{ row.studentName }}
              </td>
              <td class="text-left">
                {{ row.businessName }}
              </td>
              <td class="text-center">
                <span :class="row.totalAbsenceDays >= 30 ? 'text-negative text-weight-bold' : 'text-warning text-weight-medium'">
                  {{ row.totalAbsenceDays }} gün
                </span>
              </td>
              <td class="text-right">
                <q-btn
                  v-if="canManage && row.totalAbsenceDays >= 30"
                  :disable="periodStore.isReadOnly"
                  flat
                  dense
                  size="sm"
                  color="negative"
                  label="Tamamlayamadı İşaretle"
                  :loading="markingId === row.placementId"
                  @click="markFailed(row.placementId)"
                />
              </td>
            </tr>
          </template>
        </q-table>
      </q-card-section>
    </q-card>

    <!-- Detay Panel -->
    <DetailPanel
      v-model="detailOpen"
      :has-content="!!selected"
      :width="400"
    >
      <template #title>
        {{ selected?.studentName }}
      </template>
      <template #toolbar-actions>
        <StatusBadge
          :slug="selected?.statusSlug ?? ''"
          class="q-mr-sm"
        />
      </template>
      <template v-if="selected">
        <div class="q-gutter-sm">
          <InfoItem
            icon="person"
            label="Öğrenci"
            :value="selected.studentName"
          />
          <InfoItem
            icon="business"
            label="İşletme"
            :value="selected.businessName"
          />
          <InfoItem
            v-if="selected.teacherName"
            icon="school"
            label="Koordinatör Öğretmen"
            :value="selected.teacherName"
          />
          <InfoItem
            icon="badge"
            label="Durum"
          >
            <StatusBadge :slug="selected.statusSlug" />
          </InfoItem>
          <InfoItem
            icon="source"
            label="Kaynak"
          >
            <q-badge
              color="neutral"
              :label="selected.sourceSlug"
            />
          </InfoItem>
          <InfoItem
            icon="event"
            label="Yerleştirme Tarihi"
            :value="formatDate(selected.placedAt)"
          />
        </div>
      </template>
    </DetailPanel>
  </q-page>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, watch } from 'vue'
import type { QTableProps } from 'quasar'
import { useQuasar } from 'quasar'
import { enrollmentApi, type InternshipPlacementDto } from 'src/api/enrollment'
import { internshipApi } from 'src/api/internship'
import type { InternshipSummaryDto } from 'src/api/internship'
import { useServerPagination } from 'src/composables/useServerPagination'
import { useAcademicPeriodStore } from 'stores/academicPeriod'
import { useAuthStore } from 'stores/auth'
import AppTable from 'components/AppTable.vue'
import DetailPanel from 'components/DetailPanel.vue'
import StatusBadge from 'components/StatusBadge.vue'
import InfoItem from 'components/InfoItem.vue'
import PageHeader from 'components/PageHeader.vue'
import BranchSelector from 'components/BranchSelector.vue'
import StatCard from 'components/StatCard.vue'
import AppNotice from 'components/AppNotice.vue'

const $q = useQuasar()
const periodStore = useAcademicPeriodStore()
const authStore = useAuthStore()

const selected = ref<InternshipPlacementDto | null>(null)
const detailOpen = ref(false)
const statusFilter = ref<string | null>(null)
const branchFilter = ref<string | null>(null)

// ─── Devamsızlık bölümü ───
const absenceLoading = ref(false)
const highAbsenceRows = ref<InternshipSummaryDto[]>([])
const markingId = ref<string | null>(null)
const canManage = computed(() => authStore.hasPermission('internship:manage'))

const absenceColumns: QTableProps['columns'] = [
  { name: 'studentName', label: 'Öğrenci', field: 'studentName', align: 'left' },
  { name: 'businessName', label: 'İşletme', field: 'businessName', align: 'left' },
  { name: 'totalAbsenceDays', label: 'Devamsızlık', field: 'totalAbsenceDays', align: 'center' },
  { name: 'actions', label: '', field: 'id', align: 'right' },
]

async function loadAbsenceData() {
  if (!periodStore.selectedPeriodId) return
  absenceLoading.value = true
  try {
    const res = await internshipApi.listInternships({
      academicPeriodId: periodStore.selectedPeriodId,
      minAbsenceDays: 25,
      pageSize: 100,
    })
    highAbsenceRows.value = res.data?.items ?? []
  } finally {
    absenceLoading.value = false
  }
}

async function markFailed(placementId: string) {
  markingId.value = placementId
  try {
    await internshipApi.markAsFailedToComplete(placementId)
    $q.notify({ type: 'positive', message: 'Staj "Tamamlayamadı" olarak işaretlendi.' })
    await Promise.all([load(), loadAbsenceData()])
  } catch {
    $q.notify({ type: 'negative', message: 'İşlem başarısız oldu.' })
  } finally {
    markingId.value = null
  }
}

watch(() => periodStore.selectedPeriodId, loadAbsenceData)

const filters = computed(() => ({
  ...(statusFilter.value ? { status: statusFilter.value } : {}),
  ...(branchFilter.value ? { branchCode: branchFilter.value } : {}),
  ...(periodStore.selectedPeriodId ? { academicPeriodId: periodStore.selectedPeriodId } : {}),
}))

const { rows: placements, loading, pagination, onRequest, onSearch, search, load } =
  useServerPagination<InternshipPlacementDto>({
    fetchFn: (params) => enrollmentApi.listPlacements(params),
    filters,
    defaultSortBy: 'placedAt',
    defaultDescending: true,
  })

const statusOptions = [
  { label: 'Yerleştirildi', value: 'Matched' },
  { label: 'Fesih Yapıldı', value: 'Cancelled' },
  { label: 'Tamamlandı', value: 'Completed' },
  { label: 'Tamamlayamadı', value: 'FailedToComplete' },
]

// Özet kartlar TOPLAM sayımları backend'den alır — paginated `placements` (yalnız aktif sayfa, ≤20)
// üzerinden hesaplanmaz (yanlış toplam bug'ı). Sayıma status filtresi uygulanmaz; branch+period uygulanır.
const statusCounts = ref<Record<string, number>>({})

async function loadStatusCounts() {
  const res = await enrollmentApi.getPlacementStatusCounts({
    ...(branchFilter.value ? { branchCode: branchFilter.value } : {}),
    ...(periodStore.selectedPeriodId ? { academicPeriodId: periodStore.selectedPeriodId } : {}),
  })
  statusCounts.value = res.data
}

const stats = computed(() => ({
  placed: statusCounts.value.Matched ?? 0,
  active: statusCounts.value.Active ?? 0,
  completed: statusCounts.value.Completed ?? 0,
  cancelled: statusCounts.value.Cancelled ?? 0,
  failedToComplete: statusCounts.value.FailedToComplete ?? 0,
}))

watch([branchFilter, () => periodStore.selectedPeriodId], () => loadStatusCounts().catch(() => {}))

const columns: QTableProps['columns'] = [
  { name: 'studentName', label: 'Öğrenci', field: 'studentName', align: 'left', sortable: true },
  {
    name: 'businessName',
    label: 'İşletme',
    // Okulda stajda işletme yoktur (#159) — boş hücre "veri eksik" gibi okunmasın diye
    // türün Türkçe karşılığı yazılır ("Okulda").
    field: (row) => {
      const p = row as InternshipPlacementDto
      return p.businessId ? p.businessName : p.placementTypeSlug
    },
    align: 'left',
    sortable: true,
  },
  {
    name: 'teacherName',
    // Okulda stajda aynı alan gözetmeni (alan/atölye şefi) taşır ve ücret doğurmaz.
    label: 'Koordinatör / Gözetmen',
    field: (row) => (row as InternshipPlacementDto).teacherName ?? '—',
    align: 'left',
  },
  { name: 'statusSlug', label: 'Durum', field: 'statusSlug', align: 'left' },
  { name: 'sourceSlug', label: 'Kaynak', field: 'sourceSlug', align: 'left' },
  { name: 'placedAt', label: 'Yerleştirme Tarihi', field: 'placedAt', align: 'left', sortable: true },
  { name: 'actions', label: '', field: 'id', align: 'right' },
]

function formatDate(iso: string) {
  return new Date(iso).toLocaleDateString('tr-TR', { day: '2-digit', month: '2-digit', year: 'numeric' })
}

function openDetail(row: InternshipPlacementDto) {
  selected.value = row
  detailOpen.value = true
}

onMounted(() => {
  load()
  loadAbsenceData()
  loadStatusCounts().catch(() => {})
})
</script>
