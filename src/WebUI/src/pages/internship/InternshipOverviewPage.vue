<template>
  <q-page padding>
    <PageHeader title="Staj Takibi" />

    <!-- Özet Kartlar -->
    <div class="row q-col-gutter-md q-mb-lg">
      <div class="col-12 col-sm-6 col-md-3">
        <q-card flat bordered>
          <q-card-section class="text-center">
            <q-icon name="school" size="40px" color="primary" />
            <div class="text-h4 text-weight-bold text-primary q-mt-sm">{{ stats.placed }}</div>
            <div class="text-caption text-grey">Yerleştirildi</div>
          </q-card-section>
        </q-card>
      </div>
      <div class="col-12 col-sm-6 col-md-3">
        <q-card flat bordered>
          <q-card-section class="text-center">
            <q-icon name="work" size="40px" color="positive" />
            <div class="text-h4 text-weight-bold text-positive q-mt-sm">{{ stats.active }}</div>
            <div class="text-caption text-grey">Aktif Staj</div>
          </q-card-section>
        </q-card>
      </div>
      <div class="col-12 col-sm-6 col-md-3">
        <q-card flat bordered>
          <q-card-section class="text-center">
            <q-icon name="done_all" size="40px" color="purple" />
            <div class="text-h4 text-weight-bold text-purple q-mt-sm">{{ stats.completed }}</div>
            <div class="text-caption text-grey">Tamamlandı</div>
          </q-card-section>
        </q-card>
      </div>
      <div class="col-12 col-sm-6 col-md-3">
        <q-card flat bordered>
          <q-card-section class="text-center">
            <q-icon name="cancel" size="40px" color="negative" />
            <div class="text-h4 text-weight-bold text-negative q-mt-sm">{{ stats.cancelled }}</div>
            <div class="text-caption text-grey">İptal / Transfer</div>
          </q-card-section>
        </q-card>
      </div>
    </div>

    <!-- Yerleştirme Listesi -->
    <q-card flat bordered>
      <q-card-section>
        <div class="text-subtitle1 text-weight-medium q-mb-md">Yerleştirmeler</div>

        <div class="row q-gutter-sm q-mb-md">
          <q-select
            v-model="statusFilter"
            :options="statusOptions"
            label="Durum"
            filled dense emit-value map-options clearable
            style="min-width: 180px"
          />
        </div>

        <AppTable :rows="placements" :columns="columns" :loading="loading" :pagination="pagination" @request="onRequest">
          <template #body-cell-statusSlug="{ row }">
            <q-td><StatusBadge :slug="row.statusSlug" /></q-td>
          </template>
          <template #body-cell-placedAt="{ row }">
            <q-td>{{ formatDate(row.placedAt) }}</q-td>
          </template>
          <template #body-cell-sourceSlug="{ row }">
            <q-td><q-badge color="blue-grey" :label="row.sourceSlug" /></q-td>
          </template>
          <template #body-cell-actions="{ row }">
            <q-td class="text-right">
              <q-btn flat round dense icon="visibility" @click="openDetail(row)" />
            </q-td>
          </template>
        </AppTable>
      </q-card-section>
    </q-card>

    <!-- Detay Panel -->
    <DetailPanel v-model="detailOpen" :has-content="!!selected" :width="400">
      <template #title>{{ selected?.studentName }}</template>
      <template #toolbar-actions>
        <StatusBadge :slug="selected?.statusSlug ?? ''" class="q-mr-sm" />
      </template>
      <template v-if="selected">
        <div class="q-gutter-sm">
          <InfoItem icon="person" label="Öğrenci" :value="selected.studentName" />
          <InfoItem icon="business" label="İşletme" :value="selected.businessName" />
          <InfoItem v-if="selected.teacherName" icon="school" label="Koordinatör Öğretmen" :value="selected.teacherName" />
          <InfoItem icon="badge" label="Durum"><StatusBadge :slug="selected.statusSlug" /></InfoItem>
          <InfoItem icon="source" label="Kaynak"><q-badge color="blue-grey" :label="selected.sourceSlug" /></InfoItem>
          <InfoItem icon="event" label="Yerleştirme Tarihi" :value="formatDate(selected.placedAt)" />
          <template v-if="selected.transferredAt">
            <q-separator spaced />
            <div class="text-subtitle2 text-grey-7 q-px-md">Transfer Bilgisi</div>
            <InfoItem icon="swap_horiz" label="Transfer Tarihi" :value="formatDate(selected.transferredAt)" />
            <InfoItem v-if="selected.transferReason" icon="notes" label="Transfer Gerekçesi" :value="selected.transferReason" />
          </template>
        </div>
      </template>
    </DetailPanel>
  </q-page>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import type { QTableProps } from 'quasar'
import { enrollmentApi, type InternshipPlacementDto } from 'src/api/enrollment'
import { useServerPagination } from 'src/composables/useServerPagination'
import { useAcademicPeriodStore } from 'stores/academicPeriod'
import AppTable from 'components/AppTable.vue'
import DetailPanel from 'components/DetailPanel.vue'
import StatusBadge from 'components/StatusBadge.vue'
import InfoItem from 'components/InfoItem.vue'
import PageHeader from 'components/PageHeader.vue'

const periodStore = useAcademicPeriodStore()
const selected = ref<InternshipPlacementDto | null>(null)
const detailOpen = ref(false)
const statusFilter = ref<string | null>(null)

const filters = computed(() => ({
  ...(statusFilter.value ? { status: statusFilter.value } : {}),
  ...(periodStore.selectedPeriodId ? { academicPeriodId: periodStore.selectedPeriodId } : {}),
}))

const { rows: placements, loading, pagination, onRequest, load } =
  useServerPagination<InternshipPlacementDto>({
    fetchFn: (params) => enrollmentApi.listPlacements(params),
    filters,
    defaultSortBy: 'placedAt',
    defaultDescending: true,
  })

const statusOptions = [
  { label: 'Eşleştirildi', value: 'Matched' },
  { label: 'Aktif', value: 'Active' },
  { label: 'Tamamlandı', value: 'Completed' },
  { label: 'Transfer Edildi', value: 'Transferred' },
  { label: 'İptal Edildi', value: 'Cancelled' },
]

const stats = computed(() => ({
  placed: placements.value.filter((p) => p.status === 'Matched').length,
  active: placements.value.filter((p) => p.status === 'Active').length,
  completed: placements.value.filter((p) => p.status === 'Completed').length,
  cancelled: placements.value.filter((p) => ['Cancelled', 'Transferred'].includes(p.status)).length,
}))

const columns: QTableProps['columns'] = [
  { name: 'studentName', label: 'Öğrenci', field: 'studentName', align: 'left', sortable: true },
  { name: 'businessName', label: 'İşletme', field: 'businessName', align: 'left', sortable: true },
  { name: 'teacherName', label: 'Koordinatör', field: (row) => (row as InternshipPlacementDto).teacherName ?? '—', align: 'left' },
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

onMounted(load)
</script>
