import { computed, ref, watch } from 'vue'
import { useRoute } from 'vue-router'
import { useAuthStore } from 'stores/auth'
import { useInstitutionStore } from 'stores/institution'

/**
 * Menü görünürlüğü için izin DIŞI bağlam.
 *
 * `isUpperNode`: kullanıcının bağlı olduğu kurum bir il/ilçe müdürlüğü düğümü mü?
 * İzinle çözülemez — okul müdürü de `institution:view` taşır (hatta `institution:*`).
 */
export interface NavVisibilityContext {
  isUpperNode: boolean
}

export interface NavItem {
  title: string
  icon: string
  to: { name: string }
  permissions: string[]
  /**
   * İzne EK koşul. Verilmezse yalnız izne bakılır.
   *
   * Bu bir GÖRÜNÜRLÜK kararıdır, yetki kararı değil — yetki sunucudadır. Menüden gizlemek,
   * bilgi taşımayan bir girdiyi (okul kullanıcısına tek satırlık "liste") saklamak içindir.
   */
  visibleWhen?: (ctx: NavVisibilityContext) => boolean
}

/**
 * Bir menü girdisi görünür mü? Saf fonksiyon — store'a dokunmaz, testte tek başına koşar.
 */
export function isNavItemVisible(
  item: NavItem,
  hasAnyPermission: (permissions: string[]) => boolean,
  ctx: NavVisibilityContext,
): boolean {
  if (item.permissions.length > 0 && !hasAnyPermission(item.permissions)) return false
  if (item.visibleWhen && !item.visibleWhen(ctx)) return false
  return true
}

/**
 * Üst düğüm sinyalinin SAF kararı — store'a dokunmaz, testte tek başına koşar
 * (<c>isNavItemVisible</c> ile aynı gerekçe).
 *
 * `nodeType` TEK BAŞINA yeterli DEĞİL (Görev 10, B parçası son inceleme madde 6):
 * `institutionStore` aktif bağlama bağlandığı için il yetkilisi bir okula geçtiğinde
 * `nodeType === 'School'` olur. Aktif bağlam DOLU olması aktörün üst düğüm olduğunun
 * KANITIDIR — okul kullanıcısı bağlam SEÇEMEZ — bu yüzden ikisi OR'lanır.
 */
export function resolveIsUpperNode(
  nodeType: string | undefined,
  activeInstitutionId: string | null | undefined,
): boolean {
  return nodeType === 'Province' || nodeType === 'District' || !!activeInstitutionId
}

export interface NavGroup {
  key: string
  title: string
  icon: string
  to?: { name: string }
  permissions: string[]
  children: NavItem[]
}

/**
 * Gerçek menü tanımı — test dosyaları bunu import eder, kendi kopyasını kurmaz. Bir girdinin
 * `visibleWhen` koşulu buradan silinirse (ör. "Kurumlar") testin bunu görmesi gerekir; yerel
 * bir kopya üzerinde koşan test bu tür bir gerilemeyi asla yakalayamaz.
 */
