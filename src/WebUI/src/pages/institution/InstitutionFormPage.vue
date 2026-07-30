<template>
  <q-page padding>
    <div
      class="row items-center q-mb-lg q-mx-auto"
      style="max-width: 640px"
    >
      <q-btn
        flat
        round
        dense
        icon="arrow_back"
        aria-label="Kurum bilgilerine dön"
        class="q-mr-sm"
        @click="goBack"
      >
        <q-tooltip>Kurum bilgilerine dön</q-tooltip>
      </q-btn>
      <div class="text-h5 text-weight-bold col">
        Kurum Bilgilerini Düzenle
      </div>
    </div>

    <q-card
      flat
      bordered
      style="max-width: 640px"
      class="relative-position q-mx-auto"
    >
      <q-inner-loading :showing="loading" />
      <q-card-section class="q-gutter-md">
        <q-input
          v-model="form.fullName"
          label="Kurum Adı"
          outlined
        >
          <template #prepend>
            <q-icon name="account_balance" />
          </template>
        </q-input>
        <q-input
          v-model="form.address"
          label="Adres"
          outlined
        >
          <template #prepend>
            <q-icon name="location_on" />
          </template>
        </q-input>
        <!-- İl serbest metin DEĞİL kod olarak saklanır (#147): kapsam kararının anahtarı. -->
        <q-select
          v-model="form.provinceCode"
          :options="provinceOptions"
          option-value="code"
          option-label="name"
          emit-value
          map-options
          use-input
          input-debounce="0"
          label="İl"
          outlined
          :loading="provincesLoading"
          :rules="[provinceRule]"
          @filter="filterProvinces"
        >
          <template #prepend>
            <q-icon name="map" />
          </template>
          <template #no-option>
            <q-item>
              <q-item-section class="text-grey">
                Sonuç bulunamadı
              </q-item-section>
            </q-item>
          </template>
        </q-select>
        <q-input
          v-model="form.districtCode"
          label="İlçe Kodu (MEB)"
          hint="İsteğe bağlı — yalnız rakam"
          outlined
          :rules="[districtRule]"
          lazy-rules
        >
          <template #prepend>
            <q-icon name="pin" />
          </template>
        </q-input>
        <q-input
          v-model="form.phoneNumber"
          label="Telefon"
          outlined
        >
          <template #prepend>
            <q-icon name="phone" />
          </template>
        </q-input>
        <q-input
          v-model="form.email"
          label="E-posta"
          outlined
          type="email"
        >
          <template #prepend>
            <q-icon name="email" />
          </template>
        </q-input>
        <!-- Kaynakta engelle: güvensiz şema kaydedilirse görüntüleyen herkes risk altında. -->
        <q-input
          v-model="form.webUrl"
          label="Web Sitesi"
          outlined
          :rules="[webUrlRule]"
          lazy-rules
        >
          <template #prepend>
            <q-icon name="language" />
          </template>
        </q-input>
      </q-card-section>

      <q-separator />
      <q-card-actions
        align="right"
        class="q-pa-md"
      >
        <q-btn
          flat
          label="İptal"
          color="grey-7"
          @click="goBack"
        />
        <q-btn
          unelevated
          color="primary"
          label="Kaydet"
          :loading="saving"
          @click="handleSave"
        />
      </q-card-actions>
    </q-card>
  </q-page>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { institutionApi, type ProvinceDto } from 'src/api/institution'
import { useNotify } from 'src/composables/useNotify'
import { useInstitutionStore } from 'stores/institution'
import { isSafeUrl } from 'utils/safeUrl'

/** Boş geçilebilir; doluysa yalnız http(s) kabul edilir. */
function webUrlRule(value: string): true | string {
  if (!value) return true
  return isSafeUrl(value) || 'Geçerli bir web adresi girin (yalnız http/https).'
}

/** İl zorunlu — kapsam anahtarı olduğu için boş kaydedilemez (#147). */
function provinceRule(value: string | null): true | string {
  return !!value || 'İl seçilmelidir.'
}

/** İsteğe bağlı; doluysa yalnız rakam. Hane sayısı kısıtlanmaz (backend ile aynı kural). */
function districtRule(value: string): true | string {
  if (!value) return true
  return /^\d+$/.test(value) || 'İlçe kodu yalnız rakam içerebilir.'
}

const router = useRouter()
const notify = useNotify()
const institutionStore = useInstitutionStore()

const loading = ref(false)
const saving = ref(false)
const institutionId = ref('')

const form = reactive({
  fullName: '',
  address: '',
  provinceCode: null as string | null,
  districtCode: '',
  phoneNumber: '',
  email: '',
  webUrl: '',
})

// 81 kayıtlık ulusal referans, tek sayfada kullanılıyor — Pinia'ya konmaz, per-instance kalır.
const provinces = ref<ProvinceDto[]>([])
const provinceOptions = ref<ProvinceDto[]>([])
const provincesLoading = ref(false)

async function loadProvinces() {
  provincesLoading.value = true
  try {
    const { data } = await institutionApi.listProvinces()
    provinces.value = data
    provinceOptions.value = data
  } catch (e) {
    notify.apiError(e, 'İl listesi yüklenemedi.')
  } finally {
    provincesLoading.value = false
  }
}

function filterProvinces(needle: string, update: (fn: () => void) => void) {
  update(() => {
    const term = needle.trim().toLocaleLowerCase('tr-TR')
    provinceOptions.value = term
      ? provinces.value.filter(
          (p) => p.name.toLocaleLowerCase('tr-TR').includes(term) || p.code.startsWith(term),
        )
      : provinces.value
  })
}

function goBack() {
  router.push('/institution').catch(() => {})
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
    form.provinceCode = inst.provinceCode
    form.districtCode = inst.districtCode ?? ''
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
  // Sayfada q-form yok, :rules kaydetmeyi kendiliğinden engellemez. İl gönderilmezse backend
  // "değiştirme" olarak yorumlar ve eksik il sessizce eksik kalır — burada açıkça durdurulur.
  if (!form.provinceCode) {
    notify.error('İl seçilmelidir.')
    return
  }

  saving.value = true
  try {
    await institutionApi.update(institutionId.value, {
      fullName: form.fullName,
      address: form.address || undefined,
      provinceCode: form.provinceCode,
      districtCode: form.districtCode || undefined,
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

onMounted(() => {
  loadProvinces().catch(() => {})
  loadInstitution().catch(() => {})
})
</script>
