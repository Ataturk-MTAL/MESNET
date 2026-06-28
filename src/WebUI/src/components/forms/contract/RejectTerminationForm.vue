<template>
  <FormDialog v-model="open" title="Fesih Talebini Reddet" icon="thumb_down" color="teal" width="440px" save-label="Talebi Reddet" :saving="saving" :save-disabled="!form.rejectedBy" @save="handleSave">
        <AppNotice
          type="success"
          icon="info"
          dense
          message="Red edildiğinde sözleşme aktif duruma geri dönecektir."
        />

        <q-input
          v-model="form.rejectionNote"
          label="Red Gerekçesi (opsiyonel)"
          filled
          type="textarea"
          :rows="3"
          autogrow
        >
          <template #prepend><q-icon name="notes" /></template>
        </q-input>

        <q-input v-model="form.rejectedBy" label="Reddeden *" filled>
          <template #prepend><q-icon name="person" /></template>
        </q-input>
  </FormDialog>
</template>

<script setup lang="ts">
import { ref, reactive, watch } from 'vue'
import { contractApi } from 'src/api/contract'
import { useNotify } from 'src/composables/useNotify'
import { useAuthStore } from 'stores/auth'
import FormDialog from 'components/FormDialog.vue'
import AppNotice from 'components/AppNotice.vue'

const open = defineModel<boolean>({ required: true })

const props = defineProps<{
  contractId: string
}>()

const emit = defineEmits<{ saved: [] }>()

const notify = useNotify()
const authStore = useAuthStore()
const saving = ref(false)

const form = reactive({
  rejectedBy: '',
  rejectionNote: '',
})

watch(open, (isOpen) => {
  if (isOpen) {
    form.rejectedBy = authStore.user?.fullName ?? ''
    form.rejectionNote = ''
  }
})

async function handleSave() {
  saving.value = true
  try {
    await contractApi.rejectTermination(props.contractId, {
      rejectedBy: form.rejectedBy,
      rejectionNote: form.rejectionNote || undefined,
    })
    notify.success('Fesih talebi reddedildi. Sözleşme aktif duruma döndü.')
    open.value = false
    emit('saved')
  } catch (e) {
    notify.apiError(e, 'İşlem sırasında bir hata oluştu.')
  } finally {
    saving.value = false
  }
}
</script>
