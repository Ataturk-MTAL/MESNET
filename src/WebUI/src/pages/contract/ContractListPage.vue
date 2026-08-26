<template>
  <q-page padding>
    <PageHeader title="Sözleşmeler">
      <PermissionGuard :permission="Permissions.Internship.Contract">
        <q-btn
          :disable="periodStore.isReadOnly"
          color="primary"
          icon="add"
          label="Yeni Sözleşme"
          unelevated
          @click="openCreateDialog"
        />
      </PermissionGuard>
    </PageHeader>

    <AppNotice
      v-if="periodStore.isReadOnly"
      type="readonly"
      class="q-mb-md"
      message="Bu dönem kapatılmıştır — yalnızca görüntüleme yapılabilir."
    />

    <!-- Filtreler -->
    <div class="row q-gutter-sm q-mb-md">
      <q-select
        v-model="statusFilter"
        :options="statusOptions"
        label="Durum"
        outlined
        dense
        emit-value
        map-options
        style="min-width: 180px"
        @update:model-value="load"
      />
    </div>

    <!--
      Görünen satırların TAMAMI karar bekliyorsa (ör. "Fesih Talep Edildi" filtresi seçiliyse)
      satır rozeti hiçbir şeyi ayırt etmez, yalnız durum sütununu ikinci kez söyler. Yirmi
      rozet yerine tek cümle. Hiçbiri sırada değilse hiçbir şey gösterilmez.
    -->
    <AppNotice
      v-if="showTurnNotice"
      type="info"
      class="q-mb-md"
      :message="`Bu listedeki ${turnRows.length} sözleşmenin tamamı sizin kararınızı bekliyor.`"
    />

    <AppTable
      :rows="contracts"
      :columns="columns"
      :loading="loading"
      :pagination="pagination"
      @request="onRequest"
    >
      <template #body-cell-student="{ row }">
        <q-td>
          <div class="text-weight-medium">
            {{ studentMap[row.studentId]?.fullName ?? '—' }}
          </div>
          <div
            v-if="studentMap[row.studentId]?.info"
            class="text-caption text-grey-7"
          >
            {{ studentMap[row.studentId].info }}
          </div>
        </q-td>
      </template>
      <template #body-cell-business="{ row }">
        <q-td>{{ businessMap[row.businessId] ?? '—' }}</q-td>
      </template>
      <template #body-cell-statusSlug="{ row }">
        <q-td>
          <StatusBadge :slug="row.statusSlug" />
          <!--
            "Sıra sizde" — koşul ve gerekçe için bkz. `isMyTurn` / `showRowSignal` (script).
            Yüzey olarak DURUM hücresi seçildi: aşama bilgisinin okunduğu yer burasıdır,
            sinyal de aşamanın devamıdır. Satır zemini boyamak ya da renkli sol kenarlık
            koymak tabloyu hardala boğardı; dolu rozet tek satırda kalır ve metin etiketi
            taşır (Renk Yalnız Kanıt Kuralı — renk körlüğünde bilgi kaybı yok).
            Rozet yalnız AYIRT ETTİĞİ sayfada basılır: görünen satırların hepsi sıradaysa
            yerini tablonun üstündeki tek cümlelik bildirim alır (bkz. `showTurnNotice`).
          -->
          <q-badge
            v-if="showRowSignal && isMyTurn(row)"
            color="accent-strong"
            class="text-body2 q-px-sm q-py-xs q-ml-xs"
            label="Sıra sizde"
          />
        </q-td>
      </template>
      <template #body-cell-startDate="{ row }">
        <q-td>{{ formatDate(row.startDate) }} – {{ formatDate(row.endDate) }}</q-td>
      </template>
      <template #body-cell-signatures="{ row }">
        <q-td>
          <!--
            İmzasız halka ANLAM TAŞIYAN göstergedir: bu hücrede yanında aynı bilgiyi
            veren görünür metin yok (aria-label yalnız ekran okuyucuya gider, tooltip
            hover ister), yani WCAG 1.4.11 grafik nesnesi eşiği 3:1 geçerli.
            Ölçüm (beyaz #FFFFFF hücre zemini): grey-4 #e0e0e0 → 1,32:1 (kalıyordu),
            grey-7 #757575 → 4,61:1. Renk tek sinyal değil — ikon adı da ayrışıyor
            (check_circle / radio_button_unchecked).
          -->
          <div
            role="img"
            :aria-label="`İmzalar — Kurum: ${row.institutionSignature.isSigned ? 'imzalandı' : 'imza bekliyor'}; İşletme: ${row.businessSignature.isSigned ? 'imzalandı' : 'imza bekliyor'}; Öğrenci: ${row.studentSignature.isSigned ? 'imzalandı' : 'imza bekliyor'}; Veli: ${row.parentSignature.isSigned ? 'imzalandı' : 'imza bekliyor'}`"
          >
            <q-icon
              :name="row.institutionSignature.isSigned ? 'check_circle' : 'radio_button_unchecked'"
              :color="row.institutionSignature.isSigned ? 'positive' : 'grey-7'"
              size="xs"
            >
              <q-tooltip>Kurum{{ row.institutionSignature.signedBy ? ': ' + row.institutionSignature.signedBy : '' }}</q-tooltip>
            </q-icon>
            <q-icon
              :name="row.businessSignature.isSigned ? 'check_circle' : 'radio_button_unchecked'"
              :color="row.businessSignature.isSigned ? 'positive' : 'grey-7'"
              size="xs"
              class="q-ml-xs"
            >
              <q-tooltip>İşletme{{ row.businessSignature.signedBy ? ': ' + row.businessSignature.signedBy : '' }}</q-tooltip>
            </q-icon>
            <q-icon
              :name="row.studentSignature.isSigned ? 'check_circle' : 'radio_button_unchecked'"
              :color="row.studentSignature.isSigned ? 'positive' : 'grey-7'"
              size="xs"
              class="q-ml-xs"
            >
              <q-tooltip>Öğrenci{{ row.studentSignature.signedBy ? ': ' + row.studentSignature.signedBy : '' }}</q-tooltip>
            </q-icon>
            <q-icon
              :name="row.parentSignature.isSigned ? 'check_circle' : 'radio_button_unchecked'"
              :color="row.parentSignature.isSigned ? 'positive' : 'grey-7'"
              size="xs"
              class="q-ml-xs"
            >
              <q-tooltip>Veli{{ row.parentSignature.signedBy ? ': ' + row.parentSignature.signedBy : '' }}</q-tooltip>
            </q-icon>
          </div>
        </q-td>
      </template>
      <template #body-cell-actions="{ row }">
        <q-td class="text-right">
          <PermissionGuard :permission="Permissions.Document.Upload">
            <q-btn
              flat
              round
              dense
              icon="upload_file"
              color="secondary"
              aria-label="Evrak yükle"
              @click.stop="openUploadDialog(row)"
            >
              <q-tooltip>Evrak yükle</q-tooltip>
            </q-btn>
          </PermissionGuard>
          <q-btn
            flat
            round
            dense
            icon="folder_open"
            color="grey-7"
            aria-label="Evrakları aç"
            :badge="row.documents?.length > 0 ? String(row.documents.length) : undefined"
            badge-color="primary"
            @click.stop="openDocumentsDialog(row)"
          >
            <q-tooltip>Evrakları aç</q-tooltip>
          </q-btn>
          <q-btn
            flat
            round
            dense
            icon="open_in_new"
            aria-label="Sözleşme detayını aç"
            @click="openDetail(row)"
          >
            <q-tooltip>Sözleşme detayını aç</q-tooltip>
          </q-btn>
        </q-td>
      </template>
    </AppTable>

    <!-- Detay Panel -->
    <DetailPanel
      v-model="detailOpen"
      title="Sözleşme Detayı"
      :has-content="!!selected"
      :width="520"
    >
      <template v-if="selected">
        <!-- Durum & Tarih -->
        <div class="row items-center q-mb-md q-gutter-sm">
          <StatusBadge :slug="selected.statusSlug" />
          <q-chip
            icon="calendar_today"
            dense
            outline
            color="grey-7"
          >
            {{ formatDate(selected.startDate) }}
            <span v-if="selected.endDate"> – {{ formatDate(selected.endDate) }}</span>
          </q-chip>
        </div>

        <!-- İmza durumu -->
        <q-card
          flat
          bordered
          class="q-mb-md"
        >
          <q-card-section class="q-pb-sm">
            <div class="text-subtitle2 text-weight-medium q-mb-sm">
              İmza Durumu
            </div>
            <div class="row q-gutter-md justify-start">
              <div
                v-for="sig in signatureList"
                :key="sig.label"
                class="text-center"
              >
                <!-- İkon dekoratiftir: durum bilgisi aşağıdaki görünür metinle taşınır.
                     QIcon kendi render'ında aria-hidden="true" yazar, bu yüzden ikona
                     verilen role/aria-label erişilebilirlik ağacına HİÇ ulaşmaz. -->
                <q-icon
                  :name="sig.dto.isSigned ? 'check_circle' : 'pending'"
                  :color="sig.dto.isSigned ? 'positive' : 'grey-4'"
                  size="36px"
                />
                <div class="text-caption text-weight-medium q-mt-xs">
                  {{ sig.label }}
                </div>
                <div
                  v-if="!sig.dto.isSigned"
                  class="text-caption text-grey-7"
                >
                  İmza bekliyor
                </div>
                <div
                  v-if="sig.dto.signedBy"
                  class="text-caption text-grey-7"
                >
                  {{ sig.dto.signedBy }}
                </div>
                <div
                  v-if="sig.dto.signedAt"
                  class="text-caption text-grey-7"
                >
                  {{ formatDate(sig.dto.signedAt) }}
                </div>
              </div>
            </div>
          </q-card-section>
        </q-card>

        <!-- Yüklü Evraklar -->
        <q-card
          v-if="selected.documents?.length"
          flat
          bordered
          class="q-mb-md"
        >
          <q-card-section class="q-pb-sm">
            <div class="text-subtitle2 text-weight-medium q-mb-sm">
              Yüklü Evraklar
            </div>
            <q-list
              dense
              separator
            >
              <q-item
                v-for="doc in selected.documents"
                :key="doc.documentId"
                class="q-px-none"
              >
                <q-item-section avatar>
                  <q-icon
                    name="picture_as_pdf"
                    color="negative"
                  />
                </q-item-section>
                <q-item-section>
                  <q-item-label class="text-weight-medium">
                    {{ doc.documentTypeSlug }}
                  </q-item-label>
                  <q-item-label
                    v-if="doc.description"
                    caption
                  >
                    {{ doc.description }}
                  </q-item-label>
                  <q-item-label
                    caption
                    class="text-grey-7"
                  >
                    {{ doc.uploadedBy }} · {{ formatDate(doc.uploadedAt) }}
                  </q-item-label>
                </q-item-section>
              </q-item>
            </q-list>
          </q-card-section>
        </q-card>

        <!-- Fesih talebi bekliyor banner -->
        <AppNotice
          v-if="selected.status === 'TerminationRequested'"
          type="warning"
          icon="pending_actions"
          dense
          class="q-mb-md"
        >
          <div class="text-caption text-weight-bold">
            FESİH TALEBİ BEKLEMEDE
          </div>
          <div class="text-caption q-mt-xs">
            <span v-if="selected.terminationReasonTypeSlug">{{ selected.terminationReasonTypeSlug }}</span>
            <span v-if="selected.terminationReason"> — {{ selected.terminationReason }}</span>
          </div>
        </AppNotice>

        <!-- Feshedildi bilgisi -->
        <AppNotice
          v-if="selected.terminationReason && selected.status === 'Terminated'"
          type="error"
          icon="gavel"
          dense
          class="q-mb-md"
        >
          <div class="text-caption text-weight-medium">
            {{ selected.terminationReasonTypeSlug }}
          </div>
          <div class="text-body2">
            {{ selected.terminationReason }}
          </div>
        </AppNotice>

        <!-- Eylemler -->
        <div class="text-subtitle2 text-weight-medium q-mb-sm">
          İşlemler
        </div>
        <div class="column q-gutter-sm">
          <PermissionGuard :permission="Permissions.Internship.Contract">
            <q-btn
              v-if="selected.status === 'Draft'"
              color="primary"
              icon="send"
              label="İmzaya Gönder"
              unelevated
              :loading="saving"
              @click="doSubmit"
            />
            <q-btn
              v-if="selected.status === 'AwaitingSignature'"
              color="positive"
              icon="draw"
              label="İmzala"
              unelevated
              :loading="saving"
              @click="signDialog = true"
            />
            <q-btn
              v-if="selected.status === 'AwaitingSignature'"
              color="positive"
              icon="play_arrow"
              label="Aktifleştir"
              unelevated
              :loading="saving"
              @click="doActivate"
            />
          </PermissionGuard>

          <PermissionGuard :permission="Permissions.Internship.Manage">
            <q-btn
              v-if="selected.status === 'Active'"
              color="warning"
              icon="pause"
              label="Askıya Al"
              unelevated
              :loading="saving"
              @click="suspendDialog = true"
            />
            <q-btn
              v-if="selected.status === 'Suspended'"
              color="positive"
              icon="play_arrow"
              label="Devam Ettir"
              unelevated
              :loading="saving"
              @click="doResume"
            />
            <q-btn
              v-if="selected.status === 'Active' || selected.status === 'Suspended'"
              color="negative"
              icon="cancel"
              label="Feshet"
              unelevated
              :loading="saving"
              @click="terminateDialog = true"
            />
            <q-btn
              v-if="selected.status === 'Active'"
              color="secondary"
              icon="done_all"
              label="Tamamla"
              unelevated
              :loading="saving"
              @click="doComplete"
            />
          </PermissionGuard>

          <!-- Fesih talebi onay/red — Müdür / Müdür Yardımcısı -->
          <PermissionGuard :permission="Permissions.Internship.Approve">
            <template v-if="selected.status === 'TerminationRequested'">
              <q-btn
                color="negative"
                icon="gavel"
                label="Feshi Onayla"
                unelevated
                :loading="saving"
                @click="terminateDialog = true"
              />
              <q-btn
                color="positive"
                icon="thumb_down"
                label="Talebi Reddet"
                unelevated
                :loading="saving"
                @click="rejectTerminateDialog = true"
              />
            </template>
          </PermissionGuard>

          <!-- İşletme fesih talebi — CompanyManager -->
          <PermissionGuard :permission="Permissions.Company.Student">
            <q-btn
              v-if="selected.status === 'Active' || selected.status === 'Suspended'"
              color="negative"
              icon="report"
              label="Fesih Talebi Oluştur"
              outline
              :loading="saving"
              @click="requestTerminateDialog = true"
            />
          </PermissionGuard>

          <PermissionGuard :permission="Permissions.Document.Upload">
            <q-btn
              color="secondary"
              icon="upload_file"
              label="Evrak Yükle"
              outline
              @click="openUploadDialog(selected)"
            />
          </PermissionGuard>
        </div>
      </template>
    </DetailPanel>

    <SignContractForm
      v-model="signDialog"
      :contract-id="selected?.id ?? ''"
      @saved="refreshSelected"
    />
    <SuspendContractForm
      v-model="suspendDialog"
      :contract-id="selected?.id ?? ''"
      @saved="refreshSelected"
    />
    <TerminateContractForm
      v-model="terminateDialog"
      :contract-id="selected?.id ?? ''"
      @saved="refreshSelected"
    />
    <RequestTerminationForm
      v-model="requestTerminateDialog"
      :contract-id="selected?.id ?? ''"
      @saved="refreshSelected"
    />
    <RejectTerminationForm
      v-model="rejectTerminateDialog"
      :contract-id="selected?.id ?? ''"
      @saved="refreshSelected"
    />
    <UploadContractDocForm
      v-model="uploadDialog"
      :contract-id="uploadTarget?.id ?? ''"
      @saved="afterUploadSaved"
    />

    <!-- ── Evraklar Dialog ── -->
    <DetailDialog
      v-model="documentsDialog"
      title="Yüklü Evraklar"
      icon="folder_open"
      position="right"
      full-height
      :maximized="$q.screen.lt.sm"
      :card-style="documentsCardStyle"
    >
      <template #toolbar-actions>
        <PermissionGuard :permission="Permissions.Document.Upload">
          <q-btn
            unelevated
            color="secondary"
            icon="upload_file"
            label="Evrak Ekle"
            size="sm"
            class="q-mr-md"
            @click="() => { documentsDialog = false; if (documentsTarget) openUploadDialog(documentsTarget) }"
          />
        </PermissionGuard>
      </template>

      <q-separator />

      <!-- Kayan bölüm kalan yüksekliği alır (flex: 1 1 0 + min-height: 0), böylece
           "Kapat" çubuğu sabit bir piksel hesabına bağlı kalmadan panelin dibine yaslanır. -->
      <q-card-section
        class="scroll"
        style="flex: 1 1 0; min-height: 0"
      >
        <div
          v-if="!documentsTarget?.documents?.length"
          class="text-center q-py-lg text-grey-7"
        >
          <q-icon
            name="folder_off"
            size="48px"
            class="q-mb-sm"
          />
          <div>Henüz evrak yüklenmemiş.</div>
        </div>
        <q-list
          v-else
          separator
        >
          <q-item
            v-for="doc in documentsTarget?.documents"
            :key="doc.documentId"
          >
            <q-item-section avatar>
              <q-avatar
                color="negative-soft"
                text-color="negative-strong"
                icon="picture_as_pdf"
              />
            </q-item-section>
            <q-item-section>
              <q-item-label class="text-weight-medium">
                {{ doc.documentTypeSlug }}
              </q-item-label>
              <q-item-label
                v-if="doc.description"
                caption
              >
                {{ doc.description }}
              </q-item-label>
              <q-item-label
                caption
                class="text-grey-7"
              >
                <q-icon
                  name="person"
                  size="12px"
                /> {{ doc.uploadedBy }}
                &nbsp;·&nbsp;
                <q-icon
                  name="schedule"
                  size="12px"
                /> {{ formatDate(doc.uploadedAt) }}
              </q-item-label>
            </q-item-section>
          </q-item>
        </q-list>
      </q-card-section>

      <q-separator />
      <q-card-actions
        align="right"
        class="q-pa-md"
      >
        <q-btn
          v-close-popup
          flat
          label="Kapat"
          color="grey-7"
        />
      </q-card-actions>
    </DetailDialog>
  </q-page>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, watch } from 'vue'
