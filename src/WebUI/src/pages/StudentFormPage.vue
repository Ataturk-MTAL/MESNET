<template>
  <q-page padding>
    <div class="row items-center q-mb-lg">
      <q-btn flat round dense icon="arrow_back" aria-label="Öğrencilere dön" class="q-mr-sm" @click="goBack">
        <q-tooltip>Öğrencilere dön</q-tooltip>
      </q-btn>
      <div class="text-h5 text-weight-bold col">{{ isEdit ? 'Öğrenci Düzenle' : 'Yeni Öğrenci' }}</div>
    </div>

    <q-card flat bordered style="max-width: 760px" class="relative-position">
      <q-inner-loading :showing="loading" />
      <q-card-section class="q-gutter-md">
        <!-- Kullanıcı seçimi yalnız yeni kayıtta -->
        <q-select
          v-if="!isEdit"
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
          <template #prepend><q-icon name="person_search" /></template>
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

        <q-input v-model="form.fullName" label="Ad Soyad *" filled :error="!!errors.fullName" :error-message="errors.fullName">
          <template #prepend><q-icon name="badge" /></template>
        </q-input>
        <q-input v-model="form.email" label="Kullanıcı Adı (E-posta)" filled readonly>
          <template #prepend><q-icon name="email" /></template>
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
          <template #prepend><q-icon name="category" /></template>
          <template #no-option><SelectEmptyOption /></template>
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
          <template #prepend><q-icon name="account_tree" /></template>
        </q-select>
        <q-select
          v-if="!isEdit"
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
          <template #prepend><q-icon name="school" /></template>
        </q-select>

        <div class="row q-col-gutter-sm">
          <div class="col-6">
            <q-input v-model.number="form.classYear" label="Sınıf (9-12)" filled type="number" min="9" max="12" :error="!!errors.classYear" :error-message="errors.classYear">
              <template #prepend><q-icon name="class" /></template>
            </q-input>
          </div>
          <div class="col-6">
            <q-input v-model="form.section" label="Şube" filled :error="!!errors.section" :error-message="errors.section">
              <template #prepend><q-icon name="sort_by_alpha" /></template>
            </q-input>
          </div>
        </div>
        <q-input v-model="form.studentNumber" label="Öğrenci No" filled :error="!!errors.studentNumber" :error-message="errors.studentNumber">
          <template #prepend><q-icon name="pin" /></template>
        </q-input>
        <q-input v-model="form.tcKimlikNo" label="T.C. Kimlik No" filled maxlength="11" :error="!!errors.tcKimlikNo" :error-message="errors.tcKimlikNo">
          <template #prepend><q-icon name="fingerprint" /></template>
        </q-input>
        <q-input v-model="form.phoneNumber" label="Telefon" filled :error="!!errors.phoneNumber" :error-message="errors.phoneNumber">
          <template #prepend><q-icon name="phone" /></template>
        </q-input>

        <q-separator />
        <div class="text-subtitle2 text-grey-7">Veli Bilgileri</div>
        <q-input v-model="form.guardianName" label="Veli Adı" filled>
          <template #prepend><q-icon name="person" /></template>
        </q-input>
        <q-input v-model="form.guardianPhone" label="Veli Telefon" filled :error="!!errors.guardianPhone" :error-message="errors.guardianPhone">
          <template #prepend><q-icon name="phone" /></template>
        </q-input>
      </q-card-section>

      <q-separator />
      <q-card-actions align="right" class="q-pa-md">
        <q-btn flat label="İptal" color="grey-7" @click="goBack" />
        <q-btn unelevated color="primary" :label="isEdit ? 'Kaydet' : 'Ekle'" :loading="saving" @click="handleSave" />
      </q-card-actions>
    </q-card>
  </q-page>
</template>

