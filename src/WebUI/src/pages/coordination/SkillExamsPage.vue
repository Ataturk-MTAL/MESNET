<template>
  <q-page padding>
    <div class="text-h5 text-weight-bold q-mb-lg">
      Beceri Sınavları
    </div>

    <AppTable
      :rows="exams"
      :columns="examColumns"
      :loading="loadingExams"
      :pagination="examsPagination"
      @request="onExamsRequest"
    >
      <template #body-cell-result="{ row }">
        <q-td>
          <q-badge
            :color="row.result === 'Passed' ? 'positive' : 'negative'"
            :label="row.result === 'Passed' ? 'Başarılı' : 'Başarısız'"
          />
        </q-td>
      </template>
      <template #body-cell-examDate="{ row }">
        <q-td>{{ formatDate(row.examDate) }}</q-td>
      </template>
    </AppTable>
  </q-page>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import type { QTableProps } from 'quasar'
import { coordinationApi, type SkillExamDto } from 'src/api/coordination'
import { useServerPagination } from 'src/composables/useServerPagination'
import { useAcademicPeriodStore } from 'stores/academicPeriod'
import AppTable from 'components/AppTable.vue'

const periodStore = useAcademicPeriodStore()

const examFilters = computed(() => ({
  academicPeriodId: periodStore.selectedPeriodId ?? undefined,
}))
const { rows: exams, loading: loadingExams, pagination: examsPagination, onRequest: onExamsRequest } = useServerPagination<SkillExamDto>({
  fetchFn: (params) => coordinationApi.listSkillExams(params),
  filters: examFilters,
  defaultSortBy: 'examDate',
  defaultDescending: true,
})

const examColumns: QTableProps['columns'] = [
  { name: 'examDate', label: 'Sınav Tarihi', field: 'examDate', align: 'left', sortable: true },
  { name: 'academicYear', label: 'Yıl', field: 'academicYear', align: 'left' },
  { name: 'semester', label: 'Dönem', field: (row) => (row as SkillExamDto).semester === 'Fall' ? 'Güz' : 'Bahar', align: 'left' },
  { name: 'score', label: 'Puan', field: 'score', align: 'center' },
  { name: 'result', label: 'Sonuç', field: 'result', align: 'left' },
]

function formatDate(iso: string) {
  return new Date(iso).toLocaleDateString('tr-TR', { day: '2-digit', month: '2-digit', year: 'numeric' })
}
</script>
