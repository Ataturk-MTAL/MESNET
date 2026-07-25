<template>
  <q-page padding>
    <div class="text-h5 text-weight-bold q-mb-lg">
      Aylık Faaliyet Raporları
    </div>

    <AppTable
      :rows="activityReports"
      :columns="reportColumns"
      :loading="loadingReports"
      :pagination="reportsPagination"
      @request="onReportsRequest"
    >
      <template #body-cell-status="{ row }">
        <q-td>
          <q-badge
            :color="row.status === 'Approved' ? 'positive' : row.status === 'Submitted' ? 'info' : 'grey'"
            :label="row.status === 'Approved' ? 'Onaylandı' : row.status === 'Submitted' ? 'Gönderildi' : 'Taslak'"
          />
        </q-td>
      </template>
      <template #body-cell-reportActions="{ row }">
        <q-td class="text-right">
          <PermissionGuard :permission="Permissions.Coordinator.Report">
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
    </AppTable>
  </q-page>
</template>

<script setup lang="ts">
import { ref, computed, watch } from 'vue'
import type { QTableProps } from 'quasar'
import { coordinationApi, type MonthlyActivityReportDto } from 'src/api/coordination'
import { useNotify } from 'src/composables/useNotify'
import { useServerPagination } from 'src/composables/useServerPagination'
import { Permissions } from 'utils/permissions'
import { useAcademicPeriodStore } from 'stores/academicPeriod'
import AppTable from 'components/AppTable.vue'
import PermissionGuard from 'components/PermissionGuard.vue'

const notify = useNotify()
const periodStore = useAcademicPeriodStore()

const saving = ref(false)

const reportFilters = computed(() => ({
  academicPeriodId: periodStore.selectedPeriodId ?? undefined,
}))
const { rows: activityReports, loading: loadingReports, pagination: reportsPagination, onRequest: onReportsRequest, load: loadReports } = useServerPagination<MonthlyActivityReportDto>({
  fetchFn: (params) => coordinationApi.listActivityReports(params),
  filters: reportFilters,
  defaultSortBy: 'month',
  defaultDescending: true,
})

const reportColumns: QTableProps['columns'] = [
  { name: 'year', label: 'Yıl', field: 'year', align: 'left' },
  { name: 'month', label: 'Ay', field: 'month', align: 'left' },
  { name: 'status', label: 'Durum', field: 'status', align: 'left' },
  { name: 'reportActions', label: '', field: 'id', align: 'right' },
]

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
