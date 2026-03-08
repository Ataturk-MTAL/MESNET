<template>
  <FormDialog v-model="open" title="Sözleşme İmzala" icon="draw" color="teal" width="420px" save-label="İmzala" :saving="saving" @save="handleSave">
        <q-select
          v-model="form.party"
          :options="partyOptions"
          label="İmzacı Taraf"
          filled
          emit-value
          map-options
        >
          <template #prepend><q-icon name="group" /></template>
        </q-select>
        <q-input v-model="form.signedBy" label="İmzalayan Adı" filled>
          <template #prepend><q-icon name="badge" /></template>
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

const form = reactive<{ party: 'Institution' | 'Business' | 'Student'; signedBy: string }>({
  party: 'Institution',
  signedBy: '',
})

const partyOptions = [
  { label: 'Kurum', value: 'Institution' },
  { label: 'İşletme', value: 'Business' },
  { label: 'Öğrenci', value: 'Student' },
]

watch(open, (isOpen) => {
  if (isOpen) {
    form.party = 'Institution'
    form.signedBy = ''
  }
})

async function handleSave() {
  saving.value = true
  try {
    await contractApi.sign(props.contractId, { party: form.party, signedBy: form.signedBy })
    notify.success('Sözleşme imzalandı.')
    open.value = false
    emit('saved')
  } catch (e) {
    notify.apiError(e, 'İmzalama sırasında bir hata oluştu.')
  } finally {
    saving.value = false
  }
}
</script>
