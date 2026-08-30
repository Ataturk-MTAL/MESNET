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
        aria-label="Değerlendirmelere dön"
        class="q-mr-sm"
        @click="goBack"
      >
        <q-tooltip>Değerlendirmelere dön</q-tooltip>
      </q-btn>
      <h1 class="text-h5 text-weight-bold col q-my-none">
        Değerlendirme Ekle
      </h1>
    </div>

    <q-card
      flat
      bordered
      style="max-width: 640px"
      class="q-mx-auto"
    >
      <q-card-section class="q-gutter-md">
        <q-select
          v-model="form.businessId"
          :options="businessOpts.options.value"
          :loading="businessOpts.loading.value"
          label="İşletme *"
          outlined
          use-input
          input-debounce="0"
          emit-value
          map-options
          option-label="label"
          option-value="value"
          @filter="businessOpts.filter"
        >
          <template #prepend>
            <q-icon name="business" />
          </template>
          <template #option="{ itemProps, opt }">
            <q-item v-bind="itemProps">
              <q-item-section>
                <q-item-label>{{ opt.label }}</q-item-label>
                <q-item-label
                  v-if="opt.caption"
                  caption
                >
                  {{ opt.caption }}
                </q-item-label>
              </q-item-section>
            </q-item>
          </template>
          <template #no-option>
            <SelectEmptyOption />
          </template>
        </q-select>

        <q-input
          v-model="form.evaluationDate"
          label="Değerlendirme Tarihi"
          outlined
          type="date"
        >
          <template #prepend>
            <q-icon name="calendar_today" />
          </template>
        </q-input>

        <q-select
          v-model="form.result"
          :options="evalResultOptions"
          label="Sonuç"
          outlined
          emit-value
          map-options
        >
          <template #prepend>
            <q-icon name="fact_check" />
          </template>
        </q-select>

        <q-input
          v-model="form.notes"
          label="Notlar"
          outlined
          type="textarea"
          rows="2"
        >
          <template #prepend>
            <q-icon name="notes" />
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
/**
 * İşletme değerlendirmesi oluşturma formu.
 *
 * Neden ayrı sayfa: `BusinessEvaluation` bir VARLIK yaratılıyor — DESIGN.md "Form Sayfa
 * Kuralı" gereği oluştur/düzenle formu ayrı route sayfasıdır. `FormDialog` yan paneli
 * yalnız kısa ve bağlamsal aksiyonlar (reddet, imzala, fesih, belge yükle, silme onayı)
 * içindir; varlık oluşturma o sınıfa girmez.
 *
 * Rota izni `coordinator:visit:manage` — POST /coordination/business-evaluations ucunun
 * izniyle aynı ve liste sayfasındaki tetikleyiciyi saran `PermissionGuard` ile aynı.
 * (Okuma izniyle korunsaydı yalnız görüntüleme yetkisi olan kullanıcı formu doldurup
 * Kaydet'te 403 duvarına çarpardı — `formRoutePermissions.spec.ts` bunu kilitliyor.)
 *
 * DÜZENLEME AKIŞI YOK: `coordinationApi` yalnız `listEvaluations` + `createEvaluation`
 * sunuyor; güncelleme/silme ucu bugün mevcut değil. Bu yüzden yalnız yeni-kayıt rotası
 * eklendi, `/:id/edit` uydurulmadı.
 */
import { ref, reactive, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { coordinationApi, EVALUATION_RESULTS } from 'src/api/coordination'
import { useNotify } from 'src/composables/useNotify'
import { useBusinessOptions } from 'src/composables/useEntityOptions'
import { useAuthStore } from 'stores/auth'
import SelectEmptyOption from 'components/SelectEmptyOption.vue'

const router = useRouter()
const notify = useNotify()
const authStore = useAuthStore()
const businessOpts = useBusinessOptions()

const saving = ref(false)

const evalResultOptions = EVALUATION_RESULTS.map((r) => ({ label: r.label, value: r.value }))

const form = reactive({
  businessId: '', evaluationDate: '',
  result: 'Suitable', notes: '',
})

function goBack() {
  router.push('/coordination/evaluations').catch(() => {})
}

async function handleSave() {
  saving.value = true
  try {
    await coordinationApi.createEvaluation({
      businessId: form.businessId,
      institutionId: authStore.user?.institutionId ?? '',
      evaluationDate: new Date(form.evaluationDate).toISOString(),
      result: form.result,
      notes: form.notes || undefined,
    })
    notify.success('Değerlendirme eklendi.')
    goBack()
  } catch (e) {
    notify.apiError(e, 'Değerlendirme eklenirken bir hata oluştu.')
  } finally {
    saving.value = false
  }
}

onMounted(() => {
  // Dialog açılışındaki `reset() + load()` sırası aynen korunuyor: sayfa açılışı artık
  // dialog açılışının yerini alıyor.
  businessOpts.reset()
  businessOpts.load().catch(() => {})
})
</script>
