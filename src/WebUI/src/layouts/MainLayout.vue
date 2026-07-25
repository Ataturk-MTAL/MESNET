<template>
  <q-layout view="lHh Lpr lFf">
    <q-header elevated>
      <q-toolbar>
        <q-btn
          flat
          dense
          round
          icon="menu"
          aria-label="Menüyü aç/kapat"
          @click="drawerOpen = !drawerOpen"
        />
        <q-toolbar-title>MESNET</q-toolbar-title>
        <q-space />
        <span
          v-if="authStore.user"
          class="text-body2 q-mr-md"
        >
          {{ authStore.user.fullName }}
        </span>
        <q-btn
          flat
          round
          dense
          icon="notifications"
          aria-label="Bildirimler"
          class="q-mr-xs"
        >
          <q-badge
            v-if="unreadCount > 0"
            color="negative"
            floating
          >
            {{ unreadCount }}
          </q-badge>
          <q-tooltip>Bildirimler</q-tooltip>
          <q-menu
            anchor="bottom right"
            self="top right"
            style="min-width: 320px; max-width: 400px"
            @hide="notificationStore.markAllRead()"
          >
            <q-list separator>
              <q-item
                v-if="notificationStore.notifications.length === 0"
                dense
              >
                <q-item-section class="text-grey text-caption text-center q-pa-md">
                  Bildirim yok
                </q-item-section>
              </q-item>
              <q-item
                v-for="(n, i) in notificationStore.notifications.slice(0, 10)"
                :key="n.id"
                dense
                :class="{ 'bg-blue-1': !n.read }"
              >
                <q-item-section avatar>
                  <q-icon
                    :name="moduleIcon(n.module)"
                    color="primary"
                    size="sm"
                  />
                </q-item-section>
                <q-item-section>
                  <q-item-label class="text-caption text-weight-medium">
                    {{ eventLabel(n.eventType) }}
                  </q-item-label>
                  <q-item-label
                    caption
                    class="text-grey"
                  >
                    {{ timeAgo(n.occurredAt) }}
                  </q-item-label>
                </q-item-section>
                <q-item-section side>
                  <q-btn
                    flat
                    round
                    dense
                    size="xs"
                    icon="close"
                    aria-label="Bildirimi kaldır"
                    @click.stop="notificationStore.remove(i)"
                  />
                </q-item-section>
              </q-item>
              <q-item
                v-if="notificationStore.notifications.length > 0"
                dense
                clickable
                @click="notificationStore.clear()"
              >
                <q-item-section class="text-center text-caption text-grey">
                  Tümünü temizle
                </q-item-section>
              </q-item>
            </q-list>
          </q-menu>
        </q-btn>
        <q-btn
          flat
          round
          dense
          icon="logout"
          aria-label="Çıkış yap"
          @click="onLogout"
        />
      </q-toolbar>
    </q-header>

    <q-drawer
      v-model="drawerOpen"
      show-if-above
      bordered
    >
      <q-scroll-area class="fit">
        <q-list padding>
          <!-- Dönem Seçici -->
          <q-item
            v-if="periodStore.isLoaded && periodStore.periods.length > 0"
            class="q-mb-xs"
          >
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

          <q-separator
            v-if="periodStore.isLoaded && periodStore.periods.length > 0"
            spaced
          />

          <!-- Kapalı dönem uyarısı -->
          <AppNotice
            v-if="periodStore.isReadOnly"
            type="readonly"
            dense
            message="Geçmiş dönem — salt okunur"
            class="q-mx-sm q-mb-sm text-caption"
          />

          <template v-for="group in filteredMenu">
            <!-- Düz link (child yok veya tek child → terfi) -->
            <q-item
              v-if="group.to"
              :key="group.key"
              v-ripple
              clickable
              :to="group.to"
              :active="activeGroupKey === group.key"
            >
              <q-item-section avatar>
                <q-icon :name="group.icon" />
              </q-item-section>
              <q-item-section>{{ group.title }}</q-item-section>
            </q-item>

            <!-- Genişletilebilir grup -->
            <q-expansion-item
              v-else
              :key="'g-' + group.key"
              :icon="group.icon"
              :label="group.title"
              :model-value="isExpanded(group.key)"
              :header-class="activeGroupKey === group.key ? 'text-primary' : ''"
              dense-toggle
              @update:model-value="toggleGroup(group.key)"
            >
              <q-item
                v-for="item in group.children"
                :key="item.to.name"
                v-ripple
                clickable
                :to="item.to"
                :inset-level="1"
                dense
              >
                <q-item-section avatar>
                  <q-icon
                    :name="item.icon"
                    size="sm"
                  />
                </q-item-section>
                <q-item-section>{{ item.title }}</q-item-section>
              </q-item>
            </q-expansion-item>
          </template>

          <q-separator spaced />

          <q-item
            v-ripple
            clickable
            @click="aboutDialog = true"
          >
            <q-item-section avatar>
              <q-icon name="info" />
            </q-item-section>
            <q-item-section>Hakkında</q-item-section>
          </q-item>
        </q-list>
      </q-scroll-area>
    </q-drawer>

    <q-page-container>
      <router-view v-slot="{ Component }">
        <transition
          :name="transitionName"
          mode="out-in"
        >
          <component :is="Component" />
        </transition>
      </router-view>
    </q-page-container>

    <!-- Hakkında Dialog -->
    <DetailDialog
      v-model="aboutDialog"
      title="Hakkında"
      card-style="min-width: 400px; max-width: 500px"
    >
      <q-card-section class="text-center q-pt-md">
        <q-icon
          name="school"
          color="primary"
          size="64px"
          class="q-mb-md"
        />
        <div class="text-h5 text-weight-bold text-primary q-mb-xs">
          MESNET
        </div>
        <div class="text-subtitle2 text-grey-8 q-mb-lg">
          Mesleki Eğitim Stajları Nitelikli, Eşgüdümlü Takip Sistemi
        </div>

        <q-separator class="q-my-md" />

        <div class="text-body2 q-mb-md">
          Bu yazılım<br>
          <strong>Toroslar Atatürk Mesleki ve Teknik Anadolu Lisesi</strong><br>
          <strong>Elektrik-Elektronik Teknolojisi</strong> alan öğretmenleri<br>
          tarafından hazırlanmıştır.
        </div>

        <q-separator class="q-my-md" />

        <div class="row justify-center q-gutter-x-md text-caption text-grey-7">
          <div>
            <q-icon
              name="tag"
              size="xs"
              class="q-mr-xs"
            />
            Sürüm: <strong>{{ appVersion }}</strong>
          </div>
        </div>
      </q-card-section>

      <q-card-section class="text-center text-caption text-grey-6 q-pt-none">
        &copy; {{ currentYear }} MESNET — Tüm hakları saklıdır.
      </q-card-section>
    </DetailDialog>
  </q-layout>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from 'stores/auth'