import { useQuasar } from 'quasar'
import type { QTableProps } from 'quasar'
import { contractApi, type InternshipContractDto } from 'src/api/contract'
import { useNotify } from 'src/composables/useNotify'
import { useServerPagination } from 'src/composables/useServerPagination'
import { useStudentOptions, useBusinessOptions } from 'src/composables/useEntityOptions'
import { useAcademicPeriodStore } from 'stores/academicPeriod'
import { useAuthStore } from 'stores/auth'
import { Permissions } from 'utils/permissions'
import AppTable from 'components/AppTable.vue'
import StatusBadge from 'components/StatusBadge.vue'
import PermissionGuard from 'components/PermissionGuard.vue'
import PageHeader from 'components/PageHeader.vue'
import DetailPanel from 'components/DetailPanel.vue'
import DetailDialog from 'components/DetailDialog.vue'
import { useRouter } from 'vue-router'
import SignContractForm from 'components/forms/contract/SignContractForm.vue'
import SuspendContractForm from 'components/forms/contract/SuspendContractForm.vue'
import TerminateContractForm from 'components/forms/contract/TerminateContractForm.vue'
import RequestTerminationForm from 'components/forms/contract/RequestTerminationForm.vue'
import RejectTerminationForm from 'components/forms/contract/RejectTerminationForm.vue'
import UploadContractDocForm from 'components/forms/contract/UploadContractDocForm.vue'
import AppNotice from 'components/AppNotice.vue'

