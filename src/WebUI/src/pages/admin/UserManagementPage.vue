<template>
  <q-page padding>
    <PageHeader title="Kullanıcı Yönetimi" />

    <q-tabs
      v-model="tab"
      align="left"
      class="q-mb-md"
    >
      <q-tab
        name="users"
        label="Kullanıcılar"
        icon="group"
      />
      <q-tab
        v-if="authStore.hasPermission(Permissions.UserManagement.Approve)"
        name="invitations"
        label="Davetler"
        icon="mail"
      />
    </q-tabs>

    <q-tab-panels
      v-model="tab"
      animated
    >
      <!-- KULLANICILAR -->
      <q-tab-panel name="users">
        <div class="row items-center q-mb-md">
          <div class="col text-subtitle1 text-weight-medium">
            Kullanıcılar
          </div>
          <div class="col-auto q-gutter-sm">
            <PermissionGuard :permission="Permissions.UserManagement.Create">
              <q-btn
                outline
                color="primary"
                icon="sync"
                label="Senkronize Et"
                :loading="syncing"
                @click="syncUsers"
              >
                <q-tooltip>Keycloak'taki kullanıcıları içe aktar / güncelle</q-tooltip>
              </q-btn>
              <q-btn
                unelevated
                color="primary"
                icon="person_add"
                label="Davet Gönder"
                @click="inviteDialog = true"
              />
            </PermissionGuard>
          </div>
        </div>

        <!--
          Rol değişimiyle alan şefi yapılıp branşsız kalan kullanıcıları idarenin görmesi
          için filtre (#126). Muafiyeti olan yöneticiler bu listeye asla girmez.
        -->
        <q-toggle
          v-model="missingBranchOnly"
          label="Yalnız branş atanmamış kullanıcılar"
          class="q-mb-sm"
          @update:model-value="loadUsers().catch(() => {})"
        />

        <AppTable
          :rows="users"
          :columns="userColumns"
          :loading="usersLoading"
          :pagination="usersPagination"
          show-search
          :search="usersSearch"
          @request="onUsersRequest"
          @search="onUsersSearch"
        >
          <template #body-cell-isEnabled="{ row }">
            <q-td>
              <StatusBadge :slug="row.isEnabled ? 'Aktif' : 'Pasif'" />
            </q-td>
          </template>
          <!-- Rol çipleri Türkçe etiketle basılır (#129); etiket rol kataloğundan gelir. -->
          <template #body-cell-roles="{ row }">
            <q-td>
              <q-badge
                v-for="role in row.roles.slice(0, 2)"
                :key="role"
                color="neutral"
                :label="roleCatalog.labelFor(role)"
                class="q-mr-xs"
              />
              <span
                v-if="row.roles.length > 2"
                class="text-caption text-grey-7"
              >+{{ row.roles.length - 2 }}</span>
            </q-td>
          </template>
          <!--
            Alan (branş) sütunu (#126). Boş liste yöneticide NORMALDİR — uyarı gösterilmez.
            Yalnız alan beklenip girilmemişse (branchMissing) uyarı rozeti çıkar.
          -->
          <template #body-cell-branchCodes="{ row }">
            <q-td>
              <q-badge
                v-if="resolveBranchCellState(row) === 'missing'"
                color="warning"
                text-color="dark"
                label="Branş atanmamış"
              >
                <q-tooltip>
                  Bu kullanıcı alan bazlı koordinasyon verisine yazabilmek için en az bir alana
                  bağlı olmalıdır. Alan girilene kadar hiçbir alana yazamaz.
                </q-tooltip>
              </q-badge>
              <template v-else-if="resolveBranchCellState(row) === 'assigned'">
                <q-badge
                  v-for="code in row.branchCodes"
                  :key="code"
                  color="neutral"
                  :label="code"
                  class="q-mr-xs"
                />
              </template>
              <span
                v-else
                class="text-caption text-grey-7"
              >—</span>
            </q-td>
          </template>
          <template #body-cell-userActions="{ row }">
            <q-td class="text-right">
              <PermissionGuard :permission="Permissions.UserManagement.Update">
                <q-btn
                  flat
                  round
                  dense
                  icon="edit"
                  aria-label="Düzenle"
                  @click="openEditUser(row)"
                >
                  <q-tooltip>Kullanıcıyı düzenle</q-tooltip>
                </q-btn>
              </PermissionGuard>
              <PermissionGuard :permission="Permissions.UserManagement.RolesManage">
                <q-btn
                  flat
                  round
                  dense
                  icon="manage_accounts"
                  aria-label="Rolleri yönet"
                  @click="openRoles(row)"
                >
                  <q-tooltip>Rolleri yönet</q-tooltip>
                </q-btn>
                <q-btn
                  flat
                  round
                  dense
                  icon="school"
                  :color="row.branchMissing ? 'warning' : undefined"
                  aria-label="Alanları yönet"
                  @click="openBranches(row)"
                >
                  <q-tooltip>Alanları (branş) yönet</q-tooltip>
                </q-btn>
                <!-- Veli–öğrenci bağı (#174): velinin KAPSAMI. İzinleri tüm velilerde
                     aynıdır; erişeceği veriyi yalnız bu bağ belirler. -->
                <q-btn
                  flat
                  round
                  dense
                  icon="family_restroom"
                  aria-label="Bağlı öğrencileri yönet"
                  @click="openStudents(row)"
                >
                  <q-tooltip>Bağlı öğrencileri yönet (veli)</q-tooltip>
                </q-btn>
              </PermissionGuard>
              <!--
                Kurum (kiracı) bağı — ADR-0003 adım 2. Token'dan gelen institution_id artık
                okunmuyor ve sync de yazmıyor; bağ yalnız buradan kurulur.

                KENDİ GUARD'I VAR (RolesManage'e gömülü DEĞİL): il/ilçe
                müdürlüğü rolleri (`ProvincialAdmin`, `DistrictAdmin`) `user:roles:manage`
                TAŞIMAZ — bilerek, o izin alt ağaçtaki her okulun her kullanıcısının rollerini
                değiştirmek demektir. Onlar `directorate:institution-bootstrap` taşır ve
                sunucu bu ucu ikisinden HERHANGİ BİRİYLE açar (`AnyOf`,
                `UserInstitutionAssignOrBootstrap`). Guard tek `RolesManage` olsaydı, sunucunun
                kabul ettiği aktörden düğme tümüyle saklanırdı — düğme dururdu ama sahibi onu
                hiç görmezdi.
              -->
              <PermissionGuard
                :permission="[Permissions.UserManagement.RolesManage, Permissions.Directorate.InstitutionBootstrap]"
              >
                <q-btn
                  flat
                  round
                  dense
                  icon="domain"
                  :color="row.institutionId ? undefined : 'warning'"
                  aria-label="Kurum bağını yönet"
                  @click="openInstitution(row)"
                >
                  <q-tooltip>
                    {{ row.institutionId ? 'Kurum bağını yönet' : 'Kurum bağı yok — kapsamlı ekranlar kapalı' }}
                  </q-tooltip>
                </q-btn>
              </PermissionGuard>
              <PermissionGuard :permission="Permissions.UserManagement.Update">
                <q-btn
                  flat
                  round
                  dense
                  :icon="row.isEnabled ? 'person_off' : 'person'"
                  :color="row.isEnabled ? 'negative' : 'positive'"
                  :aria-label="row.isEnabled ? 'Kullanıcıyı pasifleştir' : 'Kullanıcıyı aktifleştir'"
                  @click="toggleStatus(row)"
                >
                  <q-tooltip>{{ row.isEnabled ? 'Pasifleştir' : 'Aktifleştir' }}</q-tooltip>
                </q-btn>
              </PermissionGuard>
            </q-td>
          </template>
        </AppTable>
      </q-tab-panel>

      <!-- DAVETLER -->
      <q-tab-panel name="invitations">
        <div class="text-subtitle1 text-weight-medium q-mb-md">
          Bekleyen Davetler
        </div>
        <AppTable
          :rows="invitations"
          :columns="invitationColumns"
          :loading="invsLoading"
          :pagination="invsPagination"
          show-search
          :search="invsSearch"
          @request="onInvsRequest"
          @search="onInvsSearch"
        >
          <!--
            Hedef rol Türkçe etiketle gösterilir (#129). Katalogda olmayan bir değer ham
            basılır — bozuk davet kaydının görünür kalması doğrudur, gizlenmesi değil.
          -->
          <template #body-cell-targetRole="{ row }">
            <q-td>{{ roleCatalog.labelFor(row.targetRole) }}</q-td>
          </template>
          <!--
            Durum rozeti tek dilde: StatusBadge. Önceki satır-içi ternary zinciri tanınmayan
            HER değeri sessizce "Süresi Doldu" diye yalanlıyordu; ayrıca `grey` (#9E9E9E)
            zemin beyaz metinle 2,68:1 veriyordu (ölçüldü) — WCAG AA eşiği 4,5:1.
          -->
          <template #body-cell-status="{ row }">
            <q-td>
              <StatusBadge :slug="invitationStatusLabel(row.status)" />
              <!--
                "Sıra sizde" — koşul ve gerekçe için bkz. `isInvitationMyTurn` (script).
                Yüzey olarak DURUM hücresi seçildi: aşama bilgisinin okunduğu yer burasıdır.
                Dolu rozet metin etiketi taşır, yani renk ikincil sinyaldir.
              -->
              <q-badge
                v-if="isInvitationMyTurn(row)"
                class="bg-accent-strong text-body2 q-px-sm q-py-xs q-ml-xs"
                label="Sıra sizde"
              />
            </q-td>
          </template>
          <template #body-cell-invActions="{ row }">
            <q-td class="text-right">
              <PermissionGuard :permission="Permissions.UserManagement.Approve">
                <q-btn
                  v-if="row.status === 'PendingApproval'"
                  flat
                  round
                  dense
                  icon="check"
                  color="positive"
                  aria-label="Daveti onayla"
                  @click="approveInvitation(row)"
                >
                  <q-tooltip>Onayla</q-tooltip>
                </q-btn>
                <q-btn
                  v-if="row.status === 'PendingApproval'"
                  flat
                  round
                  dense
                  icon="close"
                  color="negative"
                  aria-label="Daveti reddet"
                  @click="rejectInvitation(row)"
                >
                  <q-tooltip>Reddet</q-tooltip>
                </q-btn>
              </PermissionGuard>
              <q-btn
                v-if="row.status === 'Approved'"
                flat
                round
                dense
                icon="send"
                color="primary"
                aria-label="Daveti yeniden gönder"
                @click="resendInvitation(row)"
              >
                <q-tooltip>Yeniden gönder</q-tooltip>
              </q-btn>
            </q-td>
          </template>
        </AppTable>
      </q-tab-panel>
    </q-tab-panels>

    <!-- Davet Gönder — sağdan kayan yan-panel (Form Sayfa Kuralı: merkezî modal yoktur) -->
    <FormDialog
      v-model="inviteDialog"
      title="Kullanıcı Davet Et"
      icon="person_add"
      color="primary"
      save-label="Davet Gönder"
      :saving="saving"
      @save="sendInvitation"
    >
      <q-input
        v-model="inviteForm.email"
        label="E-posta"
        outlined
        type="email"
      >
        <template #prepend>
          <q-icon name="email" />
        </template>
      </q-input>
      <q-input
        v-model="inviteForm.firstName"
        label="Ad"
        outlined
      >
        <template #prepend>
          <q-icon name="person" />
        </template>
      </q-input>
      <q-input
        v-model="inviteForm.lastName"
        label="Soyad"
        outlined
      >
        <template #prepend>
          <q-icon name="person" />
        </template>
      </q-input>
      <q-select
        v-model="inviteForm.targetRole"
        :options="roleOptions"
        label="Hedef Rol"
        outlined
        emit-value
        map-options
      >
        <template #prepend>
          <q-icon name="manage_accounts" />
        </template>
      </q-select>
      <!--
        Alan (branş) kayıt sırasında girilir (#126). Zorunlu değildir — yöneticiler
        hiçbir alana bağlı değildir. Davet tamamlandığında bu değer kullanıcı kaydının
        birinci sınıf alanına ve `branch_codes` claim'ine akar.
      -->
      <q-select
        v-model="inviteForm.branchCodes"
        :options="branchOpts.options.value"
        :loading="branchOpts.loading.value"
        label="Alanlar (branş)"
        hint="Alan şefi / koordinatör için gereklidir; yöneticide boş bırakılabilir."
        outlined
        multiple
        use-chips
        use-input
        input-debounce="0"
        emit-value
        map-options
        option-label="label"
        option-value="value"
        @filter="branchOpts.filter"
      >
        <template #prepend>
          <q-icon name="school" />
        </template>
        <template #no-option>
          <SelectEmptyOption />
        </template>
      </q-select>
    </FormDialog>

    <!-- Roller — sağdan kayan yan-panel -->
    <FormDialog
      v-model="rolesDialog"
      :title="`Roller: ${selectedUser?.fullName ?? ''}`"
      icon="manage_accounts"
      color="secondary"
      :saving="saving"
      @save="saveRoles"
    >
      <q-option-group
        v-model="selectedRoles"
        :options="roleOptions"
        type="checkbox"
        role="group"
        aria-label="Kullanıcıya atanacak roller"
      />
    </FormDialog>

    <!-- Alanlar (branş) — kapsam kararı, roller/yetkiler ile aynı izin seviyesi (#126) -->
    <FormDialog
      v-model="branchesDialog"
      :title="`Alanlar: ${selectedUser?.fullName ?? ''}`"
      icon="school"
      color="secondary"
      :saving="saving"
      @save="saveBranches"
    >
      <AppNotice
        v-if="selectedUser?.branchRequired"
        type="info"
        message="Bu kullanıcı alan bazlı koordinasyon verisine yazabiliyor; en az bir alan seçilmelidir."
      />
      <AppNotice
        v-else
        type="info"
        message="Bu kullanıcı kurum genelinde yetkilidir; alan seçimi zorunlu değildir ve boş bırakılabilir."
      />
      <q-select
        v-model="selectedBranches"
        :options="branchOpts.options.value"
        :loading="branchOpts.loading.value"
        label="Alanlar"
        outlined
        multiple
        use-chips
        use-input
        input-debounce="0"
        emit-value
        map-options
        option-label="label"
        option-value="value"
        @filter="branchOpts.filter"
      >
        <template #prepend>
          <q-icon name="school" />
        </template>
        <template #no-option>
          <SelectEmptyOption />
        </template>
      </q-select>
    </FormDialog>

    <!--
      Kurum (kiracı) bağı — ADR-0003 adım 2.

      Kurum SEÇİCİSİ yoktur ve olmamalıdır: sunucu yalnız aktörün kendi kurumunu kabul eder
      (UserInstitutionScopePolicy). Seçici koymak, istemciye gerçekte var olmayan bir karar
      sunardı — kullanıcı başka kurum seçer, sunucu 422 döner.

      KAPATMA DAVRANIŞI (bilinçli): FormDialog kökünde `persistent` taşır, yani bu panel
      ESC veya arka plana tıklamayla kapanmaz — düz `q-dialog` hâlinde kapanıyordu. Kabul
      edildi: buradaki iki eylem de tek tıkla hüküm doğurur ("Bağı çöz" kurum kapsamını
      anında kaldırır, kullanıcının kapsamlı ekranları kapanır), dolayısıyla kapatma açık
      bir karar olmalıdır. Kullanıcı kilitlenmez: toolbar'daki kapatma düğmesi ve İptal
      butonu yerinde durur.
    -->
    <FormDialog
      v-model="institutionDialog"
      title="Kurum Bağı"
      icon="domain"
      color="secondary"
    >
      <div class="text-body2">
        {{ selectedUser?.fullName }}
      </div>

      <AppNotice
        v-if="selectedUser?.institutionId"
        type="info"
        :message="`Bağlı olduğu kurum: ${institutionStore.institution?.fullName ?? selectedUser.institutionId}`"
      />
      <AppNotice
        v-else
        type="warning"
        message="Bu kullanıcının kurum bağı yok. Kurum kapsamı isteyen ekranlar ona kapalıdır."
      />

      <!--
        Hedef kurum adı — BULGU 2. `saveInstitution` seçici SUNMAZ (yukarıdaki not); aktörün
        O AN davrandığı kurumu bağlar (`institutionStore.institution`). Müdürlük kendi
        bağlamındaysa bu müdürlük düğümünün kendisidir — bu, müdürlük personeli için doğru
        sonuçtur. Ama bir OKUL müdürünü bağlamak isteniyorsa aktör önce O OKULUN bağlamına
        GEÇMİŞ olmalıdır; aksi hâlde kullanıcı sessizce müdürlük düğümüne bağlanır. Ad burada
        adıyla göründüğü için "Kuruma bağla"ya basmadan ÖNCE yanlış bağlamda olunduğu görülür.
      -->
      <div
        v-if="!selectedUser?.institutionId"
        class="text-body2 text-weight-medium q-mt-sm"
      >
        Bağlanacak kurum:
        <span class="text-secondary">{{ institutionStore.institution?.fullName ?? 'Yükleniyor…' }}</span>
      </div>

      <div class="text-caption text-grey-7">
        Kurum bağı kullanıcının hangi okulun verisini göreceğini belirler. Yalnız kendi
        kurumunuza bağlayabilirsiniz.
      </div>

      <!--
        İki koşullu eylem tek save-label'a sığmadığı için varsayılan eylem çubuğu
        değiştirildi; İptal butonu bu yüzden elle eklendi.
      -->
      <template #actions>
        <q-btn
          v-close-popup
          flat
          label="İptal"
          color="grey-7"
        />
        <q-btn
          v-if="selectedUser?.institutionId"
          flat
          color="negative"
          label="Bağı çöz"
          :loading="saving"
          @click="saveInstitution(null)"
        />
        <q-btn
          v-else
          unelevated
          color="secondary"
          label="Kuruma bağla"
          :loading="saving"
          :disable="!institutionStore.institution"
          @click="saveInstitution(institutionStore.institution?.id ?? null)"
        />
      </template>
    </FormDialog>

    <!--
      Veli–öğrenci bağı (#174).

      KAPATMA DAVRANIŞI (bilinçli): FormDialog `persistent`tir — ESC ve arka plana tıklama
      bu paneli kapatmaz, düz `q-dialog` hâlinde kapatıyordu. Kabul edildi: çoklu seçim
      kaydedilmeden kapanırsa velinin öğrenci listesinde yapılan tüm düzenleme sessizce
      kaybolur. Kapatma yolu açık kalır: toolbar'daki kapatma düğmesi ve İptal butonu.
    -->
    <FormDialog
      v-model="studentsDialog"
      title="Bağlı Öğrenciler"
      icon="family_restroom"
      color="secondary"
      :saving="saving"
      @save="saveStudents"
    >
      <AppNotice
        type="info"
        message="Velinin erişebileceği veriyi yalnız bu liste belirler. Boş bırakmak bağı kaldırır; veli hiçbir öğrencinin verisine erişemez."
      />
      <q-select
        v-model="selectedStudents"
        :options="studentOpts.options.value"
        :loading="studentOpts.loading.value"
        label="Öğrenciler"
        outlined
        multiple
        use-chips
        use-input
        input-debounce="0"
        emit-value
        map-options
        option-label="label"
        option-value="value"
        @filter="studentOpts.filter"
      >
        <template #prepend>
          <q-icon name="school" />
        </template>
        <template #no-option>
          <SelectEmptyOption />
        </template>
      </q-select>
    </FormDialog>
  </q-page>
</template>

<script setup lang="ts">
import { ref, computed, reactive } from 'vue'
import type { QTableProps } from 'quasar'
import { securityApi, type UserAccountDto, type InvitationDto } from 'src/api/security'
import { useNotify } from 'src/composables/useNotify'
import { useServerPagination } from 'src/composables/useServerPagination'
import { Permissions } from 'utils/permissions'
import { useAuthStore } from 'stores/auth'
import { useEntityOptionsStore } from 'stores/entityOptions'
import { useRoleCatalogStore } from 'stores/roleCatalog'
import { useInstitutionStore } from 'stores/institution'
import AppTable from 'components/AppTable.vue'
import AppNotice from 'components/AppNotice.vue'
import FormDialog from 'components/FormDialog.vue'
import PageHeader from 'components/PageHeader.vue'
import PermissionGuard from 'components/PermissionGuard.vue'
import SelectEmptyOption from 'components/SelectEmptyOption.vue'
import StatusBadge from 'components/StatusBadge.vue'
import { useBranchOptions, useStudentOptions } from 'src/composables/useEntityOptions'
import { resolveBranchCellState } from 'utils/branchAssignment'

const notify = useNotify()
const authStore = useAuthStore()
const entityOptionsStore = useEntityOptionsStore()
// Kurum bağı dialogu kurum adını buradan gösterir; Faz 1'de tek kurum vardır (ADR-0003).
const institutionStore = useInstitutionStore()

const tab = ref('users')
const saving = ref(false)
const syncing = ref(false)

const selectedUser = ref<UserAccountDto | null>(null)
const inviteDialog = ref(false)
const rolesDialog = ref(false)
const selectedRoles = ref<string[]>([])

// ── Alan (branş) kapsamı (#126) ──
const branchesDialog = ref(false)
const selectedBranches = ref<string[]>([])
// ── Kurum (kiracı) bağı (ADR-0003 adım 2) ──
const institutionDialog = ref(false)
const studentsDialog = ref(false)
const selectedStudents = ref<string[]>([])
const missingBranchOnly = ref(false)
const branchOpts = useBranchOptions()
const studentOpts = useStudentOptions()

// ── Server-side pagination: Users ──
// Boş liste yöneticide normaldir; filtre yalnız "beklenip girilmemiş" olanları getirir.
const userFilters = computed(() =>
  missingBranchOnly.value ? { missingBranchOnly: true } : {},
)
const { rows: users, loading: usersLoading, pagination: usersPagination, search: usersSearch, onRequest: onUsersRequest, onSearch: onUsersSearch, load: loadUsers } =
  useServerPagination<UserAccountDto>({
    fetchFn: (params) => securityApi.listUsers(params),
    filters: userFilters,
    defaultSortBy: 'fullName',
  })

// ── Server-side pagination: Invitations ──
const invFilters = computed(() => ({}))
const { rows: invitations, loading: invsLoading, pagination: invsPagination, search: invsSearch, onRequest: onInvsRequest, onSearch: onInvsSearch, load: loadInvitations } =
  useServerPagination<InvitationDto>({
    fetchFn: (params) => securityApi.listInvitations(params),
    filters: invFilters,
    defaultSortBy: 'createdAt',
    defaultDescending: true,
  })

// ── Roller: tek doğruluk kaynağı GET /api/security/roles (#129) ──
// Elle yazılmış liste KALDIRILDI: gerçek rollerle eşleşmiyordu ve karşılığı olmayan adlar
// (deputy_director, coordinator_teacher, master_trainer) sunucuya gidiyordu.
const roleCatalog = useRoleCatalogStore()
const roleOptions = computed(() => roleCatalog.options)

const inviteForm = reactive({
  email: '', firstName: '', lastName: '', targetRole: '',
  branchCodes: [] as string[],
})

const userColumns: QTableProps['columns'] = [
  { name: 'fullName', label: 'Ad Soyad', field: 'fullName', align: 'left', sortable: true },
  { name: 'email', label: 'E-posta', field: 'email', align: 'left' },
  { name: 'roles', label: 'Roller', field: 'roles', align: 'left' },
  { name: 'branchCodes', label: 'Alan', field: 'branchCodes', align: 'left' },
  { name: 'isEnabled', label: 'Durum', field: 'isEnabled', align: 'center' },
  { name: 'userActions', label: '', field: 'id', align: 'right' },
]

// Davet durumu backend'den SmartEnum'un İngilizce Name'i olarak gelir (StatusName).
// Kullanıcıya Türkçe slug gösterilir; slug'lar StatusBadge'in ton haritasında zaten tanımlı.
// Tanınmayan değer HAM basılır — bozuk kaydın görünür kalması doğrudur, gizlenmesi değil.
//
// Aynı harita RolePage.vue'de de duruyor (davet tutarlılık taraması listesi). İkisi birebir
// aynıdır ve öyle kalmalıdır; ortak bir modüle taşınması bu grubun yazma kapsamı dışındadır.
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

/**
 * "SIRA SİZDE" — Tek Ses Kuralı: bu ekranda hardal TEK bağlamda görünür, o da DAVETLER
 * sekmesindeki onay kararıdır. Kullanıcılar sekmesinde bekleyen bir karar yoktur.
 *
 * Koşul VERİDEN türer, rol adından DEĞİL (ADR-0001):
 *  • `status === 'PendingApproval'` — onay/red butonlarını açan aşamanın ta kendisi
 *    (bkz. #body-cell-invActions). Sinyal o butonların durum sütunundaki izdüşümüdür.
 *  • `Permissions.UserManagement.Approve` — o butonları açan izin.
 *
 * Sekme görünürlüğü de aynı izne bakıyor olsa bile koşul burada AÇIKÇA yazılır: sekme
 * koşulu ileride gevşerse (ör. görüntüleme amaçlı açılırsa) sinyal sessizce yetkisiz
 * kullanıcıda yanardı.
 *
 * Akademik dönem kapalılığı sorgulanmadı: davet onayı döneme bağlı değildir, bu sayfa
 * dönem store'una hiç dokunmaz.
 */
function isInvitationMyTurn(row: InvitationDto): boolean {
  return (
    row.status === 'PendingApproval' &&
    authStore.hasPermission(Permissions.UserManagement.Approve)
  )
}

const invitationColumns: QTableProps['columns'] = [
  { name: 'fullName', label: 'Ad Soyad', field: 'fullName', align: 'left' },
  { name: 'email', label: 'E-posta', field: 'email', align: 'left' },
  { name: 'targetRole', label: 'Rol', field: 'targetRole', align: 'left' },
  { name: 'status', label: 'Durum', field: 'status', align: 'left' },
  { name: 'invActions', label: '', field: 'id', align: 'right' },
]

function openEditUser(row: UserAccountDto) {
  selectedUser.value = row
}

function openRoles(row: UserAccountDto) {
  selectedUser.value = row
  selectedRoles.value = [...row.roles]
  rolesDialog.value = true
}

async function toggleStatus(row: UserAccountDto) {
  saving.value = true
  try {
    await securityApi.toggleStatus(row.id)
    notify.success(`Kullanıcı ${row.isEnabled ? 'pasife alındı' : 'aktifleştirildi'}.`)
    await loadUsers()
  } catch (e) {
    notify.apiError(e, 'İşlem sırasında bir hata oluştu.')
  } finally {
    saving.value = false
  }
}

async function saveRoles() {
  if (!selectedUser.value) return
  saving.value = true
  try {
    await securityApi.changeRoles(selectedUser.value.id, { roles: selectedRoles.value })
    notify.success('Roller güncellendi.')
    rolesDialog.value = false
    // Rol değişimi alan zorunluluğunu değiştirebilir — liste tazelenince
    // branşsız kalan kullanıcı "Branş atanmamış" rozetiyle görünür hâle gelir (#126).
    await loadUsers()
  } catch (e) {
    notify.apiError(e, 'Roller güncellenirken bir hata oluştu.')
  } finally {
    saving.value = false
  }
}

function openBranches(row: UserAccountDto) {
  selectedUser.value = row
  selectedBranches.value = [...row.branchCodes]
  branchesDialog.value = true
  branchOpts.load().catch(() => {})
}

async function saveBranches() {
  if (!selectedUser.value) return
  saving.value = true
  try {
    // Boş dizi kapsamı kaldırır — alan şefliğinden yöneticiliğe geçişte geçerli işlemdir.
    await securityApi.changeBranches(selectedUser.value.id, { branchCodes: selectedBranches.value })
    notify.success('Kullanıcının alanları güncellendi.')
    branchesDialog.value = false
    await loadUsers()
  } catch (e) {
    notify.apiError(e, 'Alanlar güncellenirken bir hata oluştu.')
  } finally {
    saving.value = false
  }
}

function openInstitution(row: UserAccountDto) {
  selectedUser.value = row
  institutionDialog.value = true
  institutionStore.loadInstitution().catch(() => {})
}

/**
 * Kurum (kiracı) bağını yazar — `null` bağı çözer (ADR-0003 adım 2).
 *
 * Hangi kuruma yazılabileceğine sunucu karar verir; burada seçim yapılmaz.
 */
async function saveInstitution(institutionId: string | null) {
  if (!selectedUser.value) return
  saving.value = true
  try {
    await securityApi.changeInstitution(selectedUser.value.id, { institutionId })
    notify.success(institutionId ? 'Kurum bağı kuruldu.' : 'Kurum bağı çözüldü.')
    institutionDialog.value = false
  } catch (e) {
    notify.apiError(e, 'Kurum bağı güncellenirken bir hata oluştu.')
    return
  } finally {
    saving.value = false
  }

  // Yenileme try/catch dışında: yazma başarılı ama liste yenilenemezse hem başarı hem hata
  // bildirimi gösterilmemeli.
  await loadUsers().catch(() => {})
}

function openStudents(row: UserAccountDto) {
  selectedUser.value = row
  selectedStudents.value = [...row.linkedStudentIds]
  studentsDialog.value = true
  studentOpts.load().catch(() => {})
}

async function saveStudents() {
  if (!selectedUser.value) return
  saving.value = true
  try {
    // Boş dizi bağı kaldırır — öğrenci mezun olduğunda ya da vesayet değiştiğinde geçerli işlem.
    await securityApi.changeStudents(selectedUser.value.id, { studentIds: selectedStudents.value })
    notify.success('Velinin bağlı olduğu öğrenciler güncellendi.')
    studentsDialog.value = false
    await loadUsers()
  } catch (e) {
    notify.apiError(e, 'Bağlı öğrenciler güncellenirken bir hata oluştu.')
  } finally {
    saving.value = false
  }
}

async function sendInvitation() {
  saving.value = true
  try {
    await securityApi.createInvitation({
      email: inviteForm.email,
      firstName: inviteForm.firstName,
      lastName: inviteForm.lastName,
      targetRole: inviteForm.targetRole,
      // Davet metadata'sı yalnız TAŞIMA biçimidir; davet tamamlanınca backend bunu
      // kullanıcı kaydının birinci sınıf BranchCodes alanına yazar (#126).
      ...(inviteForm.branchCodes.length
        ? { metadata: { BranchCode: inviteForm.branchCodes.join(',') } }
        : {}),
    })
    entityOptionsStore.invalidateKeycloakUsers()
    notify.success('Davet gönderildi.')
    inviteDialog.value = false
    inviteForm.email = ''
    inviteForm.firstName = ''
    inviteForm.lastName = ''
    inviteForm.targetRole = ''
    inviteForm.branchCodes = []
    await loadInvitations()
  } catch (e) {
    notify.apiError(e, 'Davet gönderilirken bir hata oluştu.')
  } finally {
    saving.value = false
  }
}

async function approveInvitation(row: InvitationDto) {
  saving.value = true
  try {
    await securityApi.approveInvitation(row.id)
    notify.success('Davet onaylandı.')
    await loadInvitations()
  } catch (e) {
    notify.apiError(e, 'İşlem sırasında bir hata oluştu.')
  } finally {
    saving.value = false
  }
}

async function rejectInvitation(row: InvitationDto) {
  saving.value = true
  try {
    await securityApi.rejectInvitation(row.id)
    notify.success('Davet reddedildi.')
    await loadInvitations()
  } catch (e) {
    notify.apiError(e, 'İşlem sırasında bir hata oluştu.')
  } finally {
    saving.value = false
  }
}

async function resendInvitation(row: InvitationDto) {
  saving.value = true
  try {
    await securityApi.resendInvitation(row.id)
    notify.success('Davet tekrar gönderildi.')
  } catch (e) {
    notify.apiError(e, 'İşlem sırasında bir hata oluştu.')
  } finally {
    saving.value = false
  }
}

async function syncUsers() {
  syncing.value = true
  try {
    const { data } = await securityApi.syncUsers()
    entityOptionsStore.invalidateKeycloakUsers()
    notify.success(`${data.total} kullanıcı senkronize edildi (${data.created} yeni, ${data.updated} güncellendi).`)

    // Sync kiracı anahtarını Keycloak'tan kopyalamaz (ADR-0003 adım 2) — dışarıdan gelen
    // kullanıcı kapsamsız doğar. Sessiz kalırsa "senkronize edildi" iş bitti sanılır.
    if (data.withoutInstitution > 0) {
      notify.warning(
        `${data.withoutInstitution} hesabın kurum bağı yok. Kurum kapsamı isteyen ekranlar ` +
        'onlara kapalı — satırdaki kurum simgesinden bağlayın.')
    }

    await loadUsers()
  } catch (e) {
    notify.apiError(e, 'Senkronizasyon sırasında bir hata oluştu.')
  } finally {
    syncing.value = false
  }
}

// Initial load
loadUsers()
loadInvitations()
// Rol kataloğu — davet formu, rol dialogu ve rol etiketleri için (#129)
roleCatalog.load().catch(() => {})
// Alan kataloğu — davet ve alan yönetimi dialoglarında kullanılır (#126)
branchOpts.load().catch(() => {})
</script>
