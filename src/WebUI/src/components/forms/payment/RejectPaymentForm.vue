<template>
  <FormDialog v-model="open" title="Reddetme Gerekçesi" icon="cancel" color="negative" width="400px" save-label="Reddet" :saving="saving" @save="handleSave">
    <q-input v-model="reason" label="Gerekçe" outlined type="textarea" rows="3">
      <template #prepend>
        <q-icon name="notes" />
      </template>
    </q-input>
  </FormDialog>
</template>

<script setup lang="ts">
import { ref, watch } from 'vue'
import { paymentApi } from 'src/api/payment'
import { useNotify } from 'src/composables/useNotify'
import FormDialog from 'components/FormDialog.vue'

const open = defineModel<boolean>({ required: true })

const props = defineProps<{
  paymentId: string
}>()

const emit = defineEmits<{ saved: [] }>()

const notify = useNotify()
const saving = ref(false)
const reason = ref('')

watch(open, (isOpen) => {
  if (isOpen) {
    reason.value = ''
  }
})

async function handleSave() {
  saving.value = true
  try {
    await paymentApi.reject(props.paymentId, reason.value)
    notify.success('Reddedildi.')
    open.value = false
    emit('saved')
  } catch (e) {
    notify.apiError(e, 'İşlem sırasında bir hata oluştu.')
  } finally {
    saving.value = false
  }
}
</script>