const $q = useQuasar()
const router = useRouter()
const notify = useNotify()
const periodStore = useAcademicPeriodStore()
const authStore = useAuthStore()
const studentOpts = useStudentOptions()
const businessOpts = useBusinessOptions()

// ID → metadata lookup map'leri (tablo satırlarında isim göstermek için)
const studentMap = computed<Record<string, { fullName: string; info: string }>>(() => {
  const map: Record<string, { fullName: string; info: string }> = {}
  for (const opt of studentOpts.allOptions.value) {
    map[opt.value] = { fullName: opt.label, info: opt.caption ?? '' }
  }
  return map
})

const businessMap = computed<Record<string, string>>(() => {
  const map: Record<string, string> = {}
  for (const opt of businessOpts.allOptions.value) {
    map[opt.value] = opt.label
  }
  return map
})

const saving = ref(false)
const selected = ref<InternshipContractDto | null>(null)
const detailOpen = ref(false)
const signDialog = ref(false)
const suspendDialog = ref(false)
const terminateDialog = ref(false)
const requestTerminateDialog = ref(false)
const rejectTerminateDialog = ref(false)
const uploadDialog = ref(false)
const documentsDialog = ref(false)
const statusFilter = ref<string | null>(null)

// Evrak yükleme/evraklar hedef sözleşme
const uploadTarget = ref<InternshipContractDto | null>(null)
const documentsTarget = ref<InternshipContractDto | null>(null)

