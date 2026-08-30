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
      <h1 class="text-h5 text-weight-bold col q-my-none">
        Kurum Bilgilerini Düzenle
      </h1>
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
              <q-item-section class="text-grey-7">
                Sonuç bulunamadı
              </q-item-section>
            </q-item>
          </template>
        </q-select>
        <!-- İlçe kapalı listeden seçilir; il değişince liste yeniden yüklenir ve seçim
             temizlenir, yoksa kuruma başka ilin ilçesi yapışık kalırdı. -->
        <q-select
          v-model="form.districtName"
          :options="districtOptions"
          use-input
          input-debounce="0"
          clearable
          label="İlçe"
          :hint="districtHint"
          outlined
          :disable="!form.provinceCode || districtsLoading"
          :loading="districtsLoading"
          @filter="filterDistricts"
        >
          <template #prepend>
            <q-icon name="pin_drop" />
          </template>
          <template #no-option>
            <q-item>
              <q-item-section class="text-grey-7">
                Sonuç bulunamadı
              </q-item-section>
            </q-item>
          </template>
        </q-select>
        <q-input
          v-model.number="form.institutionCode"
          label="Kurum Kodu (MEB)"
          type="number"
          outlined
          :rules="[institutionCodeRule]"
          lazy-rules
        >
          <template #prepend>
            <q-icon name="badge" />
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
import { ref, reactive, computed, watch, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { institutionApi, type ProvinceDto } from 'src/api/institution'
import { useNotify } from 'src/composables/useNotify'
import { useInstitutionStore } from 'stores/institution'
import { useAuthStore } from 'stores/auth'
import { resolveEditableInstitutionId, isActiveContextInstitution } from 'src/utils/institutionScope'
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

/** İsteğe bağlı; doluysa sıfırdan büyük olmalı (backend ile aynı kural). */
function institutionCodeRule(value: number | null): true | string {
  if (value === null || value === undefined || (value as unknown as string) === '') return true
  return value > 0 || 'Kurum kodu sıfırdan büyük olmalıdır.'
}

const router = useRouter()
const route = useRoute()
const notify = useNotify()
const institutionStore = useInstitutionStore()
const authStore = useAuthStore()

const loading = ref(false)
const saving = ref(false)
const institutionId = ref('')

const form = reactive({
  fullName: '',
  address: '',
  provinceCode: null as string | null,
  districtName: null as string | null,
  institutionCode: null as number | null,
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

const districts = ref<string[]>([])
const districtOptions = ref<string[]>([])
const districtsLoading = ref(false)

/** İlçe listesi yalnız fiilen kullanılan iller için doldurulur (TurkishDistricts). */
const districtHint = computed(() => {
  if (!form.provinceCode) return 'Önce il seçiniz.'
  if (!districtsLoading.value && districts.value.length === 0)
    return 'Bu il için ilçe listesi tanımlı değil.'
  return 'İsteğe bağlı.'
})

async function loadDistricts(provinceCode: string | null) {
  if (!provinceCode) {
    districts.value = []
    districtOptions.value = []
    return
  }

  districtsLoading.value = true
  try {
    const { data } = await institutionApi.listDistricts(provinceCode)
    districts.value = data
    districtOptions.value = data
  } catch (e) {
    districts.value = []
    districtOptions.value = []
    notify.apiError(e, 'İlçe listesi yüklenemedi.')
  } finally {
    districtsLoading.value = false
  }
}

// İl değişince ilçe listesi tazelenir. Seçili ilçe yeni ilin listesinde yoksa temizlenir —
// bırakılırsa backend 422 "ilçesi kurumun iline ait değil" döner.
watch(
  () => form.provinceCode,
  async (provinceCode) => {
    await loadDistricts(provinceCode)
    if (form.districtName && !districts.value.includes(form.districtName)) {
      form.districtName = null
    }
  },
)

function filterDistricts(needle: string, update: (fn: () => void) => void) {
  update(() => {
    const term = needle.trim().toLocaleLowerCase('tr-TR')
    districtOptions.value = term
      ? districts.value.filter((d) => d.toLocaleLowerCase('tr-TR').includes(term))
      : districts.value
  })
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
    // AYNI HATA burada da vardı: "listenin ilk satırı" sıralaması olmayan bir sorguya
    // güvenmekti ve platform aktöründe her yazmadan sonra başka bir okulu düzenletiyordu.
    // InstitutionPage 27.08.2026'da düzeltilmişti; bu çağrı yeri gözden kaçmıştı.
    const routeId = typeof route.params.id === 'string' ? route.params.id : null
    // authStore.currentInstitutionId OKUNUR — user.institutionId DEĞİL: aktif bağlam varsa
    // düzenlenen kurum davranılan (bağlamdaki) okul olmalı, il yetkilisinin kendi İl MEM
    // kaydı değil (Görev 10 ile aynı disiplin — üçüncü kopya, InstitutionPage ile aynı).
    const ownId = authStore.currentInstitutionId ?? null
    const listRes = routeId || ownId ? null : await institutionApi.list({ pageSize: 100 })
    const resolved = resolveEditableInstitutionId(routeId, ownId, listRes?.data?.items ?? [])
    if (!resolved) {
      goBack()
      return
    }
    institutionId.value = resolved
    const { data: inst } = await institutionApi.get(institutionId.value)
    form.fullName = inst.fullName
    form.address = inst.address ?? ''
    form.provinceCode = inst.provinceCode
    form.institutionCode = inst.institutionCode
    // İl watch'ı listeyi yükleyip eşleşmeyen ilçeyi temizler; ilçe ondan SONRA atanır ki
    // kayıtlı değer kendi watch'ı tarafından silinmesin.
    await loadDistricts(inst.provinceCode)
    form.districtName = inst.districtName
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
      districtName: form.districtName ?? undefined,
      institutionCode: form.institutionCode ?? undefined,
      phoneNumber: form.phoneNumber || undefined,
      email: form.email || undefined,
      webUrl: form.webUrl || undefined,
    })
    // InstitutionPage ile aynı gerekçe (bkz. o dosyadaki isActiveContext yorumu): bu form
    // BAŞKA bir kurumu (rota parametresiyle açılan) düzenleyebilir; global store'u yalnız
    // düzenlenen kurum aktif bağlamın kendisiyse geçersiz kıl.
    if (isActiveContextInstitution(institutionId.value, authStore.currentInstitutionId)) {
      institutionStore.clear()
    }
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
