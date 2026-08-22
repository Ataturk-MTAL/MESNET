<template>
  <FormDialog
    v-model="open"
    title="Uzmanlıkları Düzenle"
    icon="tune"
    color="secondary"
    :saving="saving"
    @save="handleSave"
  >
    <div class="text-subtitle2 q-mb-sm">
      {{ fieldName }}
    </div>
    <div
      v-if="allSpecializations.length === 0"
      class="text-grey q-pa-md text-center"
    >
      Bu alan için uzmanlık tanımlanmamış.
    </div>
    <q-list
      v-else
      bordered
      separator
    >
      <q-item
        v-for="spec in allSpecializations"
        :key="spec.code"
        tag="label"
      >
        <q-item-section>
          <q-item-label>{{ spec.name }}</q-item-label>
          <q-item-label caption>
            {{ spec.code }}
          </q-item-label>
        </q-item-section>
        <q-item-section side>
          <q-checkbox
            v-model="selectedCodes"
            :val="spec.code"
          />
        </q-item-section>
      </q-item>
    </q-list>
  </FormDialog>
</template>

<script setup lang="ts">
import { ref, watch } from 'vue'
import { institutionApi, type SpecializationDto } from 'src/api/institution'
import { useNotify } from 'src/composables/useNotify'
import FormDialog from 'components/FormDialog.vue'

const open = defineModel<boolean>({ required: true })

const props = defineProps<{
  institutionId: string
  fieldCode: string
  fieldName: string
  allSpecializations: SpecializationDto[]
  activeSpecializations: string[]
}>()

const emit = defineEmits<{ saved: [] }>()

const notify = useNotify()
const saving = ref(false)
const selectedCodes = ref<string[]>([])

watch(open, (isOpen) => {
  if (isOpen) {
    selectedCodes.value = [...props.activeSpecializations]
  }
})

async function handleSave() {
  saving.value = true
  try {
    await institutionApi.updateSpecializations(props.institutionId, props.fieldCode, {
      activeSpecializations: selectedCodes.value,
    })
    notify.success('Uzmanlık alanları güncellendi.')
    open.value = false
    emit('saved')
  } catch (e) {
    notify.apiError(e, 'Güncelleme sırasında hata oluştu.')
  } finally {
    saving.value = false
  }
}
</script>
