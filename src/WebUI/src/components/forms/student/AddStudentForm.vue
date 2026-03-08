<template>
  <FormDialog v-model="open" title="Yeni Öğrenci Ekle" icon="person_add" :saving="saving" @save="handleSave">
        <q-select
          v-model="form.keycloakUserId"
          :options="userOpts.options.value"
          :loading="userOpts.loading.value"
          label="Kullanıcı *"
          filled
          use-input
          input-debounce="0"
          emit-value
          map-options
          option-label="label"
          option-value="value"
          :error="!!errors.keycloakUserId"
          :error-message="errors.keycloakUserId"
          @filter="userOpts.filter"
          @update:model-value="onUserSelect"
        >
          <template #prepend>
            <q-icon name="person_search" />
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
        <q-input
          v-model="form.fullName" label="Ad Soyad *" filled
          :error="!!errors.fullName" :error-message="errors.fullName"
        >
          <template #prepend>
            <q-icon name="badge" />
          </template>
        </q-input>
        <q-input
          v-model="form.email" label="Kullanıcı Adı (E-posta)" filled readonly
        >
          <template #prepend>
            <q-icon name="email" />
          </template>
        </q-input>
        <q-select
          v-model="form.branchCode"
          :options="branchOpts.options.value"
          label="Alan *"
          filled
          use-input
          input-debounce="0"
          emit-value
          map-options
          option-label="label"
          option-value="value"
          :error="!!errors.branchCode"
          :error-message="errors.branchCode"
          @filter="branchOpts.filter"
          @update:model-value="onBranchChange"
        >
          <template #prepend>
            <q-icon name="category" />
          </template>
          <template #no-option>
            <q-item>
              <q-item-section class="text-grey">Sonuç bulunamadı</q-item-section>
            </q-item>
          </template>
        </q-select>
        <q-select
          v-if="specOptions.length > 0"
          v-model="form.specializationCode"
          :options="specOptions"
          label="Dal"
          filled
          emit-value
          map-options
          option-label="label"
          option-value="value"
        >
          <template #prepend>
            <q-icon name="account_tree" />
          </template>
        </q-select>
        <q-select
          v-model="form.educationType"
          :options="educationTypeOptions"
          label="Eğitim Tipi *"
          filled
          emit-value
          map-options
          option-label="label"
          option-value="value"
          :error="!!errors.educationType"
          :error-message="errors.educationType"
        >
          <template #prepend>
            <q-icon name="school" />
          </template>
        </q-select>
        <div class="row q-col-gutter-sm">
          <div class="col-6">
            <q-input
              v-model.number="form.classYear" label="Sınıf (9-12)" filled type="number" min="9" max="12"
              :error="!!errors.classYear" :error-message="errors.classYear"
            >
              <template #prepend>
                <q-icon name="class" />
              </template>
            </q-input>
          </div>
          <div class="col-6">
            <q-input
              v-model="form.section" label="Şube" filled
              :error="!!errors.section" :error-message="errors.section"
            >
              <template #prepend>
                <q-icon name="sort_by_alpha" />
              </template>
            </q-input>
          </div>
        </div>
        <q-input
          v-model="form.studentNumber" label="Öğrenci No" filled
          :error="!!errors.studentNumber" :error-message="errors.studentNumber"
        >
          <template #prepend>
            <q-icon name="pin" />
          </template>
        </q-input>
        <q-input
          v-model="form.tcKimlikNo" label="T.C. Kimlik No" filled maxlength="11"
          :error="!!errors.tcKimlikNo" :error-message="errors.tcKimlikNo"
        >
          <template #prepend>
            <q-icon name="fingerprint" />
          </template>
        </q-input>
        <q-input
          v-model="form.phoneNumber" label="Telefon" filled
          :error="!!errors.phoneNumber" :error-message="errors.phoneNumber"
        >
          <template #prepend>
            <q-icon name="phone" />
          </template>
        </q-input>
        <q-separator />
        <div class="text-subtitle2 text-grey-7">Veli Bilgileri</div>
        <q-input v-model="form.guardianName" label="Veli Adı" filled>
          <template #prepend>
            <q-icon name="person" />
          </template>
        </q-input>
        <q-input
          v-model="form.guardianPhone" label="Veli Telefon" filled
          :error="!!errors.guardianPhone" :error-message="errors.guardianPhone"
        >
          <template #prepend>
            <q-icon name="phone" />
          </template>
        </q-input>
  </FormDialog>
