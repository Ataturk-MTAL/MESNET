<template>
  <q-page padding>
    <PageHeader
      title="Rol Yetkileri"
      subtitle="Her rolün sistemde neleri yapabildiğini gösteren salt-bilgi ekranıdır. Kullanıcılara rol atamak için Kullanıcılar sayfasını kullanın."
    />

    <!--
      Hata durumu bağlanmak ZORUNDADIR: rol kataloğu store'u hatada da `loading`i kapatır ve
      `roles` boş dizide kalır. Yalnız `empty` bağlanırsa başarısız yükleme, sunucuda roller
      dururken "Tanımlı rol bulunamadı" diye YANLIŞ bir iddiaya dönüşür.
      DataState sırası: loading → error → empty, yani hata boş durumu ezer.
    -->
    <DataState
      :loading="loading"
      :error="loadError"
      retryable
      error-text="Roller yüklenemedi"
      :empty="!loading && roles.length === 0"
      gears
      spinner-size="48px"
      padding="q-pa-xl"
      empty-icon="badge"
      empty-text="Tanımlı rol bulunamadı"
      @retry="load"
    >
      <div class="row q-col-gutter-md">
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
                class="text-caption text-grey-7"
              >
                Bu rol için tanımlı yetki yok.
              </div>
            </q-card-section>
          </q-card>
        </div>
      </div>
    </DataState>

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
          <!--
            Üç ayrı hâl, üç ayrı mesaj (#283). "Taranmadı" ile "temiz" asla aynı görünmemeli:
            yetkisiz bacak boş döner ve boş liste sessizce "sorun yok" diye okunurdu.
          -->
          <AppNotice
            v-if="!integrity.realmScanPermitted"
            type="info"
            class="q-mb-md"
            message="Realm taraması (hiç rolü olmayan hesaplar) kurum üstü yetki ister ve yapılmadı. Aşağıdaki sonuç yalnız kendi kurumunuzun davet ve hesap kayıtlarını kapsar."
          />
          <AppNotice
            v-else-if="!integrity.keycloakChecked"
            type="warning"
            class="q-mb-md"
            :message="`Kimlik sunucusu taraması yapılamadı — sonuç eksiktir. ${integrity.keycloakCheckError ?? ''}`"
          />

          <AppNotice
            v-if="integrity.totalFindings === 0 && integrity.keycloakChecked"
            type="success"
            message="Bozuk rol kaydı bulunamadı."
          />
          <AppNotice
            v-else-if="integrity.totalFindings === 0 && !integrity.realmScanPermitted"
            type="success"
            class="q-mb-md"
            message="Kurumunuzun kayıtlarında bozuk rol bulunamadı."
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
                    Rol: <strong>{{ inv.targetRole }}</strong> · Durum:
                    <StatusBadge :slug="invitationStatusLabel(inv.status)" />
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
import DataState from 'components/DataState.vue'
import PageHeader from 'components/PageHeader.vue'
import PermissionGuard from 'components/PermissionGuard.vue'
import StatusBadge from 'components/StatusBadge.vue'

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

// Yükleme hatası ayrı tutulur. Store hata hâlinde `finally` ile `loading`i kapatır ve
// `roles` boş dizide kalır; bu bayrak olmadan hata ile "hiç rol yok" ayırt edilemez.
const loadError = ref(false)

async function load() {
  loadError.value = false
  try {
    await roleCatalog.load()
  } catch (e) {
    loadError.value = true
    notify.apiError(e, 'Roller yüklenirken bir hata oluştu.')
  }
}

// Davet durumu backend'den SmartEnum'un İngilizce Name'i olarak gelir (StatusName).
// Kullanıcıya Türkçe slug gösterilir; slug'lar StatusBadge'in ton haritasında zaten tanımlı.
// Tanınmayan değer HAM basılır — bozuk kaydın görünür kalması doğrudur, gizlenmesi değil.
const INVITATION_STATUS_LABELS: Record<string, string> = {
  PendingApproval: 'Onay Bekliyor',
  Approved: 'Onaylandı',
  Rejected: 'Reddedildi',
  Completed: 'Tamamlandı',
  Expired: 'Süresi Doldu',
}

function invitationStatusLabel(status: string): string {
  return INVITATION_STATUS_LABELS[status] ?? status
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
