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

          <!-- Dönem Seçici -->
          <q-item v-if="periodStore.isLoaded && periodStore.periods.length > 0" class="q-mb-xs">
            <q-item-section>
              <q-select
                v-model="periodStore.selectedPeriodId"
                :options="periodOptions"
                option-value="value"
                option-label="label"
                emit-value
                map-options
                dense
                outlined
                label="Dönem"
              >
                <template #option="{ itemProps, opt }">
                  <q-item v-bind="itemProps">
                    <q-item-section>
                      <q-item-label>{{ opt.label }}</q-item-label>
                    </q-item-section>
                    <q-item-section side>
                      <q-badge
                        :color="opt.active ? 'green-7' : 'grey-5'"
                        :label="opt.active ? 'Aktif' : 'Kapalı'"
                      />
                    </q-item-section>
                  </q-item>
                </template>
              </q-select>
            </q-item-section>
          </q-item>

          <!-- Yarıyıl Seçici -->
          <q-item class="q-mb-xs">
            <q-item-section>
              <q-select
                v-model="periodStore.selectedSemester"
                :options="semesterOpts"
                option-value="value"
                option-label="label"
                emit-value
                map-options
                dense
                outlined
                label="Yarıyıl"
              />
            </q-item-section>
          </q-item>

          <q-separator v-if="periodStore.isLoaded && periodStore.periods.length > 0" spaced />

          <!-- Kapalı dönem uyarısı -->
          <q-banner
            v-if="periodStore.isReadOnly"
            dense
            rounded
            class="bg-orange-1 text-orange-9 q-mx-sm q-mb-sm text-caption"
          >
            <template #avatar>
              <q-icon name="lock" color="orange-7" size="xs" />
            </template>
            Geçmiş dönem — salt okunur
          </q-banner>

          <template v-for="group in filteredMenu">
            <!-- Düz link (child yok veya tek child → terfi) -->
            <q-item
              v-if="group.to"
              :key="group.key"
              clickable
              v-ripple
              :to="group.to"
              :active="activeGroupKey === group.key"
            >
              <q-item-section avatar><q-icon :name="group.icon" /></q-item-section>
              <q-item-section>{{ group.title }}</q-item-section>
            </q-item>

            <!-- Genişletilebilir grup -->
            <q-expansion-item
              v-else
              :key="'g-' + group.key"
              :icon="group.icon"
              :label="group.title"
              :model-value="isExpanded(group.key)"
              @update:model-value="toggleGroup(group.key)"
              :header-class="activeGroupKey === group.key ? 'text-primary' : ''"
              dense-toggle
            >
              <q-item
                v-for="item in group.children"
                :key="item.to.name"
                clickable
                v-ripple
                :to="item.to"
                :inset-level="1"
                dense
              >
                <q-item-section avatar><q-icon :name="item.icon" size="sm" /></q-item-section>
                <q-item-section>{{ item.title }}</q-item-section>
              </q-item>
            </q-expansion-item>
          </template>

          <q-separator spaced />

          <q-item clickable v-ripple @click="aboutDialog = true">
            <q-item-section avatar><q-icon name="info" /></q-item-section>
            <q-item-section>Hakkında</q-item-section>
          </q-item>

        </q-list>
      </q-scroll-area>
    </q-drawer>

    <q-page-container>
      <router-view />
    </q-page-container>

    <!-- Hakkında Dialog -->
    <q-dialog v-model="aboutDialog">
      <q-card style="min-width: 400px; max-width: 500px">
        <q-card-section class="row items-center q-pb-none">
          <div class="text-h6">Hakkında</div>
          <q-space />
          <q-btn icon="close" flat round dense v-close-popup />
        </q-card-section>

        <q-card-section class="text-center q-pt-md">
          <q-icon name="school" color="primary" size="64px" class="q-mb-md" />
          <div class="text-h5 text-weight-bold text-primary q-mb-xs">MESNET</div>
          <div class="text-subtitle2 text-grey-8 q-mb-lg">
            Mesleki Eğitim Stajları Nitelikli, Eşgüdümlü Takip Sistemi
          </div>

          <q-separator class="q-my-md" />

          <div class="text-body2 q-mb-md">
            Bu yazılım<br />
            <strong>Toroslar Atatürk Mesleki ve Teknik Anadolu Lisesi</strong><br />
            <strong>Elektrik-Elektronik Teknolojisi</strong> alan öğretmenleri<br />
            tarafından hazırlanmıştır.
          </div>

          <q-separator class="q-my-md" />

          <div class="row justify-center q-gutter-x-md text-caption text-grey-7">
            <div>
              <q-icon name="tag" size="xs" class="q-mr-xs" />
              Sürüm: <strong>{{ appVersion }}</strong>
            </div>
          </div>
        </q-card-section>

        <q-card-section class="text-center text-caption text-grey-6 q-pt-none">
          &copy; {{ currentYear }} MESNET — Tüm hakları saklıdır.
        </q-card-section>
      </q-card>
    </q-dialog>
  </q-layout>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useAuthStore } from 'stores/auth'
import { useNotificationStore } from 'stores/notifications'
import { useAcademicPeriodStore, semesterOptions } from 'stores/academicPeriod'
import { logout } from 'boot/auth'
import { useNavigation } from 'src/composables/useNavigation'

const authStore = useAuthStore()
const { filteredMenu, isExpanded, toggleGroup, activeGroupKey } = useNavigation()
const notificationStore = useNotificationStore()
const periodStore = useAcademicPeriodStore()
const drawerOpen = ref(false)
const aboutDialog = ref(false)
const appVersion = '0.1.0'
const currentYear = new Date().getFullYear()

const unreadCount = computed(() => notificationStore.notifications.length)

const semesterOpts = [...semesterOptions]

const periodOptions = computed(() =>
  periodStore.periods.map((p) => ({
    label: p.name,
    value: p.id,
    active: p.status === 'Active',
  })),
)

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

onMounted(async () => {
  if (authStore.isAuthenticated && !periodStore.isLoaded) {
    await periodStore.loadPeriods()
  }
})

async function onLogout() {
  await logout()
}
</script>
