<template>
  <q-page padding>
    <div class="row items-center q-mb-md">
      <q-btn flat round dense icon="arrow_back" aria-label="Geri" :to="{ name: 'RoleManagement' }" class="q-mr-sm">
        <q-tooltip>Rollere dön</q-tooltip>
      </q-btn>
      <div class="text-h5 col">Atanabilir Yetki Kapsamı</div>
      <q-btn unelevated color="primary" icon="save" label="Kaydet" :loading="saving" @click="save" />
    </div>

    <AppNotice
      type="info"
      class="q-mb-md"
      message="Her role, bireysel (direct) olarak hangi yetki ALANLARININ atanabileceğini belirler. Örn. İşletme rolüne yalnız işletme/devamsızlık/iletişim alanları atanabilsin; kurum-yönetimi atanamasın. '*' = tüm yetkiler."
    />

    <q-card v-if="data" flat bordered>
      <q-list separator>
        <q-item v-for="role in data.roles" :key="role">
          <q-item-section style="max-width: 240px">
            <q-item-label class="text-weight-medium">{{ roleLabel(role) }}</q-item-label>
            <q-item-label caption>{{ role }}</q-item-label>
          </q-item-section>
          <q-item-section>
            <q-select
              v-model="model[role]"
              :options="domainOptions"
              multiple
              use-chips
              outlined
              dense
              emit-value
              map-options
              label="Atanabilir alanlar"
            />
          </q-item-section>
        </q-item>
      </q-list>
    </q-card>
  </q-page>
</template>

<script setup lang="ts">
import { ref, reactive, computed, onMounted } from 'vue'
import { securityApi, type PermissionScopeData } from 'src/api/security'
import { useNotify } from 'src/composables/useNotify'
import AppNotice from 'components/AppNotice.vue'

const notify = useNotify()
const data = ref<PermissionScopeData | null>(null)
const model = reactive<Record<string, string[]>>({})
const saving = ref(false)

const ROLE_LABELS: Record<string, string> = {
  InstitutionManager: 'Müdür / Müdür Yardımcısı',
  InstitutionStaff: 'Kurum Personeli',
  Teacher: 'Koordinatör Öğretmen',
  DepartmentHead: 'Alan Şefi',
  CompanyManager: 'İşletme Yetkilisi',
  Student: 'Öğrenci',
}
const DOMAIN_LABELS: Record<string, string> = {
  '*': 'Tüm Yetkiler (*)',
  'institution:': 'Kurum Yönetimi',
  'company:': 'İşletme',
  'student:': 'Öğrenci',
  'internship:': 'Staj',
  'attendance:': 'Devamsızlık',
  'salary:': 'Maaş / Dekont',
  'document:': 'Belge',
  'communication:': 'İletişim',
  'coordinator:': 'Koordinasyon',
  'department:': 'Alan / Bölüm',
  'user:': 'Kullanıcı Yönetimi',
}
function roleLabel(r: string) {
  return ROLE_LABELS[r] ?? r
}
function domainLabel(d: string) {
  return DOMAIN_LABELS[d] ?? d
}

const domainOptions = computed(() =>
  (data.value?.allDomains ?? []).map((d) => ({ label: domainLabel(d), value: d })),
)

async function load() {
  try {
    const res = await securityApi.getPermissionScopes()
    data.value = res.data
    for (const role of res.data.roles) {
      model[role] = [...(res.data.allowedDomainsByRole[role] ?? [])]
    }
  } catch (e) {
    notify.apiError(e, 'Yetki kapsamları yüklenemedi.')
  }
}

async function save() {
  saving.value = true
  try {
    await securityApi.updatePermissionScopes({ allowedDomainsByRole: { ...model } })
    notify.success('Yetki kapsamları güncellendi.')
  } catch (e) {
    notify.apiError(e, 'Kaydedilemedi.')
  } finally {
    saving.value = false
  }
}

onMounted(load)
</script>
