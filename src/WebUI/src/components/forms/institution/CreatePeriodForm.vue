<template>
  <FormDialog
    v-model="open"
    title="Yeni Akademik Dönem"
    icon="date_range"
    color="indigo"
    save-label="Oluştur"
    :saving="saving"
    :save-disabled="!form.name || !form.startYear || !form.endYear || !form.startDate || !form.endDate"
    @save="handleSave"
  >
    <AppNotice
      type="warning"
      dense
      rounded
      class="text-caption"
      message="Yeni dönem oluşturulduğunda mevcut aktif dönem otomatik kapatılır."
    />
    <q-input
      v-model="form.name"
      label="Dönem Adı *"
      outlined
      hint="Örn: 2025-2026"
    >
      <template #prepend>
        <q-icon name="label" />
      </template>
    </q-input>
    <div class="row q-col-gutter-md">
      <div class="col-6">
        <q-input
          v-model.number="form.startYear"
          label="Başlangıç Yılı *"
          outlined
          type="number"
        >
          <template #prepend>
            <q-icon name="event" />
          </template>
        </q-input>
      </div>
      <div class="col-6">
        <q-input
          v-model.number="form.endYear"
          label="Bitiş Yılı *"
          outlined
          type="number"
        >
          <template #prepend>
            <q-icon name="event" />
          </template>
        </q-input>
      </div>
    </div>
    <div class="row q-col-gutter-md">
      <div class="col-6">
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
      </div>
      <div class="col-6">
        <q-input
          v-model="form.endDate"
          label="Bitiş Tarihi *"
          outlined
          type="date"
        >
          <template #prepend>
            <q-icon name="calendar_today" />
          </template>
        </q-input>
      </div>
    </div>
  </FormDialog>
</template>

<script setup lang="ts">
import { ref, reactive, watch } from 'vue'
import { institutionApi } from 'src/api/institution'
import { useNotify } from 'src/composables/useNotify'
import { useAcademicPeriodStore } from 'stores/academicPeriod'
import FormDialog from 'components/FormDialog.vue'
import AppNotice from 'components/AppNotice.vue'

const open = defineModel<boolean>({ required: true })

const props = defineProps<{
  institutionId: string
}>()

const emit = defineEmits<{ saved: [] }>()

const notify = useNotify()
const periodStore = useAcademicPeriodStore()
const saving = ref(false)

const form = reactive({
  name: '',
  startYear: new Date().getFullYear(),
  endYear: new Date().getFullYear() + 1,
  startDate: '',
  endDate: '',
})

watch(open, (isOpen) => {
  if (isOpen) {
    const year = new Date().getFullYear()
    form.name = `${year}-${year + 1}`
    form.startYear = year
    form.endYear = year + 1
    form.startDate = `${year}-09-08`
    form.endDate = `${year + 1}-06-19`
  }
})

async function handleSave() {
  saving.value = true
  try {
    await institutionApi.createAcademicPeriod(props.institutionId, {
      name: form.name,
      startYear: form.startYear,
      endYear: form.endYear,
      startDate: form.startDate,
      endDate: form.endDate,
    })
    notify.success('Akademik dönem oluşturuldu.')
    open.value = false
    emit('saved')
    await periodStore.loadPeriods()
  } catch (e) {
    notify.apiError(e, 'Dönem oluşturulurken bir hata oluştu.')
  } finally {
    saving.value = false
  }
}
</script>
