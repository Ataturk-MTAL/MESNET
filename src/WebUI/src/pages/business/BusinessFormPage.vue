<template>
  <q-page padding>
    <div
      class="q-mx-auto"
      style="max-width: 1100px"
    >
      <div class="row items-center q-mb-lg">
        <q-btn
          flat
          round
          dense
          icon="arrow_back"
          aria-label="İşletmelere dön"
          class="q-mr-sm"
          @click="goBack"
        >
          <q-tooltip>İşletmelere dön</q-tooltip>
        </q-btn>
        <h1 class="text-h5 text-weight-bold col q-my-none">
          {{ isEdit ? 'İşletme Düzenle' : 'Yeni İşletme' }}
        </h1>
      </div>

      <q-card
        flat
        bordered
        class="relative-position"
      >
        <q-inner-loading :showing="loading" />
        <q-card-section>
          <div class="row q-col-gutter-md">
            <!-- Bilgi girişleri: solda (dar ekranda üstte) -->
            <div class="col-12 col-md-6 q-gutter-md">
              <q-input
                v-model="form.name"
                label="İşletme Adı *"
                outlined
                :error="!!errors.name"
                :error-message="errors.name"
              >
                <template #prepend>
                  <q-icon name="business" />
                </template>
              </q-input>
              <q-input
                v-model="form.address"
                label="Adres *"
                outlined
                :error="!!errors.address"
                :error-message="errors.address"
              >
                <template #prepend>
                  <q-icon name="location_on" />
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
                :error="!!errors.email"
                :error-message="errors.email"
              >
                <template #prepend>
                  <q-icon name="email" />
                </template>
              </q-input>
              <q-input
                v-if="isEdit"
                v-model="form.website"
                label="Web Sitesi"
                outlined
              >
                <template #prepend>
                  <q-icon name="language" />
                </template>
              </q-input>
              <q-input
                v-model.number="form.personnelCount"
                label="Personel Sayısı"
                outlined
                type="number"
                hint="İş Kanununa tabi çalıştırılan personel sayısı — stajyer ve çıraklar dâhil edilmez. 20 ve üzeri işletmelerde öğrenci ücreti asgari ücretin %30'u, altında %15'idir."
              >
                <template #prepend>
                  <q-icon name="groups" />
                </template>
              </q-input>
              <q-select
                v-model="form.sectors"
                :options="sectorOptions"
                label="Sektörler"
                outlined
                multiple
                emit-value
                map-options
                use-chips
              >
                <template #prepend>
                  <q-icon name="category" />
                </template>
              </q-select>
              <q-toggle
                v-model="form.isPublicInstitution"
                label="Kamu kurum/kuruluşu"
                :true-value="true"
                :false-value="false"
              />
              <div class="text-caption text-grey-7 q-ml-sm">
                3308 sayılı Kanun Geçici Madde 12 gereği kamu kurum ve kuruluşlarına
                <strong>devlet katkısı ödenmez</strong>. Öğrencinin ücreti işletme tarafından
                ödenmeye devam eder; yalnız devlet payı hesaplanmaz.
              </div>
            </div>

            <!-- Harita: sağda (dar ekranda altta) -->
            <div class="col-12 col-md-6">
              <MapPicker
                :model-value="form.location"
                height="480px"
                @update:model-value="(v) => (form.location = v)"
              />
            </div>
          </div>
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
            :label="isEdit ? 'Kaydet' : 'Ekle'"
            :loading="saving"
            @click="handleSave"
          />
        </q-card-actions>
      </q-card>
    </div>
  </q-page>
</template>

