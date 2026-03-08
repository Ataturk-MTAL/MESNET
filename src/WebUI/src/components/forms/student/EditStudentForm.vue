<template>
  <FormDialog v-model="open" title="Öğrenci Düzenle" icon="edit" :saving="saving" @save="handleSave">
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
import { enrollmentApi, type StudentProfileDto } from 'src/api/enrollment'
import { editStudentSchema } from 'src/schemas/student'
import { useNotify } from 'src/composables/useNotify'
import { zodValidate } from 'src/composables/useZodValidation'
import { useKeycloakUserOptions, useBranchOptions, type SelectOption } from 'src/composables/useEntityOptions'
import FormDialog from 'components/FormDialog.vue'

const open = defineModel<boolean>({ required: true })

const props = defineProps<{
  student: StudentProfileDto | null
}>()

const emit = defineEmits<{ saved: [] }>()

const notify = useNotify()
const saving = ref(false)
const userOpts = useKeycloakUserOptions()
const branchOpts = useBranchOptions()

const form = reactive({
  fullName: '', email: '', branchCode: '', specializationCode: '',
  classYear: 11, section: '', studentNumber: '', phoneNumber: '',
  tcKimlikNo: '', guardianName: '', guardianPhone: '',
})
const errors = reactive<Record<string, string>>({})

const branchName = ref('')
const specOptions = computed(() => branchOpts.getSpecializations(form.branchCode ?? ''))

watch(open, async (isOpen) => {
  if (isOpen && props.student) {
    branchName.value = props.student.branchName
    // Keycloak'tan email bilgisini çek
    let email = ''
    if (props.student.keycloakUserId) {
      await userOpts.load()
      const found = userOpts.allOptions.value.find((o: SelectOption) => o.value === props.student!.keycloakUserId)
      if (found?.caption) {
        const emailMatch = found.caption.match(/^(.+?)\s*\(/)
        email = emailMatch ? emailMatch[1] : found.caption
      }
    }
    Object.assign(form, {
      fullName: props.student.fullName,
      email,
      branchCode: props.student.branchCode,
      specializationCode: props.student.specializationCode ?? '',
      classYear: props.student.classYear,
      section: props.student.section ?? '',
      studentNumber: props.student.studentNumber ?? '',
      phoneNumber: props.student.phoneNumber ?? '',
      tcKimlikNo: props.student.tcKimlikNo ?? '',
      guardianName: props.student.guardianName ?? '',
      guardianPhone: props.student.guardianPhone ?? '',
    })
    for (const key of Object.keys(errors)) errors[key] = ''
    branchOpts.reset()
    branchOpts.load()
  }
})

function onBranchChange(code: string | null) {
  branchName.value = code ? branchOpts.getFieldName(code) : ''
  form.specializationCode = ''
}

async function handleSave() {
  if (!props.student) return
  if (!zodValidate(editStudentSchema, form, errors)) return
  saving.value = true
  try {
    await enrollmentApi.updateStudent(props.student.id, {
      fullName: form.fullName || undefined,
      branchCode: form.branchCode || undefined,
      branchName: branchName.value || undefined,
      specializationCode: form.specializationCode || undefined,
      specializationName: specOptions.value.find((o) => o.value === form.specializationCode)?.label || undefined,
      classYear: form.classYear || undefined,
      section: form.section || undefined,
      studentNumber: form.studentNumber || undefined,
      phoneNumber: form.phoneNumber || undefined,
      tcKimlikNo: form.tcKimlikNo || undefined,
      guardianName: form.guardianName || undefined,
      guardianPhone: form.guardianPhone || undefined,
    })
    notify.success('Öğrenci bilgileri güncellendi.')
    open.value = false
    emit('saved')
  } catch (e) {
    notify.apiError(e, 'Öğrenci güncellenirken bir hata oluştu.')
  } finally {
    saving.value = false
  }
}
</script>
