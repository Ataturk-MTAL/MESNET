<template>
  <q-page padding>
    <PageHeader title="Son İşlemler" />

    <AppTable
      :rows="entries"
      :columns="columns"
      :loading="loading"
      :pagination="pagination"
      show-search
      :search="search"
      no-data-label="Bu aralıkta kayıtlı işlem yok."
      @request="onRequest"
      @search="onSearch"
    >
      <template #filters>
        <q-select
          v-model="scope"
          :options="scopeOptions"
          label="Kapsam"
          outlined
          dense
          emit-value
          map-options
          style="min-width: 220px"
        />

        <q-select
          v-model="outcomeFilter"
          :options="outcomeOptions"
          label="Sonuç"
          outlined
          dense
          clearable
          emit-value
          map-options
          style="min-width: 180px"
        />

        <!-- `mine` ucu (GetMine) `crossedTenantBoundary` parametresini HİÇ almıyor — bu
             kapsamda anahtarı görünür bırakmak, açık görünüp hiçbir şey süzmeyen bir yalan
             üretirdi. Kurum kapsamına geçilince tekrar belirir. -->
        <q-toggle
          v-if="scope === 'institution'"
          v-model="crossedOnly"
          label="Yalnız kurum sınırını aşanlar"
          dense
        />
      </template>

      <template #body-cell-occurredAt="{ row }">
        <q-td>{{ formatDateTime(row.occurredAt) }}</q-td>
      </template>

      <template #body-cell-commandLabel="{ row }">
        <q-td>
          <div class="text-weight-medium">
            {{ row.commandLabel }}
          </div>
          <div class="text-caption text-grey-7">
            {{ row.module }}
          </div>
        </q-td>
      </template>

      <template #body-cell-outcome="{ row }">
        <q-td>
          <q-badge :color="outcomeColor(row.outcome)">
            {{ row.outcomeSlug }}
          </q-badge>
          <q-badge
            v-if="row.crossedTenantBoundary"
            color="deep-orange"
            class="q-ml-xs"
          >
            Kurum dışı
          </q-badge>
          <div
            v-if="row.errorCode"
            class="text-caption text-grey-7"
          >
            {{ row.errorCode }}
          </div>
        </q-td>
      </template>

      <template #body-cell-targets="{ row }">
        <q-td>{{ formatTargets(row) }}</q-td>
      </template>
    </AppTable>
  </q-page>
</template>

<script setup lang="ts">
import { ref, computed, watch, onMounted } from 'vue'
import type { QTableProps } from 'quasar'
import PageHeader from 'components/PageHeader.vue'
import AppTable from 'components/AppTable.vue'
import { auditApi, type AuditEntryDto } from 'src/api/audit'
import { useServerPagination } from 'src/composables/useServerPagination'
import { usePermissions } from 'src/utils/permissions'
import {
  DEFAULT_SCOPE,
  DEFAULT_SORT_BY,
  DEFAULT_DESCENDING,
  buildAuditListFilters,
  type AuditScope,
} from './auditListQuery'

const { hasPermission } = usePermissions()

/**
 * Kurum kapsamı YALNIZ izni olana gösterilir. Görünürlük bir kolaylıktır; asıl karar
 * sunucudadır (uç `audit:view:institution` ile korunur). İzin kontrolü ROL ADINA bakmaz.
 */
const canViewInstitution = computed(() => hasPermission('audit:view:institution'))

const scope = ref<AuditScope>(DEFAULT_SCOPE)
const outcomeFilter = ref<string | null>(null)
const crossedOnly = ref(false)

const scopeOptions = computed(() => {
  const options = [{ label: 'İşlemlerim', value: 'mine' }]
  if (canViewInstitution.value)
    options.push({ label: 'Kurumumdaki işlemler', value: 'institution' })
  return options
})

const outcomeOptions = [
  { label: 'Başarılı', value: 'Succeeded' },
  { label: 'Reddedildi', value: 'Rejected' },
  { label: 'Hata', value: 'Failed' },
]

const filters = computed(() =>
  buildAuditListFilters(scope.value, outcomeFilter.value, crossedOnly.value),
)

const { rows: entries, loading, pagination, search, onRequest, onSearch, load } =
  useServerPagination<AuditEntryDto>({
    // Kapsam URL'i DEĞİŞTİRİR, bir sorgu parametresi değildir: iki ucun izni farklıdır ve
    // yetki kararı sunucuda uç seviyesinde verilir.
    fetchFn: (params) =>
      scope.value === 'institution'
        ? auditApi.listForInstitution(params)
        : auditApi.listMine(params),
    filters,
    defaultSortBy: DEFAULT_SORT_BY,
    defaultDescending: DEFAULT_DESCENDING,
  })

// `filters` izleyicisi kapsam değişimini HER ZAMAN görmeyebilir (kapsam gövdeye girmiyor,
// ucu değiştiriyor — `crossedOnly` kapalıyken filtre gövdesi zaten aynı kalır). Kapsam
// değişince yeniden yükleme burada elle tetiklenir.
watch(scope, (newScope) => {
  // `mine` ucu `crossedTenantBoundary` almıyor. Anahtar `institution`'dan `mine`'a geçerken
  // açık kalmışsa sıfırlanır — yoksa gizli ama açık bir süzgeç bir sonraki `institution`
  // dönüşünde sessizce tekrar gövdeye sızar.
  if (newScope === 'mine') crossedOnly.value = false
  load().catch(() => {})
})

const columns: QTableProps['columns'] = [
  { name: 'occurredAt', label: 'Tarih', field: 'occurredAt', align: 'left', sortable: true },
  { name: 'actorName', label: 'Kim', field: 'actorName', align: 'left', sortable: true },
  { name: 'commandLabel', label: 'İşlem', field: 'commandLabel', align: 'left' },
  { name: 'outcome', label: 'Sonuç', field: 'outcome', align: 'left' },
  { name: 'targets', label: 'Hedef Kayıt', field: 'targetIds', align: 'left' },
]

function outcomeColor(outcome: string): string {
  if (outcome === 'Succeeded') return 'positive'
  if (outcome === 'Rejected') return 'warning'
  return 'negative'
}

function formatDateTime(value: string): string {
  return new Date(value).toLocaleString('tr-TR')
}

/** Hedefsiz satır bir hata değildir — komut bilinen ad kümesini kullanmamıştır. */
function formatTargets(row: AuditEntryDto): string {
  const keys = Object.keys(row.targetIds ?? {})
  return keys.length > 0 ? keys.join(', ') : '—'
}

// `useServerPagination`'ın filtre izleyicisi `immediate` DEĞİLDİR; ilk yükleme burada
// tetiklenmezse sayfa kalıcı olarak boş görünür.
onMounted(() => {
  load().catch(() => {})
})
</script>
