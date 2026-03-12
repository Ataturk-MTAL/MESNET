<template>
  <q-page padding>
    <div class="text-h5 text-weight-bold q-mb-lg">Haftalık Ziyaretler</div>

    <!-- Filtreler -->
    <div class="row q-col-gutter-md q-mb-lg items-end">
      <!-- Hafta seçici — takvimden tıkla, tüm hafta seçilir -->
      <div class="col-12 col-sm-auto">
        <q-btn outline icon="event" :label="weekLabel" no-caps>
          <q-popup-proxy transition-show="scale" transition-hide="scale">
            <q-date
              :model-value="dateRangeModel"
              range
              first-day-of-week="1"
              @update:model-value="onDateSelect"
            >
              <div class="row items-center justify-end">
                <q-btn v-close-popup label="Tamam" color="primary" flat />
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
      <div v-if="scope === 'Branch'" class="col-12 col-sm-3">
        <BranchSelector
          v-model="scopeBranchCode"
        />
      </div>

      <!-- Oluştur butonu -->
      <div class="col-12 col-sm-auto">
        <q-btn
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
    <q-banner
      v-if="periodStore.isReadOnly"
      rounded
      class="bg-orange-1 text-orange-9 q-mb-md"
    >
      <template #avatar>
        <q-icon name="lock" color="orange-7" />
      </template>
      Seçili dönem kapatılmış — ziyaret oluşturma ve silme işlemleri devre dışı.
    </q-banner>

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
            @click="confirmDelete(row.id)"
          >
            <q-tooltip>Sil</q-tooltip>
          </q-btn>
        </q-td>
      </template>
    </AppTable>

    <!-- Atama Detay Dialog -->
    <q-dialog v-model="detailDialogOpen" maximized>
      <q-card>
        <q-card-section class="row items-center q-pb-none">
          <div class="text-h6">Ziyaret Atamaları</div>
          <q-space />
          <q-btn
            color="primary"
            icon="add"
            label="Eksik Atama Ekle"
            size="sm"
            :disable="periodStore.isReadOnly"
            class="q-mr-md"
            @click="openAddDialog"
          />
          <q-btn flat round dense icon="close" @click="detailDialogOpen = false" />
        </q-card-section>

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
              <q-td class="text-center">{{ formatDateTR(row.visitDate) }}</q-td>
            </template>

            <template #body-cell-day="{ row }">
              <q-td class="text-center">{{ dayLabel(row.day) }}</q-td>
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
                  @click="confirmDeleteAssignment(row.id)"
                >
                  <q-tooltip>Sil</q-tooltip>
                </q-btn>
              </q-td>
            </template>
          </AppTable>
        </q-card-section>
      </q-card>
    </q-dialog>

    <!-- Eksik Atama Ekle Dialog -->
    <q-dialog v-model="addDialogOpen" persistent>
      <q-card style="min-width: 700px; max-width: 900px">
        <q-card-section class="row items-center">
          <div>
            <div class="text-h6">Eksik Ziyaret Atamaları</div>
            <div class="text-caption text-grey-7">
              Koordinasyon atamalarında olup bu planda bulunmayan kayıtlar alan bazında listeleniyor.
            </div>
          </div>
          <q-space />
          <q-btn
            v-if="missingAssignments.length > 0"
            color="primary"
            label="Tümünü Ekle"
            icon="playlist_add"
            size="sm"
            :loading="bulkAdding"
            @click="addAllMissing"
          />
        </q-card-section>

        <q-card-section>
          <q-linear-progress v-if="missingLoading" indeterminate color="primary" class="q-mb-md" />

          <div v-if="!missingLoading && missingAssignments.length === 0" class="text-center text-grey-6 q-pa-lg">
            Tüm atamalar zaten planda mevcut — eklenecek eksik kayıt yok.
          </div>

          <div v-for="group in missingGrouped" :key="group.branchCode">
            <div class="row items-center q-mb-xs q-mt-md">
              <div class="text-subtitle2 text-weight-bold">{{ group.branchName }}</div>
              <q-badge color="grey-6" class="q-ml-sm">{{ group.items.length }}</q-badge>
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
            <q-list bordered separator class="rounded-borders q-mb-sm">
              <q-item v-for="item in group.items" :key="`${item.businessId}-${item.day}`" dense>
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
                    @click="submitMissingAssignment(item)"
                  >
                    <q-tooltip>Ekle</q-tooltip>
                  </q-btn>
                </q-item-section>
              </q-item>
            </q-list>
          </div>
        </q-card-section>

        <q-card-actions align="right">
          <q-btn flat label="Kapat" @click="addDialogOpen = false" />
        </q-card-actions>
      </q-card>
    </q-dialog>
  </q-page>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useQuasar } from 'quasar'
import type { QTableProps } from 'quasar'
import AppTable from 'src/components/AppTable.vue'
import BranchSelector from 'src/components/BranchSelector.vue'
import { useAcademicPeriodStore } from 'src/stores/academicPeriod'
import { useWeeklyVisits, dayLabel, scopeLabel } from 'src/composables/useWeeklyVisits'
import { coordinationApi, type BusinessAssignmentDto } from 'src/api/coordination'

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

