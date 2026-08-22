<template>
  <FormDialog
    v-model="open"
    title="Sağlık Raporu Yükle"
    icon="medical_information"
    color="primary"
    width="420px"
    save-label="Yükle"
    :saving="saving"
    :save-disabled="!file"
    @save="handleSave"
  >
    <q-banner
      v-if="requiresApproval"
      dense
      class="bg-blue-1 text-primary q-mb-sm"
    >
      <template #avatar>
        <q-icon name="info" />
      </template>
      Yüklediğiniz rapor koordinatör öğretmen onayına düşer. Onaylanana kadar devamsızlık türü
      değişmez.
    </q-banner>

    <q-file
      v-model="file"
      label="Rapor dosyası"
      outlined
      :accept="HEALTH_REPORT_ACCEPT"
      :max-file-size="HEALTH_REPORT_MAX_BYTES"
      clearable
      @rejected="onRejected"
    >
      <template #prepend>
        <q-icon name="attach_file" />
      </template>
    </q-file>
    <div class="text-caption text-grey-7">
      PDF, JPEG veya PNG — en fazla 10 MB.
    </div>
  </FormDialog>
</template>

<script setup lang="ts">
import { ref, watch } from 'vue'
import { attendanceApi, HEALTH_REPORT_ACCEPT, HEALTH_REPORT_MAX_BYTES } from 'src/api/attendance'
import { useNotify } from 'src/composables/useNotify'
import FormDialog from 'components/FormDialog.vue'

const open = defineModel<boolean>({ required: true })

const props = defineProps<{
  recordId: string
  /** Yükleyende `attendance:health-report:direct` yoksa rapor onaya düşer — kullanıcıya söylenir. */
  requiresApproval: boolean
}>()

const emit = defineEmits<{ saved: [] }>()

const notify = useNotify()
const saving = ref(false)
const file = ref<File | null>(null)

watch(open, (isOpen) => {
  if (isOpen) file.value = null
})

function onRejected() {
  notify.error('Dosya kabul edilmedi. PDF, JPEG veya PNG olmalı ve 10 MB’ı aşmamalıdır.')
}

async function handleSave() {
  if (!file.value) return

  saving.value = true
  try {
    await attendanceApi.uploadHealthReport(props.recordId, file.value)
    notify.success(
      props.requiresApproval
        ? 'Sağlık raporu yüklendi, koordinatör öğretmen onayına gönderildi.'
        : 'Sağlık raporu yüklendi.',
    )
    open.value = false
    emit('saved')
  } catch (e) {
    notify.apiError(e, 'Sağlık raporu yüklenirken bir hata oluştu.')
  } finally {
    saving.value = false
  }
}
</script>
