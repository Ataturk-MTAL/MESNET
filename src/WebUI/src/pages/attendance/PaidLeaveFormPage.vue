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
        aria-label="İzin başvuruları listesine dön"
        class="q-mr-sm"
        @click="goBack"
      >
        <q-tooltip>İzin başvuruları listesine dön</q-tooltip>
      </q-btn>
      <h1 class="text-h5 text-weight-bold col q-my-none">
        Ücretli İzin Başvurusu
      </h1>
    </div>

    <q-card
      flat
      bordered
      style="max-width: 640px"
      class="q-mx-auto"
    >
      <q-card-section>
        <AppNotice
          type="info"
          dense
        >
          Başvurunuz önce işletmenizin, sonra okul yönetiminin onayından geçer. İki onay
          tamamlanmadan izin günleri kaydedilmez.
        </AppNotice>
      </q-card-section>

      <q-card-section class="q-gutter-md">
        <q-input
          v-model="form.startDate"
          label="Başlangıç Tarihi *"
          outlined
          type="date"
          :min="today"
          hint="Ücretli izin önceden planlanır; geçmiş tarih seçilemez"
        >
          <template #prepend>
            <q-icon name="event" />
          </template>
        </q-input>
        <q-input
          v-model="form.endDate"
          label="Bitiş Tarihi *"
          outlined
          type="date"
          :min="form.startDate || today"
        >
          <template #prepend>
            <q-icon name="event_available" />
          </template>
        </q-input>
        <q-input
          v-model="form.reason"
          label="Gerekçe *"
          outlined
          autogrow
          counter
          :maxlength="500"
          hint="Örn. telafi eğitimi, okulda sınav, yarıyıl tatili"
        >
          <template #prepend>
            <q-icon name="notes" />
          </template>
        </q-input>

        <div
          v-if="dayCount > 0"
          class="text-body2"
          :class="isRangeTooLong ? 'text-negative' : 'text-grey-7'"
        >
          Seçilen aralık: <strong>{{ dayCount }} gün</strong>
          <span v-if="isRangeTooLong"> — en çok {{ PAID_LEAVE_MAX_DAYS }} gün olabilir.</span>
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
          label="Başvur"
          :loading="saving"
          :disable="!isValid"
          @click="handleSave"
        />
      </q-card-actions>
    </q-card>
  </q-page>
</template>

<script setup lang="ts">
import { ref, reactive, computed } from 'vue'
import { useRouter } from 'vue-router'
import { paidLeaveApi, PAID_LEAVE_MAX_DAYS } from 'src/api/paidLeave'
import { useNotify } from 'src/composables/useNotify'
import { useAcademicPeriodStore } from 'stores/academicPeriod'
import AppNotice from 'components/AppNotice.vue'

const router = useRouter()
const notify = useNotify()
const periodStore = useAcademicPeriodStore()
const saving = ref(false)

// Öğrenci kimliği forma GİRİLMEZ — sunucu token'daki student_id claim'inden alır.
const form = reactive({
  startDate: '',
  endDate: '',
  reason: '',
})

const today = new Date().toISOString().slice(0, 10)

const dayCount = computed(() => {
  if (!form.startDate || !form.endDate) return 0
  const start = new Date(form.startDate)
  const end = new Date(form.endDate)
  const diff = Math.floor((end.getTime() - start.getTime()) / (1000 * 60 * 60 * 24))
  return diff < 0 ? 0 : diff + 1
})

const isRangeTooLong = computed(() => dayCount.value > PAID_LEAVE_MAX_DAYS)

const isValid = computed(
  () => dayCount.value > 0 && !isRangeTooLong.value && form.reason.trim().length > 0,
)

function goBack() {
  router.push('/attendance/paid-leave').catch(() => {})
}

async function handleSave() {
  if (!isValid.value) return

  saving.value = true
  try {
    await paidLeaveApi.create(periodStore.selectedPeriodId ?? '', {
      startDate: new Date(form.startDate).toISOString(),
      endDate: new Date(form.endDate).toISOString(),
      reason: form.reason.trim(),
    })
    notify.success('İzin başvurunuz alındı. İşletme ve okul onayı bekleniyor.')
    goBack()
  } catch (e) {
    notify.apiError(e, 'Başvuru sırasında bir hata oluştu.')
  } finally {
    saving.value = false
  }
}
</script>
