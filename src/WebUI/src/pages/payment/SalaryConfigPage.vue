<template>
  <q-page padding>
    <div
      class="q-mx-auto"
      style="max-width: 1000px"
    >
      <div class="row items-center q-mb-md">
        <div class="col">
          <h1 class="text-h5 text-weight-bold q-my-none">
            Asgari Ücret Yönetimi
          </h1>
          <div class="text-caption text-grey-7">
            Maaş ve devlet katkısı hesabının tabanı. Asgari ücret yıl içinde birden fazla kez
            artabilir — her artış <strong>yürürlük tarihiyle</strong> girilir ve o tarihten
            itibaren hesaplanan aylara uygulanır. Geçmiş aylar kendi dönemlerinin tutarıyla
            hesaplanmaya devam eder.
          </div>
        </div>
        <!-- Görme ile değiştirme ayrı izinler (#147): okul rolleri yürürlükteki tutarı
             görür, ulusal parametreyi yazamaz. Buton yalnız yazma izniyle görünür. -->
        <q-btn
          v-if="canManageParameters"
          unelevated
          color="primary"
          icon="add"
          label="Yeni Yürürlük"
          :disable="loading"
          class="q-ml-md"
          @click="openForm"
        />
      </div>

      <q-banner
        v-if="!current && !loading"
        class="bg-orange-1 text-orange-10 q-mb-md"
        rounded
      >
        <template #avatar>
          <q-icon name="report_problem" />
        </template>
        Asgari ücret tanımlı değil. Tanımlanana kadar maaş hesabı yapılamaz.
        <span v-if="!canManageParameters">
          Ulusal parametre olduğu için girişi sistem yöneticisi yapar.
        </span>
      </q-banner>

      <div class="row q-col-gutter-md q-mb-md">
        <div class="col-12 col-sm-6">
          <q-card
            flat
            bordered
          >
            <q-card-section>
              <div class="text-caption text-grey-7">
                Yürürlükteki asgari ücret
              </div>
              <div class="text-h5 text-weight-bold">
                {{ current ? formatCurrency(current.minimumWage) : '—' }}
              </div>
              <div class="text-caption text-grey-7">
                {{ current ? `${formatDate(current.effectiveFrom)} tarihinden itibaren` : '' }}
              </div>
            </q-card-section>
          </q-card>
        </div>
        <div class="col-12 col-sm-6">
          <q-card
            flat
            bordered
          >
            <q-card-section>
              <div class="text-caption text-grey-7">
                16 yaş altı asgari ücret
              </div>
              <div class="text-h5 text-weight-bold">
                {{
                  current?.minimumWageUnder16
                    ? formatCurrency(current.minimumWageUnder16)
                    : 'Ayrı tutar yok'
                }}
              </div>
              <div class="text-caption text-grey-7">
                Ayrı tutar girilmezse yaşa bakılmaksızın genel asgari ücret uygulanır.
              </div>
            </q-card-section>
          </q-card>
        </div>
      </div>

      <q-banner
        v-if="scheduled.length > 0"
        class="bg-blue-1 text-blue-10 q-mb-md"
        rounded
      >
        <template #avatar>
          <q-icon name="event_upcoming" />
        </template>
        <span
          v-for="s in scheduled"
          :key="s.id"
        >
          {{ formatDate(s.effectiveFrom) }} tarihinde {{ formatCurrency(s.minimumWage) }} yürürlüğe
          girecek.
        </span>
      </q-banner>

      <AppTable
        :rows="history"
        :columns="columns"
        :loading="loading"
        no-data-label="Asgari ücret kaydı yok"
        row-key="id"
        flat
        bordered
      >
        <template #body-cell-period="cellProps">
          <q-td :props="cellProps">
            {{ formatDate(cellProps.row.effectiveFrom) }} —
            {{ cellProps.row.effectiveTo ? formatDate(cellProps.row.effectiveTo) : 'süresiz' }}
            <q-badge
              v-if="cellProps.row.isCurrent"
              color="positive"
              class="q-ml-sm"
            >
              Yürürlükte
            </q-badge>
            <q-badge
              v-else-if="cellProps.row.isScheduled"
              color="info"
              class="q-ml-sm"
            >
              İleri tarihli
            </q-badge>
          </q-td>
        </template>

        <template #body-cell-minimumWage="cellProps">
          <q-td :props="cellProps">
            {{ formatCurrency(cellProps.row.minimumWage) }}
          </q-td>
        </template>

        <template #body-cell-minimumWageUnder16="cellProps">
          <q-td :props="cellProps">
            {{
              cellProps.row.minimumWageUnder16
                ? formatCurrency(cellProps.row.minimumWageUnder16)
                : '—'
            }}
          </q-td>
        </template>

        <template #body-cell-updatedBy="cellProps">
          <q-td :props="cellProps">
            {{ cellProps.row.updatedBy ?? '—' }}
          </q-td>
        </template>
      </AppTable>

      <div class="text-caption text-grey-7 q-mt-md">
        Oranlar (personel eşiği, %15 / %30 taban, devlet katkısı kesirleri) 3308 sayılı Kanun'da
        yazılı sabitlerdir; kurum tercihi değildir ve buradan değiştirilemez. Mevzuat değişirse
        kodda güncellenir.
        <span v-if="current">
          Yürürlükteki kayıt: {{ current.personnelThreshold }} personel eşiği,
          %{{ Math.round(current.smallBusinessRate * 100) }} /
          %{{ Math.round(current.largeBusinessRate * 100) }} taban.
        </span>
      </div>
    </div>

    <FormDialog
      v-model="formOpen"
      title="Yeni Asgari Ücret Yürürlüğü"
      icon="payments"
      :saving="saving"
      :save-disabled="!isFormValid"
      save-label="Yürürlüğe Al"
      @save="handleSave"
    >
      <q-input
        v-model.number="form.minimumWage"
        label="Asgari ücret (net, ₺) *"
        outlined
        type="number"
        :error="!!errors.minimumWage"
        :error-message="errors.minimumWage"
      >
        <template #prepend>
          <q-icon name="payments" />
        </template>
      </q-input>

      <q-input
        v-model.number="form.minimumWageUnder16"
        label="16 yaş altı asgari ücret (₺)"
        outlined
        type="number"
        hint="Boş bırakılırsa yaş ayrımı yapılmaz."
        :error="!!errors.minimumWageUnder16"
        :error-message="errors.minimumWageUnder16"
      >
        <template #prepend>
          <q-icon name="child_care" />
        </template>
      </q-input>

      <q-input
        v-model="form.effectiveFrom"
        label="Yürürlük başlangıcı *"
        outlined
        mask="##.##.####"
        hint="Takvim yılı başlamadan da girilebilir (ör. 01.01.2027)."
        :error="!!errors.effectiveFrom"
        :error-message="errors.effectiveFrom"
      >
        <template #prepend>
          <q-icon name="event" />
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
                v-model="form.effectiveFrom"
                mask="DD.MM.YYYY"
                minimal
              />
            </q-popup-proxy>
          </q-icon>
        </template>
      </q-input>

      <q-banner
        class="bg-grey-2 text-grey-9"
        dense
        rounded
      >
        Yeni kayıt, yürürlükteki kaydı bir gün öncesinde kapatır. Geriye dönük tarih girilemez —
        geçmiş ayların hesabı bozulmasın diye engellenir.
      </q-banner>
    </FormDialog>
  </q-page>
</template>

<script setup lang="ts">
import { ref, reactive, computed, onMounted } from 'vue'
import type { QTableProps } from 'quasar'
import { paymentApi, type SalaryConfigDto } from 'src/api/payment'
import { useAuthStore } from 'stores/auth'
import { Permissions } from 'src/utils/permissions'
import { useNotify } from 'src/composables/useNotify'
import AppTable from 'components/AppTable.vue'
import FormDialog from 'components/FormDialog.vue'

const authStore = useAuthStore()
const notify = useNotify()

/**
 * Ulusal parametre yazma yetkisi (#147). Rol adına DEĞİL izne bakılır — aynı izne sahip
 * her aktör (bugün SystemAdmin, ileride Bakanlık düzeyi aktör) aynı işi yapabilir.
 */
const canManageParameters = computed(() =>
  authStore.hasPermission(Permissions.Platform.ParameterManage),
)

const loading = ref(false)
const saving = ref(false)
const formOpen = ref(false)
const history = ref<SalaryConfigDto[]>([])

const current = computed(() => history.value.find((c) => c.isCurrent) ?? null)
const scheduled = computed(() => history.value.filter((c) => c.isScheduled))

const columns: QTableProps['columns'] = [
  { name: 'period', label: 'Yürürlük', field: 'effectiveFrom', align: 'left' },
  { name: 'minimumWage', label: 'Asgari Ücret', field: 'minimumWage', align: 'right' },
  {
    name: 'minimumWageUnder16',
    label: '16 Yaş Altı',
    field: 'minimumWageUnder16',
    align: 'right',
  },
  { name: 'updatedBy', label: 'Giren', field: 'updatedBy', align: 'left' },
]

const form = reactive({
  minimumWage: 0,
  minimumWageUnder16: null as number | null,
  effectiveFrom: '',
})
const errors = reactive<Record<string, string>>({})

const isFormValid = computed(
  () => form.minimumWage > 0 && /^\d{2}\.\d{2}\.\d{4}$/.test(form.effectiveFrom),
)

function formatCurrency(amount: number): string {
  return amount.toLocaleString('tr-TR', { style: 'currency', currency: 'TRY' })
}

function formatDate(iso: string): string {
  return new Date(iso).toLocaleDateString('tr-TR')
}

/** `DD.MM.YYYY` → ISO. Ay hesabı tarihi UTC gün başında karşılaştırır, saat taşınmaz. */
function toIsoDate(value: string): string | null {
  const match = /^(\d{2})\.(\d{2})\.(\d{4})$/.exec(value)
  if (!match) return null

  const [, day, month, year] = match
  return `${year}-${month}-${day}T00:00:00Z`
}

async function loadHistory() {
  loading.value = true
  try {
    const { data } = await paymentApi.salaryConfigHistory()
    history.value = data.items
  } catch (e) {
    notify.apiError(e, 'Asgari ücret geçmişi yüklenemedi.')
  } finally {
    loading.value = false
  }
}

function openForm() {
  form.minimumWage = current.value?.minimumWage ?? 0
  form.minimumWageUnder16 = current.value?.minimumWageUnder16 ?? null
  form.effectiveFrom = ''
  for (const key of Object.keys(errors)) errors[key] = ''
  formOpen.value = true
}

function validate(): boolean {
  for (const key of Object.keys(errors)) errors[key] = ''

  if (form.minimumWage <= 0) errors.minimumWage = 'Asgari ücret sıfırdan büyük olmalıdır.'

  if (form.minimumWageUnder16 !== null && form.minimumWageUnder16 > form.minimumWage)
    errors.minimumWageUnder16 = '16 yaş altı tutar genel asgari ücretten yüksek olamaz.'

  if (!toIsoDate(form.effectiveFrom)) errors.effectiveFrom = 'Geçerli bir tarih giriniz.'

  return Object.values(errors).every((v) => !v)
}

async function handleSave() {
  if (!validate()) return

  saving.value = true
  try {
    await paymentApi.updateMinimumWage(
      form.minimumWage,
      toIsoDate(form.effectiveFrom)!,
      form.minimumWageUnder16 ?? undefined,
    )
    notify.success('Asgari ücret yürürlüğe alındı.')
    formOpen.value = false
  } catch (e) {
    notify.apiError(e, 'Asgari ücret kaydedilemedi.')
    return
  } finally {
    saving.value = false
  }

  // Yeniden yükleme try/catch dışında: kayıt başarılı ama liste yenilenemezse
  // hem başarı hem hata bildirimi çıkmasın.
  await loadHistory()
}

onMounted(() => {
  loadHistory().catch(() => {})
})
</script>
