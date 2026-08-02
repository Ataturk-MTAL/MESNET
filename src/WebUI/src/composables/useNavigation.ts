import { computed, ref, watch } from 'vue'
import { useRoute } from 'vue-router'
import { useAuthStore } from 'stores/auth'

export interface NavItem {
  title: string
  icon: string
  to: { name: string }
  permissions: string[]
}

export interface NavGroup {
  key: string
  title: string
  icon: string
  to?: { name: string }
  permissions: string[]
  children: NavItem[]
}

const menuDefinition: NavGroup[] = [
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
      { title: 'Kurum Bilgileri', icon: 'account_balance', to: { name: 'Institution' }, permissions: ['institution:view'] },
      { title: 'Kullanıcılar', icon: 'manage_accounts', to: { name: 'UserManagement' }, permissions: ['user:view', 'user:create'] },
      { title: 'Roller', icon: 'admin_panel_settings', to: { name: 'RoleManagement' }, permissions: ['user:roles:manage'] },
      { title: 'Yetki Kapsamı', icon: 'tune', to: { name: 'PermissionScope' }, permissions: ['user:roles:manage'] },
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
      { title: 'Staj Takibi', icon: 'work_history', to: { name: 'InternshipOverview' }, permissions: ['internship:view', 'internship:manage'] },
      { title: 'Sözleşmeler', icon: 'description', to: { name: 'ContractList' }, permissions: ['internship:manage', 'internship:contract:manage'] },
      { title: 'Devamsızlık', icon: 'event_available', to: { name: 'AttendanceList' }, permissions: ['attendance:view'] },
      { title: 'Ücretli İzin', icon: 'event_note', to: { name: 'PaidLeaveList' }, permissions: ['attendance:leave:request', 'attendance:leave:business-approve', 'attendance:leave:approve'] },
      { title: 'Maaş / Dekont', icon: 'payments', to: { name: 'SalaryList' }, permissions: ['salary:view'] },
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
  const route = useRoute()

  const filteredMenu = computed(() => {
    return menuDefinition
      .map((group) => {
        // Top-level link (children yok)
        if (group.to && group.children.length === 0) {
          const visible =
            group.permissions.length === 0 || authStore.hasAnyPermission(group.permissions)
          return visible ? group : null
        }

        // Children filtrele
        const visibleChildren = group.children.filter(
          (item) =>
            item.permissions.length === 0 || authStore.hasAnyPermission(item.permissions),
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
