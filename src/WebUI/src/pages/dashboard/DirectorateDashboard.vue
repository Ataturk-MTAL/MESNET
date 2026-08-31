<template>
  <div>
    <div class="row q-col-gutter-md q-mb-lg">
      <div class="col-12 col-sm-6 col-md-4">
        <StatCard
          icon="account_tree"
          :value="schoolCount"
          label="Okul"
          color="primary"
          :loading="loading"
        />
      </div>
      <div
        v-if="districtCount > 0 && isActingAsProvince(institutionStore.institution?.nodeType)"
        class="col-12 col-sm-6 col-md-4"
      >
        <StatCard
          icon="location_city"
          :value="districtCount"
          label="İlçe"
          color="secondary"
          :loading="loading"
        />
      </div>
      <div class="col-12 col-sm-6 col-md-4">
        <StatCard
          icon="person_off"
          :value="unmanagedCount"
          label="Yöneticisi Olmayan Okul"
          color="warning"
          :loading="loading"
        />
      </div>
    </div>

    <div class="row q-col-gutter-md">
      <!-- Yöneticisi olmayan okullar -->
      <div class="col-12 col-md-6">
        <q-card
          flat
          bordered
        >
          <q-card-section class="row items-center q-pb-none">
            <div class="text-subtitle1 text-weight-medium col">
              Yöneticisi Olmayan Okullar
            </div>
            <q-btn
              flat
              dense
              no-caps
              color="primary"
              label="Kullanıcı bağla"
              :to="{ name: 'UserManagement' }"
            />
          </q-card-section>

          <q-card-section v-if="unmanagedFailed">
            <div class="row items-center text-grey-7">
              <q-icon
                name="cloud_off"
                size="sm"
                class="q-mr-sm"
              />
              <span>Yöneticisi olmayan okullar yüklenemedi.</span>
            </div>
          </q-card-section>

          <q-card-section v-else-if="unmanagedCount === 0">
            <div class="row items-center text-grey-7">
              <q-icon
                name="verified"
                size="sm"
                class="q-mr-sm"
              />
              <span>Tüm okulların yöneticisi var.</span>
            </div>
          </q-card-section>

          <template v-else>
            <!--
              Zorunlu dağıtım adımı (POST /api/security/users/replay) atlanırsa read model boş
              kalır ve bu negatif filtre TÜM okulları döndürür — ilk kez kurulan bir il için bu
              sayı "gerçek" bir veri gibi okunur. İpucu, bu iki durumu ayırt edemeyen kullanıcıya
              en olası açıklamayı gösterir; kesin bir hata iddiası DEĞİLDİR (schoolCount ile tam
              eşitlik, teoride her okulun gerçekten yöneticisiz olduğu bir durumla da örtüşebilir).
            -->
            <q-card-section
              v-if="unmanagedCount === schoolCount && schoolCount > 0"
              class="q-pb-none"
            >
              <div class="text-caption text-grey-7">
                Tüm okullar yöneticisiz görünüyor — bu, dağıtımda
                <code>POST /api/security/users/replay</code> adımının çalıştırılmadığı anlamına
                gelebilir.
              </div>
            </q-card-section>

            <q-list separator>
              <q-item
                v-for="name in unmanagedNames"
                :key="name"
              >
                <q-item-section>{{ name }}</q-item-section>
              </q-item>
              <q-item v-if="unmanagedCount > unmanagedNames.length">
                <q-item-section class="text-grey-7">
                  ve {{ unmanagedCount - unmanagedNames.length }} okul daha
                </q-item-section>
              </q-item>
            </q-list>
          </template>
        </q-card>
      </div>

      <!-- Tıkanmış onaylar — yalnız müdahale edebilene gösterilir -->
      <div
        v-if="canOverride"
        class="col-12 col-md-6"
      >
        <q-card
          flat
          bordered
        >
          <q-card-section class="row items-center q-pb-none">
            <div class="text-subtitle1 text-weight-medium col">
              Tıkanmış Fesih Onayları
            </div>
            <q-btn
              flat
              dense
              no-caps
              color="primary"
              label="Fesihlere git"
              :to="{ name: 'InternshipTerminations' }"
            />
          </q-card-section>

          <q-card-section v-if="stuckFailed">
            <div class="row items-center text-grey-7">
              <q-icon
                name="cloud_off"
                size="sm"
                class="q-mr-sm"
              />
              <span>Tıkanmış onaylar yüklenemedi.</span>
            </div>
          </q-card-section>

          <q-card-section v-else-if="stuckCount === 0">
            <div class="row items-center text-grey-7">
              <q-icon
                name="task_alt"
                size="sm"
                class="q-mr-sm"
              />
              <span>{{ stuckThresholdDays }} günden uzun bekleyen onay yok.</span>
            </div>
          </q-card-section>

          <q-list
            v-else
            separator
          >
            <q-item
              v-for="row in stuckByInstitution"
              :key="row.institutionId"
            >
              <q-item-section>
                {{ institutionName(row.institutionId) }}
              </q-item-section>
              <q-item-section side>
                <div class="text-right">
                  <div class="text-weight-medium">
                    {{ row.count }}
                  </div>
                  <div class="text-caption text-grey-7">
                    {{ row.oldestDays === null ? 'süre bilinmiyor' : `en eski ${row.oldestDays} gün` }}
                  </div>
                </div>
              </q-item-section>
            </q-item>
          </q-list>
        </q-card>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useAuthStore } from 'stores/auth'
