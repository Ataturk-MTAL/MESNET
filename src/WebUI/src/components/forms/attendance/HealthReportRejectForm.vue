<template>
  <FormDialog
    v-model="open"
    title="Sağlık Raporunu Reddet"
    icon="report_off"
    color="negative"
    width="420px"
    save-label="Reddet"
    save-color="negative"
    :saving="saving"
    :save-disabled="!reason.trim()"
    @save="handleSave"
  >
    <q-banner
      dense
      class="bg-orange-1 text-warning q-mb-sm"
    >
      <template #avatar>
        <q-icon name="warning" />
      </template>
      Reddedilen rapor devamsızlık türünü değiştirmez — ücret kesintisi uygulanmaya devam eder.
    </q-banner>

    <q-input
      v-model="reason"
      label="Ret gerekçesi"
      outlined
      autogrow
      counter
      :maxlength="500"
    >
      <template #prepend>
        <q-icon name="notes" />
      </template>
    </q-input>
  </FormDialog>
</template>

<script setup lang="ts">
import { ref, watch } from 'vue'
import { attendanceApi } from 'src/api/attendance'
import { useNotify } from 'src/composables/useNotify'
import FormDialog from 'components/FormDialog.vue'

const open = defineModel<boolean>({ required: true })

const props = defineProps<{ recordId: string }>()
const emit = defineEmits<{ saved: [] }>()

const notify = useNotify()
const saving = ref(false)
const reason = ref('')

watch(open, (isOpen) => {
  if (isOpen) reason.value = ''
})

async function handleSave() {
  if (!reason.value.trim()) return

  saving.value = true
  try {
    await attendanceApi.rejectHealthReport(props.recordId, reason.value.trim())
    notify.success('Sağlık raporu reddedildi.')
    open.value = false
    emit('saved')
  } catch (e) {
    notify.apiError(e, 'Ret işlemi sırasında bir hata oluştu.')
  } finally {
    saving.value = false
  }
}
</script>