export const menuDefinition: NavGroup[] = [
  {
    key: 'home',
    title: 'Ana Sayfa',
    icon: 'dashboard',
    to: { name: 'Dashboard' },
    permissions: [],
    children: [],
  },
  {
    key: 'institution',
    title: 'Kurum Yönetimi',
    icon: 'account_balance',
    permissions: [],
    children: [
      {
        title: 'Kurumlar',
        icon: 'account_tree',
        to: { name: 'InstitutionList' },
        permissions: ['institution:view'],
        // Okul kullanıcısına gösterilmez: onun "listesi" tek satırdır ve tıklandığında zaten
        // açık olan sayfaya gider. Ayrım izinle yapılamaz — okul müdürü de institution:view
        // taşır; fark bağlı olduğu düğümün TİPİNDEDİR.
        visibleWhen: (ctx) => ctx.isUpperNode,
      },
      { title: 'Kurum Bilgileri', icon: 'account_balance', to: { name: 'Institution' }, permissions: ['institution:view'] },
      { title: 'Kullanıcılar', icon: 'manage_accounts', to: { name: 'UserManagement' }, permissions: ['user:view', 'user:create'] },
      { title: 'Roller', icon: 'admin_panel_settings', to: { name: 'RoleManagement' }, permissions: ['user:roles:manage'] },
      { title: 'Yetki Kapsamı', icon: 'tune', to: { name: 'PermissionScope' }, permissions: ['user:roles:manage'] },
      // İzin listesi BOŞ: "İşlemlerim" kapsamı herkese açıktır. Kurum kapsamı sayfa içinde
      // izinle açılır — menüyü izne bağlamak, kendi geçmişini göremeyen kullanıcılar üretirdi.
      { title: 'Son İşlemler', icon: 'history', to: { name: 'AuditLog' }, permissions: [] },
    ],
  },
  {
    key: 'enrollment',
    title: 'Kayıt & Öğrenci',
    icon: 'school',
    permissions: [],
    children: [
      { title: 'Öğrenciler', icon: 'school', to: { name: 'StudentList' }, permissions: ['student:view'] },
    ],
  },
  {
    key: 'business',
    title: 'İşletmeler',
    icon: 'business',
    permissions: [],
    children: [
      { title: 'İşletme Listesi', icon: 'business', to: { name: 'CompanyList' }, permissions: ['company:view'] },
    ],
  },
  {
    key: 'internship',
    title: 'Staj Yönetimi',
    icon: 'work_history',
    permissions: [],
    children: [
      { title: 'Staj Takibi', icon: 'work_history', to: { name: 'InternshipOverview' }, permissions: ['internship:view', 'internship:manage', 'internship:view-own'] },
      { title: 'Sözleşmeler', icon: 'description', to: { name: 'ContractList' }, permissions: ['internship:manage', 'internship:contract:manage'] },
      { title: 'Devamsızlık', icon: 'event_available', to: { name: 'AttendanceList' }, permissions: ['attendance:view', 'attendance:view-own'] },
      { title: 'Ücretli İzin', icon: 'event_note', to: { name: 'PaidLeaveList' }, permissions: ['attendance:leave:request', 'attendance:leave:business-approve', 'attendance:leave:approve'] },
      { title: 'Maaş / Dekont', icon: 'payments', to: { name: 'SalaryList' }, permissions: ['salary:view', 'salary:view-own'] },
      { title: 'Asgari Ücret', icon: 'price_change', to: { name: 'SalaryConfig' }, permissions: ['salary:parameter:view'] },
      { title: 'Dönem Notu Girişi', icon: 'edit_note', to: { name: 'TermGradeEntry' }, permissions: ['company:grade:enter', 'institution:school-grade:enter'] },
    ],
  },
  {
    key: 'coordination',
    title: 'Koordinasyon',
    icon: 'supervisor_account',
    permissions: [],
    children: [
      { title: 'Ders Programı', icon: 'calendar_month', to: { name: 'TeacherSchedule' }, permissions: ['coordinator:schedule:manage'] },
      // Sıra iş akışını yansıtır: havuz hesaplanır -> saat takdir edilir -> dağıtım yapılır.
      // Saat takdiri dağıtımın ön koşulu; dağıtılabilir saatin üst sınırı havuzdan gelir.
      // Mesafe-saat mevzuat tablosu havuzdan da önce gelir: işletme saat tavanları buradan türer.
      { title: 'Koordinasyon Ayarları', icon: 'tune', to: { name: 'CoordinationConfig' }, permissions: ['department:distribution:manage'] },
      { title: 'Ders Yükü Havuzu', icon: 'calculate', to: { name: 'WorkloadConfig' }, permissions: ['department:distribution:manage'] },
      { title: 'İşletme Saat Ayarları', icon: 'schedule', to: { name: 'BusinessHours' }, permissions: ['department:distribution:manage'] },
      { title: 'İşletme Dağıtımı', icon: 'assignment_ind', to: { name: 'BusinessAssignment' }, permissions: ['department:distribution:manage'] },
      { title: 'Haftalık Ziyaretler', icon: 'event_note', to: { name: 'WeeklyVisits' }, permissions: ['department:weekly-visit:manage'] },
      { title: 'Değerlendirmeler', icon: 'rate_review', to: { name: 'BusinessEvaluations' }, permissions: ['coordinator:visit:manage'] },
      { title: 'Beceri Sınavları', icon: 'quiz', to: { name: 'SkillExams' }, permissions: ['coordinator:visit:manage'] },
      { title: 'Faaliyet Raporları', icon: 'description', to: { name: 'ActivityReports' }, permissions: ['coordinator:report:manage'] },
      { title: 'Dönem Not Fişleri', icon: 'grading', to: { name: 'TermGradeSlips' }, permissions: ['coordinator:report:manage'] },
    ],
  },
  {
    key: 'documents',
    title: 'Belgeler & Raporlar',
    icon: 'folder_open',
    permissions: [],
    children: [
      { title: 'Belgeler', icon: 'folder_open', to: { name: 'Documents' }, permissions: ['document:view'] },
      { title: 'Raporlar', icon: 'bar_chart', to: { name: 'Reporting' }, permissions: ['internship:report:manage'] },
    ],
  },
]

const STORAGE_KEY = 'mesnet-nav-expanded'

