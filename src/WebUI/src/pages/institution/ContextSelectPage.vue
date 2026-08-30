<template>
  <q-page padding>
    <PageHeader title="Kurum Seç" />

    <AppNotice
      v-if="authStore.user?.activeInstitutionId"
      type="info"
      class="q-mb-md"
    >
      <div class="row items-center q-gutter-sm">
        <!--
          Ad çözülene kadar yer tutucu METİN gösterilmez (bulgu 2, MainLayout'taki header
          çipiyle aynı kural): "seçili kurum" yanlış-tanıdık bir metindir, kullanıcı bir an
          hangi okulda olduğunu okuyamaz. Ad gelene kadar iskelet durur, gelince yerine geçer.
        -->
        <span v-if="institutionStore.institution?.fullName">
          Şu an <strong>{{ institutionStore.institution.fullName }}</strong>
          adına çalışıyorsunuz.
        </span>
        <span
          v-else
          class="row items-center q-gutter-xs"
        >
          Şu an
          <q-skeleton
            type="text"
            width="160px"
          />
          adına çalışıyorsunuz.
        </span>
        <q-btn
          flat
          dense
          no-caps
          color="primary"
          label="Bağlamdan çık"
          :loading="context.switching.value"
          @click="exitContext"
        />
      </div>
    </AppNotice>

    <AppTable
      :rows="institutions"
      :columns="columns"
      :loading="loading"
      :pagination="pagination"
      show-search
      :search="search"
      no-data-label="Yetki alanınızda okul bulunamadı."
      @request="onRequest"
      @search="onSearch"
    >
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
            no-caps
            color="primary"
            label="Bu kuruma geç"
            :loading="context.switching.value"
            @click="switchToInstitution(row.id, row.fullName)"
          />
        </q-td>
      </template>
    </AppTable>
  </q-page>
</template>

<script setup lang="ts">
import { computed, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import type { QTableProps } from 'quasar'
import PageHeader from 'components/PageHeader.vue'
import AppTable from 'components/AppTable.vue'
import AppNotice from 'components/AppNotice.vue'
import { institutionApi, type InstitutionDto } from 'src/api/institution'
import { useServerPagination } from 'src/composables/useServerPagination'
import { useInstitutionContext } from 'src/composables/useInstitutionContext'
import { useNotify, extractApiErrorCode } from 'src/composables/useNotify'
import { useAuthStore } from 'stores/auth'
import { useInstitutionStore } from 'stores/institution'
import {
  DEFAULT_NODE_TYPE_FILTER,
  DEFAULT_SORT_BY,
  buildContextSelectFilters,
  resolveActiveContextErrorMessage,
} from './contextSelectQuery'

const router = useRouter()
const authStore = useAuthStore()
const institutionStore = useInstitutionStore()
const context = useInstitutionContext()
const notify = useNotify()

// Seçim ekranı yalnız OKULLARI listeler (bkz. contextSelectQuery.ts) — süzgeç sabit, il/ilçe
// yetkilisine gösterilecek ikinci bir seçenek yoktur.
const filters = computed(() => buildContextSelectFilters(DEFAULT_NODE_TYPE_FILTER))

const { rows: institutions, loading, pagination, search, onRequest, onSearch, load } =
  useServerPagination<InstitutionDto>({
    fetchFn: (params) => institutionApi.list(params),
    filters,
    defaultSortBy: DEFAULT_SORT_BY,
  })

const columns: QTableProps['columns'] = [
  { name: 'fullName', label: 'Kurum Adı', field: 'fullName', align: 'left', sortable: true },
  { name: 'institutionCode', label: 'Kurum Kodu', field: 'institutionCode', align: 'left', sortable: true },
  { name: 'location', label: 'İl / İlçe', field: 'provinceName', align: 'left' },
  { name: 'actions', label: '', field: 'id', align: 'right' },
]

/** İl ve ilçe tek hücrede; ikisi de boş olabilir (künyesi tamamlanmamış kayıt). */
function formatLocation(row: InstitutionDto): string {
  const parcalar = [row.provinceName, row.districtName].filter(Boolean)
  return parcalar.length > 0 ? parcalar.join(' / ') : '—'
}

/**
 * Bağlamı seçilen okula taşır ve panoya döner. `useInstitutionContext().switchTo` sunucudaki
 * kaydı ve tüm ilgili önbellekleri (kurum/dönem/lookup) tek yerden temizler — burada ikinci
 * bir invalidasyon yazılmaz.
 *
 * `institutionName` yalnız hata mesajı içindir: `Security.ActiveContextOutOfScope` sunucu
 * açıklaması hedef kurumun ham GUID'ini taşır (bkz. `resolveActiveContextErrorMessage`
 * yorumu) — ekrana kimlik değil, satırdan zaten bilinen kurum ADI yazılır.
 */
async function switchToInstitution(institutionId: string, institutionName: string) {
  try {
    await context.switchTo(institutionId)
    router.push('/dashboard').catch(() => {})
  } catch (e) {
    const message = resolveActiveContextErrorMessage(extractApiErrorCode(e), institutionName)
    if (message) {
      notify.error(message)
    } else {
      notify.apiError(e, 'Kuruma geçilirken bir hata oluştu.')
    }
  }
}

/** Kendi düğümüne dönmek bir kurum SEÇMEK değildir — bağlam temizlenir. */
async function exitContext() {
  try {
    await context.switchTo(null)
    router.push('/dashboard').catch(() => {})
  } catch (e) {
    notify.apiError(e, 'Bağlamdan çıkılırken bir hata oluştu.')
  }
}

/**
 * `useServerPagination`'ın filtre izleyicisi `immediate` değildir — bu satır yoksa sayfa
 * kalıcı olarak boş görünür (A parçasında yaşanmış hata).
 */
onMounted(() => {
  load().catch(() => {})
})
</script>
