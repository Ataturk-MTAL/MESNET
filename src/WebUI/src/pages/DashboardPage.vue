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
import { ref, reactive, computed, onMounted } from 'vue'
import { use } from 'echarts/core'
import { PieChart, BarChart } from 'echarts/charts'
import { TitleComponent, TooltipComponent, LegendComponent, GridComponent } from 'echarts/components'
import { SVGRenderer } from 'echarts/renderers'
import VChart from 'vue-echarts'
import { useAuthStore } from 'stores/auth'
import { useNotificationStore } from 'stores/notifications'
import { Permissions } from 'utils/permissions'
import { enrollmentApi } from 'src/api/enrollment'
import { businessApi } from 'src/api/business'
import { contractApi } from 'src/api/contract'
import { attendanceApi } from 'src/api/attendance'
import { institutionApi } from 'src/api/institution'
import { securityApi } from 'src/api/security'
import type { EChartsOption } from 'echarts'

// ECharts tree-shaking
use([PieChart, BarChart, TitleComponent, TooltipComponent, LegendComponent, GridComponent, SVGRenderer])

const authStore = useAuthStore()
const notificationStore = useNotificationStore()

const institutionId = authStore.user?.institutionId ?? ''
const institutionName = ref('')

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

// Stats
const stats = reactive({
  students: 0,
  studentsLoading: true,
  businesses: 0,
  businessesLoading: true,
  activeContracts: 0,
  contractsLoading: true,
  pendingTotal: 0,
  pendingLoading: true,
})

// Chart data
const studentChartOption = ref<EChartsOption | null>(null)
const contractChartOption = ref<EChartsOption | null>(null)

// Raw data holders for chart generation
const allStudents = ref<{ status: string }[]>([])
const allContracts = ref<{ status: string }[]>([])

// Module icons
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
}

// Event labels
const EVENT_LABELS: Record<string, string> = {
  'contract.created': 'Yeni sözleşme oluşturuldu',
  'contract.submitted': 'Sözleşme imzaya gönderildi',
  'contract.signed': 'Sözleşme imzalandı',
  'contract.activated': 'Sözleşme aktifleştirildi',
  'contract.suspended': 'Sözleşme askıya alındı',
  'contract.terminated': 'Sözleşme feshedildi',
  'contract.completed': 'Sözleşme tamamlandı',
  'attendance.recorded': 'Devamsızlık kaydedildi',
  'attendance.verified': 'Devamsızlık doğrulandı',
  'attendance.corrected': 'Devamsızlık düzeltildi',
  'payment.calculated': 'Maaş hesaplandı',
  'payment.receipt.uploaded': 'Dekont yüklendi',
  'payment.approved': 'Ödeme onaylandı',
  'business.registered': 'Yeni işletme kaydedildi',
  'business.approved': 'İşletme onaylandı',
  'student.registered': 'Öğrenci kaydedildi',
  'placement.created': 'Öğrenci yerleştirildi',
  'placement.transferred': 'Öğrenci transfer edildi',
  'visit.created': 'Rehberlik ziyareti eklendi',
  'visit.approved': 'Ziyaret onaylandı',
  'evaluation.created': 'İşletme değerlendirmesi eklendi',
  'invitation.created': 'Yeni davet gönderildi',
  'invitation.approved': 'Davet onaylandı',
}

function eventLabel(eventType: string): string {
  return EVENT_LABELS[eventType] ?? eventType
}

function timeAgo(iso: string): string {
  const diff = Date.now() - new Date(iso).getTime()
  const mins = Math.floor(diff / 60000)
  if (mins < 1) return 'Az önce'
  if (mins < 60) return `${mins} dk önce`
  const hours = Math.floor(mins / 60)
  if (hours < 24) return `${hours} sa önce`
  const days = Math.floor(hours / 24)
  return `${days} gün önce`
}

// Recent notifications (max 8)
const recentNotifications = computed(() => notificationStore.notifications.slice(0, 8))

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

// Status label maps
const STUDENT_STATUS_LABELS: Record<string, string> = {
  Registered: 'Kayıtlı',
  Applied: 'Başvurdu',
  Placed: 'Yerleştirildi',
  ActiveInternship: 'Aktif Staj',
  Completed: 'Tamamladı',
}

const STUDENT_STATUS_COLORS: Record<string, string> = {
  Registered: '#9E9E9E',
  Applied: '#2196F3',
  Placed: '#9C27B0',
  ActiveInternship: '#4CAF50',
  Completed: '#009688',
}

const CONTRACT_STATUS_LABELS: Record<string, string> = {
  Draft: 'Taslak',
  AwaitingSignature: 'İmza Bekliyor',
  Active: 'Aktif',
  Suspended: 'Askıda',
  Terminated: 'Feshedildi',
  Completed: 'Tamamlandı',
}

const CONTRACT_STATUS_COLORS: Record<string, string> = {
  Draft: '#9E9E9E',
  AwaitingSignature: '#FF9800',
  Active: '#4CAF50',
  Suspended: '#FF5722',
  Terminated: '#F44336',
  Completed: '#009688',
}

