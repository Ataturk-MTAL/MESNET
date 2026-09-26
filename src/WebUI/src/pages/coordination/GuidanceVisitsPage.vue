<template>
  <q-page padding>
    <PageHeader title="Rehberlik Ziyaretleri">
      <PermissionGuard :permission="Permissions.Coordinator.Visit">
        <q-btn
          unelevated
          color="primary"
          icon="add"
          label="Ziyaret Ekle"
          :disable="periodStore.isReadOnly"
          @click="openNew"
        />
      </PermissionGuard>
    </PageHeader>

    <AppTable
      :rows="visits"
      :columns="visitColumns"
      :loading="loadingVisits"
      :pagination="visitsPagination"
      :error="visitsError"
      no-data-label="Bu dönemde rehberlik ziyareti kaydı yok."
      @request="onVisitsRequest"
      @retry="loadVisits"
    >
      <template #body-cell-status="{ row }">
        <q-td>
          <StatusBadge :slug="statusLabel(row.status)" />
        </q-td>
      </template>
      <template #body-cell-visitActions="{ row }">
        <q-td class="text-right">
          <PermissionGuard :permission="Permissions.Coordinator.Visit">
            <q-btn
              flat
              round
              dense
              :icon="row.status === 'Draft' ? 'edit' : 'visibility'"
              :aria-label="row.status === 'Draft' ? 'Ziyareti düzenle' : 'Ziyareti görüntüle'"
              @click="router.push({ name: 'GuidanceVisitEdit', params: { id: row.id } }).catch(() => {})"
            >
              <q-tooltip>{{ row.status === 'Draft' ? 'Düzenle' : 'Görüntüle' }}</q-tooltip>
            </q-btn>
            <q-btn
              v-if="row.status === 'Draft'"
              flat
              round
              dense
              icon="send"
              color="primary"
              aria-label="Ziyaret raporunu gönder"
              :disable="saving"
              @click="submitVisit(row)"
            >
              <q-tooltip>Onaya gönder</q-tooltip>
            </q-btn>
          </PermissionGuard>
          <!-- Onay ayrı izindir (coordinator:report:manage): ziyareti yapan onaylamaz. -->
          <PermissionGuard :permission="Permissions.Coordinator.Report">
            <q-btn
              v-if="row.status === 'Submitted'"
              flat
              round
              dense
              icon="check"
              color="positive"
              aria-label="Ziyaret raporunu onayla"
              :disable="saving"
              @click="approveVisit(row)"
            >
              <q-tooltip>Onayla</q-tooltip>
            </q-btn>
          </PermissionGuard>
        </q-td>
      </template>
      <template #empty-action>
        <PermissionGuard :permission="Permissions.Coordinator.Visit">
          <q-btn
            outline
            color="primary"
            icon="add"
            label="İlk ziyareti ekle"
            :disable="periodStore.isReadOnly"
            @click="openNew"
          />
        </PermissionGuard>
      </template>
    </AppTable>
  </q-page>
</template>

<script setup lang="ts">
import { ref, computed, watch } from 'vue'
import { useRouter } from 'vue-router'
import type { QTableProps } from 'quasar'
import { coordinationApi, type GuidanceVisitDto } from 'src/api/coordination'
import { useNotify } from 'src/composables/useNotify'
import { useServerPagination } from 'src/composables/useServerPagination'
import { useEntityNames } from 'src/composables/useEntityNames'
import { Permissions } from 'utils/permissions'
import { useAcademicPeriodStore } from 'stores/academicPeriod'
import AppTable from 'components/AppTable.vue'
import PageHeader from 'components/PageHeader.vue'
import PermissionGuard from 'components/PermissionGuard.vue'
import StatusBadge from 'components/StatusBadge.vue'

const router = useRouter()
const notify = useNotify()
const periodStore = useAcademicPeriodStore()

const saving = ref(false)

const visitFilters = computed(() => ({
  academicPeriodId: periodStore.selectedPeriodId ?? undefined,
}))
const { rows: visits, loading: loadingVisits, pagination: visitsPagination, onRequest: onVisitsRequest, load: loadVisits, error: visitsError } =
  useServerPagination<GuidanceVisitDto>({
    fetchFn: (params) => coordinationApi.listVisits(params),
    filters: visitFilters,
    defaultSortBy: 'visitDate',
    defaultDescending: true,
  })

const names = useEntityNames()
names.load().catch(() => {})

const visitColumns: QTableProps['columns'] = [
  {
    name: 'visitDate', label: 'Ziyaret Tarihi', field: 'visitDate', align: 'left', sortable: true,
    format: (iso: string) => new Date(iso).toLocaleDateString('tr-TR', { day: '2-digit', month: '2-digit', year: 'numeric' }),
  },
  { name: 'business', label: 'İşletme', field: (row) => names.businessName((row as GuidanceVisitDto).businessId), align: 'left' },
  {
    name: 'students', label: 'Gözlenen Öğrenci', align: 'center',
    field: (row) => (row as GuidanceVisitDto).studentNotes?.length ?? 0,
  },
  { name: 'status', label: 'Durum', field: 'status', align: 'left' },
  { name: 'visitActions', label: '', field: 'id', align: 'right' },
]

function openNew() {
  router.push({ name: 'GuidanceVisitNew' }).catch(() => {})
}

/** Ziyaret durumunun Türkçe etiketi (backend `VisitStatus` slug'larıyla aynı). */
function statusLabel(status: string) {
  if (status === 'Approved') return 'Onaylandı'
  if (status === 'Submitted') return 'Gönderildi'
  return 'Taslak'
}

async function runRowAction(action: () => Promise<unknown>, success: string) {
  saving.value = true
  try {
    await action()
    notify.success(success)
  } catch (e) {
    notify.apiError(e, 'İşlem sırasında bir hata oluştu.')
    return
  } finally {
    saving.value = false
  }
  await loadVisits().catch(() => {})
}

function submitVisit(row: GuidanceVisitDto) {
  return runRowAction(() => coordinationApi.submitVisit(row.id), 'Ziyaret raporu onaya gönderildi.')
}

function approveVisit(row: GuidanceVisitDto) {
  return runRowAction(() => coordinationApi.approveVisit(row.id), 'Ziyaret raporu onaylandı.')
}

watch(() => periodStore.selectedPeriodId, () => {
  loadVisits().catch(() => {})
})
</script>
