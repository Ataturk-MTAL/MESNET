<template>
  <FormDialog v-model="open" title="Fesih Talebi Oluştur" icon="report" color="deep-orange" save-label="Talebi Gönder" :saving="saving" :save-disabled="!form.reasonType || !form.reason || !form.requestedBy" @save="handleSave">
        <AppNotice
          type="warning"
          icon="info"
          dense
          message="Talebiniz kurum yönetimine iletilecek. Onay/red kararı size bildirilir."
        />

        <q-select
          v-model="form.reasonType"
          :options="terminationReasonOptions"
          label="Fesih Sebebi *"
          filled
          emit-value
          map-options
        >
          <template #prepend><q-icon name="category" /></template>
        </q-select>

        <q-input
          v-model="form.reason"
          label="Açıklama *"
          filled
          type="textarea"
          :rows="3"
          autogrow
        >
          <template #prepend><q-icon name="notes" /></template>
        </q-input>

        <q-input v-model="form.requestedBy" label="Talep Eden *" filled>
          <template #prepend><q-icon name="person" /></template>
        </q-input>
  </FormDialog>
</template>

<script setup lang="ts">
import { ref, reactive, watch } from 'vue'
import { contractApi, TERMINATION_REASONS } from 'src/api/contract'
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
  reasonType: 'BusinessRequest',
  reason: '',
  requestedBy: '',
})

const terminationReasonOptions = TERMINATION_REASONS.map((r) => ({ label: r.label, value: r.value }))

watch(open, (isOpen) => {
  if (isOpen) {
    form.reasonType = 'BusinessRequest'
    form.reason = ''
    form.requestedBy = authStore.user?.fullName ?? ''
  }
})

async function handleSave() {
  saving.value = true
  try {
    await contractApi.requestTermination(props.contractId, {
      reason: form.reason,
      reasonType: form.reasonType,
      requestedBy: form.requestedBy,
    })
    notify.success('Fesih talebi oluşturuldu. Kurum onayı bekleniyor.')
    open.value = false
    emit('saved')
  } catch (e) {
    notify.apiError(e, 'Fesih talebi oluşturulurken bir hata oluştu.')
  } finally {
    saving.value = false
  }
}
</script>
