<template>
  <q-page padding>
    <!-- Hoş Geldin -->
    <div class="row items-center q-mb-lg">
      <div class="col">
        <div class="text-h5 text-weight-bold">{{ greeting }}</div>
        <div class="text-caption text-grey">
          {{ institutionName || '' }}{{ institutionName ? ' · ' : '' }}{{ todayFormatted }}
        </div>
      </div>
    </div>

    <!-- Özet Kartları -->
    <div class="row q-col-gutter-md q-mb-lg">
      <div v-if="authStore.hasPermission(Permissions.Student.View)" class="col-12 col-sm-6 col-md-3">
        <q-card flat bordered class="cursor-pointer stat-card" @click="$router.push('/enrollment/students')">
          <q-card-section class="row items-center no-wrap">
            <q-icon name="school" size="40px" color="primary" class="q-mr-md" />
            <div>
              <q-skeleton v-if="stats.studentsLoading" type="text" width="60px" />
              <div v-else class="text-h4 text-weight-bold text-primary">{{ stats.students }}</div>
              <div class="text-caption text-grey">Öğrenci</div>
            </div>
          </q-card-section>
        </q-card>
      </div>

      <div v-if="authStore.hasPermission(Permissions.Company.View)" class="col-12 col-sm-6 col-md-3">
        <q-card flat bordered class="cursor-pointer stat-card" @click="$router.push('/companies')">
          <q-card-section class="row items-center no-wrap">
            <q-icon name="business" size="40px" color="teal" class="q-mr-md" />
            <div>
              <q-skeleton v-if="stats.businessesLoading" type="text" width="60px" />
              <div v-else class="text-h4 text-weight-bold text-teal">{{ stats.businesses }}</div>
              <div class="text-caption text-grey">Aktif İşletme</div>
            </div>
          </q-card-section>
        </q-card>
      </div>

      <div v-if="authStore.hasPermission(Permissions.Internship.Contract)" class="col-12 col-sm-6 col-md-3">
        <q-card flat bordered class="cursor-pointer stat-card" @click="$router.push('/internship/contracts')">
          <q-card-section class="row items-center no-wrap">
            <q-icon name="description" size="40px" color="green" class="q-mr-md" />
            <div>
              <q-skeleton v-if="stats.contractsLoading" type="text" width="60px" />
              <div v-else class="text-h4 text-weight-bold text-green">{{ stats.activeContracts }}</div>
              <div class="text-caption text-grey">Aktif Sözleşme</div>
            </div>
          </q-card-section>
        </q-card>
      </div>

      <div class="col-12 col-sm-6 col-md-3">
        <q-card flat bordered class="stat-card">
          <q-card-section class="row items-center no-wrap">
            <q-icon name="pending_actions" size="40px" color="orange" class="q-mr-md" />
            <div>
              <q-skeleton v-if="stats.pendingLoading" type="text" width="60px" />
              <div v-else class="text-h4 text-weight-bold text-orange">{{ stats.pendingTotal }}</div>
              <div class="text-caption text-grey">Bekleyen İşlem</div>
            </div>
          </q-card-section>
        </q-card>
      </div>
    </div>

    <!-- Grafikler -->
    <div class="row q-col-gutter-md q-mb-lg">
      <div v-if="authStore.hasPermission(Permissions.Student.View) && studentChartOption" class="col-12 col-md-6">
        <q-card flat bordered>
          <q-card-section>
            <div class="text-subtitle1 text-weight-medium q-mb-sm">Öğrenci Durum Dağılımı</div>
            <v-chart :option="studentChartOption" :init-options="{ renderer: 'svg' }" autoresize style="height: 280px" />
          </q-card-section>
        </q-card>
      </div>

      <div v-if="authStore.hasPermission(Permissions.Internship.Contract) && contractChartOption" class="col-12 col-md-6">
        <q-card flat bordered>
          <q-card-section>
            <div class="text-subtitle1 text-weight-medium q-mb-sm">Sözleşme Durumları</div>
            <v-chart :option="contractChartOption" :init-options="{ renderer: 'svg' }" autoresize style="height: 280px" />
          </q-card-section>
        </q-card>
      </div>
    </div>

    <!-- Alt Satır: Son Aktiviteler + Hızlı Erişim -->
    <div class="row q-col-gutter-md">
      <!-- Son Aktiviteler -->
      <div class="col-12 col-md-6">
        <q-card flat bordered>
          <q-card-section>
            <div class="text-subtitle1 text-weight-medium q-mb-sm">Son Aktiviteler</div>
            <q-list v-if="recentNotifications.length > 0" separator>
              <q-item v-for="(n, i) in recentNotifications" :key="i" dense>
                <q-item-section avatar>
                  <q-icon :name="MODULE_ICONS[n.module] ?? 'info'" color="grey-6" />
                </q-item-section>
                <q-item-section>
                  <q-item-label>{{ eventLabel(n.eventType) }}</q-item-label>
                  <q-item-label caption>{{ timeAgo(n.occurredAt) }}</q-item-label>
                </q-item-section>
              </q-item>
            </q-list>
            <div v-else class="text-body2 text-grey q-pa-md text-center">
              Henüz aktivite yok
            </div>
          </q-card-section>
        </q-card>
      </div>

      <!-- Hızlı Erişim -->
      <div class="col-12 col-md-6">
        <q-card flat bordered>
          <q-card-section>
            <div class="text-subtitle1 text-weight-medium q-mb-sm">Hızlı Erişim</div>
            <div class="row q-gutter-sm">
              <div v-for="link in quickLinks" :key="link.route" class="col-5">
                <q-card
                  flat
                  bordered
                  class="cursor-pointer q-pa-sm text-center quick-link"
                  @click="$router.push(link.route)"
                >
                  <q-icon :name="link.icon" size="28px" :color="link.color" />
                  <div class="text-caption text-weight-medium q-mt-xs">{{ link.label }}</div>
                </q-card>
              </div>
            </div>
          </q-card-section>
        </q-card>
      </div>
    </div>
  </q-page>