<script setup lang="ts">
import { ref, reactive, computed, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { businessApi, type SectorDto } from 'src/api/business'
import { registerBusinessSchema, editBusinessSchema } from 'src/schemas/business'
import { useNotify } from 'src/composables/useNotify'
import { zodValidate } from 'src/composables/useZodValidation'
import { useEntityOptionsStore } from 'stores/entityOptions'
import MapPicker from 'components/MapPicker.vue'

const route = useRoute()
const router = useRouter()
const notify = useNotify()
const entityOptionsStore = useEntityOptionsStore()

const businessId = computed(() => (route.params.id as string | undefined) ?? null)
const isEdit = computed(() => !!businessId.value)

const loading = ref(false)
const saving = ref(false)

const form = reactive({
  name: '',
  address: '',
  phoneNumber: '',
  email: '',
  website: '',
  personnelCount: 0,
  // 3308 Geçici Madde 12 — kamu kurumlarına devlet katkısı ödenmez (#157).
  // Varsayılan false: özel işletme, sistemdeki çoğunluk.
  isPublicInstitution: false,
  location: null as { latitude: number; longitude: number } | null,
  sectors: [] as string[],
})
const errors = reactive<Record<string, string>>({})

const sectorOptions = ref<{ label: string; value: string }[]>([])

async function loadSectors() {
  try {
    const res = await businessApi.sectors()
    sectorOptions.value = res.data.map((s) => ({ label: s.slug, value: s.name }))
  } catch {
    /* sektör listesi yüklenemezse sessizce devam et */
  }
}

async function loadBusiness() {
  if (!businessId.value) return
  loading.value = true
  try {
    const { data: b } = await businessApi.get(businessId.value)
    Object.assign(form, {
      name: b.name,
      address: b.address,
      phoneNumber: b.phoneNumber ?? '',
      email: b.email ?? '',
      website: b.website ?? '',
      personnelCount: b.personnelCount,
      isPublicInstitution: b.isPublicInstitution,
      location: b.location ? { ...b.location } : null,
      sectors: b.sectors.map((s: SectorDto) => s.name),
    })
  } catch (e) {
    notify.apiError(e, 'İşletme bilgileri yüklenemedi.')
    goBack()
  } finally {
    loading.value = false
  }
}

function goBack() {
  router.push('/companies').catch(() => {})
}

async function handleSave() {
  for (const key of Object.keys(errors)) errors[key] = ''

  if (isEdit.value) {
    if (!zodValidate(editBusinessSchema, form, errors)) return
    saving.value = true
    try {
      await businessApi.update(businessId.value!, {
        name: form.name || undefined,
        address: form.address || undefined,
        phoneNumber: form.phoneNumber || undefined,
        email: form.email || undefined,
        website: form.website || undefined,
        personnelCount: form.personnelCount || undefined,
        // `|| undefined` KULLANILMAZ: `false || undefined` → undefined olur ve backend bunu
        // "dokunma" olarak yorumlar (kısmi güncelleme deseni). O zaman bir kez kamu
        // işaretlenen işletmenin işareti hiç kaldırılamazdı. Boolean her zaman aynen gider.
        isPublicInstitution: form.isPublicInstitution,
        location: form.location ?? undefined,
        sectors: form.sectors,
      })
      notify.success('İşletme bilgileri güncellendi.')
      goBack()
    } catch (e) {
      notify.apiError(e, 'İşletme güncellenirken bir hata oluştu.')
    } finally {
      saving.value = false
    }
  } else {
    if (!zodValidate(registerBusinessSchema, form, errors)) return
    saving.value = true
    try {
      await businessApi.register({
        name: form.name,
        address: form.address,
        phoneNumber: form.phoneNumber || undefined,
        email: form.email || undefined,
        personnelCount: form.personnelCount || undefined,
        isPublicInstitution: form.isPublicInstitution,
        location: form.location ?? undefined,
        sectors: form.sectors.length > 0 ? form.sectors : undefined,
      })
      entityOptionsStore.invalidateBusinesses()
      notify.success('İşletme başarıyla eklendi.')
      goBack()
    } catch (e) {
      notify.apiError(e, 'İşletme eklenirken bir hata oluştu.')
    } finally {
      saving.value = false
    }
  }
}

onMounted(async () => {
  await loadSectors()
  if (isEdit.value) await loadBusiness()
})
</script>
