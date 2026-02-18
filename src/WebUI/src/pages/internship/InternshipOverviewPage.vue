<template>
  <q-page padding>
    <div class="text-h5 text-weight-bold q-mb-lg">Staj Takibi</div>

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
        <div class="text-subtitle1 text-weight-medium q-mb-md">Aktif Yerleştirmeler</div>

        <div class="row q-gutter-sm q-mb-md">
          <q-select
            v-model="statusFilter"
            :options="statusOptions"
            label="Durum"
            filled dense emit-value map-options clearable
            style="min-width: 180px"
            @update:model-value="load"
          />
        </div>

        <AppTable :rows="placements" :columns="columns" :loading="loading">
          <template #body-cell-statusSlug="{ row }">
            <q-td><StatusBadge :slug="row.statusSlug" /></q-td>
          </template>
          <template #body-cell-placedAt="{ row }">
            <q-td>{{ formatDate(row.placedAt) }}</q-td>
          </template>
          <template #body-cell-sourceSlug="{ row }">
            <q-td><q-badge color="blue-grey" :label="row.sourceSlug" /></q-td>
          </template>
        </AppTable>
      </q-card-section>
    </q-card>
  </q-page>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import type { QTableProps } from 'quasar'
import { enrollmentApi, type InternshipPlacementDto } from 'src/api/enrollment'
import { useNotify } from 'src/composables/useNotify'
import AppTable from 'components/AppTable.vue'
import StatusBadge from 'components/StatusBadge.vue'

const notify = useNotify()
const loading = ref(false)
const placements = ref<InternshipPlacementDto[]>([])
const statusFilter = ref<string | null>(null)

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
  { name: 'studentId', label: 'Öğrenci ID', field: (row) => (row as InternshipPlacementDto).studentId.slice(0,8) + '…', align: 'left' },
  { name: 'businessId', label: 'İşletme ID', field: (row) => (row as InternshipPlacementDto).businessId.slice(0,8) + '…', align: 'left' },
  { name: 'statusSlug', label: 'Durum', field: 'statusSlug', align: 'left' },
  { name: 'sourceSlug', label: 'Kaynak', field: 'sourceSlug', align: 'left' },
  { name: 'placedAt', label: 'Yerleştirme Tarihi', field: 'placedAt', align: 'left', sortable: true },
]

function formatDate(iso: string) {
  return new Date(iso).toLocaleDateString('tr-TR', { day: '2-digit', month: '2-digit', year: 'numeric' })
}

async function load() {
  loading.value = true
  try {
    const res = await enrollmentApi.listPlacements({ status: statusFilter.value ?? undefined })
    placements.value = res.data
  } catch {
    notify.error('Yerleştirmeler yüklenirken bir hata oluştu.')
  } finally {
    loading.value = false
  }
}

onMounted(load)
</script>
