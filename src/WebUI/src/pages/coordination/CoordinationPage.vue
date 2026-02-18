<template>
  <q-page padding>
    <div class="text-h5 text-weight-bold q-mb-lg">Koordinasyon</div>

    <q-tabs v-model="tab" align="left" class="q-mb-md">
      <q-tab
        v-if="authStore.hasAnyPermission([Permissions.Coordinator.Visit, Permissions.Coordinator.Schedule])"
        name="visits" label="Ziyaretler" icon="directions_walk"
      />
      <q-tab
        v-if="authStore.hasPermission(Permissions.Coordinator.Visit)"
        name="evaluations" label="Değerlendirmeler" icon="rate_review"
      />
      <q-tab
        v-if="authStore.hasPermission(Permissions.Coordinator.Visit)"
        name="exams" label="Beceri Sınavları" icon="quiz"
      />
      <q-tab
        v-if="authStore.hasPermission(Permissions.Coordinator.Report)"
        name="reports" label="Faaliyet Raporları" icon="description"
      />
    </q-tabs>

    <q-tab-panels v-model="tab" animated>
      <!-- ZİYARETLER -->
      <q-tab-panel name="visits">
        <div class="row items-center q-mb-md">
          <div class="col text-subtitle1 text-weight-medium">Rehberlik Ziyaretleri</div>
          <div class="col-auto">
            <PermissionGuard :permission="Permissions.Coordinator.Visit">
              <q-btn color="primary" icon="add" label="Ziyaret Ekle" @click="openVisitDialog" />
            </PermissionGuard>
          </div>
        </div>
        <AppTable :rows="visits" :columns="visitColumns" :loading="loadingVisits">
          <template #body-cell-statusSlug="{ row }">
            <q-td>
              <q-badge
                :color="row.status === 'Approved' ? 'positive' : row.status === 'Submitted' ? 'info' : 'grey'"
                :label="row.status === 'Approved' ? 'Onaylandı' : row.status === 'Submitted' ? 'Gönderildi' : 'Taslak'"
              />
            </q-td>
          </template>
          <template #body-cell-visitDate="{ row }">
            <q-td>{{ formatDate(row.visitDate) }}</q-td>
          </template>
          <template #body-cell-visitActions="{ row }">
            <q-td class="text-right">
              <PermissionGuard :permission="Permissions.Coordinator.Visit">
                <q-btn
                  v-if="row.status === 'Draft'"
                  flat round dense icon="send"
                  color="primary"
                  @click="submitVisit(row)"
                />
              </PermissionGuard>
              <PermissionGuard :permission="Permissions.Coordinator.Schedule">
                <q-btn
                  v-if="row.status === 'Submitted'"
                  flat round dense icon="check"
                  color="positive"
                  @click="approveVisit(row)"
                />
              </PermissionGuard>
            </q-td>
          </template>
        </AppTable>
      </q-tab-panel>

      <!-- DEĞERLENDİRMELER -->
      <q-tab-panel name="evaluations">
        <div class="row items-center q-mb-md">
          <div class="col text-subtitle1 text-weight-medium">İşletme Değerlendirmeleri</div>
          <div class="col-auto">
            <PermissionGuard :permission="Permissions.Coordinator.Visit">
              <q-btn color="primary" icon="add" label="Değerlendirme Ekle" @click="openEvalDialog" />
            </PermissionGuard>
          </div>
        </div>
        <AppTable :rows="evaluations" :columns="evalColumns" :loading="loadingEvals">
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
      </q-tab-panel>

      <!-- BECERİ SINAVLARI -->
      <q-tab-panel name="exams">
        <div class="text-subtitle1 text-weight-medium q-mb-md">Beceri Sınavları</div>
        <AppTable :rows="exams" :columns="examColumns" :loading="loadingExams">
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
      </q-tab-panel>

      <!-- FAALİYET RAPORLARI -->
      <q-tab-panel name="reports">
        <div class="text-subtitle1 text-weight-medium q-mb-md">Aylık Faaliyet Raporları</div>
        <AppTable :rows="activityReports" :columns="reportColumns" :loading="loadingReports">
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
                  flat round dense icon="send" color="primary"
                  @click="submitReport(row)"
                />
                <q-btn
                  v-if="row.status === 'Submitted'"
                  flat round dense icon="check" color="positive"
                  @click="approveReport(row)"
                />
              </PermissionGuard>
            </q-td>
          </template>
        </AppTable>
      </q-tab-panel>
    </q-tab-panels>

    <!-- Ziyaret Ekle Dialog -->
    <q-dialog v-model="visitDialog" persistent>
      <q-card style="min-width: 480px">
        <q-card-section class="row items-center">
          <div class="text-h6">Ziyaret Ekle</div>
          <q-space />
          <q-btn flat round dense icon="close" @click="visitDialog = false" />
        </q-card-section>
        <q-card-section class="q-gutter-sm">
          <q-select
            v-model="visitForm.teacherId"
            :options="teacherOpts.options.value"
            :loading="teacherOpts.loading.value"
            label="Öğretmen *"
            filled
            use-input
            input-debounce="0"
            emit-value
            map-options
            option-label="label"
            option-value="value"
            @filter="teacherOpts.filter"
          >
            <template #option="{ itemProps, opt }">
              <q-item v-bind="itemProps">
                <q-item-section>
                  <q-item-label>{{ opt.label }}</q-item-label>
                </q-item-section>
              </q-item>
            </template>
            <template #no-option>
              <q-item>
                <q-item-section class="text-grey">Sonuç bulunamadı</q-item-section>
              </q-item>
            </template>
          </q-select>
          <q-select
            v-model="visitForm.businessId"
            :options="visitBusinessOpts.options.value"
            :loading="visitBusinessOpts.loading.value"
            label="İşletme *"
            filled
            use-input
            input-debounce="0"
            emit-value
            map-options
            option-label="label"
            option-value="value"
            @filter="visitBusinessOpts.filter"
          >
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
          <q-input v-model="visitForm.visitDate" label="Ziyaret Tarihi" filled type="date" />
          <q-input v-model="visitForm.generalAssessment" label="Genel Değerlendirme" filled type="textarea" rows="3" />
        </q-card-section>
        <q-card-actions align="right">
          <q-btn flat label="İptal" @click="visitDialog = false" />
          <q-btn color="primary" label="Kaydet" :loading="saving" @click="createVisit" />
        </q-card-actions>
      </q-card>
    </q-dialog>

    <!-- Değerlendirme Ekle Dialog -->
    <q-dialog v-model="evalDialog" persistent>
      <q-card style="min-width: 480px">
        <q-card-section class="row items-center">
          <div class="text-h6">Değerlendirme Ekle</div>
          <q-space />
          <q-btn flat round dense icon="close" @click="evalDialog = false" />
        </q-card-section>
        <q-card-section class="q-gutter-sm">
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
          <q-input v-model="evalForm.evaluationDate" label="Değerlendirme Tarihi" filled type="date" />
          <q-select
            v-model="evalForm.result"
            :options="evalResultOptions"
            label="Sonuç"
            filled emit-value map-options
          />
          <q-input v-model="evalForm.notes" label="Notlar" filled type="textarea" rows="2" />
        </q-card-section>
        <q-card-actions align="right">
          <q-btn flat label="İptal" @click="evalDialog = false" />
          <q-btn color="primary" label="Kaydet" :loading="saving" @click="createEvaluation" />
        </q-card-actions>
      </q-card>
    </q-dialog>
  </q-page>
