<template>
  <q-page padding>
    <PageHeader
      title="Fesih Durumu"
      icon="fact_check"
      subtitle="Fesih süreci devam eden stajlar"
    />

    <AppNotice
      class="q-mb-md"
      type="info"
      message="Fesih onayları okul tarafından verilir (koordinatör öğretmen → müdür yardımcısı → müdür). Bu sayfa sürecin hangi aşamada olduğunu gösterir."
    />

    <AppTable
      :rows="rows"
      :columns="columns"
      :loading="loading"
      row-key="id"
      :pagination="pagination"
      @request="onRequest"
    >
      <template #body-cell-next="props">
        <q-td :props="props">
          <q-skeleton
            v-if="chainOf(props.row.id) === undefined"
            type="text"
            width="120px"
          />
          <!-- Zincirin istisna hâli ile normal bekleme hâli AYNI TONA düşmemeli: ikisi de düz
               "warning" iken sütun tarandığında ayrımı yalnız ikon taşıyordu. Tonlar artık
               StatusBadge merdiveninden geliyor (TerminationsPage.vue ile aynı düzen) —
               istisna "uyarı" basamağı (koyu hardal), bekleme "bekleyen" basamağı (hardal).
               Ölçüldü (beyaz metin, sRGB): status-warning #785300 = 6,92:1, status-pending
               #9A6B00 = 4,69:1; aradaki ton farkı 1,47:1. Ayrımı ikon (bolt / schedule) ile
               etiket metni tamamlar — renk tek sinyal değildir. -->
          <q-chip
            v-else-if="chainOf(props.row.id)?.chain?.isOverridden"
            dense
            color="status-warning"
            text-color="white"
            icon="bolt"
            label="Okul yönetimi tamamladı"
          />
          <q-chip
            v-else-if="!nextStepOf(props.row.id)"
            dense
            color="positive"
            text-color="white"
            icon="check"
            label="Onaylar tamam"
          />
          <!-- Bu çip "bekleniyor" der, "SIRA SİZDE" demez: tonu `status-pending`
               (`--q-warning`), Resmî Hardal (`--q-accent`) DEĞİL. Buraya hardal
               eklemeyin — gerekçesi dosya başındaki script yorumunda. -->
          <q-chip
            v-else
            dense
            color="status-pending"
            text-color="white"
            icon="schedule"
            :label="`${nextStepOf(props.row.id)?.slug} onayı bekleniyor`"
          />
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
            aria-label="Süreci görüntüle"
            @click="openChain(props.row)"
          >
            <q-tooltip>Süreci görüntüle</q-tooltip>
          </q-btn>
        </q-td>
      </template>
    </AppTable>

    <FormDialog
      v-model="chainOpen"
      title="Fesih Süreci"
      icon="fact_check"
      width="520px"
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

        <q-list
          v-else
          dense
        >
          <q-item
            v-for="view in stepViews(selectedChain)"
            :key="view.name"
          >
            <q-item-section avatar>
              <!-- Ton birliği: satırdaki "sıradaki adım" çipi ile bu panel ikonu AYNI kaydın
                   AYNI durumunu gösterir, aynı tonda olmalıdır. Çip `status-pending`
                   kullanıyor; o sınıf `background-color: var(--q-warning)` (app.css), yani
                   #9A6B00. İkon da `warning` ile aynı tema değişkenine bağlandı — Quasar
                   `.text-warning { color: var(--q-warning) !important }` basar
                   (quasar/src/css/core/colors.sass), yani kiracı rengi değişince ikisi
                   birlikte kayar. Ham palet tonu `orange-9` bırakıldı (#ef6c00,
                   quasar/src/css/variables.sass:317): DESIGN.md "Don't" listesinde adıyla
                   geçer ve tema dışına düşer.
                   Ölçüldü (beyaz FormDialog paneli): #9A6B00 = 4,69:1, WCAG 1.4.11 grafik
                   nesnesi eşiğini (3:1) rahat geçer; #ef6c00 = 3,08:1 ile eşiğe teğetti ve
                   çipten 1,52:1 ayrı düşüyordu.
                   Sırası gelmemiş adımda "Onayı bekleniyor" caption'ı BASILMAZ (yalnız
                   isNext'te var), yani adımın durumunu tek başına ikon taşır — anlamlı
                   grafik nesnesi, eşik 3:1. grey-5 (#bdbdbd) 1,88:1 ile eşiğin altındaydı;
                   grey-7 (#757575) 4,61:1. -->
              <q-icon
                :name="view.approved ? 'check_circle' : 'radio_button_unchecked'"
                :color="view.approved ? 'positive' : view.isNext ? 'warning' : 'grey-7'"
              />
            </q-item-section>
            <q-item-section>
              <!-- Adım adı okunması gereken zincir bilgisi, devre dışı form bileşeni değil:
                   eşik 4,5:1. grey-6 (#9e9e9e) 2,68:1 idi; grey-7 (#757575) 4,61:1. -->
              <q-item-label :class="{ 'text-grey-7': !view.approved && !view.isNext }">
                {{ view.label }}
              </q-item-label>
              <q-item-label
                v-if="view.isNext"
                caption
              >
                Onayı bekleniyor
              </q-item-label>
            </q-item-section>
          </q-item>
        </q-list>
      </div>
    </FormDialog>
  </q-page>
</template>

<script setup lang="ts">
/**
 * Veli ve işletme yetkilisi için fesih **durum** sayfası (#191, #218).
 *
 * **Salt okunur.** Veli ve işletme yetkilisi fesih zincirinde onaycı DEĞİLDİR — onlar fesih
 * *talep eder*, onaylamaz. Onaylar okul tarafında verilir (koordinatör öğretmen → müdür
 * yardımcısı → müdür).
 *
 * Sayfa yine de değerli: veli çocuğunun, işletme kendi öğrencisinin fesih sürecinin hangi
 * aşamada olduğunu görebilmeli. Kapsamı sunucu çözer (veli bağı ya da işletme kimliği, ikisi
 * de claim'den) — burada ek filtre yok.
 *
 * **Resmî Hardal ("Sıra sizde") bu sayfaya KOYULMAZ — bilerek. Tekrar denemeyin.**
 * Sinyalin tanımı dar: kaydı bekleten taraf, ekrana bakan kullanıcının KENDİSİ
 * (app.css → `.bg-accent-soft` / `.bg-accent-strong`). Burada bekleyen taraf HER ZAMAN
 * okuldur; sayfanın hedef kitlesi (veli, öğrenci, işletme yetkilisi) hiçbir zaman sırada
 * değildir. Rotayı açabilen tek okul rolü aşağıda ayrıca ele alınıyor.
 *
 * Dosya adı ("MyApprovals") sistemdeki en davetkâr yer ve tam bu yüzden en yanıltıcı yer.
 * **Kuyruğu AÇAN izin ile kaydı İLERLETEN iznin ayrı olduğu yer burası.** Rota
 * `internship:view-own` VEYA `company:student:manage` ile açılır (router/index.ts,
 * `termination-status`; kapı `hasAnyPermission`, yani OR). Adımı ilerleten uç ise başka
 * izin ister: `TerminationStep` (Internship.Core/Policies/TerminationChainPolicy.cs)
 * Teacher ve Deputy adımlarına `internship:approve`, Director adımına `internship:manage`
 * bağlar.
 *
 * Koşulu veriden kurmayı denemek de çözüm değil — ama gerekçe "koşul hiç tutmaz" DEĞİL.
 * Ölçüldü (RolePermissionMap.cs + stores/auth.ts `hasPermission`):
 *   • Rol haritasında rotayı açabilen roller: Student, Parent, CompanyManager,
 *     InstitutionManager (başka rolde bu iki izin ya da onları yutan wildcard yok).
 *   • İlk üçünde adım izni (`internship:approve` / `internship:manage`) HİÇ yoktur →
 *     `useTerminationChain().canActOn(id)` onlar için zaten false; sinyal hiç yanmaz.
 *   • InstitutionManager `internship:*` (ve `company:*`) wildcard'ı taşır; `hasPermission`
 *     wildcard'ı ÖNEK olarak açar (`p.endsWith(':*') && permission.startsWith(...)`), yani
 *     o rolde koşul TRUE döner. Koşul ölü kod değil — yanlış ekranda canlı kod olurdu.
 *
 * Müdürün "sıra sizde" sinyalini gördüğü yer bu salt-okunur durum sayfası değil,
 * `/internship/terminations`'tır: TerminationsPage'de rozet `color="accent-strong"` ile
 * basılır ve `isMyTurn` = `!periodStore.isReadOnly && canActOn(id)` ile, yani ADIM iznine
 * bağlıdır. Aynı sinyali iki ekranda birden yakmak Tek Ses Kuralı'nı bozar — nadirlik
 * sinyalin kendisidir (DESIGN.md). Bu yüzden burası boş bırakıldı.
 *
 * Bu ekrandaki "bekliyor" hâli zaten anlatılıyor: StatusBadge merdiveninin
 * `status-pending` basamağı (`--q-warning`, uyarı hardalı). O ton Resmî Hardal DEĞİLDİR;
 * genel bekleme durumunun yeridir ve hardalın rolünü işgal etmez.
 */
import { ref, computed, watch } from 'vue'
import type { QTableProps } from 'quasar'
import {
  internshipApi,
  type InternshipSummaryDto,
  type TerminationChainStatusDto,
} from 'src/api/internship'
import { useServerPagination } from 'src/composables/useServerPagination'
import { useTerminationChain } from 'src/composables/useTerminationChain'
import { useAcademicPeriodStore } from 'stores/academicPeriod'
import AppTable from 'components/AppTable.vue'
import AppNotice from 'components/AppNotice.vue'
import PageHeader from 'components/PageHeader.vue'
import FormDialog from 'components/FormDialog.vue'
import InfoItem from 'components/InfoItem.vue'

const periodStore = useAcademicPeriodStore()

const { loadChains, chainOf, nextStepOf, stepViews } = useTerminationChain()

const columns: QTableProps['columns'] = [
  { name: 'studentName', label: 'Öğrenci', field: 'studentName', align: 'left' },
  { name: 'businessName', label: 'İşletme', field: 'businessName', align: 'left' },
  { name: 'next', label: 'Durum', field: 'id', align: 'left' },
  { name: 'actions', label: '', field: 'id', align: 'right' },
]

const filters = computed(() => ({
  phase: 'TerminationInProgress',
  academicPeriodId: periodStore.selectedPeriodId ?? undefined,
}))

const { rows, loading, pagination, onRequest } = useServerPagination<InternshipSummaryDto>({
  fetchFn: (params) => internshipApi.listInternships(params),
  filters,
  defaultSortBy: 'studentName',
})

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

function openChain(row: InternshipSummaryDto) {
  selected.value = row
  selectedChain.value = chainOf(row.id) ?? null
  chainOpen.value = true
}
</script>
