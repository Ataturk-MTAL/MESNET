<template>
  <q-page padding>
    <!-- Hoş Geldin -->
    <PageHeader
      :title="greeting"
      :subtitle="headerSubtitle"
    />

    <!--
      Müdürlük bağlamında okul panosu YANLIŞ veri değil, BOŞ veri gösterir: il/ilçe düğümü
      kiracı değildir ve kiracı damgalı belgelere attığı her sorgu boş döner. Bu yüzden
      dallanma bir tercih değil, doğruluk gereğidir.
    -->
    <DirectorateDashboard v-if="isDirectorate" />

    <template v-else>
      <!-- Özet Kartları -->
      <div class="row q-col-gutter-md q-mb-lg">
        <div
          v-if="authStore.hasPermission(Permissions.Student.View)"
          class="col-12 col-sm-6 col-md-3"
        >
          <StatCard
            icon="school"
            :value="stats.students"
            label="Öğrenci"
            color="primary"
            :loading="stats.studentsLoading"
            to="/enrollment/students"
          />
        </div>

        <div
          v-if="authStore.hasPermission(Permissions.Company.View)"
          class="col-12 col-sm-6 col-md-3"
        >
          <StatCard
            icon="business"
            :value="stats.businesses"
            label="Aktif İşletme"
            color="secondary"
            :loading="stats.businessesLoading"
            to="/companies"
          />
        </div>

        <div
          v-if="authStore.hasPermission(Permissions.Internship.Contract)"
          class="col-12 col-sm-6 col-md-3"
        >
          <StatCard
            icon="description"
            :value="stats.activeContracts"
            label="Aktif Sözleşme"
            color="positive"
            :loading="stats.contractsLoading"
            to="/internship/contracts"
          />
        </div>

        <!--
          "SIRA SİZDE" — bu ekrandaki TEK hardal bağlam (Tek Ses Kuralı).
          Diğer sayaçlar primary/secondary/positive, hızlı erişim primary/secondary/info,
          grafikler statusTone.* (bekleyen = uyarı hardalı #9A6B00) kullanır; hiçbiri
          Resmî Hardal değildir. Sinyal yalnız burada yanar.

          Sinyal SAYACA değil, kaydı İLERLETME yetkisine bağlıdır — bkz. isMyTurnPending.
          Sayacı dolduran izinler kuyruğu yalnız AÇAR; onlara bağlansaydı rozet, onay
          yetkisi olmayan kullanıcıda hiç sönmez ve tıklayınca 403 alınırdı (ölçüm ve
          kaynak satırları PENDING_QUEUES yorumunda). Yetkisi olmayanda kart bugünkü
          nötr `warning` görünümüne düşer.

          `color` artık koşulsuz "warning": tone="accent" iken ikonu da StatCard'ın
          kendisi accent-strong'a çevirir, iki prop'un elle senkron tutulması gerekmez.
        -->
        <div class="col-12 col-sm-6 col-md-3">
          <StatCard
            icon="pending_actions"
            :value="stats.pendingTotal"
            label="Bekleyen İşlem"
            color="warning"
            :tone="isMyTurnPending ? 'accent' : undefined"
            :loading="stats.pendingLoading"
            :to="pendingQueueRoute"
          />
        </div>
      </div>

      <!-- Grafikler -->
      <div class="row q-col-gutter-md q-mb-lg">
        <div
          v-if="authStore.hasPermission(Permissions.Student.View) && studentChartOption"
          class="col-12 col-md-6"
        >
          <q-card
            flat
            bordered
          >
            <q-card-section>
              <div class="text-subtitle1 text-weight-medium q-mb-sm">
                Öğrenci Durum Dağılımı
              </div>
              <v-chart
                :option="studentChartOption"
                :init-options="{ renderer: 'svg' }"
                autoresize
                style="height: 280px"
              />
            </q-card-section>
          </q-card>
        </div>

        <div
          v-if="authStore.hasPermission(Permissions.Internship.Contract) && contractChartOption"
          class="col-12 col-md-6"
        >
          <q-card
            flat
            bordered
          >
            <q-card-section>
              <div class="text-subtitle1 text-weight-medium q-mb-sm">
                Sözleşme Durumları
              </div>
              <v-chart
                :option="contractChartOption"
                :init-options="{ renderer: 'svg' }"
                autoresize
                style="height: 280px"
              />
            </q-card-section>
          </q-card>
        </div>
      </div>

      <!-- Alt Satır: Son Aktiviteler + Hızlı Erişim -->
      <div class="row q-col-gutter-md">
        <!-- Son Aktiviteler -->
        <div class="col-12 col-md-6">
          <q-card
            flat
            bordered
          >
            <q-card-section>
              <div class="text-subtitle1 text-weight-medium q-mb-sm">
                Son Aktiviteler
              </div>
              <DataState
                :empty="recentNotifications.length === 0"
                empty-icon="notifications_none"
                empty-text="Henüz aktivite yok"
                padding="q-pa-md"
              >
                <q-list separator>
                  <q-item
                    v-for="n in recentNotifications"
                    :key="n.id"
                    dense
                  >
                    <q-item-section avatar>
                      <!--
                        İkon modül bilgisini taşıyor: yanındaki eventLabel(n.eventType)
                        metni modül adını tekrarlamıyor (ör. "Rehberlik ziyareti eklendi"
                        Coordination modülünü söylemez), yani dekoratif değil — WCAG 1.4.11
                        grafik nesnesi eşiği 3:1 geçerli. grey-6 (#9e9e9e) beyaz zeminde
                        2,68:1 ile eşiğin altındaydı; grey-7 (#757575) 4,61:1.
                      -->
                      <q-icon
                        :name="MODULE_ICONS[n.module] ?? 'info'"
                        color="grey-7"
                      />
                    </q-item-section>
                    <q-item-section>
                      <q-item-label>{{ eventLabel(n.eventType) }}</q-item-label>
                      <q-item-label caption>
                        {{ timeAgo(n.occurredAt) }}
                      </q-item-label>
                    </q-item-section>
                  </q-item>
                </q-list>
              </DataState>
            </q-card-section>
          </q-card>
        </div>

        <!-- Hızlı Erişim -->
        <div class="col-12 col-md-6">
          <q-card
            flat
            bordered
          >
            <q-card-section>
              <div class="text-subtitle1 text-weight-medium q-mb-sm">
                Hızlı Erişim
              </div>
              <div class="row q-gutter-sm">
                <div
                  v-for="link in quickLinks"
                  :key="link.route"
                  class="col-5"
                >
                  <q-card
                    flat
                    bordered
                    role="link"
                    tabindex="0"
                    :aria-label="link.label"
                    class="cursor-pointer q-pa-sm text-center quick-link"
                    @click="$router.push(link.route)"
                    @keydown.enter.prevent="$router.push(link.route)"
                    @keydown.space.prevent="$router.push(link.route)"
                  >
                    <q-icon
                      :name="link.icon"
                      size="28px"
                      :color="link.color"
                    />
                    <div class="text-caption text-weight-medium q-mt-xs">
                      {{ link.label }}
                    </div>
                  </q-card>
                </div>
              </div>
            </q-card-section>
          </q-card>
        </div>
      </div>
    </template>
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
import { useAcademicPeriodStore } from 'stores/academicPeriod'
import { useInstitutionStore } from 'stores/institution'
import { Permissions } from 'utils/permissions'
import { isActingAsDirectorate } from 'utils/directorateContext'
import { useDashboardStats } from 'src/composables/useDashboardStats'
import { useDashboardActivity } from 'src/composables/useDashboardActivity'
import DataState from 'components/DataState.vue'
import PageHeader from 'components/PageHeader.vue'
import StatCard from 'components/StatCard.vue'
import DirectorateDashboard from 'pages/dashboard/DirectorateDashboard.vue'

// ECharts tree-shaking
use([PieChart, BarChart, TitleComponent, TooltipComponent, LegendComponent, GridComponent, SVGRenderer])

const authStore = useAuthStore()
const notificationStore = useNotificationStore()
// Yalnız okunur: dönemler MainLayout'ta bir kez yükleniyor, burada yeni istek yok.
const periodStore = useAcademicPeriodStore()
const institutionStore = useInstitutionStore()

/**
 * `resolveIsUpperNode` DEĞİL: il yetkilisi bir okula geçtiğinde kiracısı o okuldur ve okul
 * panosunu görmelidir. Fark `directorateContext.spec.ts` içinde kilitlidir.
 */
const isDirectorate = computed(() => isActingAsDirectorate(institutionStore.institution?.nodeType))

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

// Alt başlık: kurum adı varsa tarihle ayraçla birleşir, yoksa yalnız tarih.
const headerSubtitle = computed(() =>
  institutionName.value ? `${institutionName.value} · ${todayFormatted}` : todayFormatted,
)

// Son aktiviteler (bildirim akışı)
const { MODULE_ICONS, recentNotifications, eventLabel, timeAgo } = useDashboardActivity({
  notificationStore,
})

/*
 * Bekleyen iş kuyrukları. Her satırda İKİ AYRI izin vardır ve karıştırılmamalıdır:
 *
 *   permission → kuyruğu AÇAN (sayan) izin. useDashboardStats.loadPendingActions()
 *                (useDashboardStats.ts:177-217) ile BİREBİR aynıdır; sayacı hangi izin
 *                dolduruyorsa rota da ona bakar, başka bir rota uydurulmaz.
 *   action     → kaydı İLERLETEN uç noktanın istediği izin. Kaynaktan okundu:
 *                POST /api/attendance/{id}/approve → attendance:approve
 *                  (AttendanceEndpoints.cs:40)
 *                POST /api/businesses/{id}/approve → company:manage
 *                  (BusinessEndpoints.cs:24)
 *                POST /api/invitations/{id}/approve → user:approve
 *                  (InvitationEndpoints.cs:21)
 *                POST /api/contracts/{id}/sign → internship:contract:manage
 *                  (ContractEndpoints.cs:23) — yalnız bu satırda ikisi aynı izindir.
 *
 * NEDEN AYRI: okuma izni zincirin her fazında açıktır, ilerletme izni değildir.
 * RolePermissionMap.cs'ten ölçüldü — DepartmentHead `attendance:view` + `attendance:delete`
 * taşır, `attendance:approve` TAŞIMAZ; InstitutionStaff `attendance:view` + `company:view`
 * taşır, ikisinin de onay iznini taşımaz (dosyanın kendi yorumu: "yürütür, onaylamaz",
 * #129); CompanyHR bu dört kuyruk izninden yalnız `company:view`'ü taşır, `company:manage`
 * yoktur. Sinyal `permission`a bağlansaydı bu üç
 * profilde HİÇ sönmez, tıklayınca 403 alınırdı. Depo bu hataya bir kez daha çarpmış:
 * router/index.ts:158 "Alan şefi (attendance:view var, manage yok) bu formu açıp
 * Kaydet'te 403 alıyordu".
 *
 * Rotalar sorgu parametresi taşımaz: hiçbir liste sayfası `route.query`den durum filtresi
 * okumuyor (doğrulandı), uydurulmuş bir `?status=` sessizce hiçbir şey yapmazdı.
 */
const PENDING_QUEUES = [
  {
    permission: Permissions.Internship.Contract,
    action: Permissions.Internship.Contract,
    route: '/internship/contracts',
  },
  {
    permission: Permissions.Attendance.View,
    action: Permissions.Attendance.Approve,
    route: '/attendance',
  },
  {
    permission: Permissions.Company.View,
    action: Permissions.Company.Manage,
    route: '/companies',
  },
  {
    permission: Permissions.UserManagement.View,
    action: Permissions.UserManagement.Approve,
    route: '/admin/users',
  },
] as const

/*
 * Kartın hedefi: izinli İLK kuyruk. Sıra "en dolu kuyruk" DEĞİL, composable'ın kendi
 * toplama sırasıdır; başında imza bekleyen sözleşme durur (İmza Zinciri'nin ilk halkası).
 *
 * BİLİNEN BORÇ — hedef BOŞ bir kuyruk olabilir: useDashboardStats dışarıya yalnız
 * `pendingTotal` veriyor, kırılımı vermiyor (dört sayı yerel `total`de toplanıyor,
 * useDashboardStats.ts:177-217). Dört izni de taşıyan müdürde imza bekleyen sözleşme 0,
 * devamsızlık onayı 5 iken kart yanar ve boş /internship/contracts'a götürür. Çözüm bu
 * dosyada değil: composable `pendingByQueue` kırılımını dışarı vermeli, hedef ondan
 * sonra dolu kuyruğa göre seçilmeli. Kapsam dışı bırakıldı, tahminle kapatılmadı.
 *
 * Dört iznin de olmadığı kullanıcıda undefined kalır → kart tıklanamaz.
 */
const pendingQueueRoute = computed(
  () => PENDING_QUEUES.find((q) => authStore.hasPermission(q.permission))?.route,
)

/*
 * Kullanıcının GERÇEKTEN ilerletebileceği bir kuyruk var mı — kuyruğu açan izin YETMEZ,
 * kaydı ilerleten uç noktanın izni de gerekir.
 *
 * KAPSAM NOTU: izin gerek şarttır, yeter şart değildir. Kapsam da isteyen zincirlerde
 * (ör. ücretli izinde `business_id` eşleşmesi — PaidLeaveApprovalPolicy) karar sunucuda
 * daralır. Bu sayaç o kuyrukları saymıyor, bu yüzden burada ek kapsam kontrolü yok;
 * kuyruk listesine kapsam isteyen bir satır eklenirse bu not yeniden okunmalı.
 */
const hasActionableQueue = computed(() =>
  PENDING_QUEUES.some(
    (q) => authStore.hasPermission(q.permission) && authStore.hasPermission(q.action),
  ),
)

/*
 * "Sıra sizde" sinyalinin koşulu — dördü de VERİDEN ya da izinden türer, hiçbiri
 * varsayım değildir:
 *   1) sayaç yüklendi          → skeleton sırasında vaat verilmez
 *   2) sayaç > 0               → sıfır kuyruk sinyal üretmez (Kural 4)
 *   3) dönem kapalı değil      → salt-okunur dönemde yapılamayan iş vaat edilmez (Kural 3)
 *   4) ilerletebileceği kuyruk → sinyal bir EYLEME çıkmalı, 403'e değil
 *
 * Koşul 4, eski "gidilecek açık bir kuyruk var" koşulunu KAPSAR: `action` izni olan
 * satırın `permission` izni de vardır, dolayısıyla pendingQueueRoute kesinlikle
 * tanımlıdır — ayrı bir kontrol gereksiz olurdu.
 */
const isMyTurnPending = computed(
  () =>
    !stats.pendingLoading &&
    stats.pendingTotal > 0 &&
    !periodStore.isReadOnly &&
    hasActionableQueue.value,
)

// Quick links — permission-filtered
const quickLinks = computed(() => {
  const links = [
    { label: 'Öğrenciler', icon: 'school', color: 'primary', route: '/enrollment/students', permission: Permissions.Student.View },
    { label: 'Sözleşmeler', icon: 'description', color: 'secondary', route: '/internship/contracts', permission: Permissions.Internship.Contract },
    { label: 'Devamsızlık', icon: 'event_available', color: 'info', route: '/attendance', permission: Permissions.Attendance.View },
    { label: 'Maaş / Dekont', icon: 'payments', color: 'primary', route: '/salary', permission: Permissions.Salary.View },
    { label: 'Koordinasyon', icon: 'supervisor_account', color: 'secondary', route: '/coordination', permission: Permissions.Coordinator.Visit },
    { label: 'Raporlar', icon: 'bar_chart', color: 'info', route: '/reporting', permission: Permissions.Internship.Report },
  ]
  return links.filter((l) => authStore.hasPermission(l.permission))
})

onMounted(async () => {
  // Müdürlük bağlamında okul verisi sorguları gereksiz — hiçbiri kiracı damgalı belgeyle
  // eşleşmez, hepsi boş döner.
  if (isDirectorate.value) return

  await init()
})
</script>

<style scoped>
/*
 * Hover zemini tema değişkeninden türetilir (ham siyah örtü kiracı rengi değişince
 * yerinde donardı). color-mix desteklenmeyen tarayıcı için düz hex yedeği önce gelir.
 * Geçiş temel kuralda: yalnız :hover'da tanımlanırsa fare çekilirken uygulanmaz.
 */
.quick-link {
  transition: background-color 0.2s ease;
}
.quick-link:hover {
  background-color: #f6f7f9;
  background-color: color-mix(in srgb, var(--q-primary) 4%, #fff);
}
.quick-link:focus-visible {
  outline: 2px solid var(--q-primary);
  outline-offset: -2px;
}
</style>
