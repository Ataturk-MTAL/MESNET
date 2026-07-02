<template>
  <FormDialog v-model="open" title="Sözleşmeyi Feshet" icon="gavel" color="negative" width="460px" save-label="Feshet" :saving="saving" @save="handleSave">
        <q-select
          v-model="form.reasonType"
          :options="terminationReasonOptions"
          label="Fesih Sebebi *"
          outlined
          emit-value
          map-options
        >
          <template #prepend><q-icon name="category" /></template>
        </q-select>
        <q-input
          v-model="form.reason"
          label="Açıklama"
          outlined
          type="textarea"
          :rows="3"
          autogrow
        >
          <template #prepend><q-icon name="notes" /></template>
        </q-input>
  </FormDialog>
</template>

<script setup lang="ts">
import { ref, reactive, watch } from 'vue'
import { contractApi, TERMINATION_REASONS } from 'src/api/contract'
import { useNotify } from 'src/composables/useNotify'
import FormDialog from 'components/FormDialog.vue'

const open = defineModel<boolean>({ required: true })

const props = defineProps<{
  contractId: string
}>()

const emit = defineEmits<{ saved: [] }>()

const notify = useNotify()
const saving = ref(false)

const form = reactive({
  reasonType: '',
  reason: '',
})

const terminationReasonOptions = TERMINATION_REASONS.map((r) => ({ label: r.label, value: r.value }))

watch(open, (isOpen) => {
  if (isOpen) {
    form.reasonType = ''
    form.reason = ''
  }
})

async function handleSave() {
  saving.value = true
  try {
    await contractApi.terminate(props.contractId, {
      reason: form.reason,
      reasonType: form.reasonType,
    })
    notify.success('Sözleşme feshedildi.')
    open.value = false
    emit('saved')
  } catch (e) {
    notify.apiError(e, 'Fesih sırasında bir hata oluştu.')
  } finally {
    saving.value = false
  }
}
</script>
