<template>
  <q-page padding>
    <div class="row items-center q-mb-lg">
      <div class="col text-h5 text-weight-bold">İşletme Değerlendirmeleri</div>
      <div class="col-auto">
        <PermissionGuard :permission="Permissions.Coordinator.Visit">
          <q-btn color="primary" icon="add" label="Değerlendirme Ekle" @click="openEvalDialog" />
        </PermissionGuard>
      </div>
    </div>

    <AppTable :rows="evaluations" :columns="evalColumns" :loading="loadingEvals" :pagination="evalsPagination" @request="onEvalsRequest">
      <template #body-cell-result="{ row }">
        <q-td>
          <q-badge
            :color="row.result === 'Suitable' ? 'positive' : row.result === 'Conditional' ? 'warning' : 'negative'"
            :label="row.result === 'Suitable' ? 'Uygun' : row.result === 'Conditional' ? 'Şartlı' : 'Uygun Değil'"
          />
        </q-td>
      </template>
      <template #body-cell-evaluationDate="{ row }">
        <q-td>{{ formatDate(row.evaluationDate) }}</q-td>
      </template>
    </AppTable>

    <!-- Değerlendirme Ekle Dialog -->
    <q-dialog v-model="evalDialog" persistent :maximized="$q.screen.lt.sm" transition-show="slide-up" transition-hide="slide-down">
      <q-card :style="$q.screen.gt.xs ? 'width: 480px; max-width: 95vw' : ''">
        <q-toolbar class="bg-teal text-white">
          <q-icon name="rate_review" class="q-mr-sm" />
          <q-toolbar-title>Değerlendirme Ekle</q-toolbar-title>
          <q-btn flat round dense icon="close" color="white" v-close-popup />
        </q-toolbar>
        <q-card-section class="q-pt-lg q-gutter-md">
          <q-select
            v-model="evalForm.businessId"
            :options="evalBusinessOpts.options.value"
            :loading="evalBusinessOpts.loading.value"
            label="İşletme *"
            filled
            use-input
            input-debounce="0"
            emit-value
            map-options
            option-label="label"
            option-value="value"
            @filter="evalBusinessOpts.filter"
          >
            <template #prepend>
              <q-icon name="business" />
            </template>
            <template #option="{ itemProps, opt }">
              <q-item v-bind="itemProps">
                <q-item-section>
                  <q-item-label>{{ opt.label }}</q-item-label>
                  <q-item-label caption v-if="opt.caption">{{ opt.caption }}</q-item-label>
                </q-item-section>
              </q-item>
            </template>
            <template #no-option>
              <q-item>
                <q-item-section class="text-grey">Sonuç bulunamadı</q-item-section>
              </q-item>
            </template>
          </q-select>
          <q-input v-model="evalForm.evaluationDate" label="Değerlendirme Tarihi" filled type="date">
            <template #prepend>
              <q-icon name="calendar_today" />
            </template>
          </q-input>
          <q-select
            v-model="evalForm.result"
            :options="evalResultOptions"
            label="Sonuç"
            filled emit-value map-options
          >
            <template #prepend>
              <q-icon name="fact_check" />
            </template>
          </q-select>
          <q-input v-model="evalForm.notes" label="Notlar" filled type="textarea" rows="2">
            <template #prepend>
              <q-icon name="notes" />
            </template>
          </q-input>
        </q-card-section>
        <q-separator />
        <q-card-actions align="right" class="q-pa-md">
          <q-btn flat label="İptal" color="grey-7" v-close-popup />
          <q-btn unelevated color="teal" label="Kaydet" :loading="saving" @click="createEvaluation" />
        </q-card-actions>
      </q-card>
    </q-dialog>
  </q-page>
</template>

<script setup lang="ts">
import { ref, computed, reactive } from 'vue'
import type { QTableProps } from 'quasar'
import { useQuasar } from 'quasar'
import {
  coordinationApi,
  type BusinessEvaluationDto,
  EVALUATION_RESULTS,
} from 'src/api/coordination'
import { useNotify } from 'src/composables/useNotify'
import { useServerPagination } from 'src/composables/useServerPagination'
import { useBusinessOptions } from 'src/composables/useEntityOptions'
import { Permissions } from 'utils/permissions'
import { useAuthStore } from 'stores/auth'
import AppTable from 'components/AppTable.vue'
import PermissionGuard from 'components/PermissionGuard.vue'

const $q = useQuasar()
const notify = useNotify()
const authStore = useAuthStore()
const evalBusinessOpts = useBusinessOptions()

const saving = ref(false)
const evalDialog = ref(false)

const evalFilters = computed(() => ({}))
const { rows: evaluations, loading: loadingEvals, pagination: evalsPagination, onRequest: onEvalsRequest, load: loadEvaluations } = useServerPagination<BusinessEvaluationDto>({
  fetchFn: (params) => coordinationApi.listEvaluations(params),
  filters: evalFilters,
  defaultSortBy: 'evaluationDate',
  defaultDescending: true,
})

const evalResultOptions = EVALUATION_RESULTS.map((r) => ({ label: r.label, value: r.value }))

const evalForm = reactive({
  businessId: '', evaluationDate: '',
  result: 'Suitable', notes: '',
})

const evalColumns: QTableProps['columns'] = [
  { name: 'evaluationDate', label: 'Tarih', field: 'evaluationDate', align: 'left', sortable: true },
  { name: 'businessId', label: 'İşletme ID', field: (row) => (row as BusinessEvaluationDto).businessId.slice(0, 8) + '…', align: 'left' },
  { name: 'result', label: 'Sonuç', field: 'result', align: 'left' },
  { name: 'notes', label: 'Notlar', field: (row) => (row as BusinessEvaluationDto).notes ?? '—', align: 'left' },
]

function formatDate(iso: string) {
  return new Date(iso).toLocaleDateString('tr-TR', { day: '2-digit', month: '2-digit', year: 'numeric' })
}

function openEvalDialog() {
  evalForm.businessId = ''
  evalForm.evaluationDate = ''
  evalForm.result = 'Suitable'
  evalForm.notes = ''
  evalBusinessOpts.reset()
  evalBusinessOpts.load()
  evalDialog.value = true
}

async function createEvaluation() {
  saving.value = true
  try {
    await coordinationApi.createEvaluation({
      businessId: evalForm.businessId,
      institutionId: authStore.user?.institutionId ?? '',
      evaluationDate: new Date(evalForm.evaluationDate).toISOString(),
      result: evalForm.result,
      notes: evalForm.notes || undefined,
    })
    notify.success('Değerlendirme eklendi.')
    evalDialog.value = false
    await loadEvaluations()
  } catch (e) {
    notify.apiError(e, 'Değerlendirme eklenirken bir hata oluştu.')
  } finally {
    saving.value = false
  }
}
</script>
