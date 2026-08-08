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
      <template #body-cell-pending="props">
        <q-td :props="props">
          <template v-if="chainOf(props.row.id) === undefined">
            <q-skeleton
              type="text"
              width="120px"
            />
          </template>
          <template v-else-if="chainOf(props.row.id)?.chain?.isOverridden">
            <q-chip
              dense
              color="warning"
              text-color="white"
              icon="bolt"
              label="Override edildi"
            />
          </template>
          <template v-else-if="pendingOf(props.row.id).length === 0">
            <q-chip
              dense
              color="positive"
              text-color="white"
              icon="check"
              label="Onaylar tamam"
            />
          </template>
          <template v-else>
            <q-chip
              v-for="step in pendingOf(props.row.id)"
              :key="step.name"
              dense
              outline
              color="orange-9"
              :label="step.slug"
              class="q-mr-xs"
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
          <q-list dense>
            <q-item
              v-for="step in allStepsForSelected"
              :key="step.name"
            >
              <q-item-section avatar>
                <q-icon
                  :name="isApproved(selectedChain, step) ? 'check_circle' : 'radio_button_unchecked'"
                  :color="isApproved(selectedChain, step) ? 'positive' : 'grey-5'"
                />
              </q-item-section>
              <q-item-section>
                <q-item-label>{{ step.slug }}</q-item-label>
              </q-item-section>
              <q-item-section side>
                <q-btn
                  v-if="!isApproved(selectedChain, step) && canDo(step)"
                  dense
                  unelevated
                  color="primary"
                  label="Onayla"
                  :loading="acting"
                  :disable="periodStore.isReadOnly"
                  @click="approve(step)"
                />
              </q-item-section>
            </q-item>
          </q-list>

          <q-banner
            v-if="selectedChain?.chain?.isOverridden"
            dense
            class="bg-orange-1 q-mt-md"
          >
            <template #avatar>
              <q-icon
                name="bolt"
                color="warning"
              />
            </template>
            Zincir <strong>{{ selectedChain.chain.overriddenBy }}</strong> tarafından atlandı.
          </q-banner>

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
 * Fesih onay zinciri sayfası (#191).
 *
 * Backend'de yedi uç vardı ve **hiçbirinin istemcisi yoktu**; zincir arayüzden hiç
 * ilerletilemiyordu. Bu sayfa okul tarafının adımlarını bağlar.
 *
 * **İzin kararı sunucudan gelir.** Her adım kendi `permission` alanını taşır; burada
 * adım→izin eşlemesi tutulmaz. Tutulsaydı biri değişip diğeri unutulduğunda buton yanlış
 * kişiye görünürdü (ADR-0001: karar izne bakar, rol adına değil).
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

const columns: QTableProps['columns'] = [
  { name: 'studentName', label: 'Öğrenci', field: 'studentName', align: 'left', sortable: true },
  { name: 'businessName', label: 'İşletme', field: 'businessName', align: 'left' },
  { name: 'pending', label: 'Bekleyen adım', field: 'id', align: 'left' },
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

// Liste her yenilendiğinde zincirler de tazelenir. Composable'da "yüklendi" kancası yok;
// satırları izlemek aynı sonucu verir ve composable'ı değiştirmeye gerek bırakmaz.
watch(rows, (items) => { loadChains(items).catch(() => {}) }, { immediate: true })

const {
  acting,
  loadChains,
  chainOf,
  pendingOf,
  allSteps,
  isApproved,
  canDo,
  refresh,
} = useTerminationChain()

// ─── Zincir paneli ───
const chainOpen = ref(false)
const selected = ref<InternshipSummaryDto | null>(null)
const selectedChain = ref<TerminationChainStatusDto | null>(null)
const overrideReason = ref('')

const canOverride = computed(() => authStore.hasPermission(Permissions.Internship.Manage))

const allStepsForSelected = computed(() => allSteps(selectedChain.value))

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