import { useInstitutionStore } from 'stores/institution'
import { useNotify } from 'src/composables/useNotify'
import { Permissions } from 'utils/permissions'
import { isActingAsProvince } from 'utils/directorateContext'
import { institutionApi, type InstitutionDto } from 'src/api/institution'
import { internshipApi } from 'src/api/internship'
import { useDirectorateDashboard } from 'src/composables/useDirectorateDashboard'
import StatCard from 'components/StatCard.vue'

/** Kartta gösterilecek okul adı sayısı — geri kalanı "ve N okul daha" olarak özetlenir. */
const NAME_PREVIEW_COUNT = 5

const authStore = useAuthStore()
const institutionStore = useInstitutionStore()
const notify = useNotify()

const canOverride = authStore.hasPermission(Permissions.Internship.ApprovalOverride)

/**
 * Kurum adları Internship modülünde YOKTUR (şema izolasyonu) — sunucu institutionName alanını
 * her zaman null döndürür. Ad burada çözülür; depo deseni ContractListPage zenginleştirmesiyle
 * aynıdır.
 *
 * YALNIZ stuckByInstitution'daki kimlikler için çözülür (bkz. resolveInstitutionNames), TÜM
 * okul listesinden DEĞİL — `PagedQuery.SafePageSize` 100'de kilitlidir; tüm okulları tek
 * sayfada çekmeye çalışmak (eski `pageSize: 200`) 100'den fazla okullu bir ilde sessizce
 * eksik kalır ve kart, tam da öne çıkarmak için var olduğu satırlarda "Bilinmeyen kurum"
 * gösterirdi.
 */
const institutionNames = ref<Map<string, string>>(new Map())

function institutionName(id: string): string {
  return institutionNames.value.get(id) ?? 'Bilinmeyen kurum'
}

/**
 * Tıkanmış onay kartındaki kurum adlarını YALNIZ ihtiyaç duyulan ayrık kimlikler için çözer —
 * bu küme normalde küçüktür (kaç kurumda tıkanmış zincir varsa). Tek bir kimliğin çözümü
 * başarısız olursa yalnız O kimlik 'Bilinmeyen kurum' kalır (institutionName'in varsayılan
 * fallback'i); bir başarısız arama diğerlerini boşaltmaz.
 */
async function resolveInstitutionNames(institutionIds: string[]): Promise<void> {
  const distinctIds = [...new Set(institutionIds)]
  const resolved = await Promise.all(
    distinctIds.map(async (id) => {
      try {
        const { data } = await institutionApi.get(id)
        return [id, data.fullName] as const
      } catch {
        return null
      }
    }),
  )

  const map = new Map(institutionNames.value)
  for (const entry of resolved) {
    if (entry) map.set(entry[0], entry[1])
  }
  institutionNames.value = map
}

const {
  districtCount,
  schoolCount,
  unmanagedCount,
  unmanagedNames,
  unmanagedFailed,
  stuckCount,
  stuckThresholdDays,
  stuckByInstitution,
  stuckFailed,
  loading,
  load,
} = useDirectorateDashboard({
  fetchDistrictCount: async () => {
    const { data } = await institutionApi.list({ nodeType: 'District', page: 1, pageSize: 1 })
    return data.totalCount
  },
  fetchSchoolCount: async () => {
    // Yalnız toplam sayı gerekir — adlar artık burada değil, resolveInstitutionNames'te çözülür.
    const { data } = await institutionApi.list({ nodeType: 'School', page: 1, pageSize: 1 })
    return data.totalCount
  },
  fetchUnmanaged: async () => {
    const { data } = await institutionApi.listUnmanaged({
      page: 1,
      pageSize: NAME_PREVIEW_COUNT,
    })
    return {
      total: data.totalCount,
      names: data.items.map((i: InstitutionDto) => i.fullName),
    }
  },
  fetchStuck: async () => {
    // Müdahale yetkisi yoksa uç 403 döner; boş özetle geç, kart zaten gizli.
    if (!canOverride) return { totalCount: 0, thresholdDays: 14, byInstitution: [] }
    const { data } = await internshipApi.getStuckApprovals()
    return data
  },
  notify,
})

onMounted(() => {
  load()
    .then(() => resolveInstitutionNames(stuckByInstitution.value.map((row) => row.institutionId)))
    .catch(() => {})
})
</script>
