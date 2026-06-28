<template>
  <q-page padding>
    <div class="row items-center q-mb-lg">
      <q-btn flat round dense icon="arrow_back" aria-label="Sözleşmelere dön" class="q-mr-sm" @click="goBack">
        <q-tooltip>Sözleşmelere dön</q-tooltip>
      </q-btn>
      <div class="text-h5 text-weight-bold col">Yeni Sözleşme</div>
    </div>

    <q-card flat bordered style="max-width: 640px">
      <q-card-section class="q-gutter-md">
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
          <template #no-option><SelectEmptyOption /></template>
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
          <template #no-option><SelectEmptyOption /></template>
        </q-select>

        <TeacherSelector v-model="form.teacherId" label="Koordinatör Öğretmen (opsiyonel)" />

        <q-input v-model="form.startDate" label="Başlangıç Tarihi *" filled type="date">
          <template #prepend><q-icon name="calendar_today" /></template>
        </q-input>
      </q-card-section>

      <q-separator />
      <q-card-actions align="right" class="q-pa-md">
        <q-btn flat label="İptal" color="grey-7" @click="goBack" />
        <q-btn unelevated color="primary" label="Oluştur" :loading="saving" @click="handleSave" />
      </q-card-actions>
    </q-card>
  </q-page>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { contractApi } from 'src/api/contract'
import { useNotify } from 'src/composables/useNotify'
import { useStudentOptions, useBusinessOptions } from 'src/composables/useEntityOptions'
import { useAuthStore } from 'stores/auth'
import TeacherSelector from 'components/TeacherSelector.vue'
import SelectEmptyOption from 'components/SelectEmptyOption.vue'

const router = useRouter()
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

function goBack() {
  void router.push('/internship/contracts')
}

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
    goBack()
  } catch (e) {
    notify.apiError(e, 'Sözleşme oluşturulurken bir hata oluştu.')
  } finally {
    saving.value = false
  }
}

onMounted(() => {
  studentOpts.reset()
  studentOpts.load()
  businessOpts.reset()
  businessOpts.load()
})
</script>