// Evraklar yan-paneli: sm altında panel tam ekrana geçer (maximized). Orada genişlik
// AÇIKÇA verilmek ZORUNDA — Quasar'ın `.q-dialog__inner--maximized > div` kuralındaki
// `width: 100%` bu kabda hiçbir şeye çözülmez: panel `position="right"` olduğu için
// QDialog iç kaba `.fixed-right` sınıfını da basar ve o sınıfta top/right/bottom var,
// LEFT YOK (quasar/src/css/core/positioning.sass) → kap shrink-to-fit olur, yüzde
// genişlik belirsiz kaba çözülür ve kart içerik kadar kalır. Belirti: 390–599px
// bandında solda arka plan şeridi açıkta kalır ve şerit ekran genişledikçe BÜYÜR,
// ayrıca panel eni boş/dolu evrak listesine göre zıplar.
// (Sorun kuralın !important taşımaması değil; kabın genişliğinin belirsiz olması.)
// `display: flex` kartı dikey akışkan yapar: içerik kayar, eylem çubuğu dibe yaslanır.
const documentsCardStyle = computed(() =>
  $q.screen.lt.sm
    ? 'width: 100vw; display: flex; flex-direction: column'
    : 'width: 520px; max-width: 95vw; display: flex; flex-direction: column',
)

