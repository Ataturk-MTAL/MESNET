<template>
  <div>
    <!-- Arama çubuğu (server-side mod + showSearch aktifse) -->
    <div v-if="showSearch" class="row justify-end q-mb-sm">
      <q-input
        :model-value="search"
        dense
        outlined
        placeholder="Ara..."
        style="min-width: 250px"
        debounce="400"
        @update:model-value="onSearchInput"
      >
        <template #prepend>
          <q-icon name="search" />
        </template>
        <template v-if="search" #append>
          <q-icon name="close" class="cursor-pointer" @click="$emit('search', '')" />
        </template>
      </q-input>
    </div>

    <!-- İlk yükleme: spinner yerine içerik-şekilli skeleton satırlar (layout-shift'siz) -->
    <div v-if="loading && rows.length === 0" class="q-mt-xs">
      <q-skeleton
        v-for="i in 6"
        :key="i"
        type="rect"
        height="44px"
        class="q-mb-xs rounded-borders"
      />
    </div>

    <q-table
      v-else
      v-bind="$attrs"
      :rows="rows"
      :columns="columns"
      :loading="loading"
      :rows-per-page-options="[10, 20, 50]"
      :no-data-label="noDataLabel"
      :pagination="tableLocalPagination"
      flat
      bordered
      binary-state-sort
      @update:pagination="onTablePaginationUpdate"
      @request="onTableRequest"
    >
      <template v-for="(_, name) in $slots" #[name]="slotProps">
        <slot :name="name" v-bind="slotProps ?? {}" />
      </template>

      <template v-if="loading" #loading>
        <q-inner-loading showing>
          <q-spinner-gears size="40px" color="primary" />
        </q-inner-loading>
      </template>

      <template v-if="!loading && rows.length === 0" #no-data>
        <div class="full-width column flex-center q-pa-xl text-grey-6">
          <q-icon name="inbox" size="48px" class="q-mb-sm" />
          <span>{{ noDataLabel }}</span>
          <!-- Boş durumda eyleme yönlendirme (ör. 'İlk kaydı ekle') — sayfa #empty-action slot'uyla verir -->
          <div v-if="$slots['empty-action']" class="q-mt-md">
            <slot name="empty-action" />
          </div>
        </div>
      </template>
    </q-table>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, watch } from 'vue'
import type { QTableProps } from 'quasar'

type QTablePagination = NonNullable<QTableProps['pagination']>

interface Props {
  rows: unknown[]
  columns: QTableProps['columns']
  loading?: boolean
  noDataLabel?: string
  /** Server-side pagination nesnesi. Verilmezse client-side çalışır. */
  pagination?: QTablePagination
  /** Arama kutusunu göster (server-side modda). */
  showSearch?: boolean
  /** Mevcut arama terimi (v-model:search yerine prop + emit). */
  search?: string
}

const props = withDefaults(defineProps<Props>(), {
  loading: false,
  noDataLabel: 'Kayıt bulunamadı',
  pagination: undefined,
  showSearch: false,
  search: '',
})

const emit = defineEmits<{
  request: [props: { pagination: QTablePagination }]
  search: [term: string]
}>()

const isServerSide = computed(() => props.pagination !== undefined)

/**
 * q-table'a verilen local ref — q-table bunu okuyup yazabilir.
 * Parent'tan gelen prop değiştiğinde senkronize edilir.
 */
const tableLocalPagination = ref<QTablePagination>({})

watch(
  () => props.pagination,
  (val) => {
    if (val) {
      tableLocalPagination.value = { ...val }
    }
  },
  { immediate: true, deep: true },
)

function onSearchInput(val: string | number | null) {
  emit('search', String(val ?? ''))
}

/**
 * q-table internal pagination değiştiğinde (sort tıklaması vb.)
 * local ref'i güncelle — böylece q-table header okları doğru gösterilir.
 */
function onTablePaginationUpdate(newPag: QTablePagination) {
  tableLocalPagination.value = { ...tableLocalPagination.value, ...newPag }
}

function onTableRequest(reqProps: { pagination: QTablePagination }) {
  if (isServerSide.value) {
    emit('request', reqProps)
  }
}
</script>
