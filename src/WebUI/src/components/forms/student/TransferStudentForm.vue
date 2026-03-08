<template>
  <FormDialog v-model="open" title="Öğrenci Transfer Et" icon="transfer_within_a_station" color="orange" save-label="Transfer Et" save-color="warning" :saving="saving" @save="handleSave">
        <div v-if="studentName" class="text-subtitle2 q-mb-sm">Öğrenci: {{ studentName }}</div>
        <q-select
          v-model="form.newBusinessId"
          :options="businessOpts.options.value"
          :loading="businessOpts.loading.value"
          label="Yeni İşletme *"
          filled
          use-input
          input-debounce="0"
          emit-value
          map-options
          option-label="label"
          option-value="value"
          :error="!!errors.newBusinessId"
          :error-message="errors.newBusinessId"
          @filter="businessOpts.filter"
        >
          <template #prepend>
            <q-icon name="business" />
          </template>
          <template #option="{ itemProps, opt }">
            <q-item v-bind="itemProps">
              <q-item-section>
                <q-item-label>{{ opt.label }}</q-item-label>
                <q-item-label caption v-if="opt.caption">{{ opt.caption }}</q-item-label>
              </q-item-section>
            </q-item>
          </template>
          <template #no-option>
            <q-item>
              <q-item-section class="text-grey">Sonuç bulunamadı</q-item-section>
            </q-item>
          </template>
        </q-select>
        <q-input v-model="form.reason" label="Transfer Gerekçesi" filled type="textarea" rows="2">
          <template #prepend>
            <q-icon name="notes" />
          </template>
        </q-input>
  </FormDialog>
</template>

<script setup lang="ts">
import { ref, reactive, watch } from 'vue'
import { enrollmentApi } from 'src/api/enrollment'
import { transferSchema } from 'src/schemas/student'
import { useNotify } from 'src/composables/useNotify'
import { zodValidate } from 'src/composables/useZodValidation'
import { useBusinessOptions } from 'src/composables/useEntityOptions'
import FormDialog from 'components/FormDialog.vue'

const open = defineModel<boolean>({ required: true })

const props = defineProps<{
  studentId: string
  studentName: string
}>()

const emit = defineEmits<{ saved: [] }>()

const notify = useNotify()
const saving = ref(false)
const businessOpts = useBusinessOptions()

const form = reactive({ newBusinessId: '', reason: '' })
const errors = reactive<Record<string, string>>({})

// Aktif yerleştirme ID'si
const activePlacementId = ref<string | null>(null)

watch(open, async (isOpen) => {
  if (isOpen) {
    Object.assign(form, { newBusinessId: '', reason: '' })
    for (const key of Object.keys(errors)) errors[key] = ''
    businessOpts.reset()
    businessOpts.load()
    // Mevcut aktif yerleştirmeyi bul
    try {
      const res = await enrollmentApi.listPlacements({ studentId: props.studentId, status: 'Active' })
      activePlacementId.value = res.data?.items?.[0]?.id ?? null
    } catch {
      activePlacementId.value = null
    }
  }
})

async function handleSave() {
  if (!activePlacementId.value) {
    notify.error('Aktif yerleştirme bulunamadı.')
    return
  }
  if (!zodValidate(transferSchema, form, errors)) return
  saving.value = true
  try {
    await enrollmentApi.transferStudent(activePlacementId.value, {
      newBusinessId: form.newBusinessId,
      reason: form.reason ?? '',
    })
    notify.success('Öğrenci transfer edildi.')
    open.value = false
    emit('saved')
  } catch (e) {
    notify.apiError(e, 'Transfer sırasında bir hata oluştu.')
  } finally {
    saving.value = false
  }
}
</script>
