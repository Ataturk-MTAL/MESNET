<template>
  <q-page padding>
    <PageHeader title="Maaş / Dekont" />

    <AppNotice
      v-if="periodStore.isReadOnly"
      type="readonly"
      class="q-mb-md"
      message="Bu dönem kapatılmıştır — yalnızca görüntüleme yapılabilir."
    />

    <!-- Filtreler -->
    <div class="row q-gutter-sm q-mb-md items-center">
      <BranchSelector
        v-model="branchCodeFilter"
        dense
        force-select
        style="min-width: 200px"
      />
      <SearchInput
        v-model="searchFilter"
        label="Öğrenci Adı veya Numarası"
        style="min-width: 220px"
      />
      <q-select
        v-model="phaseFilter"
        :options="phaseOptions"
        label="Aşama"
        outlined
        dense
        emit-value
        map-options
        clearable
        style="min-width: 200px"
      />
      <q-input
        v-model="monthFromFilter"
        label="Başlangıç Ayı"
        outlined
        dense
        clearable
        readonly
        style="min-width: 150px"
      >
        <template #prepend>
          <q-icon name="calendar_month" />
        </template>
        <template #append>
          <q-icon
            name="event"
            class="cursor-pointer"
          >
            <q-popup-proxy
              cover
              transition-show="scale"
              transition-hide="scale"
            >
              <q-date
                v-model="monthFromFilter"
                emit-immediately
                default-view="Months"
                mask="YYYY-MM"
                years-in-month-view
                :options="(d) => !monthToFilter || d <= monthToFilter"
              >
                <div class="row items-center justify-end">
                  <q-btn
                    v-close-popup
                    label="Tamam"
                    color="primary"
                    flat
                  />
                </div>
              </q-date>
            </q-popup-proxy>
          </q-icon>
        </template>
      </q-input>
      <q-input
        v-model="monthToFilter"
        label="Bitiş Ayı"
        outlined
        dense
        clearable
        readonly
        style="min-width: 150px"
      >
        <template #prepend>
          <q-icon name="calendar_month" />
        </template>
        <template #append>
          <q-icon
            name="event"
            class="cursor-pointer"
          >
            <q-popup-proxy
              cover
              transition-show="scale"
              transition-hide="scale"
            >
              <q-date
                v-model="monthToFilter"
                emit-immediately
                default-view="Months"
                mask="YYYY-MM"
                years-in-month-view
                :options="(d) => !monthFromFilter || d >= monthFromFilter"
              >
                <div class="row items-center justify-end">
                  <q-btn
                    v-close-popup
                    label="Tamam"
                    color="primary"
                    flat
                  />
                </div>
              </q-date>
            </q-popup-proxy>
          </q-icon>
        </template>
      </q-input>
      <q-btn
        color="primary"
        icon="search"
        label="Ara"
        unelevated
        @click="load"
      />
    </div>

    <!-- Nadirlik kapısı. Görünen satırların HEPSİ sıradaysa satır rozeti hiçbir şeyi ayırt
         etmez, yalnız "Aşama" sütununu tekrar eder. Bu hâl burada süzgeçle kuruluyor:
         "Aşama" seçicisi `StudentConfirmed` ya da `TeacherApproved`'a ayarlandığında
         `salary:approve` taşıyan kullanıcıda her satır yanardı. Yirmi rozet yerine tek
         cümle. -->
    <AppNotice
      v-if="showAllTurnNotice"
      class="q-mb-md"
      type="info"
      :message="`Bu sayfadaki ${turnRowCount} kaydın tamamı sizin onayınızı bekliyor.`"
    />

    <AppTable
      :rows="payments"
      :columns="columns"
      :loading="loading"
      :pagination="pagination"
      @request="onRequest"
    >
      <template #body-cell-phaseSlug="{ row }">
        <q-td>
          <StatusBadge :slug="row.phaseSlug" />
          <!-- "SIRA SİZDE" — bu ekrandaki TEK hardal (Resmî Hardal) bağlamı.
               NE KANITLIYOR: satırı ilerleten ucun istediği izin (`salary:approve`) bu
               kullanıcıda var. NE KANITLAMIYOR: adımın sahibinin bu kullanıcı olduğu —
               öğretmen ve müdür yardımcısı adımlarının ucu AYNI izni ister
               (PaymentEndpoints.cs:26-27). Bu yüzden ne rozet ne tooltip MAKAM ADI yazar;
               hangi aşamada olduğu zaten solundaki StatusBadge'de okunuyor.
               Nadirlik kapısı `showRowSignal`: satırların yalnız BİR KISMI sıradaysa rozet
               basılır; hepsi sıradaysa yerini sayfa başındaki tek bildirim alır.
               Kontrast (sRGB relative luminance, WCAG 2.x): #796117 → L = 0,1268; beyaza
               karşı 1,05/0,1768 = 5,94:1 (metin eşiği 4,5:1). q-badge metni her zaman #fff
               (QBadge.sass:3). Saf #C9A227 kullanılmadı: beyaz üzerinde 2,42:1 ile grafik
               nesnesi eşiğini (3:1) bile geçemez.
               Renk Yalnız Kanıt Kuralı: rozet metin etiketi taşır — renk ikincil sinyaldir. -->
          <q-badge
            v-if="showRowSignal && isMyTurn(row)"
            color="accent-strong"
            class="text-body2 q-px-sm q-py-xs q-ml-xs"
            label="Sıra sizde"
          >
            <q-tooltip>{{ MY_TURN_TOOLTIP }}</q-tooltip>
          </q-badge>
        </q-td>
      </template>
      <template #body-cell-amounts="{ row }">
        <q-td>
          <div class="text-body2 text-weight-medium">
            {{ formatCurrency(row.netAmount) }}
          </div>
          <div class="text-caption text-grey-7">
            Brüt: {{ formatCurrency(row.baseWage) }}
          </div>
          <!--
            Ay ortasında işletme değişen öğrencide aynı ay için iki satır oluşur (#154).
            Kısmi ay yalnız burada görünür olmazsa tutar "eksik hesaplanmış" gibi okunur.
          -->
          <div
            v-if="row.employedDays < FULL_MONTH_DAYS"
            class="text-caption text-warning-strong"
          >
            Kısmi ay: {{ row.employedDays }} gün
          </div>
        </q-td>
      </template>
      <template #body-cell-actions="{ row }">
        <q-td class="text-right">
          <q-btn
            flat
            round
            dense
            icon="visibility"
            aria-label="Detayları görüntüle"
            @click="openDetail(row)"
          >
            <q-tooltip>Ödeme detayını görüntüle</q-tooltip>
          </q-btn>
        </q-td>
      </template>
    </AppTable>

    <!-- Detay Panel -->
    <DetailPanel
      v-model="detailOpen"
      title="Ödeme Detayı"
      :has-content="!!selected"
    >
      <template v-if="selected">
        <div class="q-gutter-sm">
          <div class="row items-center q-mb-sm">
            <StatusBadge
              :slug="selected.phaseSlug"
              class="q-mr-sm"
            />
            <span class="text-caption">{{ selected.month }}</span>
          </div>

          <!-- Ödeme tablosu -->
          <q-card
            flat
            bordered
            class="q-mb-sm"
          >
            <q-card-section>
              <div class="row justify-between q-mb-xs">
                <span class="text-caption">Baz Ücret:</span>
                <span class="text-weight-medium">{{ formatCurrency(selected.baseWage) }}</span>
              </div>
              <div class="row justify-between q-mb-xs">
                <span class="text-caption">Kesinti:</span>
                <span class="text-negative">-{{ formatCurrency(selected.deductionAmount) }}</span>
              </div>
              <q-separator class="q-my-xs" />
              <div class="row justify-between">
                <span class="text-subtitle2">Net Ücret:</span>
                <span class="text-subtitle2 text-weight-bold text-primary">{{ formatCurrency(selected.netAmount) }}</span>
              </div>
              <div class="row justify-between q-mt-xs">
                <span class="text-caption">Devlet Katkısı:</span>
                <span class="text-caption">{{ formatCurrency(selected.governmentContribution) }}</span>
              </div>
              <div class="row justify-between">
                <span class="text-caption">İşveren Ödemesi:</span>
                <span class="text-caption">{{ formatCurrency(selected.employerPayment) }}</span>
              </div>
            </q-card-section>
          </q-card>

          <!-- Onay zinciri -->
          <div class="text-subtitle2 q-mb-xs">
            Onay Zinciri
          </div>
          <!-- Tamamlanmamış adımın ikonu grey-4 (#e0e0e0, quasar/src/css/variables.sass)
               idi: beyaza karşı 1,32:1 — WCAG 1.4.11 grafik nesnesi eşiğinin (3:1) çok
               altında, ikon fiilen görünmüyordu. Bu ikon anlam taşıyor (check_circle /
               radio_button_unchecked), dolayısıyla eşiğe tabidir. grey-7 (#757575) 4,61:1 —
               TerminationsPage'de aynı gerekçeyle yapılan düzeltmenin eşi.
               Bilgi kaybı zaten yoktu: her adım `subtitle` ile "Bekleniyor" / "Tamamlandı"
               yazıyor (Renk Yalnız Kanıt Kuralı). -->
          <q-timeline
            color="primary"
            layout="dense"
            class="q-mt-xs"
          >
            <q-timeline-entry
              :icon="selected.receiptObjectPath ? 'check_circle' : 'radio_button_unchecked'"
              :color="selected.receiptObjectPath ? 'positive' : 'grey-7'"
              title="Dekont Yüklendi"
              :subtitle="selected.receiptObjectPath ? 'Tamamlandı' : 'Bekleniyor'"
            />
            <q-timeline-entry
              :icon="selected.studentConfirmedAt ? 'check_circle' : 'radio_button_unchecked'"
              :color="selected.studentConfirmedAt ? 'positive' : 'grey-7'"
              title="Öğrenci Onayı"
              :subtitle="selected.studentConfirmedAt ? 'Tamamlandı' : 'Bekleniyor'"
            />
            <q-timeline-entry
              :icon="selected.teacherApprovedAt ? 'check_circle' : 'radio_button_unchecked'"
              :color="selected.teacherApprovedAt ? 'positive' : 'grey-7'"
              title="Öğretmen Onayı"
              :subtitle="selected.teacherApprovedAt ? 'Tamamlandı' : 'Bekleniyor'"
            />
            <q-timeline-entry
              :icon="selected.deputyApprovedAt ? 'check_circle' : 'radio_button_unchecked'"
              :color="selected.deputyApprovedAt ? 'positive' : 'grey-7'"
              title="Müdür Yardımcısı Onayı"
              :subtitle="selected.deputyApprovedAt ? 'Tamamlandı' : 'Bekleniyor'"
            />
          </q-timeline>

          <!-- Eylemler -->
          <div class="q-gutter-sm q-mt-sm">
            <PermissionGuard :permission="Permissions.Company.UploadReceipt">
              <q-btn
                v-if="selected.phase === 'AwaitingReceipt' || selected.phase === 'Calculated'"
                color="secondary"
                icon="upload"
                label="Dekont Yükle (İşletme)"
                unelevated
                @click="uploadReceiptDialog = true; uploadType = 'business'"
              />
            </PermissionGuard>
            <PermissionGuard :permission="Permissions.Salary.Receipt">
              <q-btn
                v-if="selected.phase === 'AwaitingReceipt'"
                color="secondary"
                icon="upload"
                label="Dekont Yükle (Öğrenci)"
                unelevated
                @click="uploadReceiptDialog = true; uploadType = 'student'"
              />
            </PermissionGuard>
            <PermissionGuard :permission="Permissions.Salary.ViewOwn">
              <q-btn
                v-if="selected.phase === 'ReceiptUploaded'"
                color="primary"
                icon="check"
                label="Onayla (Öğrenci)"
                unelevated
                :loading="saving"
                @click="doConfirm"
              />
            </PermissionGuard>
            <PermissionGuard :permission="Permissions.Salary.Approve">
              <q-btn
                v-if="selected.phase === 'StudentConfirmed'"
                color="positive"
                icon="check_circle"
                label="Öğretmen Onayı"
                unelevated
                :loading="saving"
                @click="doApproveTeacher"
              />
              <q-btn
                v-if="selected.phase === 'TeacherApproved'"
                color="positive"
                icon="verified"
                label="Müd. Yrd. Onayı"
                unelevated
                :loading="saving"
                @click="doApproveDeputy"
              />
              <q-btn
                v-if="['ReceiptUploaded','StudentConfirmed','TeacherApproved'].includes(selected.phase)"
                color="negative"
                icon="cancel"
                label="Reddet"
                unelevated
                @click="rejectDialog = true"
              />
            </PermissionGuard>
          </div>
        </div>
      </template>
    </DetailPanel>

    <UploadReceiptForm
      v-model="uploadReceiptDialog"
      :payment-id="selected?.id ?? ''"
      :upload-type="uploadType"
      @saved="afterFormSaved"
    />
    <RejectPaymentForm
      v-model="rejectDialog"
      :payment-id="selected?.id ?? ''"
      @saved="afterFormSaved"
    />
  </q-page>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import type { QTableProps } from 'quasar'
import { paymentApi, type PaymentSummaryDto, PAYMENT_PHASES } from 'src/api/payment'
import { useNotify } from 'src/composables/useNotify'
import { useServerPagination } from 'src/composables/useServerPagination'
import { useAcademicPeriodStore } from 'stores/academicPeriod'
import { useAuthStore } from 'stores/auth'
import { Permissions } from 'utils/permissions'
import DetailPanel from 'components/DetailPanel.vue'
import SearchInput from 'components/SearchInput.vue'
import UploadReceiptForm from 'components/forms/payment/UploadReceiptForm.vue'
import RejectPaymentForm from 'components/forms/payment/RejectPaymentForm.vue'
import AppTable from 'components/AppTable.vue'
import StatusBadge from 'components/StatusBadge.vue'
import PermissionGuard from 'components/PermissionGuard.vue'
import BranchSelector from 'components/BranchSelector.vue'
import AppNotice from 'components/AppNotice.vue'
import PageHeader from 'components/PageHeader.vue'

const notify = useNotify()
const periodStore = useAcademicPeriodStore()
const authStore = useAuthStore()
const saving = ref(false)
const selected = ref<PaymentSummaryDto | null>(null)
const detailOpen = ref(false)
const uploadReceiptDialog = ref(false)
const rejectDialog = ref(false)
const searchFilter = ref('')
const branchCodeFilter = ref<string | null>(null)
const phaseFilter = ref<string | null>(null)
const monthFromFilter = ref('')
const monthToFilter = ref('')
const uploadType = ref<'business' | 'student'>('business')

const phaseOptions = PAYMENT_PHASES.map((p) => ({ label: p.label, value: p.value }))

// Dönem tarihinden YYYY-MM formatı türet
function toYearMonth(dateStr: string): string {
  return dateStr.slice(0, 7) // "2025-09-08" → "2025-09"
}

const filters = computed(() => {
  const period = periodStore.selectedPeriod
  return {
    academicPeriodId: periodStore.selectedPeriodId ?? undefined,
    search: searchFilter.value || undefined,
    branchCode: branchCodeFilter.value ?? undefined,
    phase: phaseFilter.value ?? undefined,
    monthFrom: monthFromFilter.value || (period ? toYearMonth(period.startDate) : undefined),
    monthTo: monthToFilter.value || (period ? toYearMonth(period.endDate) : undefined),
  }
})

const { rows: payments, loading, pagination, onRequest, load } = useServerPagination<PaymentSummaryDto>({
  fetchFn: (params) => paymentApi.list(params),
  filters,
  defaultSortBy: 'month',
  defaultDescending: true,
})

const columns: QTableProps['columns'] = [
  { name: 'studentName', label: 'Öğrenci', field: 'studentName', align: 'left', sortable: true },
  { name: 'studentNumber', label: 'No', field: 'studentNumber', align: 'left' },
  { name: 'branchCode', label: 'Alan', field: 'branchCode', align: 'left', sortable: true },
  { name: 'month', label: 'Ay', field: 'month', align: 'left', sortable: true },
  { name: 'amounts', label: 'Net / Brüt', field: 'netAmount', align: 'left' },
  { name: 'phaseSlug', label: 'Aşama', field: 'phaseSlug', align: 'left' },
  { name: 'actions', label: '', field: 'id', align: 'right' },
]

/** Tam ay gün sayısı — backend `EmploymentDays.FullMonthDays` ile aynı (SGK usulü 30 günlük ay). */
const FULL_MONTH_DAYS = 30

/**
 * "Sıra sizde" sinyalinin kapsadığı aşamalar — dört halkalı dekont zincirinin YALNIZ okul
 * onayı adımları (`PaymentEndpoints.cs:26-27`, ikisi de `Permissions.Salary.Approve` ister).
 *
 * **Diğer iki halka bilerek DIŞARIDA, çünkü koşulu kuran veri istemcide YOK:**
 * - `AwaitingReceipt` / `Calculated` (işletme dekont yükler) → `company:receipt:upload`.
 *   Bu izin `InstitutionManager`'a `company:*` wildcard'ıyla da gider (RolePermissionMap.cs:24),
 *   yani müdürde her satır yanardı. Gerçek aktör `CompanyManager`'ın ise bu listeyi
 *   AÇMA izni yok — rotanın istediği `salary:view` / `salary:view-own` o rolde yok, yani
 *   doğru aktör bu ekranı hiç görmüyor.
 * - `ReceiptUploaded` (öğrenci onaylar) → arayüz butonu `salary:view-own` ile korunuyor ama
 *   uç `salary:receipt:manage` istiyor (`PaymentEndpoints.cs:25`); o izin hiçbir rolde açık
 *   yazılı değil, yalnız müdüre `salary:*` ile geliyor. Öğrenciye "sıra sizde" demek
 *   yapamayacağı işi vaat etmek olurdu.
 *
 * **İZİN, ADIM SAHİPLİĞİNİ KANITLAMAZ — bu yüzden sinyal makam adı yazmaz.** Ölçüldü:
 * `salary:approve` iki adımı da açar ve `RolePermissionMap.cs`'te Teacher (:146),
 * DeputyDirector (:69) ve InstitutionManager'da (`salary:*`, :18) bulunur. Yani koordinatör
 * öğretmen `TeacherApproved` satırında da yanar. Eski tooltip oraya "Müdür yardımcısı onayı
 * sizi bekliyor" yazıyordu; o cümle o kullanıcı için YANLIŞTI ve kaldırıldı.
 * Kesin ayrım SUNUCUDA kurulur: `PaymentSummaryDto`'ya adım sahipliğini hesaplayan bir bayrak
 * eklenmelidir (Payment tarafında `PaidLeaveApprovalPolicy` idiomu zaten var). O gelene kadar
 * ikinci savunma hattı `showRowSignal` nadirlik kapısıdır.
 *
 * **Reddet butonu haritaya KATILMADI:** üç aşamada birden görünür; katılsaydı reddetme yetkisi
 * olan herkeste neredeyse her satır yanar ve nadirlik — yani anlamın kendisi — ölürdü.
 */
const MY_TURN_PHASES: readonly string[] = ['StudentConfirmed', 'TeacherApproved']

/** Makam adı YAZILMAZ — hangi adımın beklendiğini izin kanıtlamıyor (bkz. yukarısı). */
const MY_TURN_TOOLTIP = 'Onayınız bekleniyor'

/**
 * "Sıra sizde" — hardal bu ekranda başka hiçbir anlama gelmez.
 *
 * Kapalı dönemde bastırılır: geçmiş dönemde onay verilemez, yapılamayan iş vaat edilmez.
 */
function isMyTurn(row: PaymentSummaryDto): boolean {
  if (periodStore.isReadOnly) return false
  if (!MY_TURN_PHASES.includes(row.phase)) return false
  return authStore.hasPermission(Permissions.Salary.Approve)
}

/**
 * Nadirlik kapısı — sinyal AYIRT ETTİĞİ yerde durur.
 *
 * Görünür satırların yalnız bir kısmı sıradaysa satır rozeti basılır; hepsi sıradaysa yerini
 * sayfa başındaki tek bildirim alır; hiçbiri sıradaysa hiçbir şey gösterilmez.
 */
const turnRowCount = computed(() => payments.value.filter(isMyTurn).length)
const showRowSignal = computed(
  () => turnRowCount.value > 0 && turnRowCount.value < payments.value.length,
)
const showAllTurnNotice = computed(
  () => payments.value.length > 0 && turnRowCount.value === payments.value.length,
)

function formatCurrency(amount: number) {
  return amount.toLocaleString('tr-TR', { style: 'currency', currency: 'TRY' })
}

function openDetail(row: PaymentSummaryDto) {
  selected.value = row
  detailOpen.value = true
}

async function doConfirm() {
  if (!selected.value) return
  saving.value = true
  try {
    await paymentApi.confirm(selected.value.id)
    notify.success('Ödeme onaylandı.')
    await refreshSelected()
  } catch (e) {
    notify.apiError(e, 'Onaylama sırasında bir hata oluştu.')
  } finally {
    saving.value = false
  }
}

async function doApproveTeacher() {
  if (!selected.value) return
  saving.value = true
  try {
    await paymentApi.approveTeacher(selected.value.id)
    notify.success('Öğretmen onayı verildi.')
    await refreshSelected()
  } catch (e) {
    notify.apiError(e, 'Onaylama sırasında bir hata oluştu.')
  } finally {
    saving.value = false
  }
}

async function doApproveDeputy() {
  if (!selected.value) return
  saving.value = true
  try {
    await paymentApi.approveDeputy(selected.value.id)
    notify.success('Müdür Yardımcısı onayı verildi.')
    await refreshSelected()
  } catch (e) {
    notify.apiError(e, 'Onaylama sırasında bir hata oluştu.')
  } finally {
    saving.value = false
  }
}

async function afterFormSaved() {
  await refreshSelected()
}

async function refreshSelected() {
  if (!selected.value) return
  try {
    const res = await paymentApi.get(selected.value.id)
    selected.value = res.data
  } catch { /* sessiz */ }
  await load()
}

onMounted(load)
</script>
