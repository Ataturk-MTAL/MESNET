<template>
  <q-page padding>
    <div class="text-h5 text-weight-bold q-mb-lg">Öğretmen Ders Programı</div>

    <!-- Öğretmen Seçici -->
    <div class="row q-col-gutter-md q-mb-lg">
      <div class="col-12 col-sm-6 col-md-4">
        <q-select
          v-model="selectedTeacherId"
          :options="teacherOpts.options.value"
          :loading="teacherOpts.loading.value"
          label="Öğretmen"
          filled
          use-input
          input-debounce="0"
          emit-value
          map-options
          option-label="label"
          option-value="value"
          clearable
          @filter="teacherOpts.filter"
          @update:model-value="onTeacherChange"
        >
          <template #prepend>
            <q-icon name="person" />
          </template>
          <template #no-option>
            <q-item>
              <q-item-section class="text-grey">Sonuç bulunamadı</q-item-section>
            </q-item>
          </template>
        </q-select>
      </div>
    </div>

    <!-- Bilgi Mesajı -->
    <q-banner
      v-if="!selectedTeacherId"
      rounded
      class="bg-blue-1 text-blue-9 q-mb-md"
    >
      <template #avatar>
        <q-icon name="info" color="blue-7" />
      </template>
      Ders programını görüntülemek veya düzenlemek için bir öğretmen seçin.
    </q-banner>

    <!-- Schedule Config eksik -->
    <q-banner
      v-if="scheduleConfigMissing"
      rounded
      class="bg-orange-1 text-orange-9 q-mb-md"
    >
      <template #avatar>
        <q-icon name="warning" color="orange-7" />
      </template>
      Kurum için günlük ders sayısı ayarlanmamış. Lütfen önce Kurum sayfasından ders programı ayarını yapın.
    </q-banner>

    <!-- Ana İçerik: Grid + Geçmiş Panel -->
    <div v-if="selectedTeacherId && periodCount > 0" class="row q-col-gutter-md">
      <!-- Sol: Program Grid -->
      <div class="col-12 col-md-8">
        <q-card flat bordered class="q-mb-md">
          <q-card-section>
            <div class="row items-center q-mb-md">
              <div class="col">
                <div class="text-subtitle1 text-weight-medium">
                  Haftalık Program
                  <q-badge v-if="hasExistingSchedule" color="green-7" class="q-ml-sm">
                    Kayıtlı
                    <q-tooltip>Versiyon {{ currentVersion }}</q-tooltip>
                  </q-badge>
                  <q-badge v-else color="grey" class="q-ml-sm">Yeni</q-badge>
                  <q-badge v-if="viewingHistoryVersion !== null" color="orange-7" class="q-ml-sm">
                    Geçmiş: v{{ viewingHistoryVersion }}
                  </q-badge>
                </div>
                <div v-if="currentScheduleMeta" class="text-caption text-grey-6 q-mt-xs">
                  {{ currentScheduleMeta.academicYear }} - {{ currentScheduleMeta.semester }}
                  <span v-if="currentScheduleMeta.updatedAt">
                    &middot; Son güncelleme: {{ formatDate(currentScheduleMeta.updatedAt) }}
                  </span>
                </div>
              </div>
              <div class="col-auto q-gutter-sm">
                <q-btn
                  v-if="viewingHistoryVersion !== null"
                  flat
                  color="primary"
                  icon="restore"
                  label="Geçerli Programa Dön"
                  @click="loadCurrentSchedule"
                />
                <q-btn
                  v-if="!editing && viewingHistoryVersion === null"
                  flat
                  color="primary"
                  icon="edit"
                  label="Düzenle"
                  :disable="periodStore.isReadOnly"
                  @click="editing = true"
                />
                <template v-if="editing">
                  <q-btn flat color="grey-7" label="İptal" @click="cancelEditing" />
                  <q-btn
                    color="primary"
                    icon="save"
                    label="Kaydet"
                    :loading="saving"
                    @click="saveSchedule"
                  />
                </template>
              </div>
            </div>

            <ScheduleGrid
              :schedule="scheduleData"
              :period-count="periodCount"
              :editing="editing"
              @update:schedule="scheduleData = $event"
            />
          </q-card-section>
        </q-card>

        <!-- Boş Saatler Özeti -->
        <q-card v-if="hasExistingSchedule && !editing && viewingHistoryVersion === null" flat bordered>
          <q-card-section>
            <div class="text-subtitle1 text-weight-medium q-mb-sm">Boş Saat Özeti</div>
            <div class="row q-col-gutter-md">
              <div v-for="day in dayLabels" :key="day.value" class="col-12 col-sm">
                <q-card flat bordered class="text-center q-pa-sm">
                  <div class="text-caption text-grey-7">{{ day.label }}</div>
                  <div class="text-h6 text-green-8">{{ freeSlotsPerDay(day.value) }}</div>
                  <div class="text-caption text-grey-6">boş saat</div>
                </q-card>
              </div>
            </div>
          </q-card-section>
        </q-card>
      </div>

      <!-- Sağ: Değişiklik Geçmişi Paneli -->
      <div class="col-12 col-md-4">
        <q-card flat bordered>
          <q-card-section>
            <div class="row items-center q-mb-sm">
              <div class="col text-subtitle1 text-weight-medium">
                Değişiklik Geçmişi
              </div>
              <div class="col-auto">
                <q-btn
                  flat
                  dense
                  round
                  icon="refresh"
                  size="sm"
                  :loading="historyLoading"
                  @click="loadHistory"
                >
                  <q-tooltip>Geçmişi yenile</q-tooltip>
                </q-btn>
              </div>
            </div>

            <!-- Geçmiş yükleniyor -->
            <div v-if="historyLoading" class="text-center q-pa-md">
              <q-spinner color="primary" size="2em" />
              <div class="text-caption text-grey-6 q-mt-sm">Yükleniyor...</div>
            </div>

            <!-- Geçmiş yok -->
            <div v-else-if="!scheduleHistory" class="text-center q-pa-md text-grey-6">
              <q-icon name="history" size="2em" class="q-mb-sm" />
              <div class="text-caption">Henüz kayıtlı program yok</div>
            </div>

            <!-- Versiyon Listesi -->
            <q-list v-else separator dense>
              <q-item
                v-for="ver in scheduleHistory.versions.slice().reverse()"
                :key="ver.version"
                clickable
                :active="viewingHistoryVersion === ver.version"
                active-class="bg-blue-1"
                @click="viewVersion(ver)"
              >
                <q-item-section avatar>
                  <q-avatar
                    :color="ver.version === scheduleHistory?.currentVersion ? 'green-7' : 'grey-5'"
                    text-color="white"
                    size="sm"
                    font-size="12px"
                  >
                    v{{ ver.version }}
                  </q-avatar>
                </q-item-section>
                <q-item-section>
                  <q-item-label>{{ ver.eventType }}</q-item-label>
                  <q-item-label caption>
                    {{ formatDateTime(ver.timestamp) }}
                  </q-item-label>
                  <q-item-label caption class="text-grey-6">
                    {{ ver.updatedBy }}
                  </q-item-label>
                </q-item-section>
                <q-item-section side v-if="ver.version === scheduleHistory?.currentVersion">
                  <q-badge color="green-7" label="Geçerli" />
                </q-item-section>
              </q-item>
            </q-list>
          </q-card-section>
        </q-card>
      </div>
    </div>
  </q-page>
