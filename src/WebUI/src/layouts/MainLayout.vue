<template>
  <q-layout view="lHh Lpr lFf">
    <q-header elevated>
      <q-toolbar>
        <q-btn flat dense round icon="menu" @click="drawerOpen = !drawerOpen" />
        <q-toolbar-title>MESNET</q-toolbar-title>
        <q-space />
        <span v-if="authStore.user" class="text-body2 q-mr-md">
          {{ authStore.user.fullName }}
        </span>
        <q-btn flat round dense icon="notifications" class="q-mr-xs">
          <q-badge v-if="unreadCount > 0" color="negative" floating>{{ unreadCount }}</q-badge>
          <q-tooltip>Bildirimler</q-tooltip>
          <q-menu anchor="bottom right" self="top right" style="min-width: 320px; max-width: 400px">
            <q-list separator>
              <q-item v-if="notificationStore.notifications.length === 0" dense>
                <q-item-section class="text-grey text-caption text-center q-pa-md">
                  Bildirim yok
                </q-item-section>
              </q-item>
              <q-item
                v-for="(n, i) in notificationStore.notifications.slice(0, 10)"
                :key="i"
                dense
              >
                <q-item-section avatar>
                  <q-icon :name="moduleIcon(n.module)" color="primary" size="sm" />
                </q-item-section>
                <q-item-section>
                  <q-item-label class="text-caption text-weight-medium">{{ n.eventType }}</q-item-label>
                  <q-item-label caption class="text-grey">{{ n.module }}</q-item-label>
                </q-item-section>
              </q-item>
              <q-item v-if="notificationStore.notifications.length > 0" dense clickable @click="notificationStore.clear()">
                <q-item-section class="text-center text-caption text-grey">Tümünü temizle</q-item-section>
              </q-item>
            </q-list>
          </q-menu>
        </q-btn>
        <q-btn flat round dense icon="logout" @click="onLogout" />
      </q-toolbar>
    </q-header>

    <q-drawer v-model="drawerOpen" show-if-above bordered>
      <q-scroll-area class="fit">
        <q-list padding>

          <q-item clickable v-ripple :to="{ name: 'Dashboard' }" exact>
            <q-item-section avatar><q-icon name="dashboard" /></q-item-section>
            <q-item-section>Ana Sayfa</q-item-section>
          </q-item>

          <q-item
            v-if="authStore.hasPermission(Permissions.Institution.View)"
            clickable v-ripple :to="{ name: 'Institution' }"
          >
            <q-item-section avatar><q-icon name="account_balance" /></q-item-section>
            <q-item-section>Kurum</q-item-section>
          </q-item>

          <q-item
            v-if="authStore.hasPermission(Permissions.Company.View)"
            clickable v-ripple :to="{ name: 'CompanyList' }"
          >
            <q-item-section avatar><q-icon name="business" /></q-item-section>
            <q-item-section>İşletmeler</q-item-section>
          </q-item>

          <q-item
            v-if="authStore.hasPermission(Permissions.Student.View)"
            clickable v-ripple :to="{ name: 'StudentList' }"
          >
            <q-item-section avatar><q-icon name="school" /></q-item-section>
            <q-item-section>Öğrenciler</q-item-section>
          </q-item>

          <!-- Phase 2 — MEB Protokolü modülü implement edilince açılacak -->
          <!-- <q-item
            v-if="authStore.hasPermission(Permissions.Protocol.View)"
            clickable v-ripple :to="{ name: 'ProtocolList' }"
          >
            <q-item-section avatar><q-icon name="assignment" /></q-item-section>
            <q-item-section>Başvurular</q-item-section>
          </q-item> -->

          <q-item
            v-if="authStore.hasAnyPermission([Permissions.Internship.View, Permissions.Internship.Manage])"
            clickable v-ripple :to="{ name: 'InternshipOverview' }"
          >
            <q-item-section avatar><q-icon name="work_history" /></q-item-section>
            <q-item-section>Staj Takibi</q-item-section>
          </q-item>

          <q-item
            v-if="authStore.hasAnyPermission([Permissions.Internship.Manage, Permissions.Internship.Contract])"
            clickable v-ripple :to="{ name: 'ContractList' }"
          >
            <q-item-section avatar><q-icon name="description" /></q-item-section>
            <q-item-section>Sözleşmeler</q-item-section>
          </q-item>

          <q-item
            v-if="authStore.hasPermission(Permissions.Attendance.View)"
            clickable v-ripple :to="{ name: 'AttendanceList' }"
          >
            <q-item-section avatar><q-icon name="event_available" /></q-item-section>
            <q-item-section>Devamsızlık</q-item-section>
          </q-item>

          <q-item
            v-if="authStore.hasPermission(Permissions.Salary.View)"
            clickable v-ripple :to="{ name: 'SalaryList' }"
          >
            <q-item-section avatar><q-icon name="payments" /></q-item-section>
            <q-item-section>Maaş / Dekont</q-item-section>
          </q-item>

          <q-item
            v-if="authStore.hasAnyPermission([Permissions.Coordinator.Visit, Permissions.Coordinator.Schedule])"
            clickable v-ripple :to="{ name: 'Coordination' }"
          >
            <q-item-section avatar><q-icon name="supervisor_account" /></q-item-section>
            <q-item-section>Koordinasyon</q-item-section>
          </q-item>

          <q-item
            v-if="authStore.hasPermission(Permissions.Document.View)"
            clickable v-ripple :to="{ name: 'Documents' }"
          >
            <q-item-section avatar><q-icon name="folder_open" /></q-item-section>
            <q-item-section>Belgeler</q-item-section>
          </q-item>

          <q-item
            v-if="authStore.hasPermission(Permissions.Internship.Report)"
            clickable v-ripple :to="{ name: 'Reporting' }"
          >
            <q-item-section avatar><q-icon name="bar_chart" /></q-item-section>
            <q-item-section>Raporlar</q-item-section>
          </q-item>

          <q-separator spaced />

          <q-item
            v-if="authStore.hasAnyPermission([Permissions.UserManagement.View, Permissions.UserManagement.Create])"
            clickable v-ripple :to="{ name: 'UserManagement' }"
          >
            <q-item-section avatar><q-icon name="manage_accounts" /></q-item-section>
            <q-item-section>Kullanıcılar</q-item-section>
          </q-item>

          <q-item
            v-if="authStore.hasPermission(Permissions.UserManagement.RolesManage)"
            clickable v-ripple :to="{ name: 'RoleManagement' }"
          >
            <q-item-section avatar><q-icon name="admin_panel_settings" /></q-item-section>
            <q-item-section>Roller</q-item-section>
          </q-item>

        </q-list>
      </q-scroll-area>
    </q-drawer>

    <q-page-container>
      <router-view />
    </q-page-container>
  </q-layout>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue'
import { useAuthStore } from 'stores/auth'
import { useNotificationStore } from 'stores/notifications'
import { Permissions } from 'utils/permissions'
import { logout } from 'boot/auth'

const authStore = useAuthStore()
const notificationStore = useNotificationStore()
const drawerOpen = ref(false)

const unreadCount = computed(() => notificationStore.notifications.length)

const MODULE_ICONS: Record<string, string> = {
  Institution: 'account_balance',
  Business: 'business',
  Enrollment: 'school',
  Contract: 'description',
  Attendance: 'event_available',
  Payment: 'payments',
  Coordination: 'supervisor_account',
  Internship: 'work_history',
  Reporting: 'bar_chart',
  Security: 'manage_accounts',
  System: 'info',
}

function moduleIcon(module: string): string {
  return MODULE_ICONS[module] ?? 'notifications'
}

async function onLogout() {
  await logout()
}
</script>
