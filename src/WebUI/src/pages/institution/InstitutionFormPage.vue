<template>
  <q-page padding>
    <div class="row items-center q-mb-lg q-mx-auto" style="max-width: 640px">
      <q-btn flat round dense icon="arrow_back" aria-label="Kurum bilgilerine dön" class="q-mr-sm" @click="goBack">
        <q-tooltip>Kurum bilgilerine dön</q-tooltip>
      </q-btn>
      <div class="text-h5 text-weight-bold col">Kurum Bilgilerini Düzenle</div>
    </div>

    <q-card flat bordered style="max-width: 640px" class="relative-position q-mx-auto">
      <q-inner-loading :showing="loading" />
      <q-card-section class="q-gutter-md">
        <q-input v-model="form.fullName" label="Kurum Adı" filled>
          <template #prepend><q-icon name="account_balance" /></template>
        </q-input>
        <q-input v-model="form.address" label="Adres" filled>
          <template #prepend><q-icon name="location_on" /></template>
        </q-input>
        <q-input v-model="form.phoneNumber" label="Telefon" filled>
          <template #prepend><q-icon name="phone" /></template>
        </q-input>
        <q-input v-model="form.email" label="E-posta" filled type="email">
          <template #prepend><q-icon name="email" /></template>
        </q-input>
        <q-input v-model="form.webUrl" label="Web Sitesi" filled>
          <template #prepend><q-icon name="language" /></template>
        </q-input>
      </q-card-section>

      <q-separator />
      <q-card-actions align="right" class="q-pa-md">
        <q-btn flat label="İptal" color="grey-7" @click="goBack" />
        <q-btn unelevated color="primary" label="Kaydet" :loading="saving" @click="handleSave" />
      </q-card-actions>
    </q-card>
  </q-page>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { institutionApi } from 'src/api/institution'
import { useNotify } from 'src/composables/useNotify'
import { useInstitutionStore } from 'stores/institution'

const router = useRouter()
const notify = useNotify()
const institutionStore = useInstitutionStore()

const loading = ref(false)
const saving = ref(false)
const institutionId = ref('')

const form = reactive({
  fullName: '',
  address: '',
  phoneNumber: '',
  email: '',
  webUrl: '',
})

function goBack() {
  void router.push('/institution')
}

async function loadInstitution() {
  loading.value = true
  try {
    const { data: institutions } = await institutionApi.list()
    if (!institutions || institutions.length === 0) {
      goBack()
      return
    }
    institutionId.value = institutions[0].id
    const { data: inst } = await institutionApi.get(institutionId.value)
    form.fullName = inst.fullName
    form.address = inst.address ?? ''
    form.phoneNumber = inst.phoneNumber ?? ''
    form.email = inst.email ?? ''
    form.webUrl = inst.webUrl ?? ''
  } catch (e) {
    notify.apiError(e, 'Kurum bilgileri yüklenemedi.')
    goBack()
  } finally {
    loading.value = false
  }
}

async function handleSave() {
  saving.value = true
  try {
    await institutionApi.update(institutionId.value, {
      fullName: form.fullName,
      address: form.address || undefined,
      phoneNumber: form.phoneNumber || undefined,
      email: form.email || undefined,
      webUrl: form.webUrl || undefined,
    })
    institutionStore.clear()
    notify.success('Kurum bilgileri güncellendi.')
    goBack()
  } catch (e) {
    notify.apiError(e, 'Güncelleme sırasında bir hata oluştu.')
  } finally {
    saving.value = false
  }
}

onMounted(loadInstitution)
</script>
