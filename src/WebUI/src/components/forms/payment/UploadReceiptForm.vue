<template>
  <FormDialog
    v-model="open"
    title="Dekont Yükle"
    icon="upload_file"
    color="secondary"
    width="400px"
    save-label="Yükle"
    :saving="saving"
    :save-disabled="!file"
    @save="handleSave"
  >
    <q-file
      v-model="file"
      label="Dosya Seç"
      outlined
      accept=".pdf,.jpg,.jpeg,.png"
    >
      <template #prepend>
        <q-icon name="attach_file" />
      </template>
    </q-file>
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
  uploadType: 'business' | 'student'
}>()

const emit = defineEmits<{ saved: [] }>()

const notify = useNotify()
const saving = ref(false)
const file = ref<File | null>(null)

watch(open, (isOpen) => {
  if (isOpen) {
    file.value = null
  }
})

async function handleSave() {
  if (!file.value) return
  saving.value = true
  try {
    if (props.uploadType === 'business') {
      await paymentApi.uploadReceiptBusiness(props.paymentId, file.value)
    } else {
      await paymentApi.uploadReceiptStudent(props.paymentId, file.value)
    }
    notify.success('Dekont yüklendi.')
    open.value = false
    emit('saved')
  } catch (e) {
    notify.apiError(e, 'Dekont yüklenirken bir hata oluştu.')
  } finally {
    saving.value = false
  }
}
</script>
