<template>
  <q-layout view="lHh Lpr lFf">
    <q-header elevated>
      <a
        class="atlama-baglantisi"
        href="#ana-icerik"
      >İçeriğe atla</a>
      <q-toolbar>
        <q-btn
          flat
          dense
          round
          icon="menu"
          aria-label="Menüyü aç/kapat"
          :aria-expanded="drawerOpen ? 'true' : 'false'"
          @click="drawerOpen = !drawerOpen"
        >
          <q-tooltip>Menüyü aç/kapat</q-tooltip>
        </q-btn>
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
          :aria-label="unreadCount > 0 ? `Bildirimler — ${unreadCount} okunmamış` : 'Bildirimler — okunmamış yok'"
          class="q-mr-xs"
        >
          <q-badge
            v-if="unreadCount > 0"
            color="negative"
            floating
            aria-hidden="true"
            class="tabular-nums"
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
                <q-item-section class="text-grey-7 text-caption text-center q-pa-md">
                  Bildirim yok
                </q-item-section>
              </q-item>
              <q-item
                v-for="(n, i) in notificationStore.notifications.slice(0, 10)"
                :key="n.id"
                dense
                :class="{ 'bg-info-soft': !n.read }"
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
                  <!--
                    Zaman damgası grey-7 DEĞİL grey-8: bu satırın zemini okunmamış
                    bildirimde bg-info-soft (#e8edf1) oluyor — yukarıdaki :class koşulu.
                    Ölçüldü: grey-7 (#757575) o zeminde 3,91:1 ile 4,5:1 eşiğinin altında
                    kalıyor; grey-8 (#616161) beyazda 6,19:1, #e8edf1 üzerinde 5,25:1 —
                    her iki zemini de geçiyor. Quasar'ın kendi .q-item__label--caption
                    rengini bu sınıf !important ile eziyor, yani devreye giren renk budur.
                  -->
                  <q-item-label
                    caption
                    class="text-grey-8"
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
                    class="notif-remove-btn"
                    @click.stop="notificationStore.remove(i)"
                  >
                    <q-tooltip>Bildirimi kaldır</q-tooltip>
                  </q-btn>
                </q-item-section>
              </q-item>
              <q-item
                v-if="notificationStore.notifications.length > 0"
                dense
                clickable
                @click="notificationStore.clear()"
              >
                <q-item-section class="text-center text-caption text-grey-7">
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
        >
          <q-tooltip>Çıkış yap</q-tooltip>
        </q-btn>
      </q-toolbar>
    </q-header>

    <q-drawer
      v-model="drawerOpen"
      show-if-above
      bordered
    >
      <q-scroll-area class="fit">
        <q-list padding>
          <!--
            Bağlam göstergesi/seçici — dönem seçicisinin ÜSTÜNDE: aktif bağlam bu uygulamada
            en üst öncelikli bilgidir, il/ilçe yetkilisinin tüm çalışması bir okul adına
            geçtiği andan itibaren o bağlamın içinde yürür. İnce bir rozet yeterli değil —
            aktif renk + net metin + tek tıkla `/context`'e giden eylem.
          -->
          <q-item
            v-if="activeInstitutionName"
            class="q-mb-xs"
          >
            <q-item-section>
              <q-chip
                clickable
                square
                color="warning"
                text-color="white"
                icon="swap_horiz"
                class="full-width baglam-rozeti"
                @click="goToContextSelect"
              >
                {{ activeInstitutionName }} adına çalışıyorsunuz
              </q-chip>
            </q-item-section>
          </q-item>
          <q-item
            v-else-if="showContextSelectButton"
            class="q-mb-xs"
          >
            <q-item-section>
              <q-btn
                outline
                no-caps
                color="primary"
                icon="swap_horiz"
                label="Kurum Seç"
                class="full-width"
                @click="goToContextSelect"
              />
            </q-item-section>
          </q-item>

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
                      <!--
                        Kapalı dönem rozeti grey-5 olamaz: q-badge metni her zaman beyazdır
                        (quasar.css .q-badge → color: #fff, 12px normal ağırlık → metin
                        eşiği 4,5:1) ve grey-5 (#bdbdbd) beyaz metinle 1,88:1 veriyordu —
                        ne 4,5:1 metin eşiğini ne 3:1 grafik eşiğini geçiyor. grey-9
                        (#424242) beyaz metinle 10,05:1. Ton keyfî değil: StatusBadge.vue
                        "kapatılmış" durumunu da grey-9 ile gösterir (CLOSED sabiti);
                        "taslak/pasif" orada grey-8'dir.
                      -->
                      <q-badge
                        :color="opt.active ? 'positive' : 'grey-9'"
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

    <q-page-container
      id="ana-icerik"
      role="main"
      tabindex="-1"
    >
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

      <q-card-section class="text-center text-caption text-grey-7 q-pt-none">
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
import { useInstitutionStore } from 'stores/institution'
import { Permissions } from 'utils/permissions'
import { logout } from 'boot/auth'
import { useNavigation } from 'src/composables/useNavigation'
import { eventLabel, timeAgo } from 'src/utils/notificationFormat'
import AppNotice from 'components/AppNotice.vue'
import DetailDialog from 'components/DetailDialog.vue'

const authStore = useAuthStore()
const { filteredMenu, isExpanded, toggleGroup, activeGroupKey } = useNavigation()
const notificationStore = useNotificationStore()
const periodStore = useAcademicPeriodStore()
const institutionStore = useInstitutionStore()

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

/**
 * Aktif bağlamdaki kurumun adı — doluysa üst bar göstergesi görünür.
 *
 * <p>Görünürlük ölçütü `authStore.user?.activeInstitutionId` dolu mu sorusudur. Ad
 * `institutionStore.institution`'dan gelir: bağlam aktifken store `authStore.
 * currentInstitutionId`'yi (Görev 8 → aktif bağlam varsa o) okuyarak zaten AKTİF okulun
 * profilini yükler (bkz. `stores/institution.ts`), ikinci bir sorgu yazılmaz.</p>
 */
const activeInstitutionName = computed(() =>
  authStore.user?.activeInstitutionId ? institutionStore.institution?.fullName : null,
)

/**
 * "Kurum Seç" butonu yalnız bağlam YOKKEN ve kullanıcının kendi düğümü bir üst düğümse
 * (il/ilçe müdürlüğü) görünür. Okul kullanıcısında ikisi de görünmez.
 *
 * <b>Rol adına BAKILMAZ</b> (depo kuralı) — `useNavigation.ts`'teki `visibilityContext` ile
 * aynı sinyal: `institutionStore`'daki yüklü kurumun `nodeType`'ı. Bağlam yokken bu alan
 * kullanıcının EV kurumunu taşır (yine `currentInstitutionId` üzerinden), yani il/ilçe
 * müdürlüğü mü sorusuna doğru cevabı verir. Yeni bir `authStore` yardımcısına gerek yok —
 * `institutionStore.institution?.nodeType` zaten menüde aynı amaçla kullanılan mevcut sinyal.
 */
const showContextSelectButton = computed(() => {
  if (activeInstitutionName.value) return false
  const nodeType = institutionStore.institution?.nodeType
  return nodeType === 'Province' || nodeType === 'District'
})

function goToContextSelect() {
  router.push('/context').catch(() => {})
}

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

/*
 * Kiracının marka teması (#brand-palette).
 *
 * Tetikleyici burada duruyor çünkü MainLayout kimliği doğrulanmış kabuktur: her sayfa
 * bunun içinde açılır, yani tema hangi rotadan girilirse girilsin uygulanır. Temayı asıl
 * UYGULAYAN yer store'dur (`loadInstitution` → `applyBrandTheme`); burada yalnız yükleme
 * tetiklenir, renk mantığı tekrarlanmaz.
 *
 * Kapı `institution:view`: kurum ucu o izni ister ve izni olmayan rol (ör. işletme
 * yetkilisi) için istek 403 dönerdi. İzinsiz kullanıcı derleme zamanı varsayılanını
 * (Mührü Lacivert) görür — bu bir kırılma değil, kapsamın dürüst sonucudur.
 *
 * `await` edilmiyor: tema kozmetiktir, dönem yüklemesini ya da ilk boyamayı bekletmemeli.
 * `void` yerine `.catch(() => {})` — CLAUDE.md fire-and-forget kuralı.
 */
onMounted(() => {
  if (!authStore.isAuthenticated) return
  if (!authStore.hasPermission(Permissions.Institution.View)) return
  institutionStore.loadInstitution().catch(() => {})
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

<style scoped>
/*
 * Bağlam rozeti — "gösterge ince olamaz": il/ilçe yetkilisinin bütün zamanı bir bağlamın
 * içinde geçer, hangi okul adına davrandığı her an tartışmasız görünmeli. q-chip'in
 * varsayılan tek satır/kısaltma davranışı burada istenmiyor — okul adı ne kadar uzun olursa
 * olsun tam görünür ve satır kırılabilir; kalın yazı tipi göze ilk çarpan öge olmasını
 * sağlar. Renk: `$warning` (#9A6B00) bu depoda beyaz metinle 4,8:1 için özel ayarlandı
 * (bkz. quasar-variables.sass yorumu) — Quasar varsayılanı ~1,9:1 ile yetersizdi.
 */
.baglam-rozeti {
  height: auto;
  min-height: 40px;
  padding: 8px 12px;
  white-space: normal;
  font-weight: 600;
}

/* Dokunma hedefi WCAG 2.2 SC 2.5.8 (24x24 CSS px) — size="xs" görsel olarak küçük kalıyor. */
.notif-remove-btn {
  min-width: 24px;
  min-height: 24px;
}

/*
 * Blok atlama (WCAG 2.4.1) — kalıcı sol çekmece her sayfada onlarca menü bağlantısını
 * tekrarlıyor; klavye kullanıcısı buradan doğrudan #ana-icerik'e geçer.
 *
 * Zemin ÜST BARIN RENGİ OLAMAZ: Quasar'ın kendi kuralı
 * `.q-layout__section--marginal { background-color: var(--q-primary) }`
 * (node_modules/quasar/src/components/layout/QLayout.sass) üst bara zaten aynı rengi
 * veriyor — bağlantı odaklandığında üst barla birebir aynı zemine oturur ve ayrı bir
 * kontrol olarak hiç görünmez. Bu yüzden kağıt beyazı zemin + lacivert metin
 * (ölçüldü: #1E3A5F / #FFFFFF = 11,50:1, metin eşiği 4,5:1) ve 2px lacivert dış çizgi
 * (11,50:1 — grafik nesnesi eşiği 3:1).
 *
 * Dış çizginin arkasındaki zemin TARAYICI VARSAYILANI BEYAZDIR, #EDEFF2 değil: bu
 * uygulamada sayfa zeminine hiç renk verilmiyor. Quasar çekirdeği `body`ye arka plan
 * koymuyor (core/typography.sass'taki `body` bloğunda `background` yok; tek tanım
 * `body.body--dark` altında, core/dark.sass), src/assets/app.css'te body/`.q-layout`/
 * `.q-page-container` için arka plan kuralı yok ve index.html stil taşımıyor. #EDEFF2
 * yalnızca `.bg-neutral-soft` BİLEŞEN yüzeyi olarak var (app.css). Odaklı bağlantı böyle
 * bir yüzeyin üstüne denk gelirse oran 9,99:1'e iner — o da 3:1 eşiğini rahat geçer.
 *
 * Konum üst barın ALTINA iner (`top: 100%`): `top: 8px; left: 8px` bağlantıyı toolbar'ın
 * sol başındaki menü butonunun üzerine oturtuyordu.
 *
 * Metin/dış çizgi rengi `var(--q-primary)`; düz hex yedeği bilerek yazılmadı — kimlik
 * kiracıdan gelebilir, sabit bir kopya tema değişince yerinde donar.
 */
.atlama-baglantisi {
  position: absolute;
  left: -9999px;
  top: 0;
  z-index: 9999;
  padding: 8px 16px;
  background: #fff;
  color: var(--q-primary);
  border-radius: 8px;
  outline: 2px solid var(--q-primary);
  outline-offset: 2px;
}
.atlama-baglantisi:focus {
  left: 8px;
  top: calc(100% + 10px);
}
</style>
