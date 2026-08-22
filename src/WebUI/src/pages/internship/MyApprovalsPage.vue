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
          <q-chip
            v-else-if="chainOf(props.row.id)?.chain?.isOverridden"
            dense
            color="warning"
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
          <q-chip
            v-else
            dense
            outline
            color="orange-9"
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
              <q-icon
                :name="view.approved ? 'check_circle' : 'radio_button_unchecked'"
                :color="view.approved ? 'positive' : view.isNext ? 'orange-9' : 'grey-5'"
              />
            </q-item-section>
            <q-item-section>
              <q-item-label :class="{ 'text-grey-6': !view.approved && !view.isNext }">
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
