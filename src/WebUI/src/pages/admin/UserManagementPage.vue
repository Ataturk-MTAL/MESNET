<template>
  <q-page padding>
    <div class="text-h5 text-weight-bold q-mb-lg">Kullanıcı Yönetimi</div>

    <q-tabs v-model="tab" align="left" class="q-mb-md">
      <q-tab name="users" label="Kullanıcılar" icon="group" />
      <q-tab
        v-if="authStore.hasPermission(Permissions.UserManagement.Approve)"
        name="invitations" label="Davetler" icon="mail"
      />
    </q-tabs>

    <q-tab-panels v-model="tab" animated>
      <!-- KULLANICILAR -->
      <q-tab-panel name="users">
        <div class="row items-center q-mb-md">
          <div class="col text-subtitle1 text-weight-medium">Kullanıcılar</div>
          <div class="col-auto">
            <PermissionGuard :permission="Permissions.UserManagement.Create">
              <q-btn color="primary" icon="person_add" label="Davet Gönder" @click="inviteDialog = true" />
            </PermissionGuard>
          </div>
        </div>

        <AppTable :rows="users" :columns="userColumns" :loading="usersLoading" :pagination="usersPagination" show-search :search="usersSearch" @request="onUsersRequest" @search="onUsersSearch">
          <template #body-cell-isEnabled="{ row }">
            <q-td>
              <q-badge :color="row.isEnabled ? 'positive' : 'grey'" :label="row.isEnabled ? 'Aktif' : 'Pasif'" />
            </q-td>
          </template>
          <template #body-cell-roles="{ row }">
            <q-td>
              <q-badge
                v-for="role in row.roles.slice(0, 2)"
                :key="role"
                color="blue-grey"
                :label="role"
                class="q-mr-xs"
              />
              <span v-if="row.roles.length > 2" class="text-caption text-grey">+{{ row.roles.length - 2 }}</span>
            </q-td>
          </template>
          <template #body-cell-userActions="{ row }">
            <q-td class="text-right">
              <PermissionGuard :permission="Permissions.UserManagement.Update">
                <q-btn flat round dense icon="edit" @click="openEditUser(row)" />
              </PermissionGuard>
              <PermissionGuard :permission="Permissions.UserManagement.RolesManage">
                <q-btn flat round dense icon="manage_accounts" @click="openRoles(row)" />
              </PermissionGuard>
              <PermissionGuard :permission="Permissions.UserManagement.Update">
                <q-btn
                  flat round dense
                  :icon="row.isEnabled ? 'person_off' : 'person'"
                  :color="row.isEnabled ? 'negative' : 'positive'"
                  @click="toggleStatus(row)"
                />
              </PermissionGuard>
            </q-td>
          </template>
        </AppTable>
      </q-tab-panel>

      <!-- DAVETLER -->
      <q-tab-panel name="invitations">
        <div class="text-subtitle1 text-weight-medium q-mb-md">Bekleyen Davetler</div>
        <AppTable :rows="invitations" :columns="invitationColumns" :loading="invsLoading" :pagination="invsPagination" show-search :search="invsSearch" @request="onInvsRequest" @search="onInvsSearch">
          <template #body-cell-status="{ row }">
            <q-td>
              <q-badge
                :color="row.status === 'Approved' ? 'positive' : row.status === 'PendingApproval' ? 'orange' : row.status === 'Rejected' ? 'negative' : 'grey'"
                :label="row.status === 'PendingApproval' ? 'Onay Bekliyor' : row.status === 'Approved' ? 'Onaylandı' : row.status === 'Rejected' ? 'Reddedildi' : row.status === 'Completed' ? 'Tamamlandı' : 'Süresi Doldu'"
              />
            </q-td>
          </template>
          <template #body-cell-invActions="{ row }">
            <q-td class="text-right">
              <PermissionGuard :permission="Permissions.UserManagement.Approve">
                <q-btn
                  v-if="row.status === 'PendingApproval'"
                  flat round dense icon="check"
                  color="positive"
                  @click="approveInvitation(row)"
                />
                <q-btn
                  v-if="row.status === 'PendingApproval'"
                  flat round dense icon="close"
                  color="negative"
                  @click="rejectInvitation(row)"
                />
              </PermissionGuard>
              <q-btn
                v-if="row.status === 'Approved'"
                flat round dense icon="send"
                color="primary"
                @click="resendInvitation(row)"
              />
            </q-td>
          </template>
        </AppTable>
      </q-tab-panel>
    </q-tab-panels>

    <!-- Davet Gönder Dialog -->
    <q-dialog v-model="inviteDialog" persistent :maximized="$q.screen.lt.sm" transition-show="slide-up" transition-hide="slide-down">
      <q-card :style="$q.screen.gt.xs ? 'width: 480px; max-width: 95vw' : ''">
        <q-toolbar class="bg-primary text-white">
          <q-icon name="person_add" class="q-mr-sm" />
          <q-toolbar-title>Kullanıcı Davet Et</q-toolbar-title>
          <q-btn flat round dense icon="close" color="white" v-close-popup />
        </q-toolbar>
        <q-card-section class="q-pt-lg q-gutter-md">
          <q-input v-model="inviteForm.email" label="E-posta" filled type="email">
            <template #prepend>
              <q-icon name="email" />
            </template>
          </q-input>
          <q-input v-model="inviteForm.firstName" label="Ad" filled>
            <template #prepend>
              <q-icon name="person" />
            </template>
          </q-input>
          <q-input v-model="inviteForm.lastName" label="Soyad" filled>
            <template #prepend>
              <q-icon name="person" />
            </template>
          </q-input>
          <q-select
            v-model="inviteForm.targetRole"
            :options="roleOptions"
            label="Hedef Rol"
            filled emit-value map-options
          >
            <template #prepend>
              <q-icon name="manage_accounts" />
            </template>
          </q-select>
        </q-card-section>
        <q-separator />
        <q-card-actions align="right" class="q-pa-md">
          <q-btn flat label="İptal" color="grey-7" v-close-popup />
          <q-btn unelevated color="primary" label="Davet Gönder" :loading="saving" @click="sendInvitation" />
        </q-card-actions>
      </q-card>
    </q-dialog>

    <!-- Roller Dialog -->
    <q-dialog v-model="rolesDialog" persistent :maximized="$q.screen.lt.sm" transition-show="slide-up" transition-hide="slide-down">
      <q-card :style="$q.screen.gt.xs ? 'width: 480px; max-width: 95vw' : ''">
        <q-toolbar class="bg-purple text-white">
          <q-icon name="manage_accounts" class="q-mr-sm" />
          <q-toolbar-title>Roller: {{ selectedUser?.fullName }}</q-toolbar-title>
          <q-btn flat round dense icon="close" color="white" v-close-popup />
        </q-toolbar>
        <q-card-section class="q-pt-lg q-gutter-md">
          <q-option-group
            v-model="selectedRoles"
            :options="roleOptions"
            type="checkbox"
          />
        </q-card-section>
        <q-separator />
        <q-card-actions align="right" class="q-pa-md">
          <q-btn flat label="İptal" color="grey-7" v-close-popup />
          <q-btn unelevated color="purple" label="Kaydet" :loading="saving" @click="saveRoles" />
        </q-card-actions>
      </q-card>
    </q-dialog>
  </q-page>
