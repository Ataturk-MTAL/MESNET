<template>
  <q-table
    v-bind="$attrs"
    :rows="rows"
    :columns="columns"
    :loading="loading"
    :rows-per-page-options="[10, 20, 50]"
    :no-data-label="noDataLabel"
    flat
    bordered
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
</template>

<script setup lang="ts">
import type { QTableProps } from 'quasar'

interface Props {
  rows: unknown[]
  columns: QTableProps['columns']
  loading?: boolean
  noDataLabel?: string
}

withDefaults(defineProps<Props>(), {
  loading: false,
  noDataLabel: 'Kayıt bulunamadı',
})
</script>