<script setup lang="ts">
import { ref, reactive, computed, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { enrollmentApi, EDUCATION_TYPES } from 'src/api/enrollment'
import { registerStudentSchema, editStudentSchema } from 'src/schemas/student'
import { useNotify } from 'src/composables/useNotify'
import { zodValidate } from 'src/composables/useZodValidation'
import { useKeycloakUserOptions, useBranchOptions, type SelectOption } from 'src/composables/useEntityOptions'
import { useAcademicPeriodStore } from 'stores/academicPeriod'
import { useEntityOptionsStore } from 'stores/entityOptions'
import SelectEmptyOption from 'components/SelectEmptyOption.vue'

const route = useRoute()
const router = useRouter()
const notify = useNotify()
const periodStore = useAcademicPeriodStore()
const entityOptionsStore = useEntityOptionsStore()
const userOpts = useKeycloakUserOptions()
const branchOpts = useBranchOptions()

const studentId = computed(() => (route.params.id as string | undefined) ?? null)
const isEdit = computed(() => !!studentId.value)

const loading = ref(false)
const saving = ref(false)
const branchName = ref('')

const form = reactive({
  keycloakUserId: '',
  fullName: '',
  email: '',
  branchCode: '',
  specializationCode: '',
  educationType: 'Formal' as string,
  classYear: 11,
  section: '',
  studentNumber: '',
  phoneNumber: '',
  tcKimlikNo: '',
  guardianName: '',
  guardianPhone: '',
})
const errors = reactive<Record<string, string>>({})

const educationTypeOptions = [...EDUCATION_TYPES]
const specOptions = computed(() => branchOpts.getSpecializations(form.branchCode ?? ''))

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

function onBranchChange(code?: string | null) {
  branchName.value = code ? branchOpts.getFieldName(code) : ''
  form.specializationCode = ''
}

async function loadStudent() {
  if (!studentId.value) return
  loading.value = true
  try {
    const { data: s } = await enrollmentApi.getStudent(studentId.value)
    branchName.value = s.branchName
    let email = ''
    if (s.keycloakUserId) {
      await userOpts.load()
      const found = userOpts.allOptions.value.find((o: SelectOption) => o.value === s.keycloakUserId)
      if (found?.caption) {
        const m = found.caption.match(/^(.+?)\s*\(/)
        email = m ? m[1] : found.caption
      }
    }
    Object.assign(form, {
      fullName: s.fullName,
      email,
      branchCode: s.branchCode,
      specializationCode: s.specializationCode ?? '',
      classYear: s.classYear,
      section: s.section ?? '',
      studentNumber: s.studentNumber ?? '',
      phoneNumber: s.phoneNumber ?? '',
      tcKimlikNo: s.tcKimlikNo ?? '',
      guardianName: s.guardianName ?? '',
      guardianPhone: s.guardianPhone ?? '',
    })
  } catch (e) {
    notify.apiError(e, 'Öğrenci bilgileri yüklenemedi.')
    goBack()
  } finally {
    loading.value = false
  }
}

function goBack() {
  void router.push('/enrollment/students')
}

async function handleSave() {
  for (const key of Object.keys(errors)) errors[key] = ''

  if (isEdit.value) {
    if (!zodValidate(editStudentSchema, form, errors)) return
    saving.value = true
    try {
      await enrollmentApi.updateStudent(studentId.value!, {
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
      goBack()
    } catch (e) {
      notify.apiError(e, 'Öğrenci güncellenirken bir hata oluştu.')
    } finally {
      saving.value = false
    }
  } else {
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
      entityOptionsStore.invalidateStudents()
      notify.success('Öğrenci başarıyla kaydedildi.')
      goBack()
    } catch (e) {
      notify.apiError(e, 'Öğrenci eklenirken bir hata oluştu.')
    } finally {
      saving.value = false
    }
  }
}

onMounted(async () => {
  branchOpts.reset()
  await branchOpts.load()
  if (isEdit.value) {
    await loadStudent()
  } else {
    userOpts.reset()
    await userOpts.load()
  }
})
</script>