</template>

<script setup lang="ts">
import { ref, watch, onMounted } from 'vue'
import {
  coordinationApi,
  type DailyScheduleInput,
  type ScheduleHistoryDto,
  type ScheduleVersionDto,
  type TeacherScheduleDto,
} from 'src/api/coordination'
import { institutionApi } from 'src/api/institution'
import { useTeacherOptions } from 'src/composables/useEntityOptions'
import { useNotify } from 'src/composables/useNotify'
import { useAuthStore } from 'stores/auth'
import { useAcademicPeriodStore } from 'stores/academicPeriod'
import ScheduleGrid from 'components/ScheduleGrid.vue'

const notify = useNotify()
const authStore = useAuthStore()
const periodStore = useAcademicPeriodStore()
const teacherOpts = useTeacherOptions()

const selectedTeacherId = ref<string | null>(null)
const loading = ref(false)
const saving = ref(false)
const editing = ref(false)
const periodCount = ref(0)
const scheduleConfigMissing = ref(false)
const hasExistingSchedule = ref(false)
const currentVersion = ref(0)
const viewingHistoryVersion = ref<number | null>(null)

const scheduleData = ref<DailyScheduleInput[]>([])
let originalScheduleData: DailyScheduleInput[] = []

const currentScheduleId = ref<string | null>(null)
const currentScheduleMeta = ref<{
  academicYear: number
  semester: string
  updatedAt: string | null
} | null>(null)

