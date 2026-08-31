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
        v-if="districtCount > 0"
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

          <q-card-section v-if="unmanagedCount === 0">
            <div class="row items-center text-grey-7">
              <q-icon
                name="verified"
                size="sm"
                class="q-mr-sm"
              />
              <span>Tüm okulların yöneticisi var.</span>
            </div>
          </q-card-section>

          <q-list
            v-else
            separator
          >
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

          <q-card-section v-if="stuckCount === 0">
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
import { useNotify } from 'src/composables/useNotify'
import { Permissions } from 'utils/permissions'
import { institutionApi, type InstitutionDto } from 'src/api/institution'
import { internshipApi } from 'src/api/internship'
import { useDirectorateDashboard } from 'src/composables/useDirectorateDashboard'
import StatCard from 'components/StatCard.vue'

/** Kartta gösterilecek okul adı sayısı — geri kalanı "ve N okul daha" olarak özetlenir. */
const NAME_PREVIEW_COUNT = 5

const authStore = useAuthStore()
const notify = useNotify()

const canOverride = authStore.hasPermission(Permissions.Internship.ApprovalOverride)

/**
 * Kurum adları Internship modülünde YOKTUR (şema izolasyonu) — sunucu institutionName alanını
 * her zaman null döndürür. Ad burada, alt ağaç listesinden kurulan lookup map ile çözülür;
 * depo deseni ContractListPage zenginleştirmesiyle aynıdır.
 */
const institutionNames = ref<Map<string, string>>(new Map())

function institutionName(id: string): string {
  return institutionNames.value.get(id) ?? 'Bilinmeyen kurum'
}

const {
  districtCount,
  schoolCount,
  unmanagedCount,
  unmanagedNames,
  stuckCount,
  stuckThresholdDays,
  stuckByInstitution,
  loading,
  load,
} = useDirectorateDashboard({
  fetchDistrictCount: async () => {
    const { data } = await institutionApi.list({ nodeType: 'District', page: 1, pageSize: 1 })
    return data.totalCount
  },
  fetchSchoolCount: async () => {
    // pageSize okul adı lookup'ını da besler: sayı ve adlar tek çağrıdan gelir.
    const { data } = await institutionApi.list({ nodeType: 'School', page: 1, pageSize: 200 })
    institutionNames.value = new Map(
      data.items.map((i: InstitutionDto) => [i.id, i.fullName]),
    )
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
  load().catch(() => {})
})
</script>