// ── Server-side pagination ──
const filters = computed(() => ({
  academicPeriodId: periodStore.selectedPeriodId ?? undefined,
  status: statusFilter.value ?? undefined,
}))

const { rows: contracts, loading, pagination, onRequest, load } = useServerPagination<InternshipContractDto>({
  fetchFn: (params) => contractApi.list(params),
  filters,
  defaultSortBy: 'createdAt',
  defaultDescending: true,
})

const statusOptions = [
  { label: 'Tüm Durumlar', value: null },
  { label: 'Taslak', value: 'Draft' },
  { label: 'İmza Bekliyor', value: 'AwaitingSignature' },
  { label: 'Aktif', value: 'Active' },
  { label: 'Askıda', value: 'Suspended' },
  { label: 'Fesih Talep Edildi', value: 'TerminationRequested' },
  { label: 'Feshedildi', value: 'Terminated' },
  { label: 'Tamamlandı', value: 'Completed' },
]

const signatureList = computed(() =>
  selected.value
    ? [
        { label: 'Kurum',    dto: selected.value.institutionSignature },
        { label: 'İşletme',  dto: selected.value.businessSignature },
        { label: 'Öğrenci',  dto: selected.value.studentSignature },
        { label: 'Veli',     dto: selected.value.parentSignature },
      ]
    : [],
)