// History panel
const historyLoading = ref(false)
const scheduleHistory = ref<ScheduleHistoryDto | null>(null)

const dayLabels = [
  { label: 'Pazartesi', value: 'Monday' },
  { label: 'Salı', value: 'Tuesday' },
  { label: 'Çarşamba', value: 'Wednesday' },
  { label: 'Perşembe', value: 'Thursday' },
  { label: 'Cuma', value: 'Friday' },
]

function createEmptySchedule(periodCnt: number): DailyScheduleInput[] {
  return dayLabels.map((day) => ({
    day: day.value,
    periods: Array.from({ length: periodCnt }, (_, i) => ({
      periodNumber: i + 1,
      status: 'Free' as const,
      courseName: undefined,
    })),
  }))
}

function freeSlotsPerDay(dayValue: string): number {
  const day = scheduleData.value.find((d) => d.day === dayValue)
  if (!day) return 0
  return day.periods.filter((p) => p.status === 'Free').length
}

function formatDate(dateStr: string): string {
  return new Date(dateStr).toLocaleDateString('tr-TR', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
  })
}

function formatDateTime(dateStr: string): string {
  return new Date(dateStr).toLocaleString('tr-TR', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  })
}

function mapScheduleToInput(schedule: TeacherScheduleDto): DailyScheduleInput[] {
  return schedule.weeklySchedule.map((day) => ({
    day: day.day,
    periods: day.periods.map((p) => ({
      periodNumber: p.periodNumber,
      status: p.status,
      courseName: p.courseName ?? undefined,
    })),
  }))
}

async function loadScheduleConfig() {
  const instId = authStore.user?.institutionId
  if (!instId) return

  try {
    const { data } = await institutionApi.getScheduleConfig(instId)
    if (data.configured && data.dailyPeriodCount) {
      periodCount.value = data.dailyPeriodCount
      scheduleConfigMissing.value = false
    } else {
      periodCount.value = 0
      scheduleConfigMissing.value = true
    }
  } catch {
    periodCount.value = 0
    scheduleConfigMissing.value = true
  }
}

async function loadCurrentSchedule() {
  if (!selectedTeacherId.value || !periodStore.selectedPeriodId) return

  loading.value = true
  viewingHistoryVersion.value = null
  editing.value = false

  try {
    const { data } = await coordinationApi.getCurrentSchedule(
      selectedTeacherId.value,
      periodStore.selectedPeriodId,
      periodStore.selectedSemester,
    )
    scheduleData.value = mapScheduleToInput(data)
    hasExistingSchedule.value = true
    currentVersion.value = data.version
    currentScheduleId.value = data.id
    currentScheduleMeta.value = {
      academicYear: data.academicYear,
      semester: data.semester,
      updatedAt: data.updatedAt,
    }
    originalScheduleData = JSON.parse(JSON.stringify(scheduleData.value))
  } catch {
    if (periodCount.value > 0) {
      scheduleData.value = createEmptySchedule(periodCount.value)
      hasExistingSchedule.value = false
      currentVersion.value = 0
      currentScheduleId.value = null
      currentScheduleMeta.value = null
      originalScheduleData = JSON.parse(JSON.stringify(scheduleData.value))
    }
  } finally {
    loading.value = false
  }
}

