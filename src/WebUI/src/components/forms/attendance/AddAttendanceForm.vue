<template>
  <FormDialog v-model="open" title="Devamsızlık Ekle" icon="event_busy" :saving="saving" @save="handleSave">
    <q-select
      v-model="form.studentId"
      :options="placementOpts.options.value"
      :loading="placementOpts.loading.value"
      label="Öğrenci *"
      filled
      use-input
      input-debounce="0"
      emit-value
      map-options
      option-label="label"
      option-value="value"
      @filter="placementOpts.filter"
    >
      <template #prepend>
        <q-icon name="person" />
      </template>
      <template #option="{ itemProps, opt }">
        <q-item v-bind="itemProps">
          <q-item-section>
            <q-item-label>{{ opt.label }}</q-item-label>
            <q-item-label caption v-if="opt.caption">{{ opt.caption }}</q-item-label>
          </q-item-section>
        </q-item>
      </template>
      <template #no-option>
        <q-item>
          <q-item-section class="text-grey">Sonuç bulunamadı</q-item-section>
        </q-item>
      </template>
    </q-select>
    <q-input
      :model-value="form.businessName"
      label="İşletme"
      filled
      readonly
      :hint="form.businessId ? '' : 'Öğrenci seçildiğinde otomatik doldurulacaktır'"
    >
      <template #prepend>
        <q-icon name="business" />
      </template>
    </q-input>
    <q-input
      v-model="form.date" label="Tarih" filled type="date"
      :min="weekBounds.min" :max="weekBounds.max"
      hint="Sadece geçerli hafta içi tarih seçilebilir"
    >
      <template #prepend>
        <q-icon name="calendar_today" />
      </template>
    </q-input>
    <q-select
      v-model="form.absenceType"
      :options="absenceTypeOptions"
      label="Devamsızlık Türü"
      filled emit-value map-options
    >
      <template #prepend>
        <q-icon name="category" />
      </template>
    </q-select>
    <q-input v-model="form.reason" label="Gerekçe (opsiyonel)" filled>
      <template #prepend>
        <q-icon name="notes" />
      </template>
    </q-input>
  </FormDialog>
</template>

<script setup lang="ts">
import { ref, reactive, computed, watch } from 'vue'
import { attendanceApi, ABSENCE_TYPES } from 'src/api/attendance'
import { useNotify } from 'src/composables/useNotify'
import { usePlacementOptions } from 'src/composables/useEntityOptions'
import { useAcademicPeriodStore } from 'stores/academicPeriod'
import { useAuthStore } from 'stores/auth'
import FormDialog from 'components/FormDialog.vue'

const open = defineModel<boolean>({ required: true })
const emit = defineEmits<{ saved: [] }>()

const notify = useNotify()
const authStore = useAuthStore()
const periodStore = useAcademicPeriodStore()
const placementOpts = usePlacementOptions()
const saving = ref(false)

const form = reactive({
  studentId: '', businessId: '', businessName: '',
  date: '', absenceType: 'Unexcused', reason: '',
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

watch(open, (isOpen) => {
  if (isOpen) {
    form.studentId = ''
    form.businessId = ''
    form.businessName = ''
    form.date = ''
    form.absenceType = 'Unexcused'
    form.reason = ''
    placementOpts.reset()
    placementOpts.load({ academicPeriodId: periodStore.selectedPeriodId ?? undefined })
  }
})

watch(() => form.studentId, (newId) => {
  if (newId) {
    const biz = placementOpts.getBusinessForStudent(newId)
    form.businessId = biz?.businessId ?? ''
    form.businessName = biz?.businessName ?? ''
  } else {
    form.businessId = ''
    form.businessName = ''
  }
})

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
    open.value = false
    emit('saved')
  } catch (e) {
    notify.apiError(e, 'Kayıt sırasında bir hata oluştu.')
  } finally {
    saving.value = false
  }
}
</script>