// Chart builders
function buildStudentChart() {
  const grouped: Record<string, number> = {}
  for (const s of allStudents.value) {
    grouped[s.status] = (grouped[s.status] ?? 0) + 1
  }

  const data = Object.entries(grouped).map(([status, count]) => ({
    name: STUDENT_STATUS_LABELS[status] ?? status,
    value: count,
    itemStyle: { color: STUDENT_STATUS_COLORS[status] ?? '#607D8B' },
  }))

  if (data.length === 0) return

  studentChartOption.value = {
    tooltip: { trigger: 'item', formatter: '{b}: {c} ({d}%)' },
    legend: { bottom: 0, left: 'center' },
    series: [{
      type: 'pie',
      radius: ['45%', '70%'],
      center: ['50%', '45%'],
      avoidLabelOverlap: true,
      label: { show: false },
      emphasis: { label: { show: true, fontWeight: 'bold' } },
      data,
    }],
  }
}

function buildContractChart() {
  const grouped: Record<string, number> = {}
  for (const c of allContracts.value) {
    grouped[c.status] = (grouped[c.status] ?? 0) + 1
  }

  const order = ['Draft', 'AwaitingSignature', 'Active', 'Suspended', 'Terminated', 'Completed']
  const categories: string[] = []
  const values: number[] = []
  const colors: string[] = []

  for (const status of order) {
    if (grouped[status]) {
      categories.push(CONTRACT_STATUS_LABELS[status] ?? status)
      values.push(grouped[status])
      colors.push(CONTRACT_STATUS_COLORS[status] ?? '#607D8B')
    }
  }

  if (categories.length === 0) return

  contractChartOption.value = {
    tooltip: { trigger: 'axis', axisPointer: { type: 'shadow' } },
    grid: { left: 100, right: 30, top: 10, bottom: 20 },
    xAxis: { type: 'value', minInterval: 1 },
    yAxis: { type: 'category', data: categories },
    series: [{
      type: 'bar',
      data: values.map((v, i) => ({ value: v, itemStyle: { color: colors[i] } })),
      barMaxWidth: 30,
    }],
  }
}

// Data loaders
async function loadStudents() {
  try {
    const res = await enrollmentApi.listStudents({ pageSize: 100 })
    const data = res.data?.items ?? []
    stats.students = res.data?.totalCount ?? 0
    allStudents.value = data
    buildStudentChart()
  } catch { /* sessiz */ }
  stats.studentsLoading = false
}

async function loadBusinesses() {
  try {
    const res = await businessApi.list({ status: 'Approved', pageSize: 1 })
    stats.businesses = res.data?.totalCount ?? 0
  } catch { /* sessiz */ }
  stats.businessesLoading = false
}

async function loadContracts() {
  try {
    const res = await contractApi.list({ pageSize: 100 })
    const data = res.data?.items ?? []
    stats.activeContracts = data.filter((c: { status: string }) => c.status === 'Active').length
    allContracts.value = data
    buildContractChart()
  } catch { /* sessiz */ }
  stats.contractsLoading = false
}

async function loadPendingActions() {
  let total = 0
  const tasks: Promise<void>[] = []

  if (authStore.hasPermission(Permissions.Internship.Contract)) {
    tasks.push(
      contractApi.list({ status: 'AwaitingSignature', pageSize: 1 })
        .then((res) => { total += res.data?.totalCount ?? 0 })
        .catch(() => {}),
    )
  }

  if (authStore.hasPermission(Permissions.Attendance.View)) {
    tasks.push(
      attendanceApi.list({ status: 'Recorded', pageSize: 1 })
        .then((res) => { total += res.data?.totalCount ?? 0 })
        .catch(() => {}),
    )
  }

  if (authStore.hasPermission(Permissions.Company.View)) {
    tasks.push(
      businessApi.list({ status: 'PendingApproval', pageSize: 1 })
        .then((res) => { total += res.data?.totalCount ?? 0 })
        .catch(() => {}),
    )
  }

  if (authStore.hasPermission(Permissions.UserManagement.View)) {
    tasks.push(
      securityApi.listInvitations({ status: 'Pending', pageSize: 1 })
        .then((res) => { total += res.data?.totalCount ?? 0 })
        .catch(() => {}),
    )
  }

  await Promise.allSettled(tasks)
  stats.pendingTotal = total
  stats.pendingLoading = false
}

async function loadInstitution() {
  try {
    const res = await institutionApi.get(institutionId)
    institutionName.value = res.data?.fullName ?? ''
  } catch { /* sessiz */ }
}

onMounted(async () => {
  const tasks: Promise<void>[] = []

  if (authStore.hasPermission(Permissions.Student.View)) tasks.push(loadStudents())
  if (authStore.hasPermission(Permissions.Company.View)) tasks.push(loadBusinesses())
  if (authStore.hasPermission(Permissions.Internship.Contract)) tasks.push(loadContracts())
  tasks.push(loadPendingActions())
  if (authStore.hasPermission(Permissions.Institution.View) && institutionId) tasks.push(loadInstitution())

  await Promise.allSettled(tasks)
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
