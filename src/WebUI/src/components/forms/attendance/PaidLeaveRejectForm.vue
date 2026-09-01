<template>
  <FormDialog
    v-model="open"
    title="İzin Başvurusunu Reddet"
    icon="event_busy"
    color="negative"
    width="420px"
    save-label="Reddet"
    save-color="negative"
    :saving="saving"
    :save-disabled="!reason.trim()"
    @save="handleSave"
  >
    <AppNotice
      type="warning"
      dense
      class="q-mb-sm"
    >
      Reddedilen başvuru kapanır ve izin günleri için devamsızlık kaydı açılmaz. Öğrenci aynı
      tarihler için yeniden başvurabilir.
    </AppNotice>

    <q-input
      v-model="reason"
      label="Ret gerekçesi"
      outlined
      autogrow
      counter
      :maxlength="500"
    >
      <template #prepend>
        <q-icon name="notes" />
      </template>
    </q-input>
  </FormDialog>
</template>

<script setup lang="ts">
import { ref, watch } from 'vue'
import { paidLeaveApi } from 'src/api/paidLeave'
import { useNotify } from 'src/composables/useNotify'
import FormDialog from 'components/FormDialog.vue'
import AppNotice from 'components/AppNotice.vue'

const open = defineModel<boolean>({ required: true })

// `stage` hangi ucun çağrılacağını belirler: işletme adımı business_id kapsamı ister,
// okul adımı istemez. Yanlış uç 422 döner — kapsam kontrolü sunucudadır.
const props = defineProps<{ requestId: string; stage: 'business' | 'school' }>()
const emit = defineEmits<{ saved: [] }>()

const notify = useNotify()
const saving = ref(false)
const reason = ref('')

watch(open, (isOpen) => {
  if (isOpen) reason.value = ''
})

async function handleSave() {
  if (!reason.value.trim()) return

  saving.value = true
  try {
    const trimmed = reason.value.trim()
    if (props.stage === 'business') {
      await paidLeaveApi.businessReject(props.requestId, trimmed)
    } else {
      await paidLeaveApi.reject(props.requestId, trimmed)
    }
    notify.success('İzin başvurusu reddedildi.')
    open.value = false
    emit('saved')
  } catch (e) {
    notify.apiError(e, 'Ret işlemi sırasında bir hata oluştu.')
  } finally {
    saving.value = false
  }
}
</script>