/**
 * "SIRA SİZDE" — Tek Ses Kuralı: bu ekranda hardal TEK bağlamda görünür, o da fesih
 * talebi kararıdır.
 *
 * Koşul VERİDEN türer, rol adından DEĞİL (ADR-0001 / #184):
 *  • `status === 'TerminationRequested'` — karar bekleyen tek aşama. Detay panelindeki
 *    "Feshi Onayla" / "Talebi Reddet" bloğu da tam bu koşulla açılır; sinyal o butonların
 *    listedeki izdüşümüdür, yeni bir kural değil.
 *  • `Permissions.Internship.Approve` — kaydı İLERLETEN ucun istediği izin; listeyi AÇAN
 *    izin değil. Ölçüldü: `POST /api/contracts/{id}/reject-termination` →
 *    `RequireAuthorization(Permissions.Internship.Approve)` (ContractEndpoints.cs:38-39).
 *    Listeyi açan izin `internship:contract:manage`'dir (ContractEndpoints.cs:41); sinyal
 *    ona bağlansaydı, fesih kararı veremeyen kullanıcıda da yanar ve tıklayınca 403 dönerdi.
 *    Bu izni taşıyanlar (RolePermissionMap.cs): InstitutionManager ("internship:*", satır
 *    16), DeputyDirector (satır 65), Teacher (satır 135) — yani "müdür / müdür yardımcısı"
 *    demek YANLIŞTI, koordinatör öğretmen de zincirin bir adımıdır (TerminationStep.Teacher).
 *  • `!periodStore.isReadOnly` — kapalı dönemde tüm yazma yolları kapalıdır; yapılamayan
 *    iş vaat edilmez.
 *
 * NADİRLİK: rozet yalnız görünen satırların BİR KISMI sıradaysa basılır (`showRowSignal`).
 * "Fesih Talep Edildi" filtresi seçildiğinde sayfadaki her satır bu aşamadadır ve rozet
 * durum sütununun tekrarına dönerdi. Ölçüt filtre DEĞERİNE değil sayfadaki orana bakar:
 * aynı hâl filtre kullanılmadan da (tüm kayıtlar fesih talebindeyse) doğar ve o zaman da
 * tek cümlelik bildirim gösterilir.
 *
 * BİLİNEN BORÇ (bu partinin kapsamı dışı — arayüz guard'ı ile uç izni ayrışıyor):
 * "Feshi Onayla" butonu `Internship.Approve` guard'ı altındadır ama bastığı uç
 * `POST /api/contracts/{id}/terminate` `Internship.Manage` ister (ContractEndpoints.cs:32).
 * `approve` taşıyıp `manage` taşımayan kullanıcı (Teacher) o butondan 403 alır. Sinyal yine
 * de boş vaat değildir: "Talebi Reddet" yolu ona açıktır ve kaydı gerçekten ilerletir.
 *
 * Kapsam BİLEREK dar tutuldu: `Draft` → "İmzaya Gönder" ve `AwaitingSignature` → "İmzala /
 * Aktifleştir" de bir eylem bekler ve `internship:contract:manage` ile açılır. Onları da
 * işaretlemek hardalı ikinci ve üçüncü bağlama yayar; nadirlik gidince sinyalin anlamı da
 * gider (DESIGN.md "Tek Ses Kuralı").
 *
 * İMZALAR sütunu bilerek işaretlenMEDİ. Dört imzadan hangisinin BAKAN KİŞİYE ait olduğunu
 * söyleyen veri yok: `AuthUser` (stores/auth.ts) yalnız id/username/e-posta/ad/roller/
 * institutionId/branchCodes taşır — studentId, businessId ya da veli bağı yok; DTO'da da
 * "bekleyen taraf" bayrağı yok (`InternshipContractDto` yalnız isSigned/signedBy verir).
 * Rolden türetmek ("CompanyManager isem işletme imzasıdır") kapsam kararını rol adına
 * bağlamak olurdu — kesin yasak. Sunucu `pendingSignatureParty` benzeri bir alan
 * verdiğinde sinyal oraya taşınabilir; o güne kadar uydurulmaz.
 */
