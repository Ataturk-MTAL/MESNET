<template>
  <FormDialog v-model="open" title="Belge Yükle" icon="upload_file" color="secondary" width="400px" save-label="Yükle" :saving="saving" :save-disabled="!form.file || !form.type" @save="handleSave">
    <q-select v-model="form.type" :options="docTypeOptions" label="Belge Tipi *" outlined emit-value map-options>
      <template #prepend><q-icon name="description" /></template>
    </q-select>
    <q-file v-model="form.file" label="Dosya Seç *" outlined accept=".pdf,.jpg,.jpeg,.png">
      <template #prepend><q-icon name="attach_file" /></template>
    </q-file>
    <!-- Ön izleme -->
    <div v-if="form.file" class="q-mt-sm">
      <div class="text-caption text-grey q-mb-xs">Ön İzleme</div>
      <iframe
        v-if="form.file.type === 'application/pdf'"
        :src="filePreviewUrl ?? ''"
        style="width: 100%; height: 300px; border: 1px solid #ddd; border-radius: 4px"
      />
      <img
        v-else
        :src="filePreviewUrl ?? ''"
        style="max-width: 100%; max-height: 300px; border: 1px solid #ddd; border-radius: 4px"
      />
    </div>
  </FormDialog>
</template>

<script setup lang="ts">
import { ref, reactive, computed, watch } from 'vue'
import { businessApi } from 'src/api/business'
import { useNotify } from 'src/composables/useNotify'
import FormDialog from 'components/FormDialog.vue'

const open = defineModel<boolean>({ required: true })

const props = defineProps<{
  businessId: string
}>()

const emit = defineEmits<{ saved: [] }>()

const notify = useNotify()
const saving = ref(false)

const form = reactive({
  type: '',
  file: null as File | null,
})

const docTypeOptions = [
  { label: 'Ustalık Belgesi', value: 'MasteryCertificate' },
  { label: 'Usta Öğreticilik Belgesi', value: 'MasterInstructorCertificate' },
]

const _previewBlobUrl = ref<string | null>(null)

const filePreviewUrl = computed(() => {
  if (!form.file) return null
  if (_previewBlobUrl.value) {
    URL.revokeObjectURL(_previewBlobUrl.value)
  }
  const url = URL.createObjectURL(form.file)
  _previewBlobUrl.value = url
  return url
})

watch(() => form.file, (newFile) => {
  if (!newFile && _previewBlobUrl.value) {
    URL.revokeObjectURL(_previewBlobUrl.value)
    _previewBlobUrl.value = null
  }
})

watch(open, (isOpen) => {
  if (isOpen) {
    form.type = ''
    form.file = null
  }
})

async function handleSave() {
  if (!form.type || !form.file) return
  saving.value = true
  try {
    await businessApi.uploadDocument(props.businessId, form.file, form.type)
    notify.success('Belge yüklendi.')
    open.value = false
    emit('saved')
  } catch (e) {
    notify.apiError(e, 'Belge yüklenirken bir hata oluştu.')
  } finally {
    saving.value = false
  }
}
</script>
