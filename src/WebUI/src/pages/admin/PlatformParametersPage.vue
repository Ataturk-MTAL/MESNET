<template>
  <q-page padding>
    <PageHeader
      title="Ulusal Parametreler"
      subtitle="Bu değerler tüm kurumlar için geçerlidir."
    />

    <q-card
      flat
      bordered
      style="max-width: 640px"
      class="relative-position"
    >
      <q-inner-loading :showing="loading" />
      <q-card-section class="q-gutter-md">
        <q-input
          v-model.number="stuckApprovalDays"
          label="Tıkanmış onay eşiği (gün)"
          type="number"
          outlined
          hint="Bir fesih onay zinciri kaç günden sonra müdürlük panosunda tıkanmış sayılsın."
          :rules="[thresholdRule]"
          lazy-rules
        >
          <template #prepend>
            <q-icon name="hourglass_bottom" />
          </template>
        </q-input>
      </q-card-section>

      <q-separator />
      <q-card-actions
        align="right"
        class="q-pa-md"
      >
        <q-btn
          unelevated
          color="primary"
          label="Kaydet"
          :loading="saving"
          @click="save"
        />
      </q-card-actions>
    </q-card>
  </q-page>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { internshipApi } from 'src/api/internship'
import { useNotify } from 'src/composables/useNotify'
import PageHeader from 'components/PageHeader.vue'

/** Backend ile AYNI aralık — sunucu da 1..365 dışını reddeder (422). */
const MIN_DAYS = 1
const MAX_DAYS = 365

const notify = useNotify()

const stuckApprovalDays = ref<number>(14)
const loading = ref(false)
const saving = ref(false)

function thresholdRule(value: number): true | string {
  if (value >= MIN_DAYS && value <= MAX_DAYS) return true
  return `Eşik ${MIN_DAYS} ile ${MAX_DAYS} gün arasında olmalıdır.`
}

async function loadConfig() {
  loading.value = true
  try {
    const { data } = await internshipApi.getApprovalConfig()
    stuckApprovalDays.value = data.stuckApprovalDays
  } catch (e) {
    notify.apiError(e, 'Parametreler yüklenemedi.')
  } finally {
    loading.value = false
  }
}

async function save() {
  // Sayfada q-form yok; :rules kaydetmeyi kendiliğinden engellemez.
  if (thresholdRule(stuckApprovalDays.value) !== true) {
    notify.error(`Eşik ${MIN_DAYS} ile ${MAX_DAYS} gün arasında olmalıdır.`)
    return
  }

  saving.value = true
  try {
    await internshipApi.updateApprovalConfig({ stuckApprovalDays: stuckApprovalDays.value })
    notify.success('Parametreler güncellendi.')
  } catch (e) {
    notify.apiError(e, 'Güncelleme sırasında bir hata oluştu.')
  } finally {
    saving.value = false
  }
}

onMounted(() => {
  loadConfig().catch(() => {})
})
</script>
