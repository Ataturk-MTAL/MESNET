<template>
  <q-page padding>
    <div class="text-h5 text-weight-bold q-mb-lg">
      Ders Yükü Havuzu
    </div>

    <!-- Alan Seçici -->
    <div class="row q-col-gutter-md q-mb-lg items-end">
      <div class="col-12 col-sm-3">
        <BranchSelector
          v-model="branchFilter"
          @update:model-value="onBranchChange"
        />
      </div>
    </div>

    <AppNotice
      v-if="!branchFilter"
      type="info"
      message="Ders yükü havuzu yapılandırmak için önce bir alan seçin."
      class="q-mb-md"
    />

    <!-- Read-only Uyarı -->
    <AppNotice
      v-if="periodStore.isReadOnly"
      type="readonly"
      message="Kapalı dönem — yalnızca görüntüleme modu."
      class="q-mb-md"
    />

    <!-- Alan Ders Yükü Yapılandırması -->
    <q-card
      v-if="branchFilter"
      flat
      bordered
    >
      <q-card-section>
        <div class="text-subtitle1 text-weight-medium q-mb-sm">
          Alan Ders Yükü Yapılandırması
        </div>
        <div class="text-caption text-grey-7 q-mb-md">
          Norm Kadro Yönetmeliği Madde 22'ye göre grup sayısı ve şeflik saatleri ile toplam ders yükü havuzu hesaplanır.
        </div>

        <q-inner-loading :showing="workloadLoading" />

        <div class="row q-col-gutter-md q-mb-md items-end">
          <div class="col-12 col-sm-3">
            <q-select
              v-model="wlEducationType"
              :options="EDUCATION_TYPES"
              label="Eğitim Tipi"
              outlined
              dense
              emit-value
              map-options
              :disable="periodStore.isReadOnly"
              @update:model-value="loadWorkloadConfig"
            />
          </div>
          <div class="col-auto">
            <q-btn
              flat
              dense
              color="warning"
              icon="sync"
              label="Öğrenci Sayılarını Güncelle"
              :loading="syncingCounts"
              :disable="periodStore.isReadOnly"
              @click="syncStudentCounts"
            >
              <q-tooltip>Enrollment kayıtlarından öğrenci sayılarını yeniden hesapla</q-tooltip>
            </q-btn>
          </div>
        </div>

        <!-- Şeflik -->
        <div class="text-body2 text-weight-medium q-mb-sm">
          Şeflik
        </div>
        <div class="row q-col-gutter-md q-mb-md">
          <div class="col-6 col-sm-3">
            <q-input
              v-model.number="wlDeptHeadCount"
              type="number"
              label="Alan Şefi Sayısı"
              outlined
              dense
              :min="0"
              :max="1"
              :disable="periodStore.isReadOnly"
            />
          </div>
          <div class="col-6 col-sm-3">
            <q-input
              v-model.number="wlDeptHeadHours"
              type="number"
              label="Alan Şefi Saati"
              outlined
              dense
              :min="0"
              :disable="periodStore.isReadOnly"
            />
          </div>
          <div class="col-6 col-sm-3">
            <q-input
              v-model.number="wlWorkshopHeadCount"
              type="number"
              label="Atölye Şefi Sayısı"
              outlined
              dense
              :min="0"
              :disable="periodStore.isReadOnly"
            />
          </div>
          <div class="col-6 col-sm-3">
            <q-input
              v-model.number="wlWorkshopHeadHours"
              type="number"
              label="Atölye Şefi Saati"
              outlined
              dense
              :min="0"
              :disable="periodStore.isReadOnly"
            />
          </div>
        </div>
        <div class="text-body2 q-mb-md">
          Şeflik Toplamı: <strong class="text-secondary-strong">{{ wlSupervisorTotal }}</strong> saat
          <span class="text-caption text-grey-7">
            ({{ wlDeptHeadCount }} × {{ wlDeptHeadHours }} + {{ wlWorkshopHeadCount }} × {{ wlWorkshopHeadHours }})
          </span>
        </div>

        <!-- Sınıf Bazlı Ders Yükü -->
        <div class="text-body2 text-weight-medium q-mb-sm">
          Sınıf Bazlı Ders Yükü
        </div>
        <q-markup-table
          flat
          bordered
          separator="cell"
          class="q-mb-md"
        >
          <thead>
            <tr class="bg-grey-2">
              <th
                class="text-center"
                style="width: 80px"
              >
                Sınıf
              </th>
              <th
                class="text-center"
                style="width: 130px"
              >
                Öğrenci Sayısı
              </th>
              <th
                class="text-center"
                style="width: 130px"
              >
                Haftalık Ders
              </th>
              <th
                class="text-center"
                style="width: 80px"
              >
                Grup
              </th>
              <th
                class="text-center"
                style="width: 100px"
              >
                Alt Toplam
              </th>
            </tr>
          </thead>
          <tbody>
            <tr
              v-for="(cl, idx) in wlClassLevels"
              :key="cl.classYear"
            >
              <td class="text-center text-weight-medium">
                {{ cl.classYear }}. Sınıf
              </td>
              <td class="text-center text-weight-medium">
                {{ cl.studentCount }}
              </td>
              <td class="text-center">
                <q-input
                  v-model.number="wlClassLevels[idx].weeklyLessonHours"
                  type="number"
                  dense
                  outlined
                  :min="0"
                  style="max-width: 100px; margin: 0 auto"
                  :disable="periodStore.isReadOnly"
                />
              </td>
              <td class="text-center text-weight-medium text-info-strong">
                {{ estimateGroupCount(wlEducationType, cl.classYear, cl.studentCount) }}
              </td>
              <td class="text-center text-weight-medium">
                {{ cl.weeklyLessonHours * estimateGroupCount(wlEducationType, cl.classYear, cl.studentCount) }}
              </td>
            </tr>
          </tbody>
        </q-markup-table>

        <!-- Toplamlar + Kaydet -->
        <div class="row items-center">
          <div class="text-body2">
            Ders Yükü: <strong class="text-info-strong">{{ wlTeachingTotal }}</strong>
            &nbsp;+&nbsp; Şeflik: <strong class="text-secondary-strong">{{ wlSupervisorTotal }}</strong>
            &nbsp;=&nbsp; <strong class="text-positive-strong text-h6">HAVUZ: {{ wlPoolTotal }} saat</strong>
          </div>
          <q-space />
          <q-btn
            color="positive"
            icon="save"
            label="Yapılandırmayı Kaydet"
            :loading="workloadSaving"
            :disable="periodStore.isReadOnly"
            @click="saveWorkloadConfig"
          />
        </div>
      </q-card-section>
    </q-card>
  </q-page>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useNotify } from 'src/composables/useNotify'
