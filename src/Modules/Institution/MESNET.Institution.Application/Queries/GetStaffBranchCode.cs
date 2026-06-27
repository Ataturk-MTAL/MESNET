namespace MESNET.Institution.Application.Queries;

/// <summary>
/// Verilen Keycloak kullanıcısının personel kaydındaki branş kodunu çözer
/// (alan şefi / DepartmentHead kendi alanını görür).
/// </summary>
public sealed record GetStaffBranchCode(string KeycloakId);

/// <summary>
/// Nullable branchCode'u sarmalar — Wolverine <c>InvokeAsync&lt;T?&gt;</c> null dönüşte fırlatır,
/// bu yüzden sonucu non-null bir record içinde döndürürüz.
/// </summary>
public sealed record StaffBranchCodeResult(string? BranchCode);
