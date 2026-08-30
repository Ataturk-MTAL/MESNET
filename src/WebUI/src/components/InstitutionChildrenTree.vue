<template>
  <DataState
    :loading="loading"
    skeleton
    :skeleton-lines="4"
    padding="q-pa-md"
    :error="error"
    error-text="Alt kurumlar yüklenirken bir hata oluştu."
    retryable
    @retry="reload"
  >
    <DataState
      :empty="children.length === 0"
      padding="q-pa-xl"
    >
      <template #empty>
        <q-icon
          name="account_tree"
          size="48px"
          class="q-mb-sm"
        />
        <div>Bu müdürlüğe bağlı kurum bulunamadı.</div>
      </template>

      <q-list
        bordered
        separator
      >
        <!-- İl müdürlüğü: ilçeler açılabilir, her ilçe kendi okullarını gösterir. -->
        <template v-if="isProvince">
          <q-expansion-item
            v-for="district in children"
            :key="district.id"
            dense-toggle
            :model-value="!!expandedIds[district.id]"
            @update:model-value="toggleDistrict(district.id)"
          >
            <template #header>
              <q-item-section avatar>
                <q-icon name="account_balance" />
              </q-item-section>
              <q-item-section>
                <q-item-label>{{ district.fullName }}</q-item-label>
              </q-item-section>
              <q-item-section
                side
                @click.stop
              >
                <q-btn
                  flat
                  round
                  dense
                  icon="visibility"
                  aria-label="Kurum bilgilerini görüntüle"
                  @click="viewInstitution(district.id)"
                >
                  <q-tooltip>Kurum Bilgilerini Görüntüle</q-tooltip>
                </q-btn>
              </q-item-section>
            </template>

            <div
              v-if="districtSchoolsLoading[district.id]"
              class="q-pa-sm"
            >
              <q-skeleton
                type="text"
                height="24px"
                class="q-mb-xs"
              />
              <q-skeleton
                type="text"
                height="24px"
                width="70%"
              />
            </div>
            <div
              v-else-if="(districtSchools[district.id] ?? []).length === 0"
              class="text-caption text-grey-7 q-pa-md"
            >
              Bu ilçeye bağlı okul bulunamadı.
            </div>
            <q-list
              v-else
              separator
              class="q-ml-md"
            >
              <InstitutionSchoolRow
                v-for="school in districtSchools[district.id]"
                :key="school.id"
                :school="school"
                :switching="switching"
                @switch="onSwitch"
                @view="viewInstitution"
              />
            </q-list>
          </q-expansion-item>
        </template>

        <!-- İlçe müdürlüğü: doğrudan okullar. -->
        <template v-else>
          <InstitutionSchoolRow
            v-for="school in children"
            :key="school.id"
            :school="school"
            :switching="switching"
            @switch="onSwitch"
            @view="viewInstitution"
          />
        </template>
      </q-list>
    </DataState>
  </DataState>
</template>

<script setup lang="ts">
import { computed, toRef, watch } from 'vue'
import { useRouter } from 'vue-router'
import DataState from 'components/DataState.vue'
import InstitutionSchoolRow from 'components/InstitutionSchoolRow.vue'
import { useInstitutionChildren } from 'src/composables/useInstitutionChildren'
import { useInstitutionSwitch } from 'src/composables/useInstitutionSwitch'
import { useNotify } from 'src/composables/useNotify'

const props = defineProps<{
  institutionId: string
  /** `Province` veya `District` — School çağrılmaz (o düğümün çocuğu yok). */
  nodeType: string
}>()

const router = useRouter()
const notify = useNotify()

const institutionIdRef = toRef(props, 'institutionId')
const nodeTypeRef = toRef(props, 'nodeType')

const {
  loading,
  error,
  lastError,
  children,
  expandedIds,
  districtSchools,
  districtSchoolsLoading,
  load,
  toggleDistrict,
} = useInstitutionChildren(institutionIdRef, nodeTypeRef)

const { switching, switchToInstitution } = useInstitutionSwitch()

const isProvince = computed(() => props.nodeType === 'Province')

async function reload() {
  await load()
  if (error.value) {
    notify.apiError(lastError.value, 'Alt kurumlar yüklenirken bir hata oluştu.')
  }
}

function viewInstitution(id: string) {
  router.push(`/institutions/${id}`).catch(() => {})
}

function onSwitch(id: string, name: string) {
  switchToInstitution(id, name).catch(() => {})
}

// institutionId/nodeType değişince (aynı bileşen örneği farklı bir düğüme yönlendirilirse)
// ağaç yeniden yüklenir — bayat çocuk listesi kalmaz.
watch([institutionIdRef, nodeTypeRef], () => {
  reload().catch(() => {})
}, { immediate: true })
</script>
