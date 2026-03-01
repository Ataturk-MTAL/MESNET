<template>
  <q-page padding>
    <div class="text-h5 text-weight-bold q-mb-lg">Ogretmen Ders Programi</div>

    <!-- Filtreler -->
    <div class="row q-col-gutter-md q-mb-lg">
      <div class="col-12 col-sm-4">
        <q-select
          v-model="selectedTeacherId"
          :options="teacherOpts.options.value"
          :loading="teacherOpts.loading.value"
          label="Ogretmen"
          filled
          use-input
          input-debounce="0"
          emit-value
          map-options
          option-label="label"
          option-value="value"
          clearable
          @filter="teacherOpts.filter"
        >
          <template #prepend>
            <q-icon name="person" />
          </template>
          <template #no-option>
            <q-item>
              <q-item-section class="text-grey">Sonuc bulunamadi</q-item-section>
            </q-item>
          </template>
        </q-select>
      </div>
      <div class="col-12 col-sm-3">
        <q-select
          v-model="selectedSemester"
          :options="semesterOptions"
          label="Donem"
          filled
          emit-value
          map-options
        >
          <template #prepend>
            <q-icon name="date_range" />
          </template>
        </q-select>
      </div>
      <div class="col-12 col-sm-3">
        <q-input
          v-model.number="selectedYear"
          label="Akademik Yil"
          filled
          type="number"
          :min="2020"
          :max="2050"
        >
          <template #prepend>
            <q-icon name="calendar_today" />
          </template>
        </q-input>
      </div>
      <div class="col-12 col-sm-2 self-center">
        <q-btn
          color="primary"
          icon="search"
          label="Yukle"
          :loading="loading"
          :disable="!selectedTeacherId"
          @click="loadSchedule"
          class="full-width"
        />
      </div>
    </div>

    <!-- Bilgi Mesaji -->
    <q-banner
      v-if="!selectedTeacherId"
      rounded
      class="bg-blue-1 text-blue-9 q-mb-md"
    >
      <template #avatar>
        <q-icon name="info" color="blue-7" />
      </template>
      Ders programini goruntulmek veya duzenlemek icin bir ogretmen secin.
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
      Kurum icin gunluk ders sayisi ayarlanmamis. Lutfen once Kurum sayfasindan ders programi ayarini yapin.
    </q-banner>

    <!-- Grid -->
    <q-card v-if="selectedTeacherId && periodCount > 0" flat bordered class="q-mb-md">
      <q-card-section>
        <div class="row items-center q-mb-md">
          <div class="col text-subtitle1 text-weight-medium">
            Haftalik Program
            <q-badge v-if="hasExistingSchedule" color="green-7" class="q-ml-sm">Kayitli</q-badge>
            <q-badge v-else color="grey" class="q-ml-sm">Yeni</q-badge>
          </div>
          <div class="col-auto q-gutter-sm">
            <q-btn
              v-if="!editing"
              flat
              color="primary"
              icon="edit"
              label="Duzenle"
              :disable="periodStore.isReadOnly"
              @click="editing = true"
            />
            <template v-if="editing">
              <q-btn flat color="grey-7" label="Iptal" @click="cancelEditing" />
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

    <!-- Bos Saatler Ozeti -->
    <q-card v-if="selectedTeacherId && hasExistingSchedule && !editing" flat bordered>
      <q-card-section>
        <div class="text-subtitle1 text-weight-medium q-mb-sm">Bos Saat Ozeti</div>
        <div class="row q-col-gutter-md">
          <div v-for="day in dayLabels" :key="day.value" class="col-12 col-sm">
            <q-card flat bordered class="text-center q-pa-sm">
              <div class="text-caption text-grey-7">{{ day.label }}</div>
              <div class="text-h6 text-green-8">{{ freeSlotsPerDay(day.value) }}</div>
              <div class="text-caption text-grey-6">bos saat</div>
            </q-card>
          </div>
        </div>
      </q-card-section>
    </q-card>
  </q-page>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { coordinationApi, type DailyScheduleInput } from 'src/api/coordination'
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
const selectedSemester = ref('Fall')
const selectedYear = ref(new Date().getFullYear())
const loading = ref(false)
const saving = ref(false)
const editing = ref(false)
const periodCount = ref(0)
const scheduleConfigMissing = ref(false)
const hasExistingSchedule = ref(false)

const scheduleData = ref<DailyScheduleInput[]>([])
let originalScheduleData: DailyScheduleInput[] = []

const semesterOptions = [
  { label: 'Guz', value: 'Fall' },
  { label: 'Bahar', value: 'Spring' },
]

const dayLabels = [
  { label: 'Pazartesi', value: 'Monday' },
  { label: 'Sali', value: 'Tuesday' },
  { label: 'Carsamba', value: 'Wednesday' },
  { label: 'Persembe', value: 'Thursday' },
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

async function loadSchedule() {
  if (!selectedTeacherId.value) return

  loading.value = true
  try {
    const { data } = await coordinationApi.getTeacherSchedule(
      selectedTeacherId.value,
      selectedYear.value,
      selectedSemester.value,
    )

    // Backend schedule donduyse map et
    scheduleData.value = data.weeklySchedule.map((day) => ({
      day: day.day,
      periods: day.periods.map((p) => ({
        periodNumber: p.periodNumber,
        status: p.status,
        courseName: p.courseName ?? undefined,
      })),
    }))
    hasExistingSchedule.value = true
    originalScheduleData = JSON.parse(JSON.stringify(scheduleData.value))
  } catch {
    // Schedule bulunamadi — bos olustur
    if (periodCount.value > 0) {
      scheduleData.value = createEmptySchedule(periodCount.value)
      hasExistingSchedule.value = false
      originalScheduleData = JSON.parse(JSON.stringify(scheduleData.value))
    }
  } finally {
    loading.value = false
  }
}

async function saveSchedule() {
  if (!selectedTeacherId.value) return

  const instId = authStore.user?.institutionId
  const periodId = periodStore.selectedPeriodId
  if (!instId || !periodId) {
    notify.warning('Kurum veya donem bilgisi bulunamadi.')
    return
  }

  saving.value = true
  try {
    await coordinationApi.upsertTeacherSchedule(selectedTeacherId.value, {
      institutionId: instId,
      academicPeriodId: periodId,
      academicYear: selectedYear.value,
      semester: selectedSemester.value,
      weeklySchedule: scheduleData.value,
      updatedBy: authStore.user?.fullName ?? '',
    })
    notify.success('Ders programi kaydedildi.')
    hasExistingSchedule.value = true
    originalScheduleData = JSON.parse(JSON.stringify(scheduleData.value))
    editing.value = false
  } catch (e) {
    notify.apiError(e, 'Ders programi kaydedilirken hata olustu.')
  } finally {
    saving.value = false
  }
}

function cancelEditing() {
  scheduleData.value = JSON.parse(JSON.stringify(originalScheduleData))
  editing.value = false
}

onMounted(async () => {
  await loadScheduleConfig()
  teacherOpts.load()
})
</script>
