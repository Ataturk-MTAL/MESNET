<template>
  <q-page padding>
    <PageHeader
      title="Fesih Onaylarım"
      icon="how_to_reg"
      subtitle="Onayınızı bekleyen staj fesih süreçleri"
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
      @request="onRequest"
    >
      <template #body-cell-mine="props">
        <q-td :props="props">
          <template v-if="chainOf(props.row.id) === undefined">
            <q-skeleton
              type="text"
              width="120px"
            />
          </template>
          <template v-else-if="actionableSteps(props.row.id).length">
            <q-chip
              v-for="step in actionableSteps(props.row.id)"
              :key="step.name"
              dense
              color="primary"
              text-color="white"
              icon="pending_actions"
              :label="step.slug"
              class="q-mr-xs"
            />
          </template>
          <template v-else>
            <span class="text-grey-6">Sizden beklenen adım yok</span>
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
      width="520px"
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

        <q-list
          v-else
          dense
        >
          <q-item
            v-for="step in steps"
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
      </div>
    </FormDialog>
  </q-page>
</template>

<script setup lang="ts">
/**
 * Veli ve işletme yetkilisinin fesih onay sayfası (#191, 3/3).
 *
 * **Neden tek sayfa:** iki aktörün ekranı aynı — "kapsamımdaki stajlar, benim adımım".
 * Kapsamı sunucu çözer (`OwnDataScope`: veli bağı ya da işletme kimliği, ikisi de claim'den),
 * adımı da sunucu söyler (`pendingSteps[].permission`). İki ayrı sayfa yapılsaydı aynı mantık
 * iki yerde yaşar, biri düzeltilip diğeri unutulurdu.
 *
 * Okul tarafının sayfası ayrıdır (`TerminationsPage`): orada override ve tüm stajların
 * listesi var, burada yok.
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
import AppTable from 'components/AppTable.vue'
import AppNotice from 'components/AppNotice.vue'
import PageHeader from 'components/PageHeader.vue'
import FormDialog from 'components/FormDialog.vue'
import InfoItem from 'components/InfoItem.vue'

const periodStore = useAcademicPeriodStore()
const notify = useNotify()

const {
  acting,
  loadChains,
  chainOf,
  actionableSteps,
  allSteps,
  isApproved,
  canDo,
  refresh,
} = useTerminationChain()

const columns: QTableProps['columns'] = [
  { name: 'studentName', label: 'Öğrenci', field: 'studentName', align: 'left' },
  { name: 'businessName', label: 'İşletme', field: 'businessName', align: 'left' },
  { name: 'mine', label: 'Sizden beklenen', field: 'id', align: 'left' },
  { name: 'actions', label: '', field: 'id', align: 'right' },
]

// Liste sunucuda zaten kapsanıyor (veli bağı / işletme kimliği); burada ek filtre yok.
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

const steps = computed(() => allSteps(selectedChain.value))

function openChain(row: InternshipSummaryDto) {
  selected.value = row
  selectedChain.value = chainOf(row.id) ?? null
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
</script>
