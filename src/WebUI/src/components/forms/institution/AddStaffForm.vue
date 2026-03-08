<template>
  <FormDialog v-model="open" title="Personel Yetkilendir" icon="person_add" color="teal" save-label="Yetkilendir" :saving="saving" :save-disabled="!form.keycloakUserId || !form.role" @save="handleSave">
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
            <q-item-label v-if="opt.caption" caption>{{ opt.caption }}</q-item-label>
          </q-item-section>
        </q-item>
      </template>
      <template #no-option>
        <q-item>
          <q-item-section class="text-grey">Sonuç bulunamadı</q-item-section>
        </q-item>
      </template>
    </q-select>
    <q-input v-model="form.fullName" label="Ad Soyad" filled readonly>
      <template #prepend>
        <q-icon name="badge" />
      </template>
    </q-input>
    <q-select
      v-model="form.role"
      :options="staffRoleOptions"
      label="Rol *"
      filled
      emit-value
      map-options
    >
      <template #prepend>
        <q-icon name="manage_accounts" />
      </template>
    </q-select>
    <q-select
      v-model="form.branchCode"
      :options="branchOptions"
      label="Alan (opsiyonel)"
      filled
      emit-value
      map-options
      clearable
    >
      <template #prepend>
        <q-icon name="category" />
      </template>
    </q-select>
  </FormDialog>
</template>

<script setup lang="ts">
import { ref, reactive, watch } from 'vue'
import { institutionApi } from 'src/api/institution'
import { useNotify } from 'src/composables/useNotify'
import { useKeycloakUserOptions } from 'src/composables/useEntityOptions'
import FormDialog from 'components/FormDialog.vue'

const open = defineModel<boolean>({ required: true })

const props = defineProps<{
  institutionId: string
  branchOptions: Array<{ label: string; value: string }>
}>()

const emit = defineEmits<{ saved: [] }>()

const notify = useNotify()
const userOpts = useKeycloakUserOptions()
const saving = ref(false)

const form = reactive({
  keycloakUserId: '',
  fullName: '',
  role: '',
  branchCode: null as string | null,
})

const staffRoleOptions = [
  { label: 'Müdür', value: 'Principal' },
  { label: 'Müdür Yardımcısı', value: 'VicePrincipal' },
  { label: 'Koordinatör', value: 'Coordinator' },
  { label: 'Alan Şefi', value: 'DepartmentHead' },
  { label: 'Personel', value: 'Staff' },
]

watch(open, (isOpen) => {
  if (isOpen) {
    form.keycloakUserId = ''
    form.fullName = ''
    form.role = ''
    form.branchCode = null
    userOpts.reset()
    userOpts.load({ institutionId: props.institutionId })
  }
})

function onUserSelect(val: string | null) {
  if (val) {
    const selected = userOpts.allOptions.value.find((o) => o.value === val)
    if (selected) form.fullName = selected.label
  } else {
    form.fullName = ''
  }
}

async function handleSave() {
  saving.value = true
  try {
    await institutionApi.authorizeStaff(props.institutionId, {
      keycloakUserId: form.keycloakUserId,
      fullName: form.fullName,
      role: form.role,
      branchCode: form.branchCode || undefined,
    })
    notify.success('Personel başarıyla yetkilendirildi.')
    open.value = false
    emit('saved')
  } catch (e) {
    notify.apiError(e, 'Personel eklenirken bir hata oluştu.')
  } finally {
    saving.value = false
  }
}
</script>
