<template>
  <FormDialog v-model="open" title="Sözleşmeyi Askıya Al" icon="pause_circle" color="orange" width="420px" save-label="Askıya Al" :saving="saving" @save="handleSave">
        <q-input
          v-model="form.reason"
          label="Askıya alma gerekçesi"
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
import { contractApi } from 'src/api/contract'
import { useNotify } from 'src/composables/useNotify'
import FormDialog from 'components/FormDialog.vue'

const open = defineModel<boolean>({ required: true })

const props = defineProps<{
  contractId: string
}>()

const emit = defineEmits<{ saved: [] }>()

const notify = useNotify()
const saving = ref(false)
const form = reactive({ reason: '' })

watch(open, (isOpen) => {
  if (isOpen) {
    form.reason = ''
  }
})

async function handleSave() {
  saving.value = true
  try {
    await contractApi.suspend(props.contractId, { reason: form.reason })
    notify.success('Sözleşme askıya alındı.')
    open.value = false
    emit('saved')
  } catch (e) {
    notify.apiError(e, 'İşlem sırasında bir hata oluştu.')
  } finally {
    saving.value = false
  }
}
</script>