export function useNavigation() {
  const authStore = useAuthStore()
  const institutionStore = useInstitutionStore()
  const route = useRoute()

  /**
   * Kullanıcının kurumu bir üst düğüm mü?
   *
   * Kaynak `institutionStore` — aktörün DAVRANILAN kurumunu (`GET /api/institutions/{id}`,
   * aktif bağlam varsa o) yükler (MainLayout mount'ta çağırır). Store dolmadan önce
   * `false`'tur, yani menü girdisi biraz geç belirir; alternatifi `/auth/me`'ye yeni bir
   * claim eklemekti ve o kapsam anahtarı olmayan bir görünürlük kararı için fazla ağır bir
   * yol.
   *
   * `institutionStore`'un `nodeType`'ı TEK BAŞINA yeterli DEĞİL (Görev 10, B parçası): store
   * artık aktif bağlama bağlı — il yetkilisi bir okula geçtiğinde `nodeType === 'School'`
   * olur ve "Kurumlar" menü girdisi KAYBOLUR. Üst düğüm sinyali aktif bağlamdan
   * ETKİLENMEMELİDİR: aktif bağlam DOLU olması aktörün tanımı gereği üst düğüm olduğunun
   * kanıtıdır — okul kullanıcısı bağlam SEÇEMEZ (yalnız il/ilçe yetkilisi bağlama geçer).
   */
  const visibilityContext = computed<NavVisibilityContext>(() => ({
    isUpperNode: resolveIsUpperNode(
      institutionStore.institution?.nodeType,
      authStore.user?.activeInstitutionId,
    ),
  }))

  const filteredMenu = computed(() => {
    const ctx = visibilityContext.value
    const hasAny = (permissions: string[]) => authStore.hasAnyPermission(permissions)

    return menuDefinition
      .map((group) => {
        // Top-level link (children yok)
        if (group.to && group.children.length === 0) {
          const visible = group.permissions.length === 0 || hasAny(group.permissions)
          return visible ? group : null
        }

        const visibleChildren = group.children.filter((item) =>
          isNavItemVisible(item, hasAny, ctx),
        )

        if (visibleChildren.length === 0) return null

        // Tek child → düz link'e terfi ettir
        if (visibleChildren.length === 1) {
          return { ...group, to: visibleChildren[0].to, children: [] as NavItem[] }
        }

        return { ...group, children: visibleChildren }
      })
      .filter(Boolean) as NavGroup[]
  })

  // Expand state — localStorage ile kalıcı.
  //
  // Okuma ve yazma korumalı: bu composable MainLayout'un setup'ında senkron çalışır.
  // Bozuk bir JSON değeri (elle düzenleme, yarım yazma, ileride şema değişikliği) burada
  // fırlatırsa TÜM ana düzen render edilemez ve kullanıcı localStorage'ı elle temizleyene
  // kadar uygulamaya giremez. Yazma tarafı da kotayı dolduran ya da özel modda depolamayı
  // kapatan tarayıcılarda fırlatabilir. Menü açık/kapalı durumu kritik veri değil —
  // hata yutulmaz, varsayılana düşülür.
  function loadExpandedGroups(): Record<string, boolean> {
    try {
      const raw = localStorage.getItem(STORAGE_KEY)
      if (!raw) return {}
      const parsed: unknown = JSON.parse(raw)
      if (typeof parsed !== 'object' || parsed === null || Array.isArray(parsed)) return {}
      return parsed as Record<string, boolean>
    } catch {
      return {}
    }
  }

  function persistExpandedGroups() {
    try {
      localStorage.setItem(STORAGE_KEY, JSON.stringify(expandedGroups.value))
    } catch {
      // Kalıcılık kaybı kabul edilebilir; menü bu oturumda çalışmaya devam eder.
    }
  }

  const expandedGroups = ref<Record<string, boolean>>(loadExpandedGroups())

  function toggleGroup(key: string) {
    expandedGroups.value[key] = !expandedGroups.value[key]
    persistExpandedGroups()
  }

  function isExpanded(key: string): boolean {
    return expandedGroups.value[key] ?? false
  }

  // Aktif route'a göre otomatik expand
  const activeGroupKey = computed(() => {
    const currentName = route.name as string
    for (const group of filteredMenu.value) {
      if (group.to?.name === currentName) return group.key
      for (const child of group.children) {
        if (child.to.name === currentName) return group.key
      }
    }
    return null
  })

  watch(
    activeGroupKey,
    (key) => {
      if (key && !expandedGroups.value[key]) {
        expandedGroups.value[key] = true
        persistExpandedGroups()
      }
    },
    { immediate: true },
  )

  return { filteredMenu, isExpanded, toggleGroup, activeGroupKey }
}