function isMyTurn(row: InternshipContractDto): boolean {
  return (
    row.status === 'TerminationRequested' &&
    !periodStore.isReadOnly &&
    authStore.hasPermission(Permissions.Internship.Approve)
  )
}

/** Görünen sayfadaki "sıra sizde" satırları — nadirlik ölçümünün tek kaynağı. */
const turnRows = computed(() => contracts.value.filter(isMyTurn))
/** Satır rozeti: bazı satırlar sırada, hepsi değil — sinyal burada ayırt ediyor. */
const showRowSignal = computed(
  () => turnRows.value.length > 0 && turnRows.value.length < contracts.value.length,
)
/** Hepsi sırada: yirmi rozet yerine tablonun üstünde tek cümle. */
const showTurnNotice = computed(
  () => contracts.value.length > 0 && turnRows.value.length === contracts.value.length,
)

const columns: QTableProps['columns'] = [
  { name: 'student',    label: 'Öğrenci',   field: 'studentId',            align: 'left' },
  { name: 'business',   label: 'İşletme',   field: 'businessId',           align: 'left' },
  { name: 'statusSlug', label: 'Durum',     field: 'statusSlug',           align: 'left' },
  { name: 'startDate',  label: 'Dönem',     field: 'startDate',            align: 'left' },
  { name: 'signatures', label: 'İmzalar',   field: 'institutionSignature', align: 'center' },
  { name: 'actions',    label: '',          field: 'id',                   align: 'right' },
]