</template>

<script setup lang="ts">
import { ref, computed, reactive } from 'vue'
import type { QTableProps } from 'quasar'
import { useQuasar } from 'quasar'
import { securityApi, type UserAccountDto, type InvitationDto } from 'src/api/security'
import { useNotify } from 'src/composables/useNotify'
import { useServerPagination } from 'src/composables/useServerPagination'
import { Permissions } from 'utils/permissions'
import { useAuthStore } from 'stores/auth'
import { useEntityOptionsStore } from 'stores/entityOptions'
import AppTable from 'components/AppTable.vue'
import PermissionGuard from 'components/PermissionGuard.vue'

const $q = useQuasar()
const notify = useNotify()
const authStore = useAuthStore()
const entityOptionsStore = useEntityOptionsStore()

const tab = ref('users')
const saving = ref(false)

const selectedUser = ref<UserAccountDto | null>(null)
const inviteDialog = ref(false)
const rolesDialog = ref(false)
const selectedRoles = ref<string[]>([])

// ── Server-side pagination: Users ──
const userFilters = computed(() => ({}))
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

const roleOptions = [
  { label: 'Kurum Müdürü', value: 'institution_manager' },
  { label: 'Müdür Yardımcısı', value: 'deputy_director' },
  { label: 'Alan Şefi', value: 'department_head' },
  { label: 'Koordinatör Öğretmen', value: 'coordinator_teacher' },
  { label: 'İşletme Yöneticisi', value: 'company_manager' },
  { label: 'Usta Öğretici', value: 'master_trainer' },
  { label: 'Öğrenci', value: 'student' },
]

const inviteForm = reactive({
  email: '', firstName: '', lastName: '', targetRole: '',
})

const userColumns: QTableProps['columns'] = [
  { name: 'fullName', label: 'Ad Soyad', field: 'fullName', align: 'left', sortable: true },
  { name: 'email', label: 'E-posta', field: 'email', align: 'left' },
  { name: 'roles', label: 'Roller', field: 'roles', align: 'left' },
  { name: 'isEnabled', label: 'Durum', field: 'isEnabled', align: 'center' },
  { name: 'userActions', label: '', field: 'id', align: 'right' },
]

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
    await loadUsers()
  } catch (e) {
    notify.apiError(e, 'Roller güncellenirken bir hata oluştu.')
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
    })
    entityOptionsStore.invalidateKeycloakUsers()
    notify.success('Davet gönderildi.')
    inviteDialog.value = false
    inviteForm.email = ''
    inviteForm.firstName = ''
    inviteForm.lastName = ''
    inviteForm.targetRole = ''
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

// Initial load
loadUsers()
loadInvitations()
</script>