</template>

<script setup lang="ts">
import { ref, reactive, computed, watch } from 'vue'
import { enrollmentApi, EDUCATION_TYPES } from 'src/api/enrollment'
import { registerStudentSchema } from 'src/schemas/student'
import { useNotify } from 'src/composables/useNotify'
import { zodValidate } from 'src/composables/useZodValidation'
import { useKeycloakUserOptions, useBranchOptions, type SelectOption } from 'src/composables/useEntityOptions'
import { useAcademicPeriodStore } from 'stores/academicPeriod'
import FormDialog from 'components/FormDialog.vue'

const open = defineModel<boolean>({ required: true })

const emit = defineEmits<{ saved: [] }>()

const periodStore = useAcademicPeriodStore()
const notify = useNotify()
const saving = ref(false)
const userOpts = useKeycloakUserOptions()
const branchOpts = useBranchOptions()

const form = reactive({
  keycloakUserId: '', fullName: '', email: '', branchCode: '', specializationCode: '',
  educationType: 'Formal' as string,
  classYear: 11, section: '', studentNumber: '', phoneNumber: '',
  tcKimlikNo: '', guardianName: '', guardianPhone: '',
})
const errors = reactive<Record<string, string>>({})

const educationTypeOptions = [...EDUCATION_TYPES]
const specOptions = computed(() => branchOpts.getSpecializations(form.branchCode ?? ''))

watch(open, (isOpen) => {
  if (isOpen) {
    Object.assign(form, {
      keycloakUserId: '', fullName: '', email: '', branchCode: '', specializationCode: '',
      educationType: 'Formal', classYear: 11, section: '', studentNumber: '', phoneNumber: '',
      tcKimlikNo: '', guardianName: '', guardianPhone: '',
    })
    for (const key of Object.keys(errors)) errors[key] = ''
    userOpts.reset()
    userOpts.load()
    branchOpts.reset()
    branchOpts.load()
  }
})

function onUserSelect(val: string | null) {
  if (val) {
    const found = userOpts.allOptions.value.find((o: SelectOption) => o.value === val)
    form.fullName = found?.label ?? ''
    const caption = found?.caption ?? ''
    const emailMatch = caption.match(/^(.+?)\s*\(/)
    form.email = emailMatch ? emailMatch[1] : caption
  } else {
    form.fullName = ''
    form.email = ''
  }
}

function onBranchChange() {
  form.specializationCode = ''
}

async function handleSave() {
  if (!zodValidate(registerStudentSchema, form, errors)) return
  saving.value = true
  try {
    const selectedSpec = specOptions.value.find((o) => o.value === form.specializationCode)
    await enrollmentApi.registerStudent({
      keycloakUserId: form.keycloakUserId,
      fullName: form.fullName,
      branchCode: form.branchCode,
      branchName: branchOpts.getFieldName(form.branchCode) || undefined,
      academicPeriodId: periodStore.selectedPeriodId ?? undefined,
      educationType: form.educationType,
      specializationCode: form.specializationCode || undefined,
      specializationName: selectedSpec?.label || undefined,
      classYear: form.classYear,
      section: form.section || undefined,
      studentNumber: form.studentNumber || undefined,
      phoneNumber: form.phoneNumber || undefined,
      tcKimlikNo: form.tcKimlikNo || undefined,
      guardianName: form.guardianName || undefined,
      guardianPhone: form.guardianPhone || undefined,
    })
    notify.success('Öğrenci başarıyla kaydedildi.')
    open.value = false
    emit('saved')
  } catch (e) {
    notify.apiError(e, 'Öğrenci eklenirken bir hata oluştu.')
  } finally {
    saving.value = false
  }
}
</script>
