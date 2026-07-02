<template>
  <q-page padding>
    <div class="row items-center q-mb-lg q-mx-auto" style="max-width: 640px">
      <q-btn flat round dense icon="arrow_back" aria-label="Devamsızlık listesine dön" class="q-mr-sm" @click="goBack">
        <q-tooltip>Devamsızlık listesine dön</q-tooltip>
      </q-btn>
      <div class="text-h5 text-weight-bold col">Devamsızlık Ekle</div>
    </div>

    <q-card flat bordered style="max-width: 640px" class="q-mx-auto">
      <q-card-section class="q-gutter-md">
        <q-select
          v-model="form.studentId"
          :options="placementOpts.options.value"
          :loading="placementOpts.loading.value"
          label="Öğrenci *"
          outlined
          use-input
          input-debounce="0"
          emit-value
          map-options
          option-label="label"
          option-value="value"
          @filter="placementOpts.filter"
        >
          <template #prepend><q-icon name="person" /></template>
          <template #option="{ itemProps, opt }">
            <q-item v-bind="itemProps">
              <q-item-section>
                <q-item-label>{{ opt.label }}</q-item-label>
                <q-item-label v-if="opt.caption" caption>{{ opt.caption }}</q-item-label>
              </q-item-section>
            </q-item>
          </template>
          <template #no-option><SelectEmptyOption /></template>
        </q-select>
        <q-input
          :model-value="form.businessName"
          label="İşletme"
          outlined
          readonly
          :hint="form.businessId ? '' : 'Öğrenci seçildiğinde otomatik doldurulacaktır'"
        >
          <template #prepend><q-icon name="business" /></template>
        </q-input>
        <q-input
          v-model="form.date"
          label="Tarih"
          outlined
          type="date"
          :min="weekBounds.min"
          :max="weekBounds.max"
          hint="Sadece geçerli hafta içi tarih seçilebilir"
        >
          <template #prepend><q-icon name="calendar_today" /></template>
        </q-input>
        <q-select
          v-model="form.absenceType"
          :options="absenceTypeOptions"
          label="Devamsızlık Türü"
          outlined
          emit-value
          map-options
        >
          <template #prepend><q-icon name="category" /></template>
        </q-select>
        <q-input v-model="form.reason" label="Gerekçe (opsiyonel)" outlined>
          <template #prepend><q-icon name="notes" /></template>
        </q-input>
      </q-card-section>

      <q-separator />
      <q-card-actions align="right" class="q-pa-md">
        <q-btn flat label="İptal" color="grey-7" @click="goBack" />
        <q-btn unelevated color="primary" label="Kaydet" :loading="saving" @click="handleSave" />
      </q-card-actions>
    </q-card>
  </q-page>
</template>

<script setup lang="ts">
import { ref, reactive, computed, watch, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { attendanceApi, ABSENCE_TYPES } from 'src/api/attendance'
import { useNotify } from 'src/composables/useNotify'
import { usePlacementOptions } from 'src/composables/useEntityOptions'
import { useAcademicPeriodStore } from 'stores/academicPeriod'
import { useAuthStore } from 'stores/auth'
import SelectEmptyOption from 'components/SelectEmptyOption.vue'

const router = useRouter()
const notify = useNotify()
const authStore = useAuthStore()
const periodStore = useAcademicPeriodStore()
const placementOpts = usePlacementOptions()
const saving = ref(false)

const form = reactive({
  studentId: '',
  businessId: '',
  businessName: '',
  date: '',
  absenceType: 'Unexcused',
  reason: '',
})

const absenceTypeOptions = ABSENCE_TYPES.map((t) => ({ label: t.label, value: t.value }))

const weekBounds = computed(() => {
  const today = new Date()
  const day = today.getDay()
  const diffToMonday = day === 0 ? -6 : 1 - day
  const monday = new Date(today)
  monday.setDate(today.getDate() + diffToMonday)
  const sunday = new Date(monday)
  sunday.setDate(monday.getDate() + 6)
  const fmt = (d: Date) => d.toISOString().slice(0, 10)
  return { min: fmt(monday), max: fmt(sunday) }
})

watch(
  () => form.studentId,
  (newId) => {
    if (newId) {
      const biz = placementOpts.getBusinessForStudent(newId)
      form.businessId = biz?.businessId ?? ''
      form.businessName = biz?.businessName ?? ''
    } else {
      form.businessId = ''
      form.businessName = ''
    }
  },
)

function goBack() {
  void router.push('/attendance')
}

async function handleSave() {
  saving.value = true
  try {
    await attendanceApi.create({
      studentId: form.studentId,
      businessId: form.businessId,
      institutionId: authStore.user?.institutionId ?? '',
      academicPeriodId: periodStore.selectedPeriodId ?? '',
      date: new Date(form.date).toISOString(),
      absenceType: form.absenceType,
      reason: form.reason || undefined,
    })
    notify.success('Devamsızlık kaydedildi.')
    goBack()
  } catch (e) {
    notify.apiError(e, 'Kayıt sırasında bir hata oluştu.')
  } finally {
    saving.value = false
  }
}

onMounted(() => {
  placementOpts.reset()
  placementOpts.load({ academicPeriodId: periodStore.selectedPeriodId ?? undefined })
})
</script>
