<template>
  <q-page padding>
    <div
      class="row items-center q-mb-lg q-mx-auto"
      style="max-width: 640px"
    >
      <q-btn
        flat
        round
        dense
        icon="arrow_back"
        aria-label="Sözleşmelere dön"
        class="q-mr-sm"
        @click="goBack"
      >
        <q-tooltip>Sözleşmelere dön</q-tooltip>
      </q-btn>
      <h1 class="text-h5 text-weight-bold col q-my-none">
        Yeni Sözleşme
      </h1>
    </div>

    <q-card
      flat
      bordered
      style="max-width: 640px"
      class="q-mx-auto"
    >
      <q-card-section class="q-gutter-md">
        <q-select
          v-model="form.studentId"
          :options="studentOpts.options.value"
          :loading="studentOpts.loading.value"
          label="Öğrenci *"
          outlined
          use-input
          hide-selected
          fill-input
          input-debounce="0"
          emit-value
          map-options
          option-label="label"
          option-value="value"
          :error="!!errors.studentId"
          :error-message="errors.studentId"
          @filter="studentOpts.filter"
        >
          <template #prepend>
            <q-icon name="school" />
          </template>
          <template #option="{ itemProps, opt }">
            <q-item v-bind="itemProps">
              <q-item-section>
                <q-item-label>{{ opt.label }}</q-item-label>
                <q-item-label
                  v-if="opt.caption"
                  caption
                >
                  {{ opt.caption }}
                </q-item-label>
              </q-item-section>
            </q-item>
          </template>
          <template #no-option>
            <SelectEmptyOption />
          </template>
        </q-select>

        <!--
          Sınıf tekrarı katkı blokesi (#161). İşletme bunu sözleşme kurulurken bilmeli;
          ayın sonunda dekont gelirken öğrenmesi "neden katkı gelmedi" çağrısı doğurur.
        -->
        <AppNotice
          v-if="contributionBlock"
          type="warning"
          dense
          icon="info"
        >
          Bu öğrenci <strong>{{ contributionBlock.classYear }}. sınıfı tekrar ediyor</strong> ve
          bu sınıf yılı için devlet katkısı zaten alınmış ({{ contributionBlock.firstClaimedMonth }}).
          Öğrencinin ücreti değişmez; <strong>devlet katkısı ödenmez</strong>, dolayısıyla
          işletmenin ödeyeceği tutar yükselir.
        </AppNotice>

        <q-select
          v-model="form.businessId"
          :options="businessOpts.options.value"
          :loading="businessOpts.loading.value"
          label="İşletme *"
          outlined
          use-input
          hide-selected
          fill-input
          input-debounce="0"
          emit-value
          map-options
          option-label="label"
          option-value="value"
          :disable="studentBranchLoading"
          :hint="businessHint"
          :error="!!errors.businessId"
          :error-message="errors.businessId"
          @filter="businessOpts.filter"
        >
          <template #prepend>
            <q-icon name="business" />
          </template>
          <template #option="{ itemProps, opt }">
            <q-item v-bind="itemProps">
              <q-item-section>
                <q-item-label>{{ opt.label }}</q-item-label>
                <q-item-label
                  v-if="opt.caption"
                  caption
                >
                  {{ opt.caption }}
                </q-item-label>
              </q-item-section>
            </q-item>
          </template>
          <template #no-option>
            <SelectEmptyOption :text="emptyBusinessText" />
          </template>
        </q-select>

        <TeacherSelector
          v-model="form.teacherId"
          label="Koordinatör Öğretmen (opsiyonel)"
          :branch-code="studentBranchCode"
        />

        <q-input
          v-model="form.startDate"
          label="Başlangıç Tarihi *"
          outlined
          type="date"
          :error="!!errors.startDate"
          :error-message="errors.startDate"
        >
          <template #prepend>
            <q-icon name="calendar_today" />
          </template>
        </q-input>

        <q-input
          v-model.number="form.agreedMonthlyWage"
          label="Anlaşılan Aylık Ücret (₺)"
          outlined
          type="number"
          min="0"
          step="0.01"
          hint="Boş bırakılırsa 3308 sayılı Kanun'daki yasal taban uygulanır. Yasal tabanın altında bir tutar girilse bile taban ödenir."
          :error="!!errors.agreedMonthlyWage"
          :error-message="errors.agreedMonthlyWage"
        >
          <template #prepend>
            <q-icon name="payments" />
          </template>
        </q-input>
      </q-card-section>

      <q-separator />
      <q-card-actions
        align="right"
        class="q-pa-md"
      >
        <q-btn
          flat
          label="İptal"
          color="grey-7"
          @click="goBack"
        />
        <q-btn
          unelevated
          color="primary"
          label="Oluştur"
          :loading="saving"
          :disable="submitAttempted && !isValid"
          @click="handleSave"
        />
      </q-card-actions>
    </q-card>
  </q-page>
</template>

<script setup lang="ts">
import { ref, reactive, computed, watch, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { contractApi } from 'src/api/contract'
import { enrollmentApi } from 'src/api/enrollment'
import { paymentApi, type ContributionBlockDto } from 'src/api/payment'
import { createContractSchema } from 'src/schemas/contract'
import { useNotify } from 'src/composables/useNotify'
import { zodValidate } from 'src/composables/useZodValidation'
import { useStudentOptions, useBusinessOptions } from 'src/composables/useEntityOptions'
import { useAuthStore } from 'stores/auth'
import TeacherSelector from 'components/TeacherSelector.vue'
import SelectEmptyOption from 'components/SelectEmptyOption.vue'
import AppNotice from 'components/AppNotice.vue'

const router = useRouter()
const notify = useNotify()
const authStore = useAuthStore()
const saving = ref(false)
const studentOpts = useStudentOptions()

/**
 * Seçili öğrencinin alan kodu (#119): işletme seçicisi yalnız bu alandan öğrenci almaya
 * yetkili işletmeleri, öğretmen seçicisi önce bu alanın öğretmenlerini gösterir. Öğrenci
 * seçim listesi alan kodunu ayrı alan olarak taşımadığı için öğrenci kaydından okunur.
 */
const studentBranchCode = ref<string | null>(null)
const studentBranchLoading = ref(false)
const businessOpts = useBusinessOptions({ branchCode: studentBranchCode })

const businessHint = computed(() =>
  studentBranchCode.value
    ? `Yalnız '${studentBranchCode.value}' alanından öğrenci almaya yetkili işletmeler listelenir`
    : undefined,
)

const emptyBusinessText = computed(() =>
  studentBranchCode.value
    ? `'${studentBranchCode.value}' alanından öğrenci almaya yetkili işletme bulunamadı`
    : 'Sonuç bulunamadı',
)

/**
 * Katkısı bloke öğrenciler (#161). Liste küçük olduğu için tümü bir kez çekilir; seçilen
 * öğrenci değiştikçe yeni istek atılmaz.
 */
const contributionBlocks = ref<ContributionBlockDto[]>([])

const contributionBlock = computed(() =>
  contributionBlocks.value.find((b) => b.studentId === form.studentId) ?? null,
)

const form = reactive({
  studentId: '',
  businessId: '',
  teacherId: '' as string | null,
  startDate: '',
  agreedMonthlyWage: null as number | null,
})
const errors = reactive<Record<string, string>>({})

// İlk "Oluştur" denemesine kadar alanlar kırmızıya boyanmaz; sonrasında hata mesajları
// kullanıcı düzelttikçe canlı güncellenir ve form geçerli olana dek buton kapalı kalır.
const submitAttempted = ref(false)
const isValid = computed(() => createContractSchema.safeParse(form).success)

watch(form, () => {
  if (submitAttempted.value) zodValidate(createContractSchema, form, errors)
})

async function loadStudentBranch(studentId: string) {
  if (!studentId) {
    studentBranchCode.value = null
    studentBranchLoading.value = false
    return
  }
  studentBranchLoading.value = true
  try {
    const { data } = await enrollmentApi.getStudent(studentId)
    // Yanıt gelene kadar seçim değiştiyse eski yanıtı uygulama
    if (form.studentId !== studentId) return
    studentBranchCode.value = data.branchCode || null
  } catch (e: unknown) {
    if (form.studentId !== studentId) return
    studentBranchCode.value = null
    notify.apiError(e, 'Öğrencinin alan bilgisi alınamadı; işletme listesi süzülmeden gösteriliyor.')
  } finally {
    // Bayat istek yükleme durumunu indirmez: yeni öğrencinin isteği hâlâ sürüyor olabilir.
    if (form.studentId === studentId) studentBranchLoading.value = false
  }
}

watch(
  () => form.studentId,
  (studentId) => {
    loadStudentBranch(studentId).catch(() => {})
  },
)

// Alan değişince görünür listeyi yenile; seçili işletme yeni alana yetkili değilse temizle.
watch(businessOpts.allOptions, (options) => {
  businessOpts.options.value = options
  if (form.businessId && !options.some((o) => o.value === form.businessId)) {
    form.businessId = ''
  }
})

function goBack() {
  router.push('/internship/contracts').catch(() => {})
}

async function handleSave() {
  submitAttempted.value = true
  if (!zodValidate(createContractSchema, form, errors)) return

  const institutionId = authStore.currentInstitutionId
  if (!institutionId) {
    notify.error('Kurum bağlamı bulunamadı; sözleşme bir okula bağlanmadan oluşturulamaz.')
    return
  }

  saving.value = true
  try {
    await contractApi.create({
      studentId: form.studentId,
      businessId: form.businessId,
      institutionId,
      teacherId: form.teacherId || undefined,
      startDate: new Date(form.startDate).toISOString(),
      // Boş bırakıldıysa alanı hiç gönderme — backend null'ı "yasal taban uygula" diye yorumluyor
      ...(form.agreedMonthlyWage ? { agreedMonthlyWage: form.agreedMonthlyWage } : {}),
    })
    notify.success('Sözleşme oluşturuldu.')
    goBack()
  } catch (e) {
    notify.apiError(e, 'Sözleşme oluşturulurken bir hata oluştu.')
  } finally {
    saving.value = false
  }
}

async function loadContributionBlocks() {
  const { data } = await paymentApi.contributionBlocks()
  contributionBlocks.value = data
}

onMounted(() => {
  studentOpts.reset()
  studentOpts.load().catch((e: unknown) => notify.apiError(e, 'Öğrenci listesi yüklenemedi.'))
  businessOpts.reset()
  businessOpts.load().catch((e: unknown) => notify.apiError(e, 'İşletme listesi yüklenemedi.'))
  // Uyarı bilgilendirmedir; alınamazsa sözleşme kurulmaya devam edebilmeli.
  loadContributionBlocks().catch(() => {})
})
</script>

