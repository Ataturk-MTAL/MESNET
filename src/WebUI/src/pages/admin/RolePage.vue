<template>
  <q-page padding>
    <div class="text-h5 text-weight-bold">
      Rol Yetkileri
    </div>
    <div class="text-caption text-grey-7 q-mb-lg">
      Her rolün sistemde neleri yapabildiğini gösteren salt-bilgi ekranıdır. Kullanıcılara rol
      atamak için <strong>Kullanıcılar</strong> sayfasını kullanın.
    </div>

    <div
      v-if="loading"
      class="flex flex-center q-pa-xl"
    >
      <q-spinner-gears
        size="48px"
        color="primary"
      />
    </div>

    <div
      v-else
      class="row q-col-gutter-md"
    >
      <div
        v-for="role in roles"
        :key="role.roleName"
        class="col-12 col-md-6 col-lg-4"
      >
        <q-card
          flat
          bordered
          class="full-height"
        >
          <q-card-section>
            <div class="row items-center q-mb-xs no-wrap">
              <q-icon
                :name="roleMeta(role.roleName).icon"
                color="primary"
                size="24px"
                class="q-mr-sm"
              />
              <div class="text-subtitle1 text-weight-bold">
                {{ roleMeta(role.roleName).name }}
              </div>
            </div>
            <div class="text-caption text-grey-7 q-mb-md">
              {{ roleMeta(role.roleName).desc }}
            </div>

            <div class="text-caption text-weight-medium text-grey-8 q-mb-xs">
              Yapabilecekleri
            </div>
            <q-chip
              v-for="perm in role.permissions"
              :key="perm"
              dense
              color="neutral-soft"
              text-color="neutral-strong"
              class="q-ma-xs"
              size="sm"
            >
              {{ permissionLabel(perm) }}
            </q-chip>
            <div
              v-if="role.permissions.length === 0"
              class="text-caption text-grey"
            >
              Bu rol için tanımlı yetki yok.
            </div>
          </q-card-section>
        </q-card>
      </div>
    </div>
  </q-page>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { securityApi, type RolePermissionsDto } from 'src/api/security'
import { useNotify } from 'src/composables/useNotify'

const notify = useNotify()
const loading = ref(false)
const roles = ref<RolePermissionsDto[]>([])

// ── Rol adı → Türkçe ad + açıklama + ikon ──
const ROLE_META: Record<string, { name: string; desc: string; icon: string }> = {
  InstitutionManager: { name: 'Kurum Müdürü', desc: 'Kurumdaki tüm süreçleri ve kullanıcıları yönetir.', icon: 'account_balance' },
  InstitutionStaff: { name: 'Kurum Personeli', desc: 'Staj, sözleşme, devamsızlık ve maaş işlemlerini yürütür.', icon: 'badge' },
  Teacher: { name: 'Öğretmen / Koordinatör', desc: 'Öğrencileri ve staj sürecini takip eder, onaylar.', icon: 'school' },
  Student: { name: 'Öğrenci', desc: 'Kendi staj ve devamsızlık bilgilerini görüntüler.', icon: 'person' },
  DepartmentHead: { name: 'Alan Şefi', desc: 'Alanındaki koordinasyon, program ve onayları yürütür.', icon: 'supervisor_account' },
  CompanyManager: { name: 'İşletme Yöneticisi', desc: 'İşletmedeki stajyerleri ve süreçleri yönetir.', icon: 'business' },
}

function roleMeta(roleName: string): { name: string; desc: string; icon: string } {
  return ROLE_META[roleName] ?? { name: roleName, desc: '', icon: 'shield' }
}

// ── Yetki kodu (resource:action) → okunabilir Türkçe ──
const RESOURCE_LABELS: Record<string, string> = {
  institution: 'Kurum', student: 'Öğrenci', internship: 'Staj', contract: 'Sözleşme',
  attendance: 'Devamsızlık', salary: 'Maaş/Dekont', payment: 'Ödeme', document: 'Belge',
  communication: 'İletişim', company: 'İşletme', coordinator: 'Koordinasyon', department: 'Alan',
  user: 'Kullanıcı', report: 'Rapor', visit: 'Ziyaret', schedule: 'Ders programı',
  receipt: 'Dekont', trainer: 'Usta öğretici', parameter: 'Parametre', issue: 'Sorun', evaluation: 'Değerlendirme',
}
const ACTION_LABELS: Record<string, string> = {
  view: 'görüntüleme', manage: 'yönetme', approve: 'onaylama', reject: 'reddetme',
  report: 'raporlama', upload: 'yükleme', send: 'gönderme', review: 'inceleme',
  apply: 'başvurma', create: 'oluşturma', delete: 'silme', update: 'güncelleme',
  track: 'takip', request: 'talep', communication: 'iletişim',
  'view-own': 'kendi kaydını görüntüleme', 'update-own': 'kendi kaydını güncelleme',
}

function labelFor(map: Record<string, string>, key: string): string {
  return map[key] ?? key
}

function permissionLabel(code: string): string {
  const parts = code.split(':')
  const resource = labelFor(RESOURCE_LABELS, parts[0] ?? '')
  if (parts.length === 1) return resource

  const last = parts[parts.length - 1] ?? ''
  if (last === '*') return `${resource} — tüm yetkiler`

  const action = labelFor(ACTION_LABELS, last)
  if (parts.length === 2) return `${resource} ${action}`

  // 3+ parça: ortadaki alt-kaynak (ör. coordinator:schedule:manage → "Koordinasyon — Ders programı yönetme")
  const sub = labelFor(RESOURCE_LABELS, parts[1] ?? '').toLocaleLowerCase('tr-TR')
  return `${resource} — ${sub} ${action}`
}

async function load() {
  loading.value = true
  try {
    const res = await securityApi.listRoles()
    roles.value = res.data ?? []
  } catch (e) {
    notify.apiError(e, 'Roller yüklenirken bir hata oluştu.')
  } finally {
    loading.value = false
  }
}

onMounted(load)
</script>
