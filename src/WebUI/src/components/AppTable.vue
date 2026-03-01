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

    <q-table
      v-bind="$attrs"
      :rows="rows"
      :columns="columns"
      :loading="loading"
      :rows-per-page-options="[10, 20, 50]"
      :no-data-label="noDataLabel"
      :pagination="localPagination"
      flat
      bordered
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
        </div>
      </template>
    </q-table>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue'
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
 * Server-side modda: parent'tan gelen pagination (rowsNumber sayesinde q-table server moduna geçer).
 * Client-side modda: boş obje — q-table kendi internal pagination'ını kullanır.
 */
const localPagination = computed({
  get: () => (isServerSide.value ? props.pagination! : {} as QTablePagination),
  set: () => { /* parent yönetir, set ignore edilir */ },
})

function onSearchInput(val: string | number | null) {
  emit('search', String(val ?? ''))
}

function onTableRequest(reqProps: { pagination: QTablePagination }) {
  if (isServerSide.value) {
    emit('request', reqProps)
  }
}
</script>
