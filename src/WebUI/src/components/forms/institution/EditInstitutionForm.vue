<template>
  <FormDialog v-model="open" title="Kurum Bilgilerini Güncelle" icon="edit" :saving="saving" @save="handleSave">
    <q-input v-model="form.fullName" label="Kurum Adı" filled>
      <template #prepend>
        <q-icon name="account_balance" />
      </template>
    </q-input>
    <q-input v-model="form.address" label="Adres" filled>
      <template #prepend>
        <q-icon name="location_on" />
      </template>
    </q-input>
    <q-input v-model="form.phoneNumber" label="Telefon" filled>
      <template #prepend>
        <q-icon name="phone" />
      </template>
    </q-input>
    <q-input v-model="form.email" label="E-posta" filled type="email">
      <template #prepend>
        <q-icon name="email" />
      </template>
    </q-input>
    <q-input v-model="form.webUrl" label="Web Sitesi" filled>
      <template #prepend>
        <q-icon name="language" />
      </template>
    </q-input>
  </FormDialog>
</template>

<script setup lang="ts">
import { ref, reactive, watch } from 'vue'
import { institutionApi, type InstitutionDto } from 'src/api/institution'
import { useNotify } from 'src/composables/useNotify'
import FormDialog from 'components/FormDialog.vue'

const open = defineModel<boolean>({ required: true })

const props = defineProps<{
  institutionId: string
  institution: InstitutionDto | null
}>()

const emit = defineEmits<{ saved: [] }>()

const notify = useNotify()
const saving = ref(false)

const form = reactive({
  fullName: '',
  address: '',
  phoneNumber: '',
  email: '',
  webUrl: '',
})

watch(open, (isOpen) => {
  if (isOpen && props.institution) {
    form.fullName = props.institution.fullName
    form.address = props.institution.address ?? ''
    form.phoneNumber = props.institution.phoneNumber ?? ''
    form.email = props.institution.email ?? ''
    form.webUrl = props.institution.webUrl ?? ''
  }
})

async function handleSave() {
  saving.value = true
  try {
    await institutionApi.update(props.institutionId, {
      fullName: form.fullName,
      address: form.address || undefined,
      phoneNumber: form.phoneNumber || undefined,
      email: form.email || undefined,
      webUrl: form.webUrl || undefined,
    })
    notify.success('Kurum bilgileri güncellendi.')
    open.value = false
    emit('saved')
  } catch (e) {
    notify.apiError(e, 'Güncelleme sırasında bir hata oluştu.')
  } finally {
    saving.value = false
  }
}
</script>
