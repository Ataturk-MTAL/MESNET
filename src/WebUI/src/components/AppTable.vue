<template>
  <div>
    <!-- Filtre + arama çubuğu (aynı satır: filtreler solda, arama sağda) -->
    <div
      v-if="showSearch || $slots.filters"
      class="row items-center q-gutter-sm q-mb-md"
    >
      <slot name="filters" />
      <q-space />
      <q-input
        v-if="showSearch"
        ref="searchInput"
        :model-value="search"
        dense
        outlined
        placeholder="Ara..."
        aria-label="Listede ara"
        style="min-width: 250px"
        debounce="400"
        @update:model-value="onSearchInput"
      >
        <template #prepend>
          <q-icon name="search" />
        </template>
        <template
          v-if="search"
          #append
        >
          <!-- size verilmez: varsayılan 14px font → .q-btn .q-icon 1.715em = 24px ikon,
               prepend'deki arama ikonuyla (q-field__marginal 24px) aynı optik ağırlık.
               Dokunma hedefi .q-btn--dense.q-btn--round 2.4em × 14px = 33,6px
               (WCAG 2.2 SC 2.5.8 eşiği 24×24 px, payla birlikte geçilir). -->
          <q-btn
            flat
            dense
            round
            icon="close"
            aria-label="Aramayı temizle"
            @click="onClearSearch"
          >
            <q-tooltip>Aramayı temizle</q-tooltip>
          </q-btn>
        </template>
      </q-input>
    </div>

    <!-- İlk yükleme: spinner yerine içerik-şekilli skeleton satırlar (layout-shift'siz) -->
    <div
      v-if="loading && rows.length === 0"
      class="q-mt-xs"
    >
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
      <template
        v-for="(_, name) in $slots"
        #[name]="slotProps"
      >
        <slot
          :name="name"
          v-bind="slotProps ?? {}"
        />
      </template>

      <template
        v-if="loading"
        #loading
      >
        <q-inner-loading showing>
          <q-spinner-gears
            size="40px"
            color="primary"
          />
        </q-inner-loading>
      </template>

      <template
        v-if="!loading && rows.length === 0"
        #no-data
      >
        <div class="full-width column flex-center q-pa-xl text-grey-7">
          <q-icon
            name="inbox"
            size="48px"
            class="q-mb-sm"
          />
          <span>{{ noDataLabel }}</span>
          <!-- Boş durumda eyleme yönlendirme (ör. 'İlk kaydı ekle') — sayfa #empty-action slot'uyla verir -->
          <div
            v-if="$slots['empty-action']"
            class="q-mt-md"
          >
            <slot name="empty-action" />
          </div>
        </div>
      </template>
    </q-table>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, watch, nextTick } from 'vue'
import type { QInput, QTableProps } from 'quasar'

// Kök öğe q-table değil, sarmalayıcı <div>. inheritAttrs açık kalsaydı tanımsız her
// öznitelik (class, dense, selection...) hem köke düşer hem de v-bind="$attrs" ile
// q-table'a geçerdi — çift uygulama. Yalnız q-table alsın.
defineOptions({ inheritAttrs: false })

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

/** Arama girdisi — temizleme sonrası odağı geri vermek için. */
const searchInput = ref<QInput | null>(null)

/**
 * Temizleme butonu `v-if="search"` altındadır: terim boşalınca buton DOM'dan sökülür ve
 * klavye odağı <body>'ye düşer (kullanıcı listenin başına atılır). Emit sonrası odağı
 * arama girdisine geri veriyoruz — DOM yamanmasını beklemek için nextTick.
 */
function onClearSearch() {
  emit('search', '')
  nextTick(() => {
    searchInput.value?.focus()
  }).catch(() => {})
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
