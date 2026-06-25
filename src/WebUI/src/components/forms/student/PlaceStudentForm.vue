<template>
  <FormDialog v-model="open" title="Öğrenci Yerleştir" icon="place" color="teal" save-label="Yerleştir" :saving="saving" @save="handleSave">
        <div v-if="studentName" class="text-subtitle2 q-mb-sm">Öğrenci: {{ studentName }}</div>
        <q-select
          v-model="form.businessId"
          :options="businessOpts.options.value"
          :loading="businessOpts.loading.value"
          label="İşletme *"
          filled
          use-input
          input-debounce="0"
          emit-value
          map-options
          option-label="label"
          option-value="value"
          :error="!!errors.businessId"
          :error-message="errors.businessId"
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
            <SelectEmptyOption />
          </template>
        </q-select>
        <TeacherSelector
          v-model="form.teacherId"
          label="Koordinatör Öğretmen (opsiyonel)"
        />
  </FormDialog>
</template>

<script setup lang="ts">
import { ref, reactive, watch } from 'vue'
import { enrollmentApi } from 'src/api/enrollment'
import { placementSchema } from 'src/schemas/student'
import { useNotify } from 'src/composables/useNotify'
import { zodValidate } from 'src/composables/useZodValidation'
import { useBusinessOptions } from 'src/composables/useEntityOptions'
import FormDialog from 'components/FormDialog.vue'
import TeacherSelector from 'components/TeacherSelector.vue'
import SelectEmptyOption from 'components/SelectEmptyOption.vue'

const open = defineModel<boolean>({ required: true })

const props = defineProps<{
  studentId: string
  studentName: string
}>()

const emit = defineEmits<{ saved: [] }>()

const notify = useNotify()
const saving = ref(false)
const businessOpts = useBusinessOptions()

const form = reactive({ businessId: '', teacherId: '' })
const errors = reactive<Record<string, string>>({})

watch(open, (isOpen) => {
  if (isOpen) {
    Object.assign(form, { businessId: '', teacherId: '' })
    for (const key of Object.keys(errors)) errors[key] = ''
    businessOpts.reset()
    businessOpts.load()
  }
})

async function handleSave() {
  if (!zodValidate(placementSchema, form, errors)) return
  saving.value = true
  try {
    await enrollmentApi.createPlacement({
      studentId: props.studentId,
      businessId: form.businessId,
      teacherId: form.teacherId || undefined,
    })
    notify.success('Öğrenci yerleştirildi.')
    open.value = false
    emit('saved')
  } catch (e) {
    notify.apiError(e, 'Yerleştirme sırasında bir hata oluştu.')
  } finally {
    saving.value = false
  }
}
</script>