import { useNotificationStore } from 'stores/notifications'
import { useAcademicPeriodStore, semesterOptions } from 'stores/academicPeriod'
import { logout } from 'boot/auth'
import { useNavigation } from 'src/composables/useNavigation'
import { eventLabel, timeAgo } from 'src/utils/notificationFormat'
import AppNotice from 'components/AppNotice.vue'
import DetailDialog from 'components/DetailDialog.vue'

const authStore = useAuthStore()
const { filteredMenu, isExpanded, toggleGroup, activeGroupKey } = useNavigation()
const notificationStore = useNotificationStore()
const periodStore = useAcademicPeriodStore()

// Sayfa geçişi yönü: form route'una giriş → liste sola kayar/form sağdan girer; çıkış → tersi; diğer nav → fade
const router = useRouter()
const transitionName = ref('page')
router.beforeEach((to, from) => {
  if (to.meta.formRoute) transitionName.value = 'slide-left'
  else if (from.meta.formRoute) transitionName.value = 'slide-right'
  else transitionName.value = 'page'
})
const drawerOpen = ref(false)
const aboutDialog = ref(false)
const appVersion = '0.1.0'
const currentYear = new Date().getFullYear()

const unreadCount = computed(() => notificationStore.unreadCount)

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

<style>
/* Sayfalar arası ince geçiş — daha akıcı navigasyon (mode=out-in: çakışmasız) */
.page-enter-active,
.page-leave-active {
  transition:
    opacity 0.16s ease,
    transform 0.16s ease;
}
.page-enter-from {
  opacity: 0;
  transform: translateY(8px);
}
.page-leave-to {
  opacity: 0;
}

/* Yönlü kayma — form route'larına giriş/çıkış (liste sola kayar, form sağdan girer) */
.slide-left-enter-active,
.slide-left-leave-active,
.slide-right-enter-active,
.slide-right-leave-active {
  transition:
    transform 0.25s ease,
    opacity 0.25s ease;
}
.slide-left-enter-from {
  transform: translateX(40px);
  opacity: 0;
}
.slide-left-leave-to {
  transform: translateX(-40px);
  opacity: 0;
}
.slide-right-enter-from {
  transform: translateX(-40px);
  opacity: 0;
}
.slide-right-leave-to {
  transform: translateX(40px);
  opacity: 0;
}
</style>
