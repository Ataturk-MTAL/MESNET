<template>
  <q-page padding>
    <div class="text-h5 text-weight-bold q-mb-xs">
      Kurum Koordinasyon Yapılandırması
    </div>
    <div class="text-caption text-grey-7 q-mb-lg">
      Mesafe-saat mevzuat tablosu, büyükşehir sınırı ve azami haftalık ek ders saati.
      Bu ayarlar kurum genelidir ve akademik dönemden bağımsızdır.
    </div>

    <!--
      Etki uyarısı: bu ayarlar sessizce değişmemeli. Mesafe kuralları ve azami haftalık
      ek ders saati TÜM alanların işletme saat tavanlarını ve otomatik dağıtım önerilerini
      kaydırır.
    -->
    <AppNotice
      type="warning"
      class="q-mb-md"
    >
      <div class="text-weight-medium">
        Bu ayarlar kurum genelini etkiler.
      </div>
      <div>
        Mesafe-saat kuralları ve azami haftalık ek ders saati, <strong>tüm alanların</strong>
        işletme saat tavanlarını ve otomatik dağıtım önerilerini değiştirir. Kaydettiğinizde
        yeni hesaplanan işletme tavanları bu tabloya göre oluşur.
      </div>
    </AppNotice>

    <!-- Yazma yetkisi yok — yalnız görüntüleme (#130). Kapalı dönem kilidi DEĞİL. -->
    <AppNotice
      v-if="!canManage"
      type="readonly"
      message="Bu yapılandırmayı yalnızca görüntüleyebilirsiniz; değiştirme yetkisi kurum yöneticisi ve müdür yardımcısındadır."
      class="q-mb-md"
    />

    <AppNotice
      v-if="loadFailed"
      type="error"
      message="Yapılandırma sunucudan yüklenemedi. Aşağıdaki değerler varsayılanlardır; kaydetmeden önce sayfayı yenileyin."
      class="q-mb-md"
    />

    <!-- Mesafe-Saat Kuralları -->
    <q-card
      flat
      bordered
      class="q-mb-md"
    >
      <q-card-section>
        <div class="text-subtitle1 text-weight-medium q-mb-sm">
          Mesafe-Saat Kuralları
        </div>
        <div class="text-caption text-grey-7 q-mb-md">
          İşletmenin okula uzaklığı, kendisine verilebilecek azami koordinatörlük saatini
          belirler. Kurallar mesafeye göre artan sırada değerlendirilir; ilk uyan kural geçerlidir.
        </div>

        <q-inner-loading :showing="loading" />

        <q-markup-table
          flat
          bordered
          separator="cell"
          class="q-mb-md"
        >
          <thead>
            <tr class="bg-grey-2">
              <th
                class="text-left"
                style="width: 200px"
              >
                Azami Mesafe
              </th>
              <th
                class="text-center"
                style="width: 180px"
              >
                Koordinatörlük Saati
              </th>
              <th class="text-left">
                Anlamı
              </th>
              <th
                class="text-center"
                style="width: 80px"
              >
                İşlem
              </th>
            </tr>
          </thead>
          <tbody>
            <tr
              v-for="(rule, idx) in distanceHourRules"
              :key="idx"
            >
              <td class="text-left">
                <div
                  v-if="isCatchAllRule(rule)"
                  class="text-weight-medium text-grey-8"
                >
                  {{ CATCH_ALL_DISTANCE_LABEL }}
                </div>
                <q-input
                  v-else
                  v-model.number="distanceHourRules[idx].maxDistanceKm"
                  type="number"
                  dense
                  outlined
                  suffix="km"
                  :min="0"
                  :step="0.5"
                  :disable="!canManage"
                  :aria-label="`${idx + 1}. kural azami mesafe (km)`"
                  style="max-width: 150px"
                />
              </td>
              <td class="text-center">
                <q-input
                  v-model.number="distanceHourRules[idx].hours"
                  type="number"
                  dense
                  outlined
                  :min="MIN_RULE_HOURS"
                  :max="MAX_RULE_HOURS"
                  :step="1"
                  :disable="!canManage"
                  :aria-label="`${idx + 1}. kural koordinatörlük saati`"
                  style="max-width: 110px; margin: 0 auto"
                />
              </td>
              <td class="text-left text-grey-8">
                {{ describeRule(rule) }}
              </td>
              <td class="text-center">
                <q-btn
                  v-if="canRemoveRule(rule)"
                  flat
                  dense
                  round
                  color="negative"
                  icon="delete"
                  aria-label="Kuralı sil"
                  @click="removeRule(idx)"
                >
                  <q-tooltip>Kuralı sil</q-tooltip>
                </q-btn>
                <q-icon
                  v-else-if="isCatchAllRule(rule)"
                  name="lock"
                  color="grey-6"
                  size="18px"
                >
                  <q-tooltip>
                    "{{ CATCH_ALL_DISTANCE_LABEL }}" kuralı tablonun tavanıdır ve silinemez.
                  </q-tooltip>
                </q-icon>
              </td>
            </tr>
          </tbody>
        </q-markup-table>

        <q-btn
          v-if="canManage"
          flat
          dense
          color="primary"
          icon="add"
          label="Kural Ekle"
          @click="addRule"
        />
      </q-card-section>
    </q-card>

    <!-- Genel Ayarlar -->
    <q-card
      flat
      bordered
      class="q-mb-md"
    >
      <q-card-section>
        <div class="text-subtitle1 text-weight-medium q-mb-md">
          Genel Ayarlar
        </div>

        <div class="row q-col-gutter-md items-center">
          <div class="col-12 col-sm-6">
            <q-toggle
              v-model="isMetropolitan"
              label="Büyükşehir sınırları içinde"
              :disable="!canManage"
            />
            <div class="text-caption text-grey-7">
              Kurumun büyükşehir belediyesi sınırları içinde olup olmadığı.
            </div>
          </div>
          <div class="col-12 col-sm-6">
            <q-input
              v-model.number="maxWeeklyExtraHours"
              type="number"
              label="Azami Haftalık Ek Ders Saati"
              outlined
              dense
              :min="MIN_WEEKLY_EXTRA_HOURS"
              :max="MAX_WEEKLY_EXTRA_HOURS"
              :step="1"
              :disable="!canManage"
              :hint="`Öğretmen başına, ${MIN_WEEKLY_EXTRA_HOURS}-${MAX_WEEKLY_EXTRA_HOURS} saat aralığında.`"
            />
          </div>
        </div>
      </q-card-section>
    </q-card>

    <!-- Doğrulama Hataları -->
    <AppNotice
      v-if="validationErrors.length > 0"
      type="error"
      class="q-mb-md"
    >
      <div class="text-weight-medium q-mb-xs">
        Kaydetmeden önce düzeltin:
      </div>
      <ul class="q-my-none q-pl-md">
        <li
          v-for="(err, i) in validationErrors"
          :key="i"
        >
          {{ err }}
        </li>
      </ul>
    </AppNotice>

    <!-- Son Güncelleme + Kaydet -->
    <div class="row items-center q-col-gutter-md">
      <div class="col text-caption text-grey-7">
        <template v-if="lastUpdatedAt || lastUpdatedBy">
          Son güncelleme: <strong>{{ formatTimestamp(lastUpdatedAt) }}</strong>
          <span v-if="lastUpdatedBy"> — {{ lastUpdatedBy }}</span>
        </template>
        <template v-else>
          Bu yapılandırma henüz kaydedilmedi; gösterilen değerler varsayılanlardır.
        </template>
      </div>
      <div class="col-auto">
        <!-- Tooltip devre dışı düğmede açılmaz; sarmalayıcıya bağlanır. -->
        <div class="inline-block">
          <q-btn
            color="positive"
            icon="save"
            label="Yapılandırmayı Kaydet"
            :loading="saving"
            :disable="!canSave"
            @click="confirmAndSave"
          />
          <q-tooltip v-if="saveDisabledReason">
            {{ saveDisabledReason }}
          </q-tooltip>
        </div>
      </div>
    </div>
  </q-page>
</template>

<script setup lang="ts">
import { computed, onMounted } from 'vue'
import { useNotify } from 'src/composables/useNotify'
import { useConfirmDialog } from 'src/composables/useConfirmDialog'
import { useCoordinationConfig } from 'src/composables/useCoordinationConfig'
import {
  CATCH_ALL_DISTANCE_LABEL,
  MIN_RULE_HOURS,
  MAX_RULE_HOURS,
  MIN_WEEKLY_EXTRA_HOURS,
  MAX_WEEKLY_EXTRA_HOURS,
  describeRule,
  isCatchAllRule,
} from 'src/utils/coordinationConfig'
import { useAuthStore } from 'stores/auth'
import { Permissions } from 'src/utils/permissions'
import AppNotice from 'components/AppNotice.vue'

const notify = useNotify()
const authStore = useAuthStore()
const { confirm } = useConfirmDialog()

/**
 * Sayfayı GÖRME yetkisi route meta'sındadır (`department:distribution:manage`).
 * DEĞİŞTİRME ayrı bir izindir (#130) — karar rol adına değil izne bakar.
 */
const canManage = computed(() =>
  authStore.hasPermission(Permissions.Institution.CoordinationConfigManage),
)

const {
  loading,
  saving,
  loadFailed,
  distanceHourRules,
  isMetropolitan,
  maxWeeklyExtraHours,
  lastUpdatedAt,
  lastUpdatedBy,
  validationErrors,
  canSave,
  saveDisabledReason,
  load,
  save,
  addRule,
  removeRule,
  canRemoveRule,
} = useCoordinationConfig({ notify, canManage })

function formatTimestamp(value: string | null): string {
  if (!value) return '—'
  return new Date(value).toLocaleString('tr-TR', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  })
}

/** Kaydetmeden önce etki onayı — ayarlar sessizce değişmemeli. */
function confirmAndSave() {
  confirm({
    title: 'Kurum genelinde değişiklik',
    message:
      'Mesafe-saat kuralları ve azami haftalık ek ders saati tüm alanların işletme saat ' +
      'tavanlarını ve otomatik dağıtım önerilerini etkiler. Kaydetmek istiyor musunuz?',
    okLabel: 'Kaydet',
    okColor: 'positive',
    onOk: () => {
      save().catch(() => {})
    },
  })
}

onMounted(() => {
  load().catch(() => {})
})
</script>
