import { createRouter, createWebHistory } from 'vue-router'
import { useAuthStore } from 'stores/auth'

const router = createRouter({
  history: createWebHistory(),
  routes: [
    {
      path: '/auth/callback',
      name: 'AuthCallback',
      component: () => import('pages/auth/CallbackPage.vue'),
      meta: { public: true },
    },

    {
      path: '/',
      component: () => import('src/layouts/MainLayout.vue'),
      children: [
        {
          path: '',
          redirect: '/dashboard',
        },
        {
          path: 'dashboard',
          name: 'Dashboard',
          component: () => import('pages/DashboardPage.vue'),
        },

        // Kurum
        {
          path: 'institution',
          name: 'Institution',
          component: () => import('pages/institution/InstitutionPage.vue'),
          meta: { permissions: ['institution:view'] },
        },
        {
          path: 'institution/edit',
          name: 'InstitutionEdit',
          component: () => import('pages/institution/InstitutionFormPage.vue'),
          // Form YAZMA yapar (PATCH /institutions/{id} → institution:manage).
          // Okuma izniyle korunsaydı yalnız görüntüleme yetkisi olan kullanıcı formu
          // doldurup Kaydet'te 403 duvarına çarpardı.
          meta: { permissions: ['institution:manage'], formRoute: true },
        },
        // Kurum ağacı listesi (il/ilçe yetkilisi). Menüde yalnız üst düğüm kullanıcısına
        // görünür; rota izinle korunur, kapsam sunucudadır (InstitutionScopePolicy).
        {
          path: 'institutions',
          name: 'InstitutionList',
          component: () => import('pages/institution/InstitutionListPage.vue'),
          meta: { permissions: ['institution:view'] },
        },
        // Detay için ayrı sayfa YOK: mevcut kurum sayfası rota parametresiyle açılır.
        // Yazma butonları orada institution:manage ile sarılı olduğundan sayfa il
        // yetkilisinde kendiliğinden salt okunur açılır.
        {
          path: 'institutions/:id',
          name: 'InstitutionDetail',
          component: () => import('pages/institution/InstitutionPage.vue'),
          meta: { permissions: ['institution:view'] },
        },

        // Bağlam seçimi — il/ilçe yetkilisinin tek çalışma modu okulun bağlamına geçmektir.
        // İzin institution:view'dir: seçim listesi zaten o izinle geliyor ve kapsam
        // sunucudadır (InstitutionScopePolicy).
        {
          path: 'context',
          name: 'ContextSelect',
          component: () => import('pages/institution/ContextSelectPage.vue'),
          meta: { permissions: ['institution:view'] },
        },

        // Denetim izi (C parçası). Rota "İşlemlerim" kapsamıyla açılır ve o kapsam EK İZİN
        // GEREKTİRMEZ — kullanıcının kendi geçmişini görmesi bir yetki sorusu değildir.
        // Kurum kapsamı sayfa içinde `audit:view:institution` ile açılır.
        {
          path: 'audit',
          name: 'AuditLog',
          component: () => import('pages/audit/AuditLogPage.vue'),
        },

        // İşletme (Company)
        {
          path: 'companies',
          name: 'CompanyList',
          component: () => import('pages/business/BusinessListPage.vue'),
          meta: { permissions: ['company:view'] },
        },
        {
          path: 'companies/new',
          name: 'CompanyNew',
          component: () => import('pages/business/BusinessFormPage.vue'),
          meta: { permissions: ['company:manage'], formRoute: true },
        },
        {
          path: 'companies/:id/edit',
          name: 'CompanyEdit',
          component: () => import('pages/business/BusinessFormPage.vue'),
          meta: { permissions: ['company:manage'], formRoute: true },
        },

        // Kayıt / Başvuru
        {
          path: 'enrollment',
          children: [
            {
              path: 'students',
              name: 'StudentList',
              component: () => import('pages/StudentList.vue'),
              meta: { permissions: ['student:view'] },
            },
            {
              path: 'students/new',
              name: 'StudentNew',
              component: () => import('pages/StudentFormPage.vue'),
              // POST /students → student:manage
              meta: { permissions: ['student:manage'], formRoute: true },
            },
            {
              path: 'students/:id/edit',
              name: 'StudentEdit',
              component: () => import('pages/StudentFormPage.vue'),
              // PATCH /students/{id} → student:manage
              meta: { permissions: ['student:manage'], formRoute: true },
            },
            // Phase 2 — MEB Protokolü modülü implement edilince açılacak
            // {
            //   path: 'protocols',
            //   name: 'ProtocolList',
            //   component: () => import('pages/enrollment/ProtocolListPage.vue'),
            //   meta: { permissions: ['protocol:view'] },
            // },
          ],
        },

        // Staj
        {
          path: 'internship',
          children: [
            // Sözleşme listesi ve detayı — internship:contract:manage
            // Durum değiştirme (askı, fesih, tamamlama) — internship:manage — sayfa içinde kontrol edilir
            {
              path: 'contracts',
              name: 'ContractList',
              component: () => import('pages/contract/ContractListPage.vue'),
              meta: { permissions: ['internship:contract:manage'] },
            },
            {
              path: 'contracts/new',
              name: 'ContractNew',
              component: () => import('pages/contract/ContractFormPage.vue'),
              meta: { permissions: ['internship:contract:manage'], formRoute: true },
            },
            // Fesih onay zinciri (#191) — okul tarafının adımları.
            // Görüntüleme internship:view; her adımın butonu KENDİ iznine bakar ve o izin
            // sunucudan gelen adım tanımından okunur.
            {
              path: 'terminations',
              name: 'InternshipTerminations',
              component: () => import('pages/internship/TerminationsPage.vue'),
              meta: { permissions: ['internship:view', 'internship:manage'] },
            },
            // Veli ve işletme yetkilisi için fesih DURUM sayfası (#191, #218).
            // Salt okunur: onaylar okul tarafında verilir. Kapsamı SUNUCU çözer
            // (veli bağı / işletme kimliği, ikisi de claim'den); rota yalnız kapıyı açar.
            {
              path: 'termination-status',
              name: 'InternshipTerminationStatus',
              component: () => import('pages/internship/MyApprovalsPage.vue'),
              meta: { permissions: ['internship:view-own', 'company:student:manage'] },
            },
            {
              path: 'overview',
              name: 'InternshipOverview',
              component: () => import('pages/internship/InternshipOverviewPage.vue'),
              // internship:view — personel görüntüleyebilir
              // internship:manage — durum değişikliklerini sayfa içi PermissionGuard kontrol eder
              meta: { permissions: ['internship:view', 'internship:manage', 'internship:view-own'] },
            },
          ],
        },

        // Devamsızlık
        {
          path: 'attendance',
          name: 'AttendanceList',
          component: () => import('pages/attendance/AttendancePage.vue'),
          // Öğrenci ve veli de kendi kapsamında görür (#182); daraltmayı sunucu yapar.
          meta: { permissions: ['attendance:view', 'attendance:view-own'] },
        },
        {
          path: 'attendance/new',
          name: 'AttendanceNew',
          component: () => import('pages/attendance/AttendanceFormPage.vue'),
          // POST /attendance → attendance:manage. Alan şefi (attendance:view var,
          // manage yok) bu formu açıp Kaydet'te 403 alıyordu.
          meta: { permissions: ['attendance:manage'], formRoute: true },
        },

        // MESEM ücretli izin başvurusu (#177) — zincirin üç tarafı da aynı listeyi görür,
        // kapsamı sunucu daraltır (öğrenci kendi, işletme kendi öğrencileri, okul kurumu).
        {
          path: 'attendance/paid-leave',
          name: 'PaidLeaveList',
          component: () => import('pages/attendance/PaidLeavePage.vue'),
          meta: {
            permissions: [
              'attendance:leave:request',
              'attendance:leave:business-approve',
              'attendance:leave:approve',
            ],
          },
        },
        {
          path: 'attendance/paid-leave/new',
          name: 'PaidLeaveNew',
          component: () => import('pages/attendance/PaidLeaveFormPage.vue'),
          meta: { permissions: ['attendance:leave:request'], formRoute: true },
        },

        // Ücret / Maaş (Salary)
        {
          path: 'salary',
          name: 'SalaryList',
          component: () => import('pages/payment/PaymentPage.vue'),
          meta: { permissions: ['salary:view', 'salary:view-own'] },
        },
        {
          path: 'salary/config',
          name: 'SalaryConfig',
          component: () => import('pages/payment/SalaryConfigPage.vue'),
          // Görme yetkisi burada; DEĞİŞTİRME ayrı ve ULUSAL bir izindir
          // ('platform:parameter:manage') ve sayfa içinde kontrol edilir (#147).
          meta: { permissions: ['salary:parameter:view'] },
        },

        // İşletme Değerlendirmeleri
        {
          path: 'coordination/evaluations',
          name: 'BusinessEvaluations',
          component: () => import('pages/coordination/BusinessEvaluationsPage.vue'),
          meta: { permissions: ['coordinator:visit:manage'] },
        },
        {
          path: 'coordination/evaluations/new',
          name: 'BusinessEvaluationNew',
          component: () => import('pages/coordination/BusinessEvaluationFormPage.vue'),
          // Form YAZMA yapar (POST /coordination/business-evaluations →
          // coordinator:visit:manage). Liste sayfasındaki tetikleyiciyi saran
          // PermissionGuard ile aynı izin — biri gizlerken diğeri açık kalmasın.
          meta: { permissions: ['coordinator:visit:manage'], formRoute: true },
        },

        // Beceri Sınavları
        {
          path: 'coordination/skill-exams',
          name: 'SkillExams',
          component: () => import('pages/coordination/SkillExamsPage.vue'),
          meta: { permissions: ['coordinator:visit:manage'] },
        },

        // Aylık Faaliyet Raporları
        {
          path: 'coordination/activity-reports',
          name: 'ActivityReports',
          component: () => import('pages/coordination/ActivityReportsPage.vue'),
          meta: { permissions: ['coordinator:report:manage'] },
        },

        // Ders Programı
        {
          path: 'coordination/schedule',
          name: 'TeacherSchedule',
          component: () => import('pages/coordination/TeacherSchedulePage.vue'),
          meta: { permissions: ['coordinator:schedule:manage'] },
        },

        // İşletme Dağıtımı
        // İşletme Saat Ayarları — dağıtımın ön koşulu (saat takdiri + harita)
        {
          path: 'coordination/business-hours',
          name: 'BusinessHours',
          component: () => import('pages/coordination/BusinessHoursPage.vue'),
          meta: { permissions: ['department:distribution:manage'] },
        },

        {
          path: 'coordination/assignments',
          name: 'BusinessAssignment',
          component: () => import('pages/coordination/BusinessAssignmentPage.vue'),
          meta: { permissions: ['department:distribution:manage'] },
        },

        // Ders Yükü Havuzu
        {
          path: 'coordination/workload-config',
          name: 'WorkloadConfig',
          component: () => import('pages/coordination/WorkloadConfigPage.vue'),
          meta: { permissions: ['department:distribution:manage'] },
        },

        // Kurum Koordinasyon Yapılandırması (#134)
        // Görme yetkisi burada; DEĞİŞTİRME ayrı bir izindir
        // (`institution:coordination-config:manage`) ve sayfa içinde kontrol edilir (#130).
        {
          path: 'coordination/config',
          name: 'CoordinationConfig',
          component: () => import('pages/coordination/CoordinationConfigPage.vue'),
          meta: { permissions: ['department:distribution:manage'] },
        },

        // Haftalık Ziyaretler
        {
          path: 'coordination/weekly-visits',
          name: 'WeeklyVisits',
          component: () => import('pages/coordination/WeeklyVisitPage.vue'),
          meta: { permissions: ['department:weekly-visit:manage'] },
        },

        // Belgeler
        {
          path: 'documents',
          name: 'Documents',
          component: () => import('pages/reporting/DocumentsPage.vue'),
          meta: { permissions: ['document:view'] },
        },

        // Raporlar
        {
          path: 'reporting',
          name: 'Reporting',
          component: () => import('pages/reporting/ReportingPage.vue'),
          meta: { permissions: ['internship:report:manage'] },
        },

        // Dönem Notu Girişi — işletmede staj (işletme) + okulda staj (okul, #171).
        // Sayfa hangi sekmeleri göstereceğine izne bakarak karar verir.
        {
          path: 'term-grades',
          name: 'TermGradeEntry',
          component: () => import('pages/coordination/TermGradeEntryPage.vue'),
          meta: { permissions: ['company:grade:enter', 'institution:school-grade:enter'] },
        },

        // Dönem Not Fişleri (koordinatör/okul)
        {
          path: 'term-grade-slips',
          name: 'TermGradeSlips',
          component: () => import('pages/coordination/TermGradeSlipsPage.vue'),
          meta: { permissions: ['coordinator:report:manage'] },
        },

        // Admin / Kullanıcı Yönetimi
        {
          path: 'admin',
          children: [
            {
              path: 'users',
              name: 'UserManagement',
              component: () => import('pages/admin/UserManagementPage.vue'),
              meta: { permissions: ['user:view', 'user:create'] },
            },
            {
              path: 'roles',
              name: 'RoleManagement',
              component: () => import('pages/admin/RolePage.vue'),
              meta: { permissions: ['user:roles:manage'] },
            },
            {
              path: 'permission-scopes',
              name: 'PermissionScope',
              component: () => import('pages/admin/PermissionScopePage.vue'),
              meta: { permissions: ['user:roles:manage'] },
            },
          ],
        },
      ],
    },

    // 403
    {
      path: '/unauthorized',
      name: 'Unauthorized',
      component: () => import('pages/errors/UnauthorizedPage.vue'),
      meta: { public: true },
    },

    // 404
    {
      path: '/:pathMatch(.*)*',
      name: 'NotFound',
      component: () => import('pages/errors/NotFoundPage.vue'),
      meta: { public: true },
    },
  ],
})

// Navigation Guard — auth + izin kontrolü
router.beforeEach((to) => {
  if (to.meta.public) return true

  const authStore = useAuthStore()

  if (!authStore.isInitialized) return true

  if (!authStore.isAuthenticated) {
    return { name: 'Unauthorized' }
  }

  const requiredPermissions = to.meta.permissions as string[] | undefined
  if (requiredPermissions && requiredPermissions.length > 0) {
    if (!authStore.hasAnyPermission(requiredPermissions)) {
      return { name: 'Unauthorized' }
    }
  }

  return true
})

export default router
