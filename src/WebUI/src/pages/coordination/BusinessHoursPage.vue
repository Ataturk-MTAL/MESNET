<template>
  <q-page padding>
    <h1 class="text-h5 text-weight-bold q-mb-xs q-mt-none">
      İşletme Saat Ayarları
    </h1>
    <div class="text-caption text-grey-7 q-mb-lg">
      İşletme dağıtımından ÖNCE yapılır: her işletmeye takdir edilen koordinasyon saati
      burada belirlenir. Toplam, alanın ders yükü havuzunu aşamaz.
    </div>

    <!-- Alan Seçici -->
    <div class="row q-col-gutter-md q-mb-lg items-end">
      <div class="col-12 col-sm-3">
        <!-- Yazma bağlamı (#126): saat dağıtımı kaydedilen sayfa — yetkisiz alan listelenmez -->
        <BranchSelector
          v-model="branchFilter"
          write-context
          @update:model-value="onBranchChange"
        />
      </div>
    </div>

    <AppNotice
      v-if="!branchFilter"
      type="info"
      message="İşletme saatlerini düzenlemek için önce bir alan seçin."
      class="q-mb-md"
    />

    <AppNotice
      v-if="periodStore.isReadOnly"
      type="readonly"
      message="Kapalı dönem — yalnızca görüntüleme modu."
      class="q-mb-md"
    />

    <div v-if="branchFilter">
      <!-- Harita Bölümü -->
      <q-card
        flat
        bordered
        class="q-mb-md"
      >
        <q-card-section>
          <div class="text-subtitle1 text-weight-medium q-mb-sm">
            Harita
          </div>

          <!-- Harita araç çubuğu -->
          <div class="row items-center q-gutter-sm q-mb-md">
            <q-input
              v-model.number="clusterEps"
              type="number"
              label="Yarıçap (m)"
              outlined
              dense
              style="width: 130px"
              :min="100"
              :max="10000"
              :step="100"
            />
            <q-input
              v-model.number="clusterMinPoints"
              type="number"
              label="Min. Nokta"
              outlined
              dense
              style="width: 110px"
              :min="2"
              :max="20"
            />
            <q-btn
              color="primary"
              icon="refresh"
              label="Kümele"
              :loading="clusterLoading"
              @click="loadClusters"
            />
            <q-separator
              vertical
              inset
              class="q-mx-sm"
            />
            <q-btn
              color="secondary"
              icon="route"
              label="Mesafe Hesapla"
              :loading="recalculating"
              :disable="periodStore.isReadOnly"
              @click="recalculateDistances"
            />
            <q-btn
              color="secondary"
              outline
              icon="sync"
              label="Kayıtları Yenile"
              :loading="resyncing"
              :disable="periodStore.isReadOnly"
              @click="resyncCoordinationViews"
            >
              <q-tooltip>
                Koordinasyon kayıtlarını alan bazlı modele göre yeniden kurar; eski
                tek-alanlı kayıtları temizler ve öğrenci sayaçlarını yeniden hesaplar.
              </q-tooltip>
            </q-btn>
          </div>

          <DataState
            :loading="clusterLoading"
            :error="clusterError"
            :empty="clusterData.length === 0"
            loading-text="İşletme kümeleri yükleniyor..."
            padding="q-pa-xl"
            spinner-size="3em"
          >
            <template #error>
              <q-icon
                name="warning"
                size="3em"
                color="warning"
                class="q-mb-sm"
              />
              <div>Kümeleme verisi yüklenemedi.</div>
              <div class="text-caption q-mt-sm">
                PostGIS eklentisi henüz etkin olmayabilir. Sistem yöneticisi ile iletişime geçin.
              </div>
            </template>

            <template #empty>
              <q-icon
                name="location_off"
                size="3em"
                class="q-mb-sm"
              />
              <div>Konum verisi olan işletme bulunamadı.</div>
              <div class="text-caption q-mt-sm">
                İşletmelere koordinat atandıktan sonra harita burada gösterilecek.
              </div>
            </template>

            <!--
              Küme özeti burada TEKRARLANMAZ (#110): BusinessClusterMap kendi özet
              çiplerini marker'larla AYNI paletten üretiyor. Sayfa ikinci bir özet
              basınca aynı küme iki farklı renkte ve iki farklı numara tabanıyla
              (0 vs 1) görünüyordu.
            -->
            <BusinessClusterMap
              :businesses="clusterData"
              :school-location="null"
              :assigned-hours="editedHours"
              :honorary-visits="editedHonorary"
              :editable="!periodStore.isReadOnly"
              height="500px"
              @update:hours="onMapHoursUpdate"
              @update:honorary="onMapHonoraryUpdate"
            />
          </DataState>
        </q-card-section>
      </q-card>

      <!-- Saat Tablosu Bölümü -->
      <q-card
        flat
        bordered
      >
        <q-card-section>
          <div class="text-subtitle1 text-weight-medium q-mb-md">
            İşletme Takdir Edilen Saatler
          </div>

          <!-- Otomatik dağıtım araç çubuğu (#118).
            Öneri KAYDETMEZ: değerleri tabloya doldurur, karar koordinatörde kalır. -->
          <div class="row items-center q-gutter-sm q-mb-md">
            <q-btn
              color="primary"
              icon="auto_awesome"
              label="Otomatik Dağıt"
              :loading="suggesting"
              :disable="!canAutoDistribute || periodStore.isReadOnly"
              @click="autoDistribute"
            >
              <q-tooltip>
                Havuzu ağırlığa (verilebilir maks. saat × öğrenci sayısı) göre dağıtır.
                Kilitli satırlara dokunmaz. Öneri tabloya doldurulur, kaydedilmez.
              </q-tooltip>
            </q-btn>
            <q-btn
              flat
              color="primary"
              icon="undo"
              label="Geri Al"
              :disable="!canUndo || periodStore.isReadOnly"
              @click="undoSuggestion"
            >
              <q-tooltip>Öneriyi uygulamadan önceki saatlere döner.</q-tooltip>
            </q-btn>
            <div
              v-if="pinnedCount > 0"
              class="text-caption text-neutral-strong"
            >
              {{ pinnedCount }} satır kilitli — yeniden dağıtımda korunur.
            </div>
          </div>

          <!-- Tanılama şeridi — "havuz nereye gitti" sorusunun cevabı (#118). -->
          <div
            v-if="diagnostics"
            class="text-body2 bg-neutral-soft text-neutral-strong q-pa-sm rounded-borders q-mb-md"
          >
            Havuz: <strong>{{ diagnostics.pool }}</strong>
            &nbsp;|&nbsp; Öğretmen kapasitesi: <strong>{{ diagnostics.teacherCapacity }}</strong>
            &nbsp;|&nbsp; Σ Maks: <strong>{{ diagnostics.sumOfMax }}</strong>
            &nbsp;|&nbsp; Dağıtılan: <strong>{{ diagnostics.totalAllocated }}</strong>
            &nbsp;|&nbsp; Fahri: <strong>{{ diagnostics.honoraryCount }}</strong> işletme
            &nbsp;|&nbsp; Alan dışı önerilen: <strong>{{ diagnostics.outOfBranchHours }}</strong> saat
          </div>

          <!-- Sessiz kırpma yok: her eksik/aşım ayrı ayrı yazıyla söylenir. -->
          <AppNotice
            v-if="pinnedOverPool"
            type="error"
            class="q-mb-md"
          >
            Kilitli satırların toplamı ders yükü havuzunu <strong>{{ pinnedOverflowHours }}</strong>
            saat aşıyor. Dağıtılacak saat kalmadı — bazı kilitleri açın ya da havuzu artırın.
          </AppNotice>
          <AppNotice
            v-if="hasHonoraryFallback"
            type="warning"
            class="q-mb-md"
          >
            Havuz tüm işletmelere yetmedi:
            <strong>{{ diagnostics?.honoraryCount }}</strong> işletme fahri ziyarete bırakıldı
            (en düşük ağırlıktan başlanarak). Fahri satırlar 0 saatle ve fahri işaretiyle kaydedilir.
          </AppNotice>
          <AppNotice
            v-if="hasOutOfBranchOverflow"
            type="warning"
            class="q-mb-md"
          >
            Alan öğretmenlerinin kalan kapasitesi
            (<strong>{{ diagnostics?.teacherCapacity }}</strong> saat) havuzu karşılamıyor:
            <strong>{{ diagnostics?.outOfBranchHours }}</strong> saat alan dışı öğretmene önerildi.
          </AppNotice>
          <AppNotice
            v-if="hasUndistributedSurplus"
            type="info"
            class="q-mb-md"
          >
            Havuzun <strong>{{ diagnostics?.undistributed }}</strong> saati dağıtılamadı:
            tüm işletmeler mesafe tavanına ulaştı.
          </AppNotice>

          <!-- Uyarı Banner'ları.
            Sıra önemli: havuz tanımsızken aşım/eşik kıyası yapılamaz (#111), o yüzden
            "havuz yok" uyarısı zincirin başında durur. -->
          <AppNotice
            v-if="hoursPoolUndefined && !workloadLoading"
            type="warning"
            class="q-mb-md"
          >
            <div class="row items-center q-col-gutter-sm">
              <div class="col">
                {{ WORKLOAD_POOL_MISSING_MESSAGE }}
                <span v-if="hoursTotalAssigned > 0">
                  Şu an takdir edilen toplam: <strong>{{ hoursTotalAssigned }}</strong> saat.
                </span>
              </div>
              <div class="col-auto">
                <q-btn
                  flat
                  dense
                  no-caps
                  color="warning"
                  icon-right="arrow_forward"
                  label="Ders Yükü Havuzuna Git"
                  :to="{ name: 'WorkloadConfig' }"
                />
              </div>
            </div>
          </AppNotice>
          <AppNotice
            v-else-if="hoursOverLimit"
            type="error"
            class="q-mb-md"
          >
            Toplam takdir edilen saat ({{ hoursTotalAssigned }}) ders yükü havuzunu ({{ hoursWorkloadPool }}) aşıyor!
          </AppNotice>
          <AppNotice
            v-else-if="hoursNearLimit"
            type="warning"
            class="q-mb-md"
          >
            Toplam takdir edilen saat havuza yaklaşıyor: {{ hoursTotalAssigned }} / {{ hoursWorkloadPool }}
          </AppNotice>

          <q-markup-table
            flat
            bordered
            separator="cell"
            class="q-mb-md"
          >
            <thead>
              <tr class="bg-grey-2">
                <th class="text-left">
                  İşletme
                </th>
                <th
                  class="text-center"
                  style="width: 100px"
                >
                  Mesafe
                </th>
                <th
                  class="text-center"
                  style="width: 100px"
                >
                  Öğrenci
                </th>
                <th
                  class="text-center"
                  style="width: 120px"
                >
                  Verilebilir Maks.
                </th>
                <th
                  class="text-center"
                  style="width: 90px"
                >
                  Fahri
                </th>
                <th
                  class="text-center"
                  style="width: 140px"
                >
                  Takdir Edilen
                </th>
                <th
                  class="text-center"
                  style="width: 80px"
                >
                  Kilit
                </th>
              </tr>
            </thead>
            <tbody>
              <tr
                v-for="biz in assignments"
                :key="biz.businessId"
              >
                <td class="text-left">
                  {{ biz.businessName }}
                  <!-- Kayıtlı fahri işareti — ayrım metinle de taşınır, yalnız renkle değil -->
                  <q-badge
                    v-if="biz.isHonoraryVisit"
                    color="neutral-soft"
                    text-color="neutral-strong"
                    class="q-ml-xs"
                    :label="HONORARY_LABEL"
                  >
                    <q-tooltip>{{ HONORARY_HINT }}</q-tooltip>
                  </q-badge>
                  <!-- Kova rozeti — öneri sonrası satırın hangi kovaya düştüğü (#118).
                    Ayrım hem renk (app.css anlamsal tonları) hem METİN ile taşınır. -->
                  <q-badge
                    v-if="bucketBadge(biz.businessId)"
                    :color="bucketBadge(biz.businessId)!.color"
                    :text-color="bucketBadge(biz.businessId)!.textColor"
                    class="q-ml-xs"
                    :label="bucketBadge(biz.businessId)!.label"
                  >
                    <q-tooltip>{{ bucketBadge(biz.businessId)!.hint }}</q-tooltip>
                  </q-badge>
                  <q-btn
                    flat
                    round
                    dense
                    icon="history"
                    color="grey-6"
                    size="xs"
                    class="q-ml-xs"
                    aria-label="Atama geçmişini göster"
                    @click="showHistory(biz.businessId, biz.businessName, biz.branchCode, periodStore.selectedPeriodId ?? '')"
                  >
                    <q-tooltip>Atama geçmişi</q-tooltip>
                  </q-btn>
                </td>
                <td class="text-center text-caption">
                  {{ biz.distanceToSchoolKm != null ? `${biz.distanceToSchoolKm.toFixed(1)} km` : '—' }}
                </td>
                <td class="text-center">
                  {{ biz.activeStudentCount }}
                </td>
                <td class="text-center text-weight-medium text-positive-strong">
                  {{ biz.maxCoordinationHours }}
                </td>
                <td class="text-center">
                  <q-toggle
                    :model-value="editedHonorary[biz.businessId] ?? false"
                    dense
                    color="primary"
                    :disable="periodStore.isReadOnly"
                    :aria-label="`${biz.businessName} — fahri (ücretsiz) ziyaret`"
                    @update:model-value="v => setHonorary(biz.businessId, v)"
                  >
                    <q-tooltip>{{ HONORARY_HINT }}</q-tooltip>
                  </q-toggle>
                </td>
                <td class="text-center">
                  <!-- Fahri satırda saat girişi yok: değer tanım gereği 0 (#115) -->
                  <q-badge
                    v-if="editedHonorary[biz.businessId]"
                    color="neutral-soft"
                    text-color="neutral-strong"
                    label="Fahri — 0 saat"
                  >
                    <q-tooltip>{{ HONORARY_HINT }}</q-tooltip>
                  </q-badge>
                  <q-input
                    v-else
                    v-model.number="editedHours[biz.businessId]"
                    type="number"
                    dense
                    outlined
                    :min="1"
                    :max="biz.maxCoordinationHours"
                    style="max-width: 90px; margin: 0 auto"
                    :disable="periodStore.isReadOnly"
                    :rules="[v => (v > 0 && v <= biz.maxCoordinationHours) || `1-${biz.maxCoordinationHours}`]"
                  />
                </td>
                <td class="text-center">
                  <!-- Satır kilitleme (#118): kilitli satır yeniden dağıtımda korunur,
                    algoritma kalan havuzu kalan satırlara dağıtır. -->
                  <q-btn
                    flat
                    round
                    dense
                    size="sm"
                    :icon="isPinned(biz.businessId) ? 'lock' : 'lock_open'"
                    :color="isPinned(biz.businessId) ? 'primary' : 'grey-6'"
                    :disable="periodStore.isReadOnly"
                    :aria-label="isPinned(biz.businessId)
                      ? `${biz.businessName} — satır kilidini aç`
                      : `${biz.businessName} — satırı kilitle`"
                    :aria-pressed="isPinned(biz.businessId)"
                    @click="togglePin(biz.businessId)"
                  >
                    <q-tooltip>
                      {{ isPinned(biz.businessId)
                        ? 'Kilitli — otomatik dağıtım bu satırı değiştirmez. Açmak için tıklayın.'
                        : 'Satırı kilitle — otomatik dağıtımda bu saat korunur.' }}
                    </q-tooltip>
                  </q-btn>
                </td>
              </tr>
            </tbody>
          </q-markup-table>

          <!-- Özet + Kaydet -->
          <div class="row items-center q-mt-md">
            <div class="text-body2">
              Havuz: <strong :class="workloadPoolToneClass(hoursWorkloadPool)">{{ workloadPoolLabel(hoursWorkloadPool) }}</strong>
              &nbsp;|&nbsp; Σ Takdir: <strong :class="hoursTotalAssignedClass">{{ hoursTotalAssigned }}</strong>
              &nbsp;|&nbsp; Kalan: <strong :class="hoursRemainingClass">{{ hoursRemainingLabel }}</strong>
              &nbsp;|&nbsp; Σ Maks: <strong class="text-grey-6">{{ hoursTotalMaxHours }}</strong>
              <span v-if="honoraryCount > 0">
                &nbsp;|&nbsp; Fahri: <strong class="text-neutral-strong">{{ honoraryCount }}</strong> işletme
                <q-tooltip>Fahri ziyaretler havuz toplamına girmez — ek ders ücreti doğurmaz.</q-tooltip>
              </span>
            </div>
            <q-space />
            <q-btn
              color="primary"
              icon="save"
              label="Saatleri Kaydet"
              :loading="hoursSaving"
              :disable="changedHoursCount === 0 || periodStore.isReadOnly"
              @click="saveHours"
            >
              <q-badge
                v-if="changedHoursCount > 0"
                color="negative"
                floating
              >
                {{ changedHoursCount }}
              </q-badge>
            </q-btn>
          </div>

          <!-- Sonraki adım: saatler kaydedildikten sonra dağıtım yapılır. -->
          <q-separator class="q-my-md" />
          <div class="row items-center">
            <div class="text-caption text-grey-7">
              Saatler kaydedildikten sonra işletmeleri öğretmenlere dağıtabilirsiniz.
            </div>
            <q-space />
            <q-btn
              flat
              color="primary"
              icon-right="arrow_forward"
              label="İşletme Dağıtımına Git"
              :to="{ name: 'BusinessAssignment' }"
            />
          </div>
        </q-card-section>
      </q-card>
    </div>

    <!-- Atama Geçmişi Dialogu -->
    <DetailDialog
      v-model="historyDialog"
      title="Atama Geçmişi"
      position="right"
      full-height
      card-style="min-width: 420px; max-width: 500px"
    >
      <q-card-section class="text-subtitle2 text-grey-7 q-pt-none">
        {{ historyBusinessName }}
      </q-card-section>

      <q-separator />

      <q-card-section
        class="scroll"
        style="max-height: calc(100vh - 140px)"
      >
        <DataState
          :loading="historyLoading"
          :empty="historyEntries.length === 0"
          padding="q-pa-lg"
        >
          <template #empty>
            Henüz geçmiş kaydı bulunmuyor.
          </template>

          <q-timeline
            color="primary"
            layout="dense"
          >
            <q-timeline-entry
              v-for="(entry, idx) in historyEntries"
              :key="idx"
              :icon="historyIcon(entry.action)"
              :color="historyColor(entry.action)"
            >
              <template #subtitle>
                {{ formatDate(entry.timestamp) }} — {{ entry.performedByName ?? 'Bilinmiyor' }}
              </template>
              <div class="text-body2">
                {{ entry.details }}
              </div>
              <div
                v-if="entry.assignedHours"
                class="text-caption text-grey-7"
              >
                Takdir edilen saat: {{ entry.assignedHours }}
              </div>
            </q-timeline-entry>
          </q-timeline>
        </DataState>
      </q-card-section>
    </DetailDialog>
  </q-page>
</template>

<script setup lang="ts">
/**
 * İşletme Saat Ayarları.
 *
 * Daha önce BusinessAssignmentPage içinde "İşletme Saatleri & Harita" sekmesiydi. Saat
 * takdiri dağıtımın ÖN KOŞULU — dağıtılabilecek saatin iki üst sınırı var (işletme başına
 * maxCoordinationHours, toplamda alanın ders yükü havuzu) — ama sekme olarak gömülü olması
 * bu sırayı gizliyordu. Bağımsız sayfaya çıkarıldı ve menüde dağıtımın ÜSTÜNE kondu.
 *
 * Akış: Ders Yükü Havuzu -> İşletme Saat Ayarları -> İşletme Dağıtımı
 */
import { ref, computed, onMounted } from 'vue'
import { coordinationApi, type BusinessAssignmentDto } from 'src/api/coordination'
import { useNotify } from 'src/composables/useNotify'
import { useWorkloadConfig } from 'src/composables/useWorkloadConfig'
import { useAssignedHours } from 'src/composables/useAssignedHours'
import { useHoursSuggestion } from 'src/composables/useHoursSuggestion'
import { useClusterMap } from 'src/composables/useClusterMap'
import { useAssignmentHistory } from 'src/composables/useAssignmentHistory'
import { useAuthStore } from 'stores/auth'
import { useAcademicPeriodStore } from 'stores/academicPeriod'
import {
  WORKLOAD_POOL_MISSING_MESSAGE,
  workloadPoolLabel,
  workloadPoolToneClass,
} from 'src/utils/workloadPool'
import { HONORARY_LABEL, HONORARY_HINT } from 'src/utils/coordinationHours'
import { bucketPresentation } from 'src/utils/allocationBuckets'
import BranchSelector from 'components/BranchSelector.vue'
import BusinessClusterMap from 'components/BusinessClusterMap.vue'
import AppNotice from 'components/AppNotice.vue'
import DataState from 'components/DataState.vue'
import DetailDialog from 'components/DetailDialog.vue'

const notify = useNotify()
const authStore = useAuthStore()
const periodStore = useAcademicPeriodStore()

const branchFilter = ref<string | null>(null)
const loading = ref(false)
const assignments = ref<BusinessAssignmentDto[]>([])

const institutionId = computed(() => authStore.user?.institutionId ?? undefined)
const periodId = computed(() => periodStore.selectedPeriodId)

const workload = useWorkloadConfig({ branchFilter, periodId, institutionId, notify })

const hours = useAssignedHours({
  assignments,
  workloadConfig: workload.workloadConfig,
  academicPeriodId: periodId,
  branchCode: branchFilter,
  notify,
  loadData,
})

const cluster = useClusterMap({ notify, loadData, branchFilter })

const { workloadLoading } = workload

const {
  editedHours, editedHonorary, honoraryCount, setHonorary,
  hoursSaving, hoursTotalMaxHours, hoursWorkloadPool,
  hoursTotalAssigned, hoursTotalAssignedClass,
  hoursRemainingLabel, hoursRemainingClass,
  hoursPoolUndefined, hoursOverLimit, hoursNearLimit,
  changedHoursCount, saveHours,
} = hours

const suggestion = useHoursSuggestion({
  assignments,
  editedHours,
  editedHonorary,
  branchCode: branchFilter,
  academicPeriodId: periodId,
  semester: computed(() => periodStore.selectedSemester),
  poolUndefined: hoursPoolUndefined,
  notify,
})

const {
  suggesting, diagnostics, pinnedCount,
  canAutoDistribute, canUndo,
  hasHonoraryFallback, hasOutOfBranchOverflow, hasUndistributedSurplus,
  pinnedOverPool, pinnedOverflowHours,
  isPinned, togglePin, bucketOf,
  autoDistribute, undoSuggestion, clearSuggestion,
} = suggestion

/** Satırın kova rozeti — öneri yokken null döner, rozet basılmaz. */
function bucketBadge(businessId: string) {
  return bucketPresentation(bucketOf(businessId))
}

const {
  clusterData, clusterLoading, clusterError,
  clusterEps, clusterMinPoints,
  recalculating, resyncing,
  loadClusters, recalculateDistances, resyncCoordinationViews,
} = cluster

const {
  historyDialog, historyLoading, historyBusinessName, historyEntries,
  showHistory, historyIcon, historyColor, formatDate,
} = useAssignmentHistory({ notify })

/**
 * Atamaları çeker, ardından düzenlenebilir saat alanlarını doldurur.
 * `function` bildirimi bilinçli: useAssignedHours/useClusterMap'e yukarıda referans
 * veriliyor, hoisting olmadan tanımsız kalırdı.
 */
async function loadData() {
  if (!branchFilter.value) return
  loading.value = true
  try {
    const assignRes = await coordinationApi.listAssignments({
      branchCode: branchFilter.value,
      academicPeriodId: periodStore.selectedPeriodId ?? undefined,
    })
    assignments.value = assignRes.data ?? []
    hours.initEditedHours()
    // Sunucudan taze veri geldi: eski öneri (rozetler, tanılama, geri al) artık geçersiz.
    // Kilitler korunur — koordinatörün niyeti veri yenilenmesiyle değişmez.
    clearSuggestion()
  } catch (e) {
    notify.apiError(e, 'İşletme listesi yüklenirken hata oluştu.')
  } finally {
    loading.value = false
  }
}

/**
 * Haritadan saat düzenleme — tablodaki alanla aynı state'i yazar.
 * Fahri satır fahri kalır: saat girişi sessizce yok sayılmaz, işaret kaldırılır ve
 * girilen saat uygulanır (kullanıcı saat girdiyse ücretli yapmak istiyordur).
 */
function onMapHoursUpdate(businessId: string, hoursValue: number) {
  if (editedHonorary.value[businessId]) setHonorary(businessId, false)
  editedHours.value[businessId] = hoursValue
}

/** Haritadan fahri işaretleme — tablodaki toggle ile aynı state'i yazar. */
function onMapHonoraryUpdate(businessId: string, value: boolean) {
  setHonorary(businessId, value)
}

async function onBranchChange() {
  if (!branchFilter.value) return
  // Kilitler alana özgüdür — alan değişince öneri de kilitler de sıfırlanır.
  suggestion.resetAll()
  await workload.loadWorkloadConfig()
  await loadData()
  await loadClusters()
}

onMounted(() => {
  if (branchFilter.value) onBranchChange().catch(() => {})
})
</script>
