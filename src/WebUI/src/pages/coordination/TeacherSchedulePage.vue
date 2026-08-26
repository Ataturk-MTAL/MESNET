<template>
  <q-page padding>
    <PageHeader title="Öğretmen Ders Programı" />

    <!-- Filtreler -->
    <div class="row q-col-gutter-md q-mb-lg items-end">
      <div class="col-12 col-sm-6 col-md-5">
        <BranchSelector
          v-model="branchFilter"
          @update:model-value="onBranchChange"
        />
      </div>
      <div class="col-12 col-sm-6 col-md-5">
        <TeacherSelector
          v-model="selectedTeacherId"
          :branch-code="branchFilter"
          @update:model-value="onTeacherChange"
        />
      </div>
    </div>

    <!-- Bilgi Mesajı -->
    <AppNotice
      v-if="!selectedTeacherId"
      type="info"
      message="Ders programını görüntülemek veya düzenlemek için bir öğretmen seçin."
      class="q-mb-md"
    />

    <!-- Schedule Config eksik -->
    <AppNotice
      v-if="scheduleConfigMissing"
      type="warning"
      message="Kurum için günlük ders sayısı ayarlanmamış. Lütfen önce Kurum sayfasından ders programı ayarını yapın."
      class="q-mb-md"
    />

    <!-- Ana İçerik: Grid + Geçmiş Panel -->
    <div
      v-if="selectedTeacherId && periodCount > 0"
      class="row q-col-gutter-md"
    >
      <!-- Sol: Program Grid -->
      <div class="col-12 col-md-8">
        <q-card
          flat
          bordered
          class="q-mb-md"
        >
          <q-card-section>
            <div class="row items-center q-mb-md">
              <div class="col">
                <div class="text-subtitle1 text-weight-medium">
                  Haftalık Program
                  <q-badge
                    v-if="hasExistingSchedule"
                    color="positive"
                    class="q-ml-sm"
                  >
                    Kayıtlı
                    <q-tooltip>Versiyon {{ currentVersion }}</q-tooltip>
                  </q-badge>
                  <!-- Anlamsal durum taşımayan taslak rozeti → bg-neutral (#465a73):
                       beyaz metinle 7,07:1. Quasar "grey" (#9e9e9e) zemininde QBadge'in
                       varsayılan #fff metni 2,68:1'de kalıyordu — ÖLÇÜLDÜ. -->
                  <q-badge
                    v-else
                    color="neutral"
                    class="q-ml-sm"
                  >
                    Yeni
                  </q-badge>
                  <q-badge
                    v-if="viewingHistoryVersion !== null"
                    color="warning"
                    class="q-ml-sm"
                  >
                    Geçmiş: v{{ viewingHistoryVersion }}
                  </q-badge>
                </div>
                <div
                  v-if="currentScheduleMeta"
                  class="text-caption text-grey-7 q-mt-xs"
                >
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
                  <q-btn
                    flat
                    color="grey-7"
                    label="İptal"
                    @click="cancelEditing"
                  />
                  <q-btn
                    unelevated
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
        <q-card
          v-if="hasExistingSchedule && !editing && viewingHistoryVersion === null"
          flat
          bordered
        >
          <q-card-section>
            <div class="text-subtitle1 text-weight-medium q-mb-sm">
              Boş Saat Özeti
            </div>
            <div class="row q-col-gutter-md">
              <div
                v-for="day in dayLabels"
                :key="day.value"
                class="col-12 col-sm"
              >
                <q-card
                  flat
                  bordered
                  class="text-center q-pa-sm"
                >
                  <div class="text-caption text-grey-7">
                    {{ day.label }}
                  </div>
                  <h2 class="text-h6 text-positive-strong q-my-none">
                    {{ freeSlotsPerDay(day.value) }}
                  </h2>
                  <div class="text-caption text-grey-7">
                    boş saat
                  </div>
                </q-card>
              </div>
            </div>
          </q-card-section>
        </q-card>
      </div>

      <!-- Sağ: Değişiklik Geçmişi Paneli -->
      <div class="col-12 col-md-4">
        <q-card
          flat
          bordered
        >
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
                  aria-label="Geçmişi yenile"
                  @click="loadHistory"
                >
                  <q-tooltip>Geçmişi yenile</q-tooltip>
                </q-btn>
              </div>
            </div>

            <!-- Geçmiş: yükleniyor / boş / versiyon listesi -->
            <DataState
              :loading="historyLoading"
              :empty="!scheduleHistory"
              loading-text="Yükleniyor..."
              empty-icon="history"
              empty-text="Henüz kayıtlı program yok"
              padding="q-pa-md"
            >
              <!-- Versiyon Listesi -->
              <q-list
                separator
                dense
              >
                <q-item
                  v-for="ver in (scheduleHistory?.versions ?? []).slice().reverse()"
                  :key="ver.version"
                  clickable
                  :active="viewingHistoryVersion === ver.version"
                  active-class="bg-info-soft"
                  @click="viewVersion(ver)"
                >
                  <q-item-section avatar>
                    <!-- font-size prop'u kaldırıldı: ÖLÇÜLDÜ, hesaplanan değerle birebir
                         aynıydı. size="sm" QAvatar'ın köküne inline font-size:24px basar
                         (useSizeDefaults.sm = 24), Quasar'ın .q-avatar__content kuralı ise
                         .5em uygular → 12px. Yani prop hiçbir şeyi değiştirmiyordu; kaldırmak
                         piksel eşdeğeri ve inline tipografi ihlali de ortadan kalkıyor.
                         (Kök üzerindeki .q-item__section--side > .q-avatar { font-size: 40px }
                         kuralını inline stil zaten eziyor.)

                         Pasif dal grey-5 (#bdbdbd) idi: text-color="white" ile 1,88:1 — 12px
                         rozet metni olduğu için büyük-metin istisnası da yok. bg-neutral
                         (#465a73) beyaz metinle 7,07:1. Aktif dal 'positive' (#2E7D5B) beyazla
                         5,00:1 ile zaten eşiği geçiyor, korundu. ÖLÇÜLDÜ. -->
                    <q-avatar
                      :color="ver.version === scheduleHistory?.currentVersion ? 'positive' : 'neutral'"
                      text-color="white"
                      size="sm"
                    >
                      v{{ ver.version }}
                    </q-avatar>
                  </q-item-section>
                  <q-item-section>
                    <q-item-label>{{ ver.eventType }}</q-item-label>
                    <!-- İki caption da text-grey-8 (#616161) — varsayılan caption rengi
                         SEÇİLİ satırda eşiğin altına düşüyordu. Bu <q-item>
                         active-class="bg-info-soft" taşıyor; o zemin app.css'te
                         color-mix(in srgb, var(--q-info) 12%, #fff) ve düz hex yedeği
                         #e8edf1 ile birebir aynı (--q-info = #3E6B89). Quasar'ın
                         .q-item__label--caption kuralı yarı saydamdır (rgba(0,0,0,.54)),
                         yani ÖNCE zeminle harmanlanır: #e8edf1 üzerinde efektif #6b6d6f →
                         4,41:1, metin eşiği 4,5:1. ÖLÇÜLDÜ.

                         text-grey-8 opaktır ve `color: #616161 !important` ile caption
                         kuralını ezer: beyaz zeminde 6,19:1, #e8edf1 üzerinde 5,25:1 —
                         seçili ve seçili olmayan hâl birlikte temiz, iki kardeş caption
                         aynı tonda. ÖLÇÜLDÜ.

                         Daha açık tona DÖNÜLMEZ: grey-6 (#9e9e9e) beyazda 2,68:1 /
                         #e8edf1 üzerinde 2,27:1; grey-7 (#757575) beyazda 4,61:1 ama
                         #e8edf1 üzerinde 3,91:1 ile eşiğin altında kalır. ÖLÇÜLDÜ. -->
                    <q-item-label
                      caption
                      class="text-grey-8"
                    >
                      {{ formatDateTime(ver.timestamp) }}
                    </q-item-label>
                    <q-item-label
                      caption
                      class="text-grey-8"
                    >
                      {{ ver.updatedByName ?? 'Bilinmiyor' }}
                    </q-item-label>
                  </q-item-section>
                  <q-item-section
                    v-if="ver.version === scheduleHistory?.currentVersion"
                    side
                  >
                    <q-badge
                      color="positive"
                      label="Geçerli"
                    />
                  </q-item-section>
                </q-item>
              </q-list>
            </DataState>
          </q-card-section>
        </q-card>
      </div>
    </div>
  </q-page>
</template>

<script setup lang="ts">
import { ref, watch, onMounted } from 'vue'
import { storeToRefs } from 'pinia'
import {
  coordinationApi,
  type DailyScheduleInput,
  type ScheduleVersionDto,
  type TeacherScheduleDto,
} from 'src/api/coordination'
import { useNotify } from 'src/composables/useNotify'
import { useTeacherScheduleHistory } from 'src/composables/useTeacherScheduleHistory'
import { useTeacherScheduleFormat } from 'src/composables/useTeacherScheduleFormat'
import { useAuthStore } from 'stores/auth'
import { useAcademicPeriodStore } from 'stores/academicPeriod'
import { useInstitutionStore } from 'stores/institution'
import ScheduleGrid from 'components/ScheduleGrid.vue'
import TeacherSelector from 'components/TeacherSelector.vue'
import BranchSelector from 'components/BranchSelector.vue'
import AppNotice from 'components/AppNotice.vue'
import DataState from 'components/DataState.vue'
import PageHeader from 'components/PageHeader.vue'

const notify = useNotify()
const authStore = useAuthStore()
const periodStore = useAcademicPeriodStore()
const institutionStore = useInstitutionStore()

// Ders programı config artık merkezi store cache'inden okunur (doğrudan API çağrısı yok)
const { periodCount, scheduleConfigMissing } = storeToRefs(institutionStore)

const branchFilter = ref<string | null>(null)
const selectedTeacherId = ref<string | null>(null)
const loading = ref(false)
const saving = ref(false)
const editing = ref(false)
const hasExistingSchedule = ref(false)
const currentVersion = ref(0)
const viewingHistoryVersion = ref<number | null>(null)

const scheduleData = ref<DailyScheduleInput[]>([])
const originalScheduleData = ref<DailyScheduleInput[]>([])

const currentScheduleId = ref<string | null>(null)
const currentScheduleMeta = ref<{
  academicYear: number
  semester: string
  updatedAt: string | null
} | null>(null)

// History panel — bağımsız concern composable'a taşındı
const { historyLoading, scheduleHistory, loadHistory } = useTeacherScheduleHistory({
  selectedTeacherId,
  currentScheduleId,
})

// Tarih biçimlendirme yardımcıları
const { formatDate, formatDateTime } = useTeacherScheduleFormat()

const dayLabels = [
  { label: 'Pazartesi', value: 'Monday' },
  { label: 'Salı', value: 'Tuesday' },
  { label: 'Çarşamba', value: 'Wednesday' },
  { label: 'Perşembe', value: 'Thursday' },
  { label: 'Cuma', value: 'Friday' },
]

/** Vue reactive proxy'leri kırarak düz obje kopyası oluşturur (structuredClone uyumsuzluğu için) */
function cloneSchedule(data: DailyScheduleInput[]): DailyScheduleInput[] {
  return data.map((day) => ({
    day: day.day,
    periods: day.periods.map((p) => ({
      periodNumber: p.periodNumber,
      status: p.status,
      courseName: p.courseName || undefined,
    })),
  }))
}

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
    originalScheduleData.value = cloneSchedule(scheduleData.value)
  } catch {
    if (periodCount.value > 0) {
      scheduleData.value = createEmptySchedule(periodCount.value)
      hasExistingSchedule.value = false
      currentVersion.value = 0
      currentScheduleId.value = null
      currentScheduleMeta.value = null
      originalScheduleData.value = cloneSchedule(scheduleData.value)
    }
  } finally {
    loading.value = false
  }
}

function viewVersion(ver: ScheduleVersionDto) {
  if (scheduleHistory.value && ver.version === scheduleHistory.value.currentVersion) {
    // Geçerli versiyona tıklandı — normal moda dön
    viewingHistoryVersion.value = null
    scheduleData.value = cloneSchedule(originalScheduleData.value)
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
      // updatedBy gönderilmez — aktör token'dan damgalanır (#137)
    })
    notify.success('Ders programı kaydedildi.')
    hasExistingSchedule.value = true
    originalScheduleData.value = cloneSchedule(scheduleData.value)
    editing.value = false
  } catch (e) {
    notify.apiError(e, 'Ders programı kaydedilirken hata oluştu.')
  } finally {
    saving.value = false
  }

  // Kayıt sonrası güncel veriyi yeniden yükle (hata olsa bile UI bozulmasın)
  await loadCurrentSchedule()
  loadHistory().catch(() => {})
}

function cancelEditing() {
  scheduleData.value = cloneSchedule(originalScheduleData.value)
  editing.value = false
}

function onBranchChange() {
  selectedTeacherId.value = null
  scheduleData.value = []
  hasExistingSchedule.value = false
  currentVersion.value = 0
  viewingHistoryVersion.value = null
  currentScheduleId.value = null
  currentScheduleMeta.value = null
  scheduleHistory.value = null
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
    loadHistory().catch(() => {})
  }
}

// Drawer'da dönem veya yarıyıl değiştiğinde schedule'ı yeniden yükle
watch(
  () => [periodStore.selectedPeriodId, periodStore.selectedSemester],
  async () => {
    if (selectedTeacherId.value) {
      await loadCurrentSchedule()
      loadHistory().catch(() => {})
    }
  },
)

onMounted(async () => {
  await institutionStore.loadScheduleConfig()
  // Alan ön-seçimi kapsam kaydından gelir, rol adından DEĞİL (ADR-0001, #192).
  // Eski koşul `isDepartmentHead && user.branchCode` idi ve HİÇ tutmuyordu: `branchCode`
  // #126 ile deprecate edilip token'dan kurulurken `null` atanıyor. Ön-seçim sessizce
  // çalışmıyordu; kullanıcı her girişte alanı elle seçiyordu.
  const scopedBranch =
    authStore.writableBranchCodes?.length === 1 ? authStore.writableBranchCodes[0] : null

  if (scopedBranch) {
    branchFilter.value = scopedBranch
  }
})
</script>
