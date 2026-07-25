<template>
  <FormDialog
    v-model="open"
    title="Ders Programı Ayarları"
    icon="schedule"
    color="orange"
    width="400px"
    :saving="saving"
    @save="handleSave"
  >
    <div class="text-subtitle2 q-mb-md">
      Günlük Ders Sayısı
    </div>
    <q-slider
      v-model="dailyPeriodCount"
      :min="1"
      :max="12"
      :step="1"
      label
      label-always
      color="primary"
      markers
    />
    <div class="text-center text-h5 text-primary q-mt-sm">
      {{ dailyPeriodCount }} ders
    </div>
  </FormDialog>
</template>

<script setup lang="ts">
import { ref, watch } from 'vue'
import { institutionApi } from 'src/api/institution'
import { useNotify } from 'src/composables/useNotify'
import { useAuthStore } from 'stores/auth'
import FormDialog from 'components/FormDialog.vue'

const open = defineModel<boolean>({ required: true })

const props = defineProps<{
  institutionId: string
  currentCount: number
}>()

const emit = defineEmits<{ saved: [] }>()

const notify = useNotify()
const authStore = useAuthStore()
const saving = ref(false)
const dailyPeriodCount = ref(8)

watch(open, (isOpen) => {
  if (isOpen) {
    dailyPeriodCount.value = props.currentCount || 8
  }
})

async function handleSave() {
  saving.value = true
  try {
    await institutionApi.updateScheduleConfig(props.institutionId, {
      dailyPeriodCount: dailyPeriodCount.value,
      updatedBy: authStore.user?.fullName ?? '',
    })
    notify.success('Ders programı ayarları güncellendi.')
    open.value = false
    emit('saved')
  } catch (e) {
    notify.apiError(e, 'Ayarlar kaydedilirken hata oluştu.')
  } finally {
    saving.value = false
  }
}
</script>
