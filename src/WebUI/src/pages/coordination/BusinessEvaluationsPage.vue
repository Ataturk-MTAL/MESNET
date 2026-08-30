<template>
  <q-page padding>
    <PageHeader title="İşletme Değerlendirmeleri">
      <PermissionGuard :permission="Permissions.Coordinator.Visit">
        <q-btn
          unelevated
          color="primary"
          icon="add"
          label="Değerlendirme Ekle"
          @click="goToNewEvaluation"
        />
      </PermissionGuard>
    </PageHeader>

    <AppTable
      :rows="evaluations"
      :columns="evalColumns"
      :loading="loadingEvals"
      :pagination="evalsPagination"
      @request="onEvalsRequest"
    >
      <template #body-cell-result="{ row }">
        <q-td>
          <StatusBadge :slug="resultLabel(row.result)" />
        </q-td>
      </template>
      <template #body-cell-evaluationDate="{ row }">
        <q-td>{{ formatDate(row.evaluationDate) }}</q-td>
      </template>
    </AppTable>
  </q-page>
</template>

<script setup lang="ts">
import { computed, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import type { QTableProps } from 'quasar'
import {
  coordinationApi,
  type BusinessEvaluationDto,
} from 'src/api/coordination'
import { useServerPagination } from 'src/composables/useServerPagination'
import { Permissions } from 'utils/permissions'
import AppTable from 'components/AppTable.vue'
import PageHeader from 'components/PageHeader.vue'
import StatusBadge from 'components/StatusBadge.vue'
import PermissionGuard from 'components/PermissionGuard.vue'

const router = useRouter()

const evalFilters = computed(() => ({}))
const { rows: evaluations, loading: loadingEvals, pagination: evalsPagination, onRequest: onEvalsRequest, load: loadEvaluations } = useServerPagination<BusinessEvaluationDto>({
  fetchFn: (params) => coordinationApi.listEvaluations(params),
  filters: evalFilters,
  defaultSortBy: 'evaluationDate',
  defaultDescending: true,
})

const evalColumns: QTableProps['columns'] = [
  { name: 'evaluationDate', label: 'Tarih', field: 'evaluationDate', align: 'left', sortable: true },
  { name: 'businessId', label: 'İşletme ID', field: (row) => (row as BusinessEvaluationDto).businessId.slice(0, 8) + '…', align: 'left' },
  { name: 'result', label: 'Sonuç', field: 'result', align: 'left' },
  { name: 'notes', label: 'Notlar', field: (row) => (row as BusinessEvaluationDto).notes ?? '—', align: 'left' },
]

function formatDate(iso: string) {
  return new Date(iso).toLocaleDateString('tr-TR', { day: '2-digit', month: '2-digit', year: 'numeric' })
}

/**
 * Değerlendirme sonucunun Türkçe etiketi. Renk kararı StatusBadge'in
 * STATUS_COLORS haritasına aittir — sayfada renk ternary'si kalmaz.
 */
function resultLabel(result: string) {
  if (result === 'Suitable') return 'Uygun'
  if (result === 'Conditional') return 'Şartlı'
  return 'Uygun Değil'
}

/**
 * Oluşturma formu artık ayrı route sayfası (DESIGN.md "Form Sayfa Kuralı"): varlık
 * yaratan bir form yan-panele değil kendi sayfasına açılır. MainLayout geçiş yönünü
 * hedef rotanın `meta.formRoute` işaretinden okur.
 */
function goToNewEvaluation() {
  router.push('/coordination/evaluations/new').catch(() => {})
}

// Açılış isteği BİLEREK eklendi: bu bir davranış koruması değil, önceden var olan
// "liste hiç yüklenmiyor" hatasının düzeltmesidir. Ölçüldü (`git show HEAD`): dosyada
// onMounted yoktu ve tek yükleme çağrısı dialog'un `createEvaluation()` sonundaki
// `await loadEvaluations()` idi — sayfa, aynı oturumda kayıt eklenene kadar HER açılışta
// boş geliyordu. Otomatik tetikleyicilerin ikisi de ölü: `useServerPagination` mount'ta
// yüklemez (`load()` yalnız onRequest/onSearch/filtre watch'ından çağrılır) ve
// `evalFilters` reaktif bağımlılık taşımadığı için o watch hiç tetiklenmez (immediate de
// değil); q-table `request`'i yalnız `requestServerInteraction` üzerinden yayar
// (quasar/src/components/table/table-pagination.js:66), QTable.js'te mount'ta
// setPagination çağıran kanca yoktur. Aynı desen: WeeklyVisitPage.vue:454.
onMounted(() => {
  loadEvaluations().catch(() => {})
})
</script>
