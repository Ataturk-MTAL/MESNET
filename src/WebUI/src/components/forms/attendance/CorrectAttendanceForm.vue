<template>
  <FormDialog v-model="open" title="Devamsızlık Düzelt" icon="edit_calendar" color="orange" width="400px" save-label="Düzelt" :saving="saving" @save="handleSave">
    <q-select
      v-model="form.absenceType"
      :options="absenceTypeOptions"
      label="Devamsızlık Türü"
      filled emit-value map-options
    >
      <template #prepend>
        <q-icon name="category" />
      </template>
    </q-select>
    <q-input v-model="form.reason" label="Gerekçe" filled>
      <template #prepend>
        <q-icon name="notes" />
      </template>
    </q-input>
  </FormDialog>
</template>

<script setup lang="ts">
import { ref, reactive, watch } from 'vue'
import { attendanceApi, ABSENCE_TYPES } from 'src/api/attendance'
import { useNotify } from 'src/composables/useNotify'
import FormDialog from 'components/FormDialog.vue'

const open = defineModel<boolean>({ required: true })

const props = defineProps<{
  recordId: string
  absenceType: string
  reason: string
}>()

const emit = defineEmits<{ saved: [] }>()

const notify = useNotify()
const saving = ref(false)

const form = reactive({ absenceType: 'Unexcused', reason: '' })

const absenceTypeOptions = ABSENCE_TYPES.map((t) => ({ label: t.label, value: t.value }))

watch(open, (isOpen) => {
  if (isOpen) {
    form.absenceType = props.absenceType || 'Unexcused'
    form.reason = props.reason || ''
  }
})

async function handleSave() {
  saving.value = true
  try {
    await attendanceApi.correct(props.recordId, {
      absenceType: form.absenceType,
      reason: form.reason || undefined,
    })
    notify.success('Devamsızlık düzeltildi.')
    open.value = false
    emit('saved')
  } catch (e) {
    notify.apiError(e, 'Düzeltme sırasında bir hata oluştu.')
  } finally {
    saving.value = false
  }
}
</script>
