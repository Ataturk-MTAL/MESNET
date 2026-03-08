<template>
  <FormDialog v-model="open" title="Reddetme Gerekçesi" icon="cancel" color="negative" width="400px" save-label="Reddet" :saving="saving" @save="handleSave">
    <q-input v-model="form.reason" label="Gerekçe *" filled type="textarea" rows="3" :error="!!errors.reason" :error-message="errors.reason">
      <template #prepend><q-icon name="notes" /></template>
    </q-input>
  </FormDialog>
</template>

<script setup lang="ts">
import { ref, reactive, watch } from 'vue'
import { businessApi } from 'src/api/business'
import { rejectBusinessSchema } from 'src/schemas/business'
import { useNotify } from 'src/composables/useNotify'
import { zodValidate } from 'src/composables/useZodValidation'
import FormDialog from 'components/FormDialog.vue'

const open = defineModel<boolean>({ required: true })

const props = defineProps<{
  businessId: string
}>()

const emit = defineEmits<{ saved: [] }>()

const notify = useNotify()
const saving = ref(false)
const form = reactive({ reason: '' })
const errors = reactive<Record<string, string>>({})

watch(open, (isOpen) => {
  if (isOpen) {
    form.reason = ''
    for (const key of Object.keys(errors)) errors[key] = ''
  }
})

async function handleSave() {
  if (!zodValidate(rejectBusinessSchema, form, errors)) return
  saving.value = true
  try {
    await businessApi.reject(props.businessId, form.reason)
    notify.success('İşletme reddedildi.')
    open.value = false
    emit('saved')
  } catch (e) {
    notify.apiError(e, 'Reddetme sırasında bir hata oluştu.')
  } finally {
    saving.value = false
  }
}
</script>