</template>

<script setup lang="ts">
import { ref, onMounted, reactive } from 'vue'
import type { QTableProps } from 'quasar'
import {
  coordinationApi,
  type GuidanceVisitDto,
  type BusinessEvaluationDto,
  type SkillExamDto,
  type MonthlyActivityReportDto,
  EVALUATION_RESULTS,
} from 'src/api/coordination'
import { useNotify } from 'src/composables/useNotify'
import { useTeacherOptions, useBusinessOptions } from 'src/composables/useEntityOptions'
import { Permissions } from 'utils/permissions'
import { useAuthStore } from 'stores/auth'
import AppTable from 'components/AppTable.vue'
import PermissionGuard from 'components/PermissionGuard.vue'

const notify = useNotify()
const authStore = useAuthStore()
const teacherOpts = useTeacherOptions()
const visitBusinessOpts = useBusinessOptions()
const evalBusinessOpts = useBusinessOptions()

const tab = ref('visits')
const saving = ref(false)

const visits = ref<GuidanceVisitDto[]>([])
const evaluations = ref<BusinessEvaluationDto[]>([])
const exams = ref<SkillExamDto[]>([])
const activityReports = ref<MonthlyActivityReportDto[]>([])

const loadingVisits = ref(false)
const loadingEvals = ref(false)
const loadingExams = ref(false)
const loadingReports = ref(false)

const visitDialog = ref(false)
const evalDialog = ref(false)

const evalResultOptions = EVALUATION_RESULTS.map((r) => ({ label: r.label, value: r.value }))

const visitForm = reactive({
  teacherId: '', businessId: '',
  visitDate: '', generalAssessment: '',
})

const evalForm = reactive({
  businessId: '', evaluationDate: '',
  result: 'Suitable', notes: '',
})

const visitColumns: QTableProps['columns'] = [
  { name: 'visitDate', label: 'Ziyaret Tarihi', field: 'visitDate', align: 'left', sortable: true },
  { name: 'businessId', label: 'İşletme ID', field: (row) => (row as GuidanceVisitDto).businessId.slice(0,8) + '…', align: 'left' },
  { name: 'statusSlug', label: 'Durum', field: 'status', align: 'left' },
  { name: 'visitActions', label: '', field: 'id', align: 'right' },
]

