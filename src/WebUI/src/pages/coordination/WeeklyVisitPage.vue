<template>
  <q-page padding>
    <PageHeader title="Haftalık Ziyaretler" />

    <!-- Filtreler -->
    <div class="row q-col-gutter-md q-mb-lg items-end">
      <!-- Hafta seçici — takvimden tıkla, tüm hafta seçilir -->
      <div class="col-12 col-sm-auto">
        <q-btn
          outline
          icon="event"
          :label="weekLabel"
          no-caps
        >
          <q-popup-proxy
            transition-show="scale"
            transition-hide="scale"
          >
            <q-date
              :model-value="dateRangeModel"
              range
              first-day-of-week="1"
              @update:model-value="onDateSelect"
            >
              <div class="row items-center justify-end">
                <q-btn
                  v-close-popup
                  label="Tamam"
                  color="primary"
                  flat
                />
              </div>
            </q-date>
          </q-popup-proxy>
        </q-btn>
      </div>

      <!-- Kapsam seçici -->
      <div class="col-12 col-sm-2">
        <q-select
          v-model="scope"
          :options="scopeOptions"
          label="Kapsam"
          outlined
          dense
          emit-value
          map-options
        />
      </div>

      <!-- Alan seçici (Scope=Branch) -->
      <div
        v-if="scope === 'Branch'"
        class="col-12 col-sm-3"
      >
        <BranchSelector
          v-model="scopeBranchCode"
        />
      </div>

      <!-- Oluştur butonu -->
      <div class="col-12 col-sm-auto">
        <q-btn
          unelevated
          color="primary"
          icon="add"
          label="Ziyaret Oluştur"
          :loading="generating"
          :disable="periodStore.isReadOnly || !periodStore.selectedPeriodId"
          @click="onGenerate"
        />
      </div>
    </div>

    <!-- Salt okunur uyarı -->
    <AppNotice
      v-if="periodStore.isReadOnly"
      type="warning"
      icon="lock"
      message="Seçili dönem kapatılmış — ziyaret oluşturma ve silme işlemleri devre dışı."
      class="q-mb-md"
    />

    <!-- Plan tablosu -->
    <AppTable
      :rows="plans"
      :columns="planColumns"
      :loading="plansLoading"
      :pagination="plansPagination"
      row-key="id"
      no-data-label="Bu hafta için ziyaret planı bulunamadı."
      @request="onPlansRequest"
    >
      <template #body-cell-scope="{ row }">
        <q-td>
          {{ formatScope(row) }}
        </q-td>
      </template>

      <template #body-cell-dateRange="{ row }">
        <q-td>
          {{ formatDateTR(row.weekStartDate) }} — {{ formatDateTR(row.weekEndDate) }}
        </q-td>
      </template>

      <template #body-cell-generatedAt="{ row }">
        <q-td>
          {{ new Date(row.generatedAt).toLocaleString('tr-TR') }}
        </q-td>
      </template>

      <template #body-cell-actions="{ row }">
        <q-td class="q-gutter-xs">
          <q-btn
            flat
            dense
            color="primary"
            icon="visibility"
            size="sm"
            aria-label="Ziyaret detayını göster"
            @click="openDetail(row.id)"
          >
            <q-tooltip>Detay</q-tooltip>
          </q-btn>
          <q-btn
            flat
            dense
            color="negative"
            icon="delete"
            size="sm"
            :disable="periodStore.isReadOnly"
            :loading="deleting"
            aria-label="Ziyareti sil"
            @click="confirmDelete(row.id)"
          >
            <q-tooltip>Sil</q-tooltip>
          </q-btn>
        </q-td>
      </template>
    </AppTable>

    <!-- Atama Detay Dialog -->
    <DetailDialog
      v-model="detailDialogOpen"
      title="Ziyaret Atamaları"
      maximized
    >
      <template #toolbar-actions>
        <q-btn
          unelevated
          color="primary"
          icon="add"
          label="Eksik Atama Ekle"
          size="sm"
          :disable="periodStore.isReadOnly"
          class="q-mr-md"
          @click="openAddDialog"
        />
      </template>

      <q-card-section>
        <AppTable
          :rows="assignments"
          :columns="assignmentColumns"
          :loading="assignmentsLoading"
          :pagination="assignmentsPagination"
          row-key="id"
          show-search
          :search="assignmentSearch"
          no-data-label="Atama bulunamadı."
          @request="onAssignmentsRequest"
          @search="onAssignmentSearch"
        >
          <template #body-cell-visitDate="{ row }">
            <q-td class="text-center">
              {{ formatDateTR(row.visitDate) }}
            </q-td>
          </template>

          <template #body-cell-day="{ row }">
            <q-td class="text-center">
              {{ dayLabel(row.day) }}
            </q-td>
          </template>

          <template #body-cell-actions="{ row }">
            <q-td class="q-gutter-xs">
              <q-btn
                flat
                dense
                color="negative"
                icon="delete"
                size="sm"
                :disable="periodStore.isReadOnly"
                :loading="deletingAssignment"
                aria-label="Ziyaret atamasını sil"
                @click="confirmDeleteAssignment(row.id)"
              >
                <q-tooltip>Sil</q-tooltip>
              </q-btn>
            </q-td>
          </template>
        </AppTable>
      </q-card-section>
    </DetailDialog>

    <!-- Eksik Atama Ekle Dialog -->
    <FormDialog
      v-model="addDialogOpen"
      title="Eksik Ziyaret Atamaları"
      icon="playlist_add"
      color="primary"
      width="720px"
    >
      <div class="text-caption text-grey-7">
        Koordinasyon atamalarında olup bu planda bulunmayan kayıtlar alan bazında listeleniyor.
      </div>

      <DataState
        :loading="missingLoading"
        :empty="missingAssignments.length === 0"
        empty-icon="playlist_add_check"
        empty-text="Tüm atamalar zaten planda mevcut — eklenecek eksik kayıt yok."
        padding="q-pa-lg"
      >
        <div
          v-for="group in missingGrouped"
          :key="group.branchCode"
        >
          <div class="row items-center q-mb-xs q-mt-md">
            <div class="text-subtitle2 text-weight-bold">
              {{ group.branchName }}
            </div>
            <!-- Sayaç rozeti anlamsal durum taşımaz → bg-neutral (#465a73), beyaz metinle
                 7,07:1. grey-6 (#9e9e9e) zemininde QBadge'in varsayılan #fff metni 2,68:1'de
                 kalıyordu — ÖLÇÜLDÜ. -->
            <q-badge
              color="neutral"
              class="q-ml-sm"
            >
              {{ group.items.length }}
            </q-badge>
            <q-space />
            <q-btn
              dense
              flat
              color="primary"
              label="Alan Ekle"
              icon="add"
              size="sm"
              :loading="bulkAdding"
              @click="addBranchMissing(group.branchCode)"
            />
          </div>
          <q-list
            bordered
            separator
            class="rounded-borders q-mb-sm"
          >
            <q-item
              v-for="item in group.items"
              :key="`${item.businessId}-${item.day}`"
              dense
            >
              <q-item-section>
                <q-item-label>{{ item.businessName }}</q-item-label>
                <q-item-label caption>
                  {{ item.teacherName }} — {{ dayLabel(item.day) }} — {{ item.periodCount }} saat
                </q-item-label>
              </q-item-section>
              <q-item-section side>
                <q-btn
                  dense
                  flat
                  color="primary"
                  icon="add"
                  size="sm"
                  :loading="addingAssignment"
                  aria-label="Eksik ziyaret atamasını ekle"
                  @click="submitMissingAssignment(item)"
                >
                  <q-tooltip>Ekle</q-tooltip>
                </q-btn>
              </q-item-section>
            </q-item>
          </q-list>
        </div>
      </DataState>

      <template #actions>
        <q-btn
          flat
          label="Kapat"
          color="grey-7"
          @click="addDialogOpen = false"
        />
        <q-btn
          v-if="missingAssignments.length > 0"
          unelevated
          color="primary"
          label="Tümünü Ekle"
          icon="playlist_add"
          :loading="bulkAdding"
          @click="addAllMissing"
        />
      </template>
    </FormDialog>
  </q-page>
</template>

<script setup lang="ts">
import { computed, onMounted } from 'vue'
import { useQuasar } from 'quasar'
import type { QTableProps } from 'quasar'
import AppTable from 'src/components/AppTable.vue'
import BranchSelector from 'src/components/BranchSelector.vue'
import AppNotice from 'src/components/AppNotice.vue'
import DetailDialog from 'src/components/DetailDialog.vue'
import FormDialog from 'src/components/FormDialog.vue'
import DataState from 'src/components/DataState.vue'
import PageHeader from 'src/components/PageHeader.vue'
import { useAcademicPeriodStore } from 'src/stores/academicPeriod'
import { useWeeklyVisits, dayLabel, scopeLabel } from 'src/composables/useWeeklyVisits'
import { useMissingAssignments } from 'src/composables/useMissingAssignments'

const $q = useQuasar()
const periodStore = useAcademicPeriodStore()

const {
  dateRangeModel,
  onDateSelect,
  selectedYear,
  selectedWeek,
  weekLabel,
  scope,
  scopeBranchCode,
  plans,
  plansLoading,
  plansPagination,
  loadPlans,
  onPlansRequest,
  assignments,
  assignmentsLoading,
  assignmentsPagination,
  assignmentSearch,
  onAssignmentsRequest,
  onAssignmentSearch,
  detailDialogOpen,
  openDetail,
  generating,
  generate,
  deleting,
  deletePlan,
  deletingAssignment,
  deleteAssignment,
  addingAssignment,
  addDialogOpen,
  addAssignment,
} = useWeeklyVisits({
  academicPeriodId: computed(() => periodStore.selectedPeriodId),
})

// ── Eksik Atama Yönetimi (composable'a çıkarıldı) ──
const {
  missingLoading,
  missingAssignments,
  bulkAdding,
  missingGrouped,
  openAddDialog,
  submitMissingAssignment,
  addBranchMissing,
  addAllMissing,
} = useMissingAssignments({
  academicPeriodId: computed(() => periodStore.selectedPeriodId),
  assignments,
  addDialogOpen,
  addAssignment,
})

const scopeOptions = [
  { label: 'Tümü', value: 'All' },
  { label: 'Alan', value: 'Branch' },
]

/** ISO tarih string'ini Türkçe formata çevirir: "2026-03-18" → "18.03.2026" */
function formatDateTR(dateStr: string): string {
  if (!dateStr) return ''
  const d = new Date(dateStr)
  return d.toLocaleDateString('tr-TR', { day: '2-digit', month: '2-digit', year: 'numeric' })
}

const planColumns: QTableProps['columns'] = [
  { name: 'weekNumber', label: 'Hafta', field: 'weekNumber', align: 'center', sortable: true },
  { name: 'dateRange', label: 'Tarih Aralığı', field: 'weekStartDate', align: 'left' },
  { name: 'scope', label: 'Kapsam', field: 'scope', align: 'left' },
  { name: 'assignmentCount', label: 'Ziyaret Sayısı', field: 'assignmentCount', align: 'center', sortable: true },
  { name: 'generatedBy', label: 'Oluşturan', field: 'generatedBy', align: 'left' },
  { name: 'generatedAt', label: 'Oluşturma Tarihi', field: 'generatedAt', align: 'left', sortable: true },
  { name: 'actions', label: 'İşlemler', field: 'id', align: 'center' },
]

const assignmentColumns: QTableProps['columns'] = [
  { name: 'teacherName', label: 'Öğretmen', field: 'teacherName', align: 'left', sortable: true },
  { name: 'businessName', label: 'İşletme', field: 'businessName', align: 'left', sortable: true },
  { name: 'branchName', label: 'Alan', field: 'branchName', align: 'left' },
  { name: 'visitDate', label: 'Tarih', field: 'visitDate', align: 'center', sortable: true },
  { name: 'day', label: 'Gün', field: 'day', align: 'center' },
  { name: 'periodCount', label: 'Saat', field: 'periodCount', align: 'center', sortable: true },
  { name: 'actions', label: 'İşlemler', field: 'id', align: 'center' },
]

function formatScope(row: { scope: string; scopeTeacherId?: string; scopeBranchCode?: string }) {
  const label = scopeLabel(row.scope)
  if (row.scope === 'Branch' && row.scopeBranchCode) return `${label}: ${row.scopeBranchCode}`
  return label
}

function onGenerate() {
  $q.dialog({
    title: 'Ziyaret Oluştur',
    message: `${selectedYear.value} yılı ${selectedWeek.value}. hafta için ziyaret atamaları oluşturulacak. Devam etmek istiyor musunuz?`,
    cancel: { label: 'İptal', flat: true },
    ok: { label: 'Oluştur', color: 'primary' },
    persistent: true,
  }).onOk(() => {
    generate().catch(() => {})
  })
}

function confirmDelete(planId: string) {
  $q.dialog({
    title: 'Planı Sil',
    message: 'Bu haftalık ziyaret planı ve tüm atamaları silinecek. Devam etmek istiyor musunuz?',
    cancel: { label: 'İptal', flat: true },
    ok: { label: 'Sil', color: 'negative' },
    persistent: true,
  }).onOk(() => {
    deletePlan(planId).catch(() => {})
  })
}

function confirmDeleteAssignment(assignmentId: string) {
  $q.dialog({
    title: 'Atamayı Sil',
    message: 'Bu ziyaret ataması silinecek. Devam etmek istiyor musunuz?',
    cancel: { label: 'İptal', flat: true },
    ok: { label: 'Sil', color: 'negative' },
    persistent: true,
  }).onOk(() => {
    deleteAssignment(assignmentId).catch(() => {})
  })
}

onMounted(() => {
  if (periodStore.selectedPeriodId) {
    loadPlans().catch(() => {})
  }
})
</script>
