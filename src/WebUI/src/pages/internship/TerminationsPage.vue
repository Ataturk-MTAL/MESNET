<template>
  <q-page padding>
    <PageHeader
      title="Fesih Süreçleri"
      icon="gavel"
      subtitle="Onay zinciri devam eden staj fesihleri"
    />

    <AppNotice
      v-if="periodStore.isReadOnly"
      class="q-mb-md"
      type="info"
      message="Geçmiş dönem salt okunurdur — onay verilemez."
    />

    <!-- Nadirlik kapısı. Görünen satırların HEPSİ sıradaysa satır rozeti hiçbir şeyi ayırt
         etmez, yalnız "Sıradaki adım" sütununu tekrar eder. Bu liste zaten yalnız
         `TerminationInProgress` satırlarını getiriyor (filters.phase) ve müdür
         (`internship:*`, RolePermissionMap.cs:16) ile müdür yardımcısı
         (RolePermissionMap.cs:64-65) zincirin üç adımının da iznini taşır — o iki rolde
         açık her satır yanardı. Yirmi rozet yerine tek cümle. -->
    <AppNotice
      v-if="showAllTurnNotice"
      class="q-mb-md"
      type="info"
      :message="`Bu sayfadaki ${turnRowCount} fesih kaydının tamamı sizin onayınızı bekliyor.`"
    />

    <AppTable
      :rows="rows"
      :columns="columns"
      :loading="loading"
      row-key="id"
      :pagination="pagination"
      show-search
      :search="search"
      @request="onRequest"
      @search="onSearch"
    >
      <template #body-cell-next="props">
        <q-td :props="props">
          <q-skeleton
            v-if="chainOf(props.row.id) === undefined"
            type="text"
            width="120px"
          />
          <!-- Zincirin istisna hâli (override) DOLU çiptir; normal bekleme hâli OUTLINE
               kalır. Üçü de dolu olsaydı sütunda üç hardal-ailesi zemin yan yana dururdu:
               "Override edildi" #785300 (L=0,1018), "Sıra sizde" #796117 (L=0,1268), bekleme
               #9A6B00 (L=0,1739). Ölçüldü (sRGB relative luminance, WCAG 2.x): #796117 ile
               #785300 arası 1,16:1, #796117 ile #9A6B00 arası 1,27:1 — hardal sinyalini
               ayakta tutan şey nadirliği ve ayrışmasıdır, üç dolu zemin ikisini de öldürür.
               Ayrım fill/outline + ikon (bolt / schedule) + etiket metniyle kurulur. -->
          <q-chip
            v-else-if="chainOf(props.row.id)?.chain?.isOverridden"
            dense
            color="status-warning"
            text-color="white"
            icon="bolt"
            label="Override edildi"
          />
          <q-chip
            v-else-if="!nextStepOf(props.row.id)"
            dense
            color="positive"
            text-color="white"
            icon="check"
            label="Onaylar tamam"
          />
          <template v-else>
            <!-- Bekleme çipinin rengi tema değişkenidir: `warning` → `.text-warning
                 { color: var(--q-warning) }` (quasar.css), yani #9A6B00
                 (assets/quasar-variables.sass:26). Ham `orange-9` bırakılmadı — temadan
                 bağımsızdır, DESIGN.md "Türetme Kuralı"na aykırıdır.
                 `status-pending` KULLANILAMAZ: app.css'te yalnız `.bg-status-pending` var,
                 `.text-status-pending` YOK; QChip outline modda `bg-` değil `text-${color}`
                 sınıfı basar (QChip.js:100-110), yani çip tümüyle renksiz kalırdı.
                 Ölçüldü (beyaz zemin): #9A6B00 = 4,69:1 — metin eşiğini (4,5:1) de kenarlık
                 için grafik nesnesi eşiğini (3:1) de geçer. -->
            <q-chip
              dense
              outline
              color="warning"
              icon="schedule"
              :label="nextStepOf(props.row.id)?.slug"
            />
            <!-- "SIRA SİZDE" — bu ekrandaki TEK hardal (Resmî Hardal) bağlamı.
                 NE KANITLIYOR: sıradaki adımın kendi `permission` alanı (sunucudan gelir,
                 ADR-0001) bu kullanıcıda var — yani satırı ilerleten uç bu kullanıcıya açık.
                 NE KANITLAMIYOR: adımın sahibinin bu kullanıcı olduğu.
                 ÖLÇÜLDÜ (TerminationChainPolicy.cs:19 ve :22): Teacher ve Deputy adımlarının
                 izni AYNI — `internship:approve`. O izin RolePermissionMap.cs'te Teacher
                 (:135), DeputyDirector (:65) ve InstitutionManager'da (`internship:*`, :16)
                 var. Bu yüzden rozet MAKAM ADI YAZMAZ: eski "Sıra sizde — Müdür Yardımcısı
                 onayı" etiketi koordinatör öğretmene de görünüyordu ve onun için YANLIŞ bir
                 iddiaydı. Hangi adımın beklendiği solundaki çipte durur; rozet yalnız
                 "sizde iş var" der.
                 Kesin çözüm SUNUCUDADIR: GetTerminationChainHandler.cs:44 adımı zaten
                 üretiyor; satır başına `isActionableByMe` eklenirse koşul kesinleşir.
                 Nadirlik kapısı `showRowSignal`: satırların yalnız BİR KISMI sıradaysa rozet
                 basılır; hepsi sıradaysa sayfa başındaki tek bildirim devreye girer.
                 Kontrast (sRGB relative luminance, WCAG 2.x): #796117 → L = 0,1268; beyaza
                 karşı 1,05/0,1768 = 5,94:1 (metin eşiği 4,5:1). q-badge metni her zaman #fff
                 (QBadge.sass:3), yani oran garantidir. Saf #C9A227 kullanılmaz: beyaz
                 üzerinde 2,42:1 ile grafik nesnesi eşiğini (3:1) bile geçemez. -->
            <q-badge
              v-if="showRowSignal && isMyTurn(props.row.id)"
              color="accent-strong"
              class="text-body2 q-px-sm q-py-xs q-ml-xs"
              label="Sıra sizde"
            />
          </template>
        </q-td>
      </template>

      <template #body-cell-actions="props">
        <q-td
          :props="props"
          class="text-right"
        >
          <q-btn
            flat
            dense
            round
            icon="visibility"
            aria-label="Onay zincirini aç"
            @click="openChain(props.row)"
          >
            <q-tooltip>Onay zincirini aç</q-tooltip>
          </q-btn>
        </q-td>
      </template>
    </AppTable>

    <FormDialog
      v-model="chainOpen"
      title="Fesih Onay Zinciri"
      icon="gavel"
      width="560px"
      :saving="acting"
      save-label="Kapat"
      @save="chainOpen = false"
    >
      <div
        v-if="selected"
        class="q-gutter-sm"
      >
        <InfoItem
          icon="person"
          label="Öğrenci"
          :value="selected.studentName"
        />
        <InfoItem
          icon="business"
          label="İşletme"
          :value="selected.businessName || 'Okulda staj'"
        />
        <InfoItem
          v-if="selectedChain?.terminationReason"
          icon="notes"
          label="Gerekçe"
          :value="selectedChain.terminationReason"
        />

        <q-separator class="q-my-md" />

        <div
          v-if="!selectedChain?.isActive"
          class="text-grey-7"
        >
          Bu staj için fesih süreci açılmamış.
        </div>

        <template v-else>
          <div class="text-caption text-grey-7 q-mb-sm">
            Onaylar sırayla verilir; sırası gelmeyen adım onaylanamaz.
          </div>

          <q-list dense>
            <q-item
              v-for="view in stepViews(selectedChain)"
              :key="view.name"
            >
              <q-item-section avatar>
                <!-- Ton birliği: satırdaki "sıradaki adım" çipi ile bu panel ikonu AYNI
                     kaydın AYNI durumunu gösterir, aynı tonda olmalıdır. Çip `warning`
                     kullanıyor (outline, `.text-warning` → `var(--q-warning)`), yani
                     #9A6B00. İkon da `warning` ile aynı tema değişkenine bağlandı — Quasar
                     `.text-warning { color: var(--q-warning) }` basar, yani kiracı rengi
                     değişince ikisi birlikte kayar. Ham palet tonu `orange-9` bırakıldı
                     (#ef6c00, quasar/src/css/variables.sass:318): DESIGN.md "Don't" listesinde
                     adıyla geçer ve tema dışına düşer.
                     Ölçüldü (beyaz zemin): #9A6B00 = 4,69:1, WCAG 1.4.11 grafik nesnesi eşiğini
                     (3:1) rahat geçer; #ef6c00 = 3,08:1 ile eşiğe teğetti.
                     Sırası gelmemiş adımda "Sırada" caption'ı BASILMAZ — durumu tek başına ikon
                     taşır, yani anlamlı grafik nesnesidir ve 3:1 ister. grey-5 (#bdbdbd) 1,88:1
                     ile eşiğin altında kalıyordu; grey-7 (#757575) 4,61:1. -->
                <q-icon
                  :name="view.approved ? 'check_circle' : 'radio_button_unchecked'"
                  :color="view.approved ? 'positive' : view.isNext ? 'warning' : 'grey-7'"
                />
              </q-item-section>
              <q-item-section>
                <!-- Adım adı okunması gereken zincir bilgisi, devre dışı form bileşeni değil:
                     WCAG 1.4.3 muafiyeti geçerli değil, eşik 4,5:1. grey-6 (#9e9e9e) 2,68:1
                     idi; grey-7 (#757575) 4,61:1. Caption yine de gövdeden soluk kalır. -->
                <q-item-label :class="{ 'text-grey-7': !view.approved && !view.isNext }">
                  {{ view.label }}
                </q-item-label>
                <q-item-label
                  v-if="view.isNext"
                  caption
                >
                  Sırada
                </q-item-label>
              </q-item-section>
              <q-item-section side>
                <q-btn
                  v-if="view.isNext && view.step && canDo(view.step)"
                  dense
                  unelevated
                  color="primary"
                  label="Onayla"
                  :loading="acting"
                  :disable="periodStore.isReadOnly"
                  @click="approve(view.step)"
                />
              </q-item-section>
            </q-item>
          </q-list>

          <AppNotice
            v-if="selectedChain?.chain?.isOverridden"
            type="warning"
            dense
            icon="bolt"
            class="q-mt-md"
          >
            Zincir <strong>{{ selectedChain.chain.overriddenBy }}</strong> tarafından atlandı.
          </AppNotice>

          <div
            v-else-if="canOverride"
            class="q-mt-md"
          >
            <q-separator class="q-mb-md" />
            <div class="text-caption text-grey-7 q-mb-sm">
              Zincir takıldıysa onay adımları atlanabilir. İşlem gerekçesiyle birlikte kaydedilir.
            </div>
            <q-input
              v-model="overrideReason"
              outlined
              dense
              type="textarea"
              autogrow
              label="Override gerekçesi"
              :rules="[(v: string) => !!v?.trim() || 'Gerekçe zorunludur']"
            />
            <q-btn
              class="q-mt-sm"
              color="warning"
              unelevated
              icon="bolt"
              label="Zinciri atla"
              :loading="acting"
              :disable="!overrideReason.trim() || periodStore.isReadOnly"
              @click="doOverride"
            />
          </div>
        </template>
      </div>
    </FormDialog>
  </q-page>
</template>

<script setup lang="ts">
/**
 * Fesih onay zinciri sayfası — okul tarafı (#191, #218).
 *
 * Zincir **sıralıdır**: koordinatör öğretmen → müdür yardımcısı → müdür. Aynı anda yalnız bir
 * adımın butonu etkindir; sunucu da sırayı dayatır, arayüz onu yalnız yansıtır.
 *
 * Veli ve işletme yetkilisi bu zincirde **yoktur** — onlar fesih talep eder, onaylamaz.
 *
 * **İzin kararı sunucudan gelir.** Sıradaki adım kendi `permission` alanını taşır; burada
 * adım→izin eşlemesi tutulmaz (ADR-0001).
 */
import { ref, computed, watch } from 'vue'
import type { QTableProps } from 'quasar'
import {
  internshipApi,
  type InternshipSummaryDto,
  type TerminationChainStatusDto,
  type TerminationStepDto,
} from 'src/api/internship'
import { useServerPagination } from 'src/composables/useServerPagination'
import { useTerminationChain } from 'src/composables/useTerminationChain'
import { useNotify } from 'src/composables/useNotify'
import { useAcademicPeriodStore } from 'stores/academicPeriod'
import { useAuthStore } from 'stores/auth'
import { Permissions } from 'src/utils/permissions'
import AppTable from 'components/AppTable.vue'
import AppNotice from 'components/AppNotice.vue'
import PageHeader from 'components/PageHeader.vue'
import FormDialog from 'components/FormDialog.vue'
import InfoItem from 'components/InfoItem.vue'

const periodStore = useAcademicPeriodStore()
const authStore = useAuthStore()
const notify = useNotify()

const { acting, loadChains, chainOf, nextStepOf, canActOn, stepViews, canDo, refresh } =
  useTerminationChain()

/**
 * "Sıra sizde" sinyalinin koşulu — hardal bu ekranda başka hiçbir anlama gelmez.
 *
 * `canActOn` iki şeyi arar: sıradaki adım var mı ve o adımın **kendi izni** bu kullanıcıda mı
 * (izin sunucudan gelen adım tanımından okunur, burada eşleme tutulmaz — ADR-0001). Buraya
 * eklenen tek şey **kapalı dönem bastırması**: geçmiş dönemde onay butonu zaten `disable`
 * (bkz. panel), yapılamayan iş "sırası sizde" diye vaat edilmez.
 *
 * **Bu koşul adım SAHİPLİĞİNİ kanıtlamaz — kanıtlayamaz.** Ölçüldü: zincirin üç adımından
 * ikisi (`TerminationChainPolicy.cs:19`, `:22`) aynı izni istiyor. Bu yüzden iki daraltma
 * eklendi: (1) rozet makam adı yazmaz, (2) `showRowSignal` nadirlik kapısı. Kesin ayrım
 * ancak sunucudan satır başına gelen bir "bu adımı sen yapabilirsin" bayrağıyla kurulur.
 *
 * Zincir satır satır ayrı istekle yükleniyor; henüz gelmemiş satırda `nextStepOf` null döner ve
 * hücre skeleton'da kalır — sinyal sonradan yanıp göz zıplatmaz.
 */
function isMyTurn(id: string): boolean {
  return !periodStore.isReadOnly && canActOn(id)
}

const columns: QTableProps['columns'] = [
  { name: 'studentName', label: 'Öğrenci', field: 'studentName', align: 'left', sortable: true },
  { name: 'businessName', label: 'İşletme', field: 'businessName', align: 'left' },
  { name: 'next', label: 'Sıradaki adım', field: 'id', align: 'left' },
  { name: 'actions', label: '', field: 'id', align: 'right' },
]

const filters = computed(() => ({
  phase: 'TerminationInProgress',
  academicPeriodId: periodStore.selectedPeriodId ?? undefined,
}))

const { rows, loading, pagination, search, onRequest, onSearch, load } =
  useServerPagination<InternshipSummaryDto>({
    fetchFn: (params) => internshipApi.listInternships(params),
    filters,
    defaultSortBy: 'studentName',
  })

/**
 * Nadirlik kapısı — sinyal AYIRT ETTİĞİ yerde durur.
 *
 * Görünür satırların yalnız bir kısmı sıradaysa satır rozeti basılır. Hepsi sıradaysa rozet
 * hiçbir şeyi ayırt etmez (yalnız sütunu tekrar eder) ve yerini sayfa başındaki tek bildirim
 * alır. Hiçbiri sıradaysa hiçbir şey gösterilmez.
 *
 * Zinciri henüz yüklenmemiş satır `isMyTurn`'de false döner; yükleme bitene kadar sayı eksik
 * kalır, bu da yalnız bildirimin geç görünmesine yol açar — yanlış bir şey göstermez.
 */
const turnRowCount = computed(() => rows.value.filter((r) => isMyTurn(r.id)).length)
const showRowSignal = computed(
  () => turnRowCount.value > 0 && turnRowCount.value < rows.value.length,
)
const showAllTurnNotice = computed(
  () => rows.value.length > 0 && turnRowCount.value === rows.value.length,
)

// Liste her yenilendiğinde zincirler de tazelenir. Composable'da "yüklendi" kancası yok;
// satırları izlemek aynı sonucu verir ve composable'ı değiştirmeye gerek bırakmaz.
watch(
  rows,
  (items) => {
    loadChains(items).catch(() => {})
  },
  { immediate: true },
)

const chainOpen = ref(false)
const selected = ref<InternshipSummaryDto | null>(null)
const selectedChain = ref<TerminationChainStatusDto | null>(null)
const overrideReason = ref('')

const canOverride = computed(() => authStore.hasPermission(Permissions.Internship.Manage))

function openChain(row: InternshipSummaryDto) {
  selected.value = row
  selectedChain.value = chainOf(row.id) ?? null
  overrideReason.value = ''
  chainOpen.value = true
}

async function approve(step: TerminationStepDto) {
  if (!selected.value) return
  const internshipId = selected.value.id

  acting.value = true
  try {
    await internshipApi.approveTerminationStep(internshipId, step.endpoint)
    notify.success(`${step.slug} onayı verildi.`)
  } finally {
    acting.value = false
  }

  // Yenileme try/catch dışında: onay başarılı ama yenileme başarısızsa hem başarı hem hata
  // bildirimi gösterilmemeli.
  await refresh(internshipId, selectedChain).catch(() => {})
}

async function doOverride() {
  if (!selected.value || !overrideReason.value.trim()) return
  const internshipId = selected.value.id

  acting.value = true
  try {
    await internshipApi.overrideTermination(internshipId, { reason: overrideReason.value.trim() })
    notify.success('Onay zinciri atlandı.')
    overrideReason.value = ''
  } finally {
    acting.value = false
  }

  await refresh(internshipId, selectedChain).catch(() => {})
  load().catch(() => {})
}
</script>
