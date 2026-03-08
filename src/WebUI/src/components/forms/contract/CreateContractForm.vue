<template>
  <FormDialog v-model="open" title="Yeni Sözleşme Oluştur" icon="description" width="520px" save-label="Oluştur" :saving="saving" @save="handleSave">
        <q-select
          v-model="form.studentId"
          :options="studentOpts.options.value"
          :loading="studentOpts.loading.value"
          label="Öğrenci *"
          filled
          use-input
          input-debounce="0"
          emit-value
          map-options
          option-label="label"
          option-value="value"
          @filter="studentOpts.filter"
        >
          <template #prepend><q-icon name="school" /></template>
          <template #option="{ itemProps, opt }">
            <q-item v-bind="itemProps">
              <q-item-section>
                <q-item-label>{{ opt.label }}</q-item-label>
                <q-item-label v-if="opt.caption" caption>{{ opt.caption }}</q-item-label>
              </q-item-section>
            </q-item>
          </template>
          <template #no-option>
            <q-item><q-item-section class="text-grey">Sonuç bulunamadı</q-item-section></q-item>
          </template>
        </q-select>

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
          @filter="businessOpts.filter"
        >
          <template #prepend><q-icon name="business" /></template>
          <template #option="{ itemProps, opt }">
            <q-item v-bind="itemProps">
              <q-item-section>
                <q-item-label>{{ opt.label }}</q-item-label>
                <q-item-label v-if="opt.caption" caption>{{ opt.caption }}</q-item-label>
              </q-item-section>
            </q-item>
          </template>
          <template #no-option>
            <q-item><q-item-section class="text-grey">Sonuç bulunamadı</q-item-section></q-item>
          </template>
        </q-select>

        <TeacherSelector
          v-model="form.teacherId"
          label="Koordinatör Öğretmen (opsiyonel)"
        />

        <q-input v-model="form.startDate" label="Başlangıç Tarihi *" filled type="date">
          <template #prepend><q-icon name="calendar_today" /></template>
        </q-input>
  </FormDialog>
</template>

<script setup lang="ts">
import { ref, reactive, watch } from 'vue'
import { contractApi } from 'src/api/contract'
import { useNotify } from 'src/composables/useNotify'
import { useStudentOptions, useBusinessOptions } from 'src/composables/useEntityOptions'
import { useAuthStore } from 'stores/auth'
import FormDialog from 'components/FormDialog.vue'
import TeacherSelector from 'components/TeacherSelector.vue'

const open = defineModel<boolean>({ required: true })

const emit = defineEmits<{ saved: [] }>()

const notify = useNotify()
const authStore = useAuthStore()
const saving = ref(false)
const studentOpts = useStudentOptions()
const businessOpts = useBusinessOptions()

const form = reactive({
  studentId: '',
  businessId: '',
  teacherId: '',
  startDate: '',
})

watch(open, (isOpen) => {
  if (isOpen) {
    form.studentId = ''
    form.businessId = ''
    form.teacherId = ''
    form.startDate = ''
    studentOpts.reset()
    studentOpts.load()
    businessOpts.reset()
    businessOpts.load()
  }
})

async function handleSave() {
  saving.value = true
  try {
    await contractApi.create({
      studentId: form.studentId,
      businessId: form.businessId,
      institutionId: authStore.user?.institutionId ?? '',
      teacherId: form.teacherId || undefined,
      startDate: new Date(form.startDate).toISOString(),
    })
    notify.success('Sözleşme oluşturuldu.')
    open.value = false
    emit('saved')
  } catch (e) {
    notify.apiError(e, 'Sözleşme oluşturulurken bir hata oluştu.')
  } finally {
    saving.value = false
  }
}
</script>