</template>

<script setup lang="ts">
import { computed, onMounted } from 'vue'
import { use } from 'echarts/core'
import { PieChart, BarChart } from 'echarts/charts'
import { TitleComponent, TooltipComponent, LegendComponent, GridComponent } from 'echarts/components'
import { SVGRenderer } from 'echarts/renderers'
import VChart from 'vue-echarts'
import { useAuthStore } from 'stores/auth'
import { useNotificationStore } from 'stores/notifications'
import { Permissions } from 'utils/permissions'
import { useDashboardStats } from 'src/composables/useDashboardStats'
import { useDashboardActivity } from 'src/composables/useDashboardActivity'

// ECharts tree-shaking
use([PieChart, BarChart, TitleComponent, TooltipComponent, LegendComponent, GridComponent, SVGRenderer])

const authStore = useAuthStore()
const notificationStore = useNotificationStore()

const institutionId = authStore.user?.institutionId ?? ''

// Greeting
const greeting = computed(() => {
  const name = authStore.user?.fullName ?? 'Kullanıcı'
  return `Hoş geldiniz, ${name}!`
})

const todayFormatted = new Date().toLocaleDateString('tr-TR', {
  weekday: 'long',
  day: 'numeric',
  month: 'long',
  year: 'numeric',
})

// Stats, charts ve veri yükleme
const { institutionName, stats, studentChartOption, contractChartOption, init } = useDashboardStats({
  authStore,
  institutionId,
})

// Son aktiviteler (bildirim akışı)
const { MODULE_ICONS, recentNotifications, eventLabel, timeAgo } = useDashboardActivity({
  notificationStore,
})

// Quick links — permission-filtered
const quickLinks = computed(() => {
  const links = [
    { label: 'Öğrenciler', icon: 'school', color: 'primary', route: '/enrollment/students', permission: Permissions.Student.View },
    { label: 'Sözleşmeler', icon: 'description', color: 'green', route: '/internship/contracts', permission: Permissions.Internship.Contract },
    { label: 'Devamsızlık', icon: 'event_available', color: 'orange', route: '/attendance', permission: Permissions.Attendance.View },
    { label: 'Maaş / Dekont', icon: 'payments', color: 'purple', route: '/salary', permission: Permissions.Salary.View },
    { label: 'Koordinasyon', icon: 'supervisor_account', color: 'indigo', route: '/coordination', permission: Permissions.Coordinator.Visit },
    { label: 'Raporlar', icon: 'bar_chart', color: 'blue-grey', route: '/reporting', permission: Permissions.Internship.Report },
  ]
  return links.filter((l) => authStore.hasPermission(l.permission))
})

onMounted(async () => {
  await init()
})
</script>

<style scoped>
.stat-card:hover {
  background-color: rgba(0, 0, 0, 0.02);
  transition: background-color 0.2s;
}

.quick-link:hover {
  background-color: rgba(0, 0, 0, 0.03);
  transition: background-color 0.2s;
}
</style>
