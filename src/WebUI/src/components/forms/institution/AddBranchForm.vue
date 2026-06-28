<template>
  <FormDialog v-model="open" title="Alan Ekle" icon="add_circle" color="green" save-label="Aktifleştir" :saving="saving" :save-disabled="!form.fieldCode" @save="handleSave">
    <q-select
      v-model="form.educationType"
      :options="educationTypeOptions"
      label="Eğitim Türü"
      outlined
      emit-value
      map-options
      @update:model-value="onEducationTypeChange"
    >
      <template #prepend>
        <q-icon name="school" />
      </template>
    </q-select>
    <q-select
      v-model="form.fieldCode"
      :options="availableFields"
      :loading="loadingCatalog"
      label="Alan *"
      outlined
      use-input
      input-debounce="0"
      emit-value
      map-options
      option-label="label"
      option-value="value"
      :disable="!form.educationType"
      @filter="filterFields"
    >
      <template #prepend>
        <q-icon name="category" />
      </template>
      <template #option="{ itemProps, opt }">
        <q-item v-bind="itemProps">
          <q-item-section>
            <q-item-label>{{ opt.label }}</q-item-label>
            <q-item-label v-if="opt.caption" caption>{{ opt.caption }}</q-item-label>
          </q-item-section>
        </q-item>
      </template>
      <template #no-option>
        <SelectEmptyOption />
      </template>
    </q-select>
  </FormDialog>
</template>

<script setup lang="ts">
import { ref, reactive, watch } from 'vue'
import { institutionApi } from 'src/api/institution'
import { useNotify } from 'src/composables/useNotify'
import FormDialog from 'components/FormDialog.vue'
import SelectEmptyOption from 'components/SelectEmptyOption.vue'

const open = defineModel<boolean>({ required: true })

const props = defineProps<{
  institutionId: string
  activeBranchCodes: string[]
}>()

const emit = defineEmits<{ saved: [] }>()

const notify = useNotify()
const saving = ref(false)
const loadingCatalog = ref(false)

const form = reactive({ educationType: '', fieldCode: '' })

const educationTypeOptions = [
  { label: 'MESEM', value: 'Mesem' },
  { label: 'Örgün', value: 'Formal' },
]

const allFieldOptions = ref<Array<{ label: string; value: string; caption: string }>>([])
const availableFields = ref<Array<{ label: string; value: string; caption: string }>>([])

watch(open, (isOpen) => {
  if (isOpen) {
    form.educationType = ''
    form.fieldCode = ''
    availableFields.value = []
    allFieldOptions.value = []
  }
})

async function onEducationTypeChange(val: string) {
  form.fieldCode = ''
  loadingCatalog.value = true
  try {
    const res = await institutionApi.getFieldCatalog(val || undefined)
    const activeCodes = new Set(props.activeBranchCodes)
    allFieldOptions.value = res.data
      .filter((f) => !activeCodes.has(f.code))
      .map((f) => ({ label: f.name, value: f.code, caption: `${f.code} — ${f.typeSlug}` }))
    availableFields.value = allFieldOptions.value
  } catch (e) {
    notify.apiError(e, 'Alan kataloğu yüklenirken hata oluştu.')
  } finally {
    loadingCatalog.value = false
  }
}

function filterFields(val: string, update: (fn: () => void) => void) {
  update(() => {
    const needle = val.toLowerCase()
    availableFields.value = needle
      ? allFieldOptions.value.filter(
          (o) => o.label.toLowerCase().includes(needle) || o.caption.toLowerCase().includes(needle),
        )
      : allFieldOptions.value
  })
}

async function handleSave() {
  saving.value = true
  try {
    await institutionApi.activateBranch(props.institutionId, form.fieldCode)
    notify.success('Alan aktifleştirildi.')
    open.value = false
    emit('saved')
  } catch (e) {
    notify.apiError(e, 'Alan aktifleştirilirken hata oluştu.')
  } finally {
    saving.value = false
  }
}
</script>
