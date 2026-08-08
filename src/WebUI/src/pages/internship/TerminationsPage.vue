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
          <q-chip
            v-else
            dense
            outline
            color="orange-9"
            :label="nextStepOf(props.row.id)?.slug"
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

const { acting, loadChains, chainOf, nextStepOf, stepViews, canDo, refresh } =
  useTerminationChain()

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
