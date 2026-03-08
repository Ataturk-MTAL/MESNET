<template>
  <FormDialog v-model="open" title="Evrak Yükle" icon="upload_file" color="secondary" width="460px" save-label="Yükle" :saving="saving" :save-disabled="!form.file || !form.documentType || !form.uploadedBy" @save="handleSave">
        <q-select
          v-model="form.documentType"
          :options="documentTypeOptions"
          label="Evrak Türü *"
          filled
          emit-value
          map-options
        >
          <template #prepend><q-icon name="category" /></template>
        </q-select>

        <q-file
          v-model="form.file"
          label="PDF Dosyası *"
          filled
          accept=".pdf"
        >
          <template #prepend><q-icon name="attach_file" /></template>
          <template #hint>Yalnızca PDF, maks. 10 MB</template>
        </q-file>

        <q-input
          v-model="form.description"
          label="Açıklama (opsiyonel)"
          filled
        >
          <template #prepend><q-icon name="notes" /></template>
        </q-input>

        <q-input
          v-model="form.uploadedBy"
          label="Yükleyen *"
          filled
        >
          <template #prepend><q-icon name="person" /></template>
        </q-input>
  </FormDialog>
</template>

<script setup lang="ts">
import { ref, reactive, watch } from 'vue'
import { contractApi, DOCUMENT_TYPES } from 'src/api/contract'
import { useNotify } from 'src/composables/useNotify'
import { useAuthStore } from 'stores/auth'
import FormDialog from 'components/FormDialog.vue'

const open = defineModel<boolean>({ required: true })

const props = defineProps<{
  contractId: string
}>()

const emit = defineEmits<{ saved: [] }>()

const notify = useNotify()
const authStore = useAuthStore()
const saving = ref(false)

const form = reactive({
  documentType: 'SignedContract' as 'SignedContract' | 'TerminationLetter' | 'Other',
  file: null as File | null,
  description: '',
  uploadedBy: '',
})

const documentTypeOptions = DOCUMENT_TYPES.map((d) => ({ label: d.label, value: d.value }))

watch(open, (isOpen) => {
  if (isOpen) {
    form.documentType = 'SignedContract'
    form.file = null
    form.description = ''
    form.uploadedBy = authStore.user?.fullName ?? ''
  }
})

async function handleSave() {
  if (!form.file) return
  saving.value = true
  try {
    await contractApi.uploadDocument(props.contractId, {
      documentType: form.documentType,
      file: form.file,
      description: form.description || undefined,
      uploadedBy: form.uploadedBy,
    })
    notify.success('Evrak başarıyla yüklendi.')
    open.value = false
    emit('saved')
  } catch (e) {
    notify.apiError(e, 'Evrak yüklenirken bir hata oluştu.')
  } finally {
    saving.value = false
  }
}
</script>
