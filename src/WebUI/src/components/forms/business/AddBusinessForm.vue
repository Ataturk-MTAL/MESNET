<template>
  <FormDialog v-model="open" title="Yeni İşletme Ekle" icon="add_business" width="600px" :saving="saving" @save="handleSave">
    <q-input v-model="form.name" label="İşletme Adı *" filled :error="!!errors.name" :error-message="errors.name">
      <template #prepend><q-icon name="business" /></template>
    </q-input>
    <q-input v-model="form.address" label="Adres *" filled :error="!!errors.address" :error-message="errors.address">
      <template #prepend><q-icon name="location_on" /></template>
    </q-input>
    <q-input v-model="form.phoneNumber" label="Telefon" filled>
      <template #prepend><q-icon name="phone" /></template>
    </q-input>
    <q-input v-model="form.email" label="E-posta" filled type="email" :error="!!errors.email" :error-message="errors.email">
      <template #prepend><q-icon name="email" /></template>
    </q-input>
    <q-input v-model.number="form.personnelCount" label="Personel Sayısı" filled type="number">
      <template #prepend><q-icon name="groups" /></template>
    </q-input>
    <q-select v-model="form.sectors" :options="sectorOptions" label="Sektörler" filled multiple emit-value map-options use-chips>
      <template #prepend><q-icon name="category" /></template>
    </q-select>
    <div class="text-subtitle2 q-mt-md q-mb-xs">
      <q-icon name="map" class="q-mr-xs" />Konum
    </div>
    <MapPicker :model-value="locationComputed" @update:model-value="v => form.location = v" height="250px" />
  </FormDialog>
</template>

<script setup lang="ts">
import { ref, reactive, computed, watch } from 'vue'
import { businessApi } from 'src/api/business'
import { registerBusinessSchema } from 'src/schemas/business'
import { useNotify } from 'src/composables/useNotify'
import { zodValidate } from 'src/composables/useZodValidation'
import FormDialog from 'components/FormDialog.vue'
import MapPicker from 'components/MapPicker.vue'
import type { SelectOption } from 'src/composables/useEntityOptions'

const open = defineModel<boolean>({ required: true })

defineProps<{
  sectorOptions: SelectOption[]
}>()

const emit = defineEmits<{ saved: [] }>()

const notify = useNotify()
const saving = ref(false)

const form = reactive({
  name: '', address: '', phoneNumber: '', email: '',
  personnelCount: 0, location: null as { latitude: number; longitude: number } | null, sectors: [] as string[],
})
const errors = reactive<Record<string, string>>({})

const locationComputed = computed(() => form.location)

watch(open, (isOpen) => {
  if (isOpen) {
    Object.assign(form, {
      name: '', address: '', phoneNumber: '', email: '',
      personnelCount: 0, location: null, sectors: [],
    })
    for (const key of Object.keys(errors)) errors[key] = ''
  }
})

async function handleSave() {
  if (!zodValidate(registerBusinessSchema, form, errors)) return
  saving.value = true
  try {
    await businessApi.register({
      name: form.name,
      address: form.address,
      phoneNumber: form.phoneNumber || undefined,
      email: form.email || undefined,
      personnelCount: form.personnelCount || undefined,
      location: form.location ?? undefined,
      sectors: form.sectors.length > 0 ? form.sectors : undefined,
    })
    notify.success('İşletme başarıyla eklendi.')
    open.value = false
    emit('saved')
  } catch (e) {
    notify.apiError(e, 'İşletme eklenirken bir hata oluştu.')
  } finally {
    saving.value = false
  }
}
</script>