// ── Eksik Atama Ekleme ──

interface MissingAssignment {
  businessId: string
  businessName: string
  teacherId: string
  teacherName: string
  branchCode: string
  branchName: string
  day: string
  periodCount: number
}

interface MissingGroup {
  branchCode: string
  branchName: string
  items: MissingAssignment[]
}

const missingLoading = ref(false)
const missingAssignments = ref<MissingAssignment[]>([])
const bulkAdding = ref(false)

/** Eksik atamaları alan bazında grupla */
const missingGrouped = computed<MissingGroup[]>(() => {
  const map = new Map<string, MissingGroup>()
  for (const item of missingAssignments.value) {
    let group = map.get(item.branchCode)
    if (!group) {
      group = { branchCode: item.branchCode, branchName: item.branchName, items: [] }
      map.set(item.branchCode, group)
    }
    group.items.push(item)
  }
  return [...map.values()].sort((a, b) => a.branchName.localeCompare(b.branchName, 'tr'))
})

async function openAddDialog() {
  addDialogOpen.value = true
  missingLoading.value = true
  missingAssignments.value = []

  try {
    const res = await coordinationApi.listAssignments({
      assignedOnly: true,
      academicPeriodId: periodStore.selectedPeriodId ?? undefined,
    })
    const coordData = res.data as unknown as BusinessAssignmentDto[]

    // Koordinasyon atamalarından işletme-gün çiftlerini çıkar
    const allPairs: MissingAssignment[] = []
    for (const biz of coordData) {
      if (!biz.assignedTeacherId || biz.assignedSlots.length === 0) continue

      // Gün bazında grupla (1 işletme + 1 gün = 1 ziyaret)
      const slotsByDay = new Map<string, number>()
      for (const slot of biz.assignedSlots) {
        slotsByDay.set(slot.day, (slotsByDay.get(slot.day) ?? 0) + 1)
      }

      for (const [day, count] of slotsByDay) {
        allPairs.push({
          businessId: biz.businessId,
          businessName: biz.businessName,
          teacherId: biz.assignedTeacherId,
          teacherName: biz.assignedTeacherName ?? '',
          branchCode: biz.branchCode,
          branchName: biz.branchName,
          day,
          periodCount: count,
        })
      }
    }

    // Mevcut plandaki atamaları set olarak tut
    const existingKeys = new Set(
      assignments.value.map(a => `${a.businessId}::${a.day}`),
    )

    // Eksik olanları filtrele
    missingAssignments.value = allPairs.filter(
      p => !existingKeys.has(`${p.businessId}::${p.day}`),
    )
  } catch {
    missingAssignments.value = []
  } finally {
    missingLoading.value = false
  }
}

async function submitMissingAssignment(item: MissingAssignment) {
  await addAssignment({
    teacherId: item.teacherId,
    teacherName: item.teacherName,
    businessId: item.businessId,
    businessName: item.businessName,
    branchCode: item.branchCode,
    branchName: item.branchName,
    day: item.day,
    periodCount: item.periodCount,
  }).catch(() => {})

  // Eklenen kaydı listeden kaldır
  missingAssignments.value = missingAssignments.value.filter(
    m => !(m.businessId === item.businessId && m.day === item.day),
  )
}

/** Belirli bir alanın tüm eksik atamalarını sırayla ekle */
async function addBranchMissing(branchCode: string) {
  const items = missingAssignments.value.filter(m => m.branchCode === branchCode)
  if (items.length === 0) return

  bulkAdding.value = true
  try {
    for (const item of items) {
      await addAssignment({
        teacherId: item.teacherId,
        teacherName: item.teacherName,
        businessId: item.businessId,
        businessName: item.businessName,
        branchCode: item.branchCode,
        branchName: item.branchName,
        day: item.day,
        periodCount: item.periodCount,
      }).catch(() => {})
    }
    // Eklenen alanı listeden kaldır
    missingAssignments.value = missingAssignments.value.filter(m => m.branchCode !== branchCode)
  } finally {
    bulkAdding.value = false
  }
}

/** Tüm eksik atamaları sırayla ekle */
async function addAllMissing() {
  if (missingAssignments.value.length === 0) return

  bulkAdding.value = true
  try {
    const items = [...missingAssignments.value]
    for (const item of items) {
      await addAssignment({
        teacherId: item.teacherId,
        teacherName: item.teacherName,
        businessId: item.businessId,
        businessName: item.businessName,
        branchCode: item.branchCode,
        branchName: item.branchName,
        day: item.day,
        periodCount: item.periodCount,
      }).catch(() => {})
    }
    missingAssignments.value = []
  } finally {
    bulkAdding.value = false
  }
}

onMounted(() => {
  if (periodStore.selectedPeriodId) {
    loadPlans().catch(() => {})
  }
})
</script>
