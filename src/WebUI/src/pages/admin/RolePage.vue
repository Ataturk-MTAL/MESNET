<template>
  <q-page padding>
    <h1 class="text-h5 text-weight-bold q-my-none">
      Rol Yetkileri
    </h1>
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
                :name="roleIcon(role.roleName)"
                color="primary"
                size="24px"
                class="q-mr-sm"
              />
              <div class="text-subtitle1 text-weight-bold">
                {{ role.label }}
              </div>
            </div>
            <div class="text-caption text-grey-7 q-mb-md">
              {{ role.description }}
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

    <!--
      Rol modeli tutarlılık taraması (#129) — yalnız TESPİT.
      Otomatik düzeltme bilinçli olarak yoktur: kimin müdür yardımcısı kimin personel olduğu
      okulun bilgisidir. Liste idareye gösterilir, düzeltmeyi idare yapar.
    -->
    <PermissionGuard :permission="Permissions.UserManagement.RolesManage">
      <q-card
        flat
        bordered
        class="q-mt-lg"
      >
        <q-card-section class="row items-center q-gutter-sm">
          <q-icon
            name="fact_check"
            color="primary"
            size="24px"
          />
          <div class="col">
            <div class="text-subtitle1 text-weight-bold">
              Rol Kaydı Tutarlılık Taraması
            </div>
            <div class="text-caption text-grey-7">
              Sistemde tanımlı olmayan rol adı taşıyan kayıtları ve hiç realm rolü olmayan
              hesapları listeler. Düzeltme <strong>önerilir, otomatik uygulanmaz</strong>.
            </div>
          </div>
          <q-btn
            outline
            color="primary"
            icon="search"
            label="Tara"
            :loading="integrityLoading"
            @click="runIntegrityScan"
          />
        </q-card-section>

        <q-separator v-if="integrity" />

        <q-card-section v-if="integrity">
          <AppNotice
            v-if="!integrity.keycloakChecked"
            type="warning"
            class="q-mb-md"
            :message="`Kimlik sunucusu taraması yapılamadı — sonuç eksiktir. ${integrity.keycloakCheckError ?? ''}`"
          />
          <AppNotice
            v-else-if="integrity.totalFindings === 0"
            type="success"
            message="Bozuk rol kaydı bulunamadı."
          />

          <div
            v-if="integrity.invitationsWithUnknownRole.length"
            class="q-mb-md"
          >
            <div class="text-subtitle2 q-mb-xs">
              Tanınmayan rolle oluşturulmuş davetler
            </div>
            <q-list
              dense
              separator
            >
              <q-item
                v-for="inv in integrity.invitationsWithUnknownRole"
                :key="inv.invitationId"
              >
                <q-item-section>
                  <q-item-label>{{ inv.fullName }} — {{ inv.email }}</q-item-label>
                  <q-item-label caption>
                    Rol: <strong>{{ inv.targetRole }}</strong> · Durum: {{ inv.status }}
                    <template v-if="inv.suggestedRole">
                      · Öneri: {{ roleCatalog.labelFor(inv.suggestedRole) }}
                    </template>
                  </q-item-label>
                </q-item-section>
              </q-item>
            </q-list>
          </div>

          <div
            v-if="integrity.accountsWithUnknownRole.length"
            class="q-mb-md"
          >
            <div class="text-subtitle2 q-mb-xs">
              Tanınmayan rol taşıyan kullanıcılar
            </div>
            <q-list
              dense
              separator
            >
              <q-item
                v-for="acc in integrity.accountsWithUnknownRole"
                :key="acc.userAccountId"
              >
                <q-item-section>
                  <q-item-label>{{ acc.fullName }} ({{ acc.username }})</q-item-label>
                  <q-item-label caption>
                    Tanınmayan: <strong>{{ acc.unknownRoles.join(', ') }}</strong>
                    <template v-if="acc.suggestedRoles.length">
                      · Öneri: {{ acc.suggestedRoles.map(roleCatalog.labelFor).join(', ') }}
                    </template>
                  </q-item-label>
                </q-item-section>
              </q-item>
            </q-list>
          </div>

          <div v-if="integrity.accountsWithoutRealmRole.length">
            <div class="text-subtitle2 q-mb-xs">
              Hiç realm rolü olmayan hesaplar
            </div>
            <q-list
              dense
              separator
            >
              <q-item
                v-for="acc in integrity.accountsWithoutRealmRole"
                :key="acc.keycloakUserId"
              >
                <q-item-section>
                  <q-item-label>{{ acc.username }}</q-item-label>
                  <q-item-label caption>
                    {{ acc.email }} — bu hesap giriş yapabilir ama hiçbir şey göremez.
                  </q-item-label>
                </q-item-section>
              </q-item>
            </q-list>
          </div>
        </q-card-section>
      </q-card>
    </PermissionGuard>
  </q-page>
</template>

<script setup lang="ts">
import { computed, ref, onMounted } from 'vue'
import { securityApi, type RoleIntegrityReport } from 'src/api/security'
import { useNotify } from 'src/composables/useNotify'
import { useRoleCatalogStore } from 'stores/roleCatalog'
import { Permissions } from 'utils/permissions'
import AppNotice from 'components/AppNotice.vue'
import PermissionGuard from 'components/PermissionGuard.vue'

const notify = useNotify()
// Rol listesi + Türkçe etiketler tek kaynaktan: GET /api/security/roles (#129)
const roleCatalog = useRoleCatalogStore()
const roles = computed(() => roleCatalog.roles)
const loading = computed(() => roleCatalog.loading)

// Rol adı ve açıklaması API'den gelir (#129) — burada YALNIZ ikon eşlemesi kalır; ikon saf
// görsel bir tercihtir, yetki modelinin parçası değildir. Bilinmeyen rol için nötr ikon.
const ROLE_ICONS: Record<string, string> = {
  InstitutionManager: 'account_balance',
  DeputyDirector: 'assignment_ind',
  InstitutionStaff: 'badge',
  DepartmentHead: 'supervisor_account',
  Teacher: 'school',
  CompanyManager: 'business',
  MasterTrainer: 'engineering',
  Student: 'person',
}

function roleIcon(roleName: string): string {
  return ROLE_ICONS[roleName] ?? 'shield'
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
  try {
    await roleCatalog.load()
  } catch (e) {
    notify.apiError(e, 'Roller yüklenirken bir hata oluştu.')
  }
}

// ── Tutarlılık taraması (#129) — istek üzerine çalışır, sayfa açılışında değil ──
// Tarama Keycloak kullanıcı listesini de çeker; her sayfa açılışında koşturmak gereksiz yüktür.
const integrity = ref<RoleIntegrityReport | null>(null)
const integrityLoading = ref(false)

async function runIntegrityScan() {
  integrityLoading.value = true
  try {
    const { data } = await securityApi.getRoleIntegrity()
    integrity.value = data
  } catch (e) {
    notify.apiError(e, 'Tutarlılık taraması yapılamadı.')
  } finally {
    integrityLoading.value = false
  }
}

onMounted(load)
</script>
