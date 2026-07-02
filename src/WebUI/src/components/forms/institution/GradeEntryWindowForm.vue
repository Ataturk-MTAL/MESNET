<template>
  <FormDialog
    v-model="open"
    title="Not Giriş Penceresi"
    icon="event_available"
    color="teal"
    save-label="Pencereyi Aç"
    :saving="saving"
    :save-disabled="!form.startDate || !form.endDate"
    @save="handleSave"
  >
    <AppNotice
      type="info"
      dense
      rounded
      class="text-caption"
      message="Bu tarih aralığında işletmeler, öğrencilerinin dönem notlarını girebilir. Aralık dışında giriş kapalıdır."
    />
    <div class="text-caption text-grey-7 q-mb-sm">
      Dönem: <strong>{{ period?.name }}</strong>
    </div>
    <div class="row q-col-gutter-md">
      <div class="col-6">
        <q-input v-model="form.startDate" label="Başlangıç Tarihi *" outlined type="date">
          <template #prepend>
            <q-icon name="calendar_today" />
          </template>
        </q-input>
      </div>
      <div class="col-6">
        <q-input v-model="form.endDate" label="Bitiş Tarihi *" outlined type="date">
          <template #prepend>
            <q-icon name="event" />
          </template>
        </q-input>
      </div>
    </div>
  </FormDialog>
</template>

<script setup lang="ts">
import { ref, reactive, watch } from 'vue'
import { institutionApi, type AcademicPeriodDto } from 'src/api/institution'
import { useNotify } from 'src/composables/useNotify'
import FormDialog from 'components/FormDialog.vue'
import AppNotice from 'components/AppNotice.vue'

const open = defineModel<boolean>({ required: true })

const props = defineProps<{
  institutionId: string
  period: AcademicPeriodDto | null
}>()

const emit = defineEmits<{ saved: [] }>()

const notify = useNotify()
const saving = ref(false)

const form = reactive({
  startDate: '',
  endDate: '',
})

watch(open, (isOpen) => {
  if (isOpen && props.period) {
    form.startDate = props.period.gradeEntryStartDate ?? ''
    form.endDate = props.period.gradeEntryEndDate ?? ''
  }
})

async function handleSave() {
  if (!props.period) return
  saving.value = true
  try {
    await institutionApi.setGradeEntryWindow(props.institutionId, props.period.id, {
      startDate: form.startDate,
      endDate: form.endDate,
    })
    notify.success('Not giriş penceresi ayarlandı.')
    open.value = false
    emit('saved')
  } catch (e) {
    notify.apiError(e, 'Not giriş penceresi ayarlanırken bir hata oluştu.')
  } finally {
    saving.value = false
  }
}
</script>
