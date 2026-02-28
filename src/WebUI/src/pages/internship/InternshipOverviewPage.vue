<template>
  <q-page padding>
    <div class="row items-center q-mb-lg">
      <div class="col">
        <div class="text-h5 text-weight-bold">Staj Takibi</div>
      </div>
    </div>

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
          <template #body-cell-actions="{ row }">
            <q-td class="text-right">
              <q-btn flat round dense icon="visibility" @click="openDetail(row)" />
            </q-td>
          </template>
        </AppTable>
      </q-card-section>
    </q-card>

    <!-- Detay Panel -->
    <transition name="slide-right">
      <div v-if="selected" class="detail-backdrop" @click.self="closeDetail" />
    </transition>
    <transition name="slide-right">
      <div v-if="selected" class="detail-panel">
        <q-toolbar>
          <q-toolbar-title class="text-subtitle1 text-weight-bold">{{ selected.studentName }}</q-toolbar-title>
          <StatusBadge :slug="selected.statusSlug" class="q-mr-sm" />
          <q-btn flat round dense icon="close" @click="closeDetail" />
        </q-toolbar>
        <q-separator />
        <div class="detail-panel-scroll">
          <div class="q-pa-md q-gutter-sm">
            <q-item dense>
              <q-item-section avatar><q-icon name="person" /></q-item-section>
              <q-item-section>
                <q-item-label caption>Öğrenci</q-item-label>
                <q-item-label>{{ selected.studentName }}</q-item-label>
              </q-item-section>
            </q-item>
            <q-item dense>
              <q-item-section avatar><q-icon name="business" /></q-item-section>
              <q-item-section>
                <q-item-label caption>İşletme</q-item-label>
                <q-item-label>{{ selected.businessName }}</q-item-label>
              </q-item-section>
            </q-item>
            <q-item v-if="selected.teacherName" dense>
              <q-item-section avatar><q-icon name="school" /></q-item-section>
              <q-item-section>
                <q-item-label caption>Koordinatör Öğretmen</q-item-label>
                <q-item-label>{{ selected.teacherName }}</q-item-label>
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
              <q-item-section avatar><q-icon name="source" /></q-item-section>
              <q-item-section>
                <q-item-label caption>Kaynak</q-item-label>
                <q-item-label><q-badge color="blue-grey" :label="selected.sourceSlug" /></q-item-label>
              </q-item-section>
            </q-item>
            <q-item dense>
              <q-item-section avatar><q-icon name="event" /></q-item-section>
              <q-item-section>
                <q-item-label caption>Yerleştirme Tarihi</q-item-label>
                <q-item-label>{{ formatDate(selected.placedAt) }}</q-item-label>
              </q-item-section>
            </q-item>
            <template v-if="selected.transferredAt">
              <q-separator spaced />
              <div class="text-subtitle2 text-grey-7 q-px-md">Transfer Bilgisi</div>
              <q-item dense>
                <q-item-section avatar><q-icon name="swap_horiz" /></q-item-section>
                <q-item-section>
                  <q-item-label caption>Transfer Tarihi</q-item-label>
                  <q-item-label>{{ formatDate(selected.transferredAt) }}</q-item-label>
                </q-item-section>
              </q-item>
              <q-item v-if="selected.transferReason" dense>
                <q-item-section avatar><q-icon name="notes" /></q-item-section>
                <q-item-section>
                  <q-item-label caption>Transfer Gerekçesi</q-item-label>
                  <q-item-label>{{ selected.transferReason }}</q-item-label>
                </q-item-section>
              </q-item>
            </template>
          </div>
        </div>
      </div>
    </transition>
  </q-page>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, watch } from 'vue'
import type { QTableProps } from 'quasar'
import { enrollmentApi, type InternshipPlacementDto } from 'src/api/enrollment'
import { useNotify } from 'src/composables/useNotify'
import { useAcademicPeriodStore } from 'stores/academicPeriod'
import AppTable from 'components/AppTable.vue'
import StatusBadge from 'components/StatusBadge.vue'

const notify = useNotify()
const periodStore = useAcademicPeriodStore()
const loading = ref(false)
const placements = ref<InternshipPlacementDto[]>([])
const selected = ref<InternshipPlacementDto | null>(null)
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
}

function closeDetail() {
  selected.value = null
}

async function load() {
  loading.value = true
  try {
    const res = await enrollmentApi.listPlacements({
      status: statusFilter.value ?? undefined,
      academicPeriodId: periodStore.selectedPeriodId ?? undefined,
    })
    placements.value = res.data
  } catch {
    notify.error('Yerleştirmeler yüklenirken bir hata oluştu.')
  } finally {
    loading.value = false
  }
}

watch(() => periodStore.selectedPeriodId, () => load())
onMounted(load)
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
