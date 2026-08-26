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
          input-debounce="0"
          emit-value
          map-options
          option-label="label"
          option-value="value"
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
          input-debounce="0"
          emit-value
          map-options
          option-label="label"
          option-value="value"
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
            <SelectEmptyOption />
          </template>
        </q-select>

        <TeacherSelector
          v-model="form.teacherId"
          label="Koordinatör Öğretmen (opsiyonel)"
        />

        <q-input
          v-model="form.startDate"
          label="Başlangıç Tarihi *"
          outlined
          type="date"
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
          @click="handleSave"
        />
      </q-card-actions>
    </q-card>
  </q-page>
</template>

<script setup lang="ts">
import { ref, reactive, computed, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { contractApi } from 'src/api/contract'
import { paymentApi, type ContributionBlockDto } from 'src/api/payment'
import { useNotify } from 'src/composables/useNotify'
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
const businessOpts = useBusinessOptions()

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
  teacherId: '',
  startDate: '',
  agreedMonthlyWage: null as number | null,
})

function goBack() {
  router.push('/internship/contracts').catch(() => {})
}

async function handleSave() {
  saving.value = true
  try {
    await contractApi.create({
      studentId: form.studentId,
      businessId: form.businessId,
      institutionId: authStore.user?.institutionId ?? '',
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
  studentOpts.load()
  businessOpts.reset()
  businessOpts.load()
  // Uyarı bilgilendirmedir; alınamazsa sözleşme kurulmaya devam edebilmeli.
  loadContributionBlocks().catch(() => {})
})
</script>