function formatDate(iso: string | null | undefined) {
  if (!iso) return '—'
  return new Date(iso).toLocaleDateString('tr-TR', { day: '2-digit', month: '2-digit', year: 'numeric' })
}

function openCreateDialog() {
  router.push('/internship/contracts/new').catch(() => {})
}

function openDetail(row: InternshipContractDto) {
  selected.value = row
  detailOpen.value = true
}

function openUploadDialog(contract: InternshipContractDto) {
  uploadTarget.value = contract
  uploadDialog.value = true
}

function openDocumentsDialog(contract: InternshipContractDto) {
  documentsTarget.value = contract
  documentsDialog.value = true
}

// Doğrudan eylemler (form gerektirmeyen)
async function doSubmit() {
  if (!selected.value) return
  saving.value = true
  try {
    await contractApi.submit(selected.value.id)
    notify.success('Sözleşme imzaya gönderildi.')
    await refreshSelected()
  } catch (e) {
    notify.apiError(e, 'İşlem sırasında bir hata oluştu.')
  } finally {
    saving.value = false
  }
}

async function doActivate() {
  if (!selected.value) return
  saving.value = true
  try {
    await contractApi.activate(selected.value.id)
    notify.success('Sözleşme aktifleştirildi.')
    await refreshSelected()
  } catch (e) {
    notify.apiError(e, 'İşlem sırasında bir hata oluştu.')
  } finally {
    saving.value = false
  }
}

async function doResume() {
  if (!selected.value) return
  saving.value = true
  try {
    await contractApi.resume(selected.value.id)
    notify.success('Sözleşme devam ettirildi.')
    await refreshSelected()
  } catch (e) {
    notify.apiError(e, 'İşlem sırasında bir hata oluştu.')
  } finally {
    saving.value = false
  }
}

async function doComplete() {
  if (!selected.value) return
  saving.value = true
  try {
    await contractApi.complete(selected.value.id)
    notify.success('Sözleşme tamamlandı.')
    await refreshSelected()
  } catch (e) {
    notify.apiError(e, 'İşlem sırasında bir hata oluştu.')
  } finally {
    saving.value = false
  }
}

async function refreshSelected() {
  if (!selected.value) return
  try {
    const res = await contractApi.get(selected.value.id)
    selected.value = res.data
    const idx = contracts.value.findIndex((c) => c.id === res.data.id)
    if (idx !== -1) contracts.value[idx] = res.data
  } catch { /* sessiz */ }
}

async function afterUploadSaved() {
  await load()
  if (uploadTarget.value && selected.value?.id === uploadTarget.value.id) {
    await refreshSelected()
  }
}

watch(() => periodStore.selectedPeriodId, () => load())

onMounted(async () => {
  studentOpts.load().catch(() => {})
  businessOpts.load().catch(() => {})
  await load()
})
</script>
