<template>
  <q-page padding>
    <PageHeader title="Beceri Sınavları">
      <PermissionGuard :permission="Permissions.Coordinator.Visit">
        <q-btn
          unelevated
          color="primary"
          icon="add"
          label="Sınav Ekle"
          :disable="periodStore.isReadOnly"
          @click="openNew"
        />
      </PermissionGuard>
    </PageHeader>

    <AppTable
      :rows="exams"
      :columns="examColumns"
      :loading="loadingExams"
      :pagination="examsPagination"
      :error="examsError"
      no-data-label="Bu dönemde beceri sınavı kaydı yok."
      @request="onExamsRequest"
      @retry="loadExams"
    >
      <template #body-cell-result="{ row }">
        <q-td>
          <StatusBadge :slug="row.result === 'Passed' ? 'Başarılı' : 'Başarısız'" />
        </q-td>
      </template>
      <template #body-cell-examDate="{ row }">
        <q-td>{{ formatDate(row.examDate) }}</q-td>
      </template>
      <template #body-cell-examActions="{ row }">
        <q-td class="text-right">
          <PermissionGuard :permission="Permissions.Coordinator.Visit">
            <q-btn
              flat
              round
              dense
              icon="edit"
              aria-label="Sınavı düzenle"
              :disable="periodStore.isReadOnly"
              @click="router.push({ name: 'SkillExamEdit', params: { id: row.id } }).catch(() => {})"
            >
              <q-tooltip>Düzenle</q-tooltip>
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
            label="İlk sınavı ekle"
            :disable="periodStore.isReadOnly"
            @click="openNew"
          />
        </PermissionGuard>
      </template>
    </AppTable>
  </q-page>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { useRouter } from 'vue-router'
import type { QTableProps } from 'quasar'
import { coordinationApi, type SkillExamDto } from 'src/api/coordination'
import { useServerPagination } from 'src/composables/useServerPagination'
import { useEntityNames } from 'src/composables/useEntityNames'
import { useAcademicPeriodStore, semesterOptions } from 'stores/academicPeriod'
import AppTable from 'components/AppTable.vue'
import PageHeader from 'components/PageHeader.vue'
import StatusBadge from 'components/StatusBadge.vue'
import PermissionGuard from 'components/PermissionGuard.vue'
import { Permissions } from 'utils/permissions'

const router = useRouter()
const periodStore = useAcademicPeriodStore()

const examFilters = computed(() => ({
  academicPeriodId: periodStore.selectedPeriodId ?? undefined,
}))
const { rows: exams, loading: loadingExams, pagination: examsPagination, onRequest: onExamsRequest, load: loadExams, error: examsError } = useServerPagination<SkillExamDto>({
  fetchFn: (params) => coordinationApi.listSkillExams(params),
  filters: examFilters,
  defaultSortBy: 'examDate',
  defaultDescending: true,
})

const names = useEntityNames()
names.load().catch(() => {})

const examColumns: QTableProps['columns'] = [
  { name: 'student', label: 'Öğrenci', field: (row) => names.studentName((row as SkillExamDto).studentId), align: 'left' },
  { name: 'business', label: 'İşletme', field: (row) => names.businessName((row as SkillExamDto).businessId), align: 'left' },
  { name: 'examDate', label: 'Sınav Tarihi', field: 'examDate', align: 'left', sortable: true },
  { name: 'academicYear', label: 'Yıl', field: 'academicYear', align: 'left' },
  // MEB terimi: "1. Dönem" / "2. Dönem" (Güz/Bahar değil). Tek kaynak: semesterOptions.
  {
    name: 'semester', label: 'Dönem', align: 'left',
    field: (row) => semesterOptions.find((o) => o.value === (row as SkillExamDto).semester)?.label ?? (row as SkillExamDto).semester,
  },
  { name: 'score', label: 'Puan', field: 'score', align: 'center' },
  { name: 'result', label: 'Sonuç', field: 'result', align: 'left' },
  { name: 'examActions', label: '', field: 'id', align: 'right' },
]

function openNew() {
  router.push({ name: 'SkillExamNew' }).catch(() => {})
}

function formatDate(iso: string) {
  return new Date(iso).toLocaleDateString('tr-TR', { day: '2-digit', month: '2-digit', year: 'numeric' })
}
</script>
