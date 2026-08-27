<template>
  <q-page padding>
    <PageHeader title="Kurumlar" />

    <AppTable
      :rows="institutions"
      :columns="columns"
      :loading="loading"
      :pagination="pagination"
      show-search
      :search="search"
      no-data-label="Kapsamınızda kurum bulunamadı."
      @request="onRequest"
      @search="onSearch"
    >
      <template #filters>
        <q-select
          v-model="nodeTypeFilter"
          :options="nodeTypeOptions"
          label="Kurum Türü"
          outlined
          dense
          emit-value
          map-options
          style="min-width: 220px"
          @update:model-value="load"
        />
      </template>

      <template #body-cell-fullName="{ row }">
        <q-td>
          <div class="text-weight-medium">
            {{ row.fullName }}
          </div>
          <div
            v-if="row.parentName"
            class="text-caption text-grey-7"
          >
            {{ row.parentName }}
          </div>
        </q-td>
      </template>

      <template #body-cell-location="{ row }">
        <q-td>{{ formatLocation(row) }}</q-td>
      </template>

      <template #body-cell-actions="{ row }">
        <q-td class="text-right">
          <q-btn
            flat
            dense
            round
            icon="visibility"
            aria-label="Kurum bilgilerini görüntüle"
            @click="openInstitution(row.id)"
          >
            <q-tooltip>Kurum Bilgilerini Görüntüle</q-tooltip>
          </q-btn>
        </q-td>
      </template>
    </AppTable>
  </q-page>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import type { QTableProps } from 'quasar'
import PageHeader from 'components/PageHeader.vue'
import AppTable from 'components/AppTable.vue'
import { institutionApi, type InstitutionDto } from 'src/api/institution'
import { useServerPagination } from 'src/composables/useServerPagination'

const router = useRouter()

/**
 * Kurum türü süzgeci. Varsayılan OKUL: il yetkilisinin aradığı şey neredeyse her zaman bir
 * okuldur; ilçe müdürlükleri listesi ayrı bir sorudur ve karıştırılırsa okul sayısı yanlış
 * okunur.
 */
const nodeTypeFilter = ref<string>('School')

const nodeTypeOptions = [
  { label: 'Okullar', value: 'School' },
  { label: 'İlçe Müdürlükleri', value: 'District' },
  { label: 'İl Müdürlükleri', value: 'Province' },
]

const filters = computed(() => ({ nodeType: nodeTypeFilter.value }))

const { rows: institutions, loading, pagination, search, onRequest, onSearch, load } =
  useServerPagination<InstitutionDto>({
    fetchFn: (params) => institutionApi.list(params),
    filters,
    defaultSortBy: 'fullName',
  })

const columns: QTableProps['columns'] = [
  { name: 'fullName', label: 'Kurum Adı', field: 'fullName', align: 'left', sortable: true },
  { name: 'institutionCode', label: 'Kurum Kodu', field: 'institutionCode', align: 'left', sortable: true },
  { name: 'location', label: 'İl / İlçe', field: 'provinceName', align: 'left' },
  { name: 'nodeTypeSlug', label: 'Tür', field: 'nodeTypeSlug', align: 'left' },
  { name: 'actions', label: '', field: 'id', align: 'right' },
]

/** İl ve ilçe tek hücrede; ikisi de boş olabilir (künyesi tamamlanmamış kayıt). */
function formatLocation(row: InstitutionDto): string {
  const parcalar = [row.provinceName, row.districtName].filter(Boolean)
  return parcalar.length > 0 ? parcalar.join(' / ') : '—'
}

/**
 * Detay için ayrı sayfa YOK: mevcut kurum sayfası açılır. Yazma butonları orada
 * `institution:manage` ile sarılı olduğundan sayfa il yetkilisinde kendiliğinden salt okunur
 * açılır — ikinci bir yetki kopyası yazılmaz.
 */
function openInstitution(id: string) {
  router.push(`/institutions/${id}`).catch(() => {})
}

onMounted(() => {
  load().catch(() => {})
})
</script>
