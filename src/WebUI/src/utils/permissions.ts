import { useAuthStore } from 'stores/auth'

/**
 * MESNET Phase 1 izin sabitleri.
 * Backend: src/MESNET.Common.Shared/Security/Permissions.cs ile senkronize.
 * Format: "kaynak:eylem"
 * Wildcard: "student:*" → student altındaki tüm izinleri kapsar.
 */
export const Permissions = {
  Institution: {
    View: 'institution:view',
    Manage: 'institution:manage',
    Delete: 'institution:delete',
    Staff: 'institution:staff:manage',
    Report: 'institution:report:view',
    ManageGradeWindow: 'institution:grade-window:manage',
    /**
     * Kurum geneli koordinasyon yapılandırmasını DEĞİŞTİRME yetkisi (#130) —
     * mesafe-saat kuralları, büyükşehir sınırı, azami haftalık ek ders saati.
     * Okuma ayrıdır: `DepartmentHead.Distribution` ile açıktır (alan şefi görür, yazamaz).
     * Görünürlük kararı her zaman bu izne bakar, rol adına DEĞİL.
     */
    CoordinationConfigManage: 'institution:coordination-config:manage',
  },

  Student: {
    View: 'student:view',
    Manage: 'student:manage',
    ViewOwn: 'student:view-own',
    UpdateOwn: 'student:update-own',
    Attendance: 'student:attendance:manage',
    Salary: 'student:salary:manage',
  },

  // ── Phase 2 ────────────────────────────────────────────────────────────────
  // Protocol: MEB protokolü modülü henüz implement edilmedi.
  // Backend modülü oluşturulduğunda bu bloğu aktif hale getir
  // ve router/index.ts ile MainLayout.vue'daki yorum satırlarını aç.
  //
  // Protocol: {
  //   View: 'protocol:view',
  //   Create: 'protocol:create',
  //   Approve: 'protocol:approve',
  //   Manage: 'protocol:manage',
  //   Program: 'protocol:program:manage',
  // },
  // ────────────────────────────────────────────────────────────────────────────

  Company: {
    View: 'company:view',
    Manage: 'company:manage',
    Document: 'company:document:manage',
    Student: 'company:student:manage',
    Visit: 'company:visit:manage',
    RequestStudent: 'company:student:request',
    Attendance: 'company:attendance:manage',
    UploadReceipt: 'company:receipt:upload',
    MasterTrainer: 'company:trainer:manage',
  },

  Internship: {
    Apply: 'internship:apply',
    View: 'internship:view',
    Review: 'internship:review',
    Approve: 'internship:approve',
    ViewOwn: 'internship:view-own',
    Manage: 'internship:manage',
    Contract: 'internship:contract:manage',
    Report: 'internship:report:manage',
  },

  Attendance: {
    View: 'attendance:view',
    ViewOwn: 'attendance:view-own',
    Manage: 'attendance:manage',
    Report: 'attendance:report',
    // Sağlık raporu yükleme (#172). Giriş geniştir: işletme, usta öğretici, işletme İK ve
    // öğrenci de yükler — ama yükleme tek başına hüküm doğurmaz.
    Upload: 'attendance:upload',
    Approve: 'attendance:approve',
    Delete: 'attendance:delete',
    // Girilen kaydın onay beklemeden geçerli olması (#172). Yalnız okul rollerindedir;
    // arayüzde "onay bekliyor" rozetini gizlemek ve düzeltme butonunu göstermek için okunur.
    DirectEntry: 'attendance:direct-entry',
    HealthReportDirect: 'attendance:health-report:direct',
    // MESEM ücretli izin başvurusu (#177). Başvuru öğrencinin, 1. onay işletmenin, 2. onay
    // okulun. İşletme adımını izin değil `business_id` KAPSAMI bağlar — okul müdürü
    // `attendance:*` wildcard'ıyla bu izne de sahiptir, ama o claim'i yoktur.
    LeaveRequest: 'attendance:leave:request',
    LeaveBusinessApprove: 'attendance:leave:business-approve',
    LeaveApprove: 'attendance:leave:approve',
  },

  Salary: {
    View: 'salary:view',
    ViewOwn: 'salary:view-own',
    Calculate: 'salary:calculate',
    Approve: 'salary:approve',
    Receipt: 'salary:receipt:manage',
    // Görüntüleme okullarda; YAZMA ulusal izindir (#147) → Platform.ParameterManage
    ParameterView: 'salary:parameter:view',
  },

  // Ulusal (kurum üstü) izinler (#147). Hiçbir okul rolünde yoktur.
  Platform: {
    ParameterManage: 'platform:parameter:manage',
  },

  Coordinator: {
    Assign: 'coordinator:assign',
    Schedule: 'coordinator:schedule:manage',
    Visit: 'coordinator:visit:manage',
    Report: 'coordinator:report:manage',
    Communication: 'coordinator:communication',
  },

  DepartmentHead: {
    Distribution: 'department:distribution:manage',
    Workload: 'department:workload:view',
    TeacherAssign: 'department:teacher:assign',
    ScheduleView: 'department:schedule:view',
  },

  Document: {
    View: 'document:view',
    Upload: 'document:upload',
    Approve: 'document:approve',
    Scan: 'document:scan',
    Verify: 'document:verify',
    Track: 'document:track',
  },

  Communication: {
    SendMessage: 'communication:send',
    ViewMessages: 'communication:view',
    ReportIssue: 'communication:issue:report',
    ManageIssues: 'communication:issue:manage',
  },

  UserManagement: {
    View: 'user:view',
    Create: 'user:create',
    Update: 'user:update',
    Delete: 'user:delete',
    RolesManage: 'user:roles:manage',
    Approve: 'user:approve',
  },
} as const

export type Permission = (typeof Permissions)[keyof typeof Permissions][keyof (typeof Permissions)[keyof typeof Permissions]]

// Composable — bileşenler ve composable'lar için
export function usePermissions() {
  const authStore = useAuthStore()

  return {
    hasPermission: (permission: string) => authStore.hasPermission(permission),
    hasAnyPermission: (permissions: string[]) => authStore.hasAnyPermission(permissions),
    hasAllPermissions: (permissions: string[]) => authStore.hasAllPermissions(permissions),
  }
}
