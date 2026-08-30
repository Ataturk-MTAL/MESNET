<template>
  <FormDialog
    v-model="open"
    title="Belge Yükle"
    icon="upload_file"
    color="secondary"
    width="400px"
    save-label="Yükle"
    :saving="saving"
    :save-disabled="!form.file || !form.type"
    @save="handleSave"
  >
    <q-select
      v-model="form.type"
      :options="docTypeOptions"
      label="Belge Tipi *"
      outlined
      emit-value
      map-options
    >
      <template #prepend>
        <q-icon name="description" />
      </template>
    </q-select>
    <q-file
      v-model="form.file"
      label="Dosya Seç *"
      outlined
      accept=".pdf,.jpg,.jpeg,.png"
    >
      <template #prepend>
        <q-icon name="attach_file" />
      </template>
    </q-file>
    <!-- Ön izleme -->
    <div
      v-if="form.file"
      class="q-mt-sm"
    >
      <div class="text-caption text-grey-7 q-mb-xs">
        Ön İzleme
      </div>
      <iframe
        v-if="form.file.type === 'application/pdf'"
        :src="filePreviewUrl ?? ''"
        title="Belge ön izlemesi"
        class="doc-preview"
      />
      <img
        v-else
        :src="filePreviewUrl ?? ''"
        alt="Yüklenecek belgenin ön izlemesi"
        class="doc-preview doc-preview--img"
      >
    </div>
  </FormDialog>
</template>

<script setup lang="ts">
import { ref, reactive, watch, onBeforeUnmount } from 'vue'
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

// Blob URL yaşam döngüsü computed içinde yönetiliyordu: computed önbelleklenir, yeniden
// değerlendirilmesi öngörülemez ve içinde ref'e yazmak reaktivite döngüsü riski taşır.
// Yan etki watch'a taşındı — her dosya değişiminde önceki URL serbest bırakılır (#68).
const filePreviewUrl = ref<string | null>(null)

watch(() => form.file, (newFile) => {
  if (filePreviewUrl.value) {
    URL.revokeObjectURL(filePreviewUrl.value)
    filePreviewUrl.value = null
  }
  if (newFile) {
    filePreviewUrl.value = URL.createObjectURL(newFile)
  }
})

onBeforeUnmount(() => {
  if (filePreviewUrl.value) URL.revokeObjectURL(filePreviewUrl.value)
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

<style scoped>
.doc-preview {
  width: 100%;
  height: 300px;
  border-radius: 8px;
  border: 1px solid rgba(30, 58, 95, 0.14);
  border: 1px solid color-mix(in srgb, var(--q-primary) 14%, transparent);
}

.doc-preview--img {
  width: auto;
  height: auto;
  max-width: 100%;
  max-height: 300px;
}
</style>