const evalColumns: QTableProps['columns'] = [
  { name: 'evaluationDate', label: 'Tarih', field: 'evaluationDate', align: 'left', sortable: true },
  { name: 'businessId', label: 'İşletme ID', field: (row) => (row as BusinessEvaluationDto).businessId.slice(0,8) + '…', align: 'left' },
  { name: 'result', label: 'Sonuç', field: 'result', align: 'left' },
  { name: 'notes', label: 'Notlar', field: (row) => (row as BusinessEvaluationDto).notes ?? '—', align: 'left' },
]

const examColumns: QTableProps['columns'] = [
  { name: 'examDate', label: 'Sınav Tarihi', field: 'examDate', align: 'left', sortable: true },
  { name: 'academicYear', label: 'Yıl', field: 'academicYear', align: 'left' },
  { name: 'semester', label: 'Dönem', field: (row) => (row as SkillExamDto).semester === 'Fall' ? 'Güz' : 'Bahar', align: 'left' },
  { name: 'score', label: 'Puan', field: 'score', align: 'center' },
  { name: 'result', label: 'Sonuç', field: 'result', align: 'left' },
]

const reportColumns: QTableProps['columns'] = [
  { name: 'year', label: 'Yıl', field: 'year', align: 'left' },
  { name: 'month', label: 'Ay', field: 'month', align: 'left' },
  { name: 'status', label: 'Durum', field: 'status', align: 'left' },
  { name: 'reportActions', label: '', field: 'id', align: 'right' },
]

function formatDate(iso: string) {
  return new Date(iso).toLocaleDateString('tr-TR', { day: '2-digit', month: '2-digit', year: 'numeric' })
}

async function loadVisits() {
  loadingVisits.value = true
  try {
    const res = await coordinationApi.listVisits()
    visits.value = res.data
  } catch {
    notify.error('Ziyaretler yüklenirken bir hata oluştu.')
  } finally {
    loadingVisits.value = false
  }
}

async function loadEvaluations() {
  loadingEvals.value = true
  try {
    const res = await coordinationApi.listEvaluations()
    evaluations.value = res.data
  } catch {
    notify.error('Değerlendirmeler yüklenirken bir hata oluştu.')
  } finally {
    loadingEvals.value = false
  }
}

async function loadExams() {
  loadingExams.value = true
  try {
    const res = await coordinationApi.listSkillExams()
    exams.value = res.data
  } catch {
    notify.error('Sınavlar yüklenirken bir hata oluştu.')
  } finally {
    loadingExams.value = false
  }
}

async function loadReports() {
  loadingReports.value = true
  try {
    const res = await coordinationApi.listActivityReports()
    activityReports.value = res.data
  } catch {
    notify.error('Raporlar yüklenirken bir hata oluştu.')
  } finally {
    loadingReports.value = false
  }
}

function openVisitDialog() {
  visitForm.teacherId = ''
  visitForm.businessId = ''
  visitForm.visitDate = ''
  visitForm.generalAssessment = ''
  teacherOpts.reset()
  teacherOpts.load()
  visitBusinessOpts.reset()
  visitBusinessOpts.load()
  visitDialog.value = true
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

async function createVisit() {
  saving.value = true
  try {
    await coordinationApi.createVisit({
      teacherId: visitForm.teacherId,
      businessId: visitForm.businessId,
      institutionId: authStore.user?.institutionId ?? '',
      visitDate: new Date(visitForm.visitDate).toISOString(),
      generalAssessment: visitForm.generalAssessment || undefined,
    })
    notify.success('Ziyaret eklendi.')
    visitDialog.value = false
    await loadVisits()
  } catch {
    notify.error('Ziyaret eklenirken bir hata oluştu.')
  } finally {
    saving.value = false
  }
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
  } catch {
    notify.error('Değerlendirme eklenirken bir hata oluştu.')
  } finally {
    saving.value = false
  }
}

async function submitVisit(row: GuidanceVisitDto) {
  saving.value = true
  try {
    await coordinationApi.submitVisit(row.id)
    notify.success('Ziyaret gönderildi.')
    await loadVisits()
  } catch {
    notify.error('İşlem sırasında bir hata oluştu.')
  } finally {
    saving.value = false
  }
}

async function approveVisit(row: GuidanceVisitDto) {
  saving.value = true
  try {
    await coordinationApi.approveVisit(row.id)
    notify.success('Ziyaret onaylandı.')
    await loadVisits()
  } catch {
    notify.error('İşlem sırasında bir hata oluştu.')
  } finally {
    saving.value = false
  }
}

async function submitReport(row: MonthlyActivityReportDto) {
  saving.value = true
  try {
    await coordinationApi.submitActivityReport(row.id)
    notify.success('Rapor gönderildi.')
    await loadReports()
  } catch {
    notify.error('İşlem sırasında bir hata oluştu.')
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
  } catch {
    notify.error('İşlem sırasında bir hata oluştu.')
  } finally {
    saving.value = false
  }
}

onMounted(() => {
  loadVisits()
  loadEvaluations()
  loadExams()
  loadReports()
})
</script>