async function loadHistory() {
  if (!selectedTeacherId.value || !currentScheduleId.value) {
    scheduleHistory.value = null
    return
  }

  historyLoading.value = true
  try {
    const { data } = await coordinationApi.getScheduleHistory(
      selectedTeacherId.value,
      currentScheduleId.value,
    )
    scheduleHistory.value = data
  } catch {
    scheduleHistory.value = null
  } finally {
    historyLoading.value = false
  }
}

function viewVersion(ver: ScheduleVersionDto) {
  if (scheduleHistory.value && ver.version === scheduleHistory.value.currentVersion) {
    // Geçerli versiyona tıklandı — normal moda dön
    viewingHistoryVersion.value = null
    scheduleData.value = JSON.parse(JSON.stringify(originalScheduleData))
    return
  }

  // Geçmiş versiyonu göster (salt okunur)
  editing.value = false
  viewingHistoryVersion.value = ver.version
  scheduleData.value = ver.weeklySchedule.map((day) => ({
    day: day.day,
    periods: day.periods.map((p) => ({
      periodNumber: p.periodNumber,
      status: p.status,
      courseName: p.courseName ?? undefined,
    })),
  }))
}

async function saveSchedule() {
  if (!selectedTeacherId.value) return

  if (periodStore.isReadOnly) {
    notify.warning('Geçmiş dönemde değişiklik yapılamaz.')
    return
  }

  const instId = authStore.user?.institutionId
  const periodId = periodStore.selectedPeriodId
  if (!instId || !periodId) {
    notify.warning('Kurum veya dönem bilgisi bulunamadı.')
    return
  }

  // academicYear ve semester: her zaman store'dan (drawer'daki seçili dönem/yarıyıl)
  const academicYear = periodStore.academicYear
  const semester = periodStore.selectedSemester

  saving.value = true
  try {
    await coordinationApi.upsertTeacherSchedule(selectedTeacherId.value, {
      institutionId: instId,
      academicPeriodId: periodId,
      academicYear,
      semester,
      weeklySchedule: scheduleData.value,
      updatedBy: authStore.user?.fullName ?? '',
    })
    notify.success('Ders programı kaydedildi.')
    hasExistingSchedule.value = true
    originalScheduleData = JSON.parse(JSON.stringify(scheduleData.value))
    editing.value = false

    // Önce güncel schedule'ı yükle (scheduleId'yi set eder), sonra geçmişi
    await loadCurrentSchedule()
    await loadHistory()
  } catch (e) {
    notify.apiError(e, 'Ders programı kaydedilirken hata oluştu.')
  } finally {
    saving.value = false
  }
}

function cancelEditing() {
  scheduleData.value = JSON.parse(JSON.stringify(originalScheduleData))
  editing.value = false
}

async function onTeacherChange(teacherId: string | null) {
  // Öğretmen değiştiğinde state'i sıfırla
  scheduleData.value = []
  hasExistingSchedule.value = false
  currentVersion.value = 0
  viewingHistoryVersion.value = null
  currentScheduleId.value = null
  currentScheduleMeta.value = null
  scheduleHistory.value = null
  editing.value = false

  if (teacherId) {
    await loadCurrentSchedule()
    loadHistory()
  }
}

// Drawer'da dönem veya yarıyıl değiştiğinde schedule'ı yeniden yükle
watch(
  () => [periodStore.selectedPeriodId, periodStore.selectedSemester],
  async () => {
    if (selectedTeacherId.value) {
      await loadCurrentSchedule()
      loadHistory()
    }
  },
)

onMounted(async () => {
  await loadScheduleConfig()
  teacherOpts.load()
})
</script>
