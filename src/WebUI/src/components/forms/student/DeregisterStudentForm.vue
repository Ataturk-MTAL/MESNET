<template>
  <FormDialog v-model="open" title="Kayıt Sil" icon="person_remove" color="negative" width="400px" save-label="Kayıt Sil" :saving="saving" :save-disabled="!form.reason" @save="handleSave">
        <div class="text-body2 q-mb-md">
          <strong>{{ studentName }}</strong> adlı öğrencinin kaydını silmek istediğinize emin misiniz?
          Bu işlem geri alınamaz.
        </div>
        <q-input
          v-model="form.reason"
          label="Sebep *"
          filled
          type="textarea"
          rows="2"
          :rules="[v => !!v || 'Sebep belirtilmelidir']"
        >
          <template #prepend>
            <q-icon name="notes" />
          </template>
        </q-input>
  </FormDialog>
</template>

<script setup lang="ts">
import { ref, reactive, watch } from 'vue'
import { enrollmentApi } from 'src/api/enrollment'
import { useNotify } from 'src/composables/useNotify'
import { useEntityOptionsStore } from 'stores/entityOptions'
import FormDialog from 'components/FormDialog.vue'

const open = defineModel<boolean>({ required: true })

const props = defineProps<{
  studentId: string
  studentName: string
}>()

const emit = defineEmits<{ saved: [] }>()

const notify = useNotify()
const entityOptionsStore = useEntityOptionsStore()
const saving = ref(false)
const form = reactive({ reason: '' })

watch(open, (isOpen) => {
  if (isOpen) {
    form.reason = ''
  }
})

async function handleSave() {
  if (!form.reason) return
  saving.value = true
  try {
    await enrollmentApi.deregisterStudent(props.studentId, form.reason)
    entityOptionsStore.invalidateStudents()
    notify.success('Öğrenci kaydı silindi.')
    open.value = false
    emit('saved')
  } catch (e) {
    notify.apiError(e, 'Kayıt silme sırasında bir hata oluştu.')
  } finally {
    saving.value = false
  }
}
</script>
