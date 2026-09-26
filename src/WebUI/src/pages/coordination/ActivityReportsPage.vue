<template>
  <q-page padding>
    <PageHeader title="Aylık Faaliyet Raporları">
      <PermissionGuard :permission="Permissions.Coordinator.Report">
        <q-btn
          unelevated
          color="primary"
          icon="add"
          label="Rapor Oluştur"
          :disable="periodStore.isReadOnly"
          @click="openNew"
        />
      </PermissionGuard>
    </PageHeader>

    <AppTable
      :rows="activityReports"
      :columns="reportColumns"
      :loading="loadingReports"
      :pagination="reportsPagination"
      :error="reportsError"
      no-data-label="Bu dönemde faaliyet raporu yok."
      @request="onReportsRequest"
      @retry="loadReports"
    >
      <template #body-cell-status="{ row }">
        <q-td>
          <StatusBadge :slug="statusLabel(row.status)" />
        </q-td>
      </template>
      <template #body-cell-reportActions="{ row }">
        <q-td class="text-right">
          <PermissionGuard :permission="Permissions.Coordinator.Report">
            <q-btn
              flat
              round
              dense
              :icon="row.status === 'Draft' ? 'edit' : 'visibility'"
              :aria-label="row.status === 'Draft' ? 'Raporu düzenle' : 'Raporu görüntüle'"
              @click="router.push({ name: 'ActivityReportEdit', params: { id: row.id } }).catch(() => {})"
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
              aria-label="Raporu gönder"
              @click="submitReport(row)"
            >
              <q-tooltip>Gönder</q-tooltip>
            </q-btn>
          </PermissionGuard>
          <!-- Onay ucu `internship:manage` ister (raporu yazan öğretmen onaylamaz). Düğme
               eskiden rapor izninin altındaydı: öğretmen görür, tıklar, 403 alırdı. -->
          <PermissionGuard :permission="Permissions.Internship.Manage">
            <q-btn
              v-if="row.status === 'Submitted'"
              flat
              round
              dense
              icon="check"
              color="positive"
              aria-label="Raporu onayla"
              @click="approveReport(row)"
            >
              <q-tooltip>Onayla</q-tooltip>
            </q-btn>
          </PermissionGuard>
        </q-td>
      </template>
      <template #empty-action>
        <PermissionGuard :permission="Permissions.Coordinator.Report">
          <q-btn
            outline
            color="primary"
            icon="add"
            label="İlk raporu oluştur"
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
import { coordinationApi, type MonthlyActivityReportDto } from 'src/api/coordination'
import { useNotify } from 'src/composables/useNotify'
import { useServerPagination } from 'src/composables/useServerPagination'
import { useEntityNames } from 'src/composables/useEntityNames'
import { Permissions } from 'utils/permissions'
import { useAcademicPeriodStore } from 'stores/academicPeriod'
import AppTable from 'components/AppTable.vue'
import PageHeader from 'components/PageHeader.vue'
import StatusBadge from 'components/StatusBadge.vue'
import PermissionGuard from 'components/PermissionGuard.vue'

const router = useRouter()
const notify = useNotify()
const periodStore = useAcademicPeriodStore()

const saving = ref(false)

const reportFilters = computed(() => ({
  academicPeriodId: periodStore.selectedPeriodId ?? undefined,
}))
const { rows: activityReports, loading: loadingReports, pagination: reportsPagination, onRequest: onReportsRequest, load: loadReports, error: reportsError } = useServerPagination<MonthlyActivityReportDto>({
  fetchFn: (params) => coordinationApi.listActivityReports(params),
  filters: reportFilters,
  defaultSortBy: 'month',
  defaultDescending: true,
})

const names = useEntityNames()
names.load().catch(() => {})

const monthFormat = new Intl.DateTimeFormat('tr-TR', { month: 'long' })

const reportColumns: QTableProps['columns'] = [
  { name: 'student', label: 'Öğrenci', field: (row) => names.studentName((row as MonthlyActivityReportDto).studentId), align: 'left' },
  { name: 'business', label: 'İşletme', field: (row) => names.businessName((row as MonthlyActivityReportDto).businessId), align: 'left' },
  { name: 'year', label: 'Yıl', field: 'year', align: 'left' },
  {
    name: 'month', label: 'Ay', field: 'month', align: 'left',
    // Ay adı (ör. "Eylül"); sıralama yine sayısal alan üzerinden çalışır.
    format: (month: number) => monthFormat.format(new Date(2000, month - 1, 1)),
  },
  { name: 'status', label: 'Durum', field: 'status', align: 'left' },
  { name: 'reportActions', label: '', field: 'id', align: 'right' },
]

function openNew() {
  router.push({ name: 'ActivityReportNew' }).catch(() => {})
}

/**
 * Rapor durumunun Türkçe etiketi. DTO `statusSlug` taşımadığı için eşleme burada;
 * renk kararı StatusBadge'in STATUS_COLORS haritasına aittir.
 */
function statusLabel(status: string) {
  if (status === 'Approved') return 'Onaylandı'
  if (status === 'Submitted') return 'Gönderildi'
  return 'Taslak'
}

async function submitReport(row: MonthlyActivityReportDto) {
  saving.value = true
  try {
    await coordinationApi.submitActivityReport(row.id)
    notify.success('Rapor gönderildi.')
    await loadReports()
  } catch (e) {
    notify.apiError(e, 'İşlem sırasında bir hata oluştu.')
  } finally {
    saving.value = false
  }
}

async function approveReport(row: MonthlyActivityReportDto) {
  saving.value = true
  try {
    await coordinationApi.approveActivityReport(row.id)
    notify.success('Rapor onaylandı.')
    await loadReports()
  } catch (e) {
    notify.apiError(e, 'İşlem sırasında bir hata oluştu.')
  } finally {
    saving.value = false
  }
}

watch(() => periodStore.selectedPeriodId, () => {
  loadReports()
})
</script>