import { useWorkloadConfig, estimateGroupCount, EDUCATION_TYPES } from 'src/composables/useWorkloadConfig'
import { useAuthStore } from 'stores/auth'
import { useAcademicPeriodStore } from 'stores/academicPeriod'
import BranchSelector from 'components/BranchSelector.vue'
import AppNotice from 'components/AppNotice.vue'

const notify = useNotify()
const authStore = useAuthStore()
const periodStore = useAcademicPeriodStore()

const branchFilter = ref<string | null>(null)

const institutionId = computed(() => authStore.user?.institutionId ?? undefined)
const periodId = computed(() => periodStore.selectedPeriodId)

const {
  workloadLoading, workloadSaving, syncingCounts,
  wlEducationType, wlDeptHeadCount, wlWorkshopHeadCount,
  wlDeptHeadHours, wlWorkshopHeadHours, wlClassLevels,
  wlSupervisorTotal, wlTeachingTotal, wlPoolTotal,
  loadWorkloadConfig, saveWorkloadConfig, syncStudentCounts,
} = useWorkloadConfig({
  branchFilter,
  periodId,
  institutionId,
  notify,
})

function onBranchChange() {
  loadWorkloadConfig().catch(() => {})
}

onMounted(() => {
  // Alan Şefi ise BranchSelector otomatik seçer, onMounted'da yükleme yapılır
  if (authStore.isDepartmentHead && authStore.user?.branchCode) {
    branchFilter.value = authStore.user.branchCode
    loadWorkloadConfig().catch(() => {})
  }
})
</script>
