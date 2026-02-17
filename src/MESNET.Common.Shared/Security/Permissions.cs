namespace MESNET.Common.Shared.Security;

/// <summary>
/// MESNET Phase 1 izin sabitleri.
/// Her izin "kaynak:eylem" formatındadır.
/// Wildcard desteği: "student:*" → student altındaki tüm izinleri kapsar.
/// </summary>
public static class Permissions
{
    public static class Institution
    {
        public const string View = "institution:view";
        public const string Manage = "institution:manage";
        public const string Delete = "institution:delete";
        public const string Staff = "institution:staff:manage";
        public const string Report = "institution:report:view";
    }

    public static class Student
    {
        public const string View = "student:view";
        public const string Manage = "student:manage";
        public const string ViewOwn = "student:view-own";
        public const string UpdateOwn = "student:update-own";
        public const string Attendance = "student:attendance:manage";
        public const string Salary = "student:salary:manage";
    }

    public static class Protocol
    {
        public const string View = "protocol:view";
        public const string Create = "protocol:create";
        public const string Approve = "protocol:approve";
        public const string Manage = "protocol:manage";
        public const string Program = "protocol:program:manage";
    }

    public static class Company
    {
        public const string View = "company:view";
        public const string Manage = "company:manage";
        public const string Document = "company:document:manage";
        public const string Student = "company:student:manage";
        public const string Visit = "company:visit:manage";
        public const string RequestStudent = "company:student:request";
        public const string Attendance = "company:attendance:manage";
        public const string UploadReceipt = "company:receipt:upload";
        public const string MasterTrainer = "company:trainer:manage";
    }

    public static class Internship
    {
        public const string Apply = "internship:apply";
        public const string Review = "internship:review";
        public const string Approve = "internship:approve";
        public const string ViewOwn = "internship:view-own";
        public const string Manage = "internship:manage";
        public const string Contract = "internship:contract:manage";
        public const string Report = "internship:report:manage";
    }

    public static class Attendance
    {
        public const string View = "attendance:view";
        public const string ViewOwn = "attendance:view-own";
        public const string Manage = "attendance:manage";
        public const string Report = "attendance:report";
        public const string Upload = "attendance:upload";
        public const string Approve = "attendance:approve";
    }

    public static class Salary
    {
        public const string View = "salary:view";
        public const string ViewOwn = "salary:view-own";
        public const string Calculate = "salary:calculate";
        public const string Approve = "salary:approve";
        public const string Receipt = "salary:receipt:manage";
        public const string Parameter = "salary:parameter:manage";
    }

    public static class Coordinator
    {
        public const string Assign = "coordinator:assign";
        public const string Schedule = "coordinator:schedule:manage";
        public const string Visit = "coordinator:visit:manage";
        public const string Report = "coordinator:report:manage";
        public const string Communication = "coordinator:communication";
    }

    public static class DepartmentHead
    {
        public const string Distribution = "department:distribution:manage";
        public const string Workload = "department:workload:view";
        public const string TeacherAssign = "department:teacher:assign";
        public const string ScheduleView = "department:schedule:view";
    }

    public static class Document
    {
        public const string View = "document:view";
        public const string Upload = "document:upload";
        public const string Approve = "document:approve";
        public const string Scan = "document:scan";
        public const string Verify = "document:verify";
        public const string Track = "document:track";
    }

    public static class Communication
    {
        public const string SendMessage = "communication:send";
        public const string ViewMessages = "communication:view";
        public const string ReportIssue = "communication:issue:report";
        public const string ManageIssues = "communication:issue:manage";
    }

    public static class UserManagement
    {
        public const string View = "user:view";
        public const string Create = "user:create";
        public const string Update = "user:update";
        public const string Delete = "user:delete";
        public const string RolesManage = "user:roles:manage";
        public const string Approve = "user:approve";
    }

    /// <summary>
    /// Tüm permission sabitlerini reflection ile toplar.
    /// Policy oluşturma ve UI listeleme için kullanılır.
    /// </summary>
    public static IReadOnlyList<string> GetAll()
    {
        return typeof(Permissions)
            .GetNestedTypes()
            .SelectMany(t => t.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static))
            .Where(f => f.IsLiteral && !f.IsInitOnly && f.FieldType == typeof(string))
            .Select(f => (string)f.GetRawConstantValue()!)
            .ToList();
    }
}
