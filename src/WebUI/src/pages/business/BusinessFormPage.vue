<template>
  <q-page padding>
    <div class="q-mx-auto" style="max-width: 1100px">
      <div class="row items-center q-mb-lg">
        <q-btn flat round dense icon="arrow_back" aria-label="İşletmelere dön" class="q-mr-sm" @click="goBack">
          <q-tooltip>İşletmelere dön</q-tooltip>
        </q-btn>
        <div class="text-h5 text-weight-bold col">{{ isEdit ? 'İşletme Düzenle' : 'Yeni İşletme' }}</div>
      </div>

      <q-card flat bordered class="relative-position">
        <q-inner-loading :showing="loading" />
        <q-card-section>
          <div class="row q-col-gutter-md">
            <!-- Harita: geniş ekranda solda yarım, dar ekranda en altta tam genişlik -->
            <div class="col-12 col-md-6 business-map-col">
              <div class="text-subtitle2 q-mb-xs">
                <q-icon name="map" class="q-mr-xs" />Konum
              </div>
              <MapPicker :model-value="form.location" height="480px" @update:model-value="(v) => (form.location = v)" />
            </div>

            <!-- Form alanları: geniş ekranda sağda, dar ekranda üstte -->
            <div class="col-12 col-md-6 business-fields-col q-gutter-md">
              <q-input v-model="form.name" label="İşletme Adı *" filled :error="!!errors.name" :error-message="errors.name">
                <template #prepend><q-icon name="business" /></template>
              </q-input>
              <q-input v-model="form.address" label="Adres *" filled :error="!!errors.address" :error-message="errors.address">
                <template #prepend><q-icon name="location_on" /></template>
              </q-input>
              <q-input v-model="form.phoneNumber" label="Telefon" filled>
                <template #prepend><q-icon name="phone" /></template>
              </q-input>
              <q-input v-model="form.email" label="E-posta" filled type="email" :error="!!errors.email" :error-message="errors.email">
                <template #prepend><q-icon name="email" /></template>
              </q-input>
              <q-input v-if="isEdit" v-model="form.website" label="Web Sitesi" filled>
                <template #prepend><q-icon name="language" /></template>
              </q-input>
              <q-input v-model.number="form.personnelCount" label="Personel Sayısı" filled type="number">
                <template #prepend><q-icon name="groups" /></template>
              </q-input>
              <q-select v-model="form.sectors" :options="sectorOptions" label="Sektörler" filled multiple emit-value map-options use-chips>
                <template #prepend><q-icon name="category" /></template>
              </q-select>
            </div>
          </div>
        </q-card-section>

        <q-separator />
        <q-card-actions align="right" class="q-pa-md">
          <q-btn flat label="İptal" color="grey-7" @click="goBack" />
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
  void router.push('/companies')
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

<style scoped>
/* Dar ekranda (md altı) harita en alta iner, form alanları üstte kalır */
@media (max-width: 1023px) {
  .business-map-col {
    order: 2;
  }
  .business-fields-col {
    order: 1;
  }
}
</style>
