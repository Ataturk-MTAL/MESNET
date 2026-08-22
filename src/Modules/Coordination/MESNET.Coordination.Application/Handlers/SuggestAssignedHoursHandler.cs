using Marten;
using MESNET.Common.Shared;
using MESNET.Coordination.Application.Dtos;
using MESNET.Coordination.Application.Errors;
using MESNET.Coordination.Application.Queries;
using MESNET.Coordination.Core.Entities;
using MESNET.Coordination.Core.Enums;
using MESNET.Coordination.Core.ReadModels;
using MESNET.Coordination.Core.Services;

namespace MESNET.Coordination.Application.Handlers;

/// <summary>
/// Havuz dağıtım önerisini üretir (issue #116). <b>Salt okunur</b> — hiçbir yan etkisi yoktur.
///
/// <para>Handler yalnız <b>toplama ve çevirme</b> yapar; dağıtım mantığının tamamı saf
/// <see cref="HoursAllocationCalculator"/>, kapasite hesabı saf
/// <see cref="TeacherCapacityCalculator"/> içindedir (Marten oturumu gerektirmeden
/// birim testlenebilsinler).</para>
///
/// <para>Toplanan girdiler:</para>
/// <list type="bullet">
///   <item><description><c>P</c> ← <see cref="BranchWorkloadConfig.TotalWorkloadPool"/></description></item>
///   <item><description><c>max_i</c> ← <see cref="BusinessCoordinationView.MaxCoordinationHours"/></description></item>
///   <item><description><c>s_i</c> ← <see cref="BusinessCoordinationView.ActiveStudentCount"/> (alan bazlı, #114)</description></item>
///   <item><description><c>C</c> ← alan öğretmenlerinin boş slotu ve kalan ek ders kotası</description></item>
/// </list>
/// </summary>
public static class SuggestAssignedHoursHandler
{
    public static async Task<HoursSuggestionDto> Handle(
        SuggestAssignedHours query,
        IQuerySession session,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(query.BranchCode) || query.AcademicPeriodId == Guid.Empty)
            throw new DomainException(CoordinationErrors.BranchScopeRequired());

        if (!AcademicSemester.TryFromName(query.Semester, true, out _))
            throw new DomainException(CoordinationErrors.InvalidSemester(query.Semester));

        if (!PinnedHoursSelection.TryParse(query.Pinned, out var pinned, out var pinError))
            throw new DomainException(CoordinationErrors.InvalidPinnedHoursSelection(pinError!));

        var rows = await LoadBranchRowsAsync(session, query, cancellationToken);
        var pool = await LoadWorkloadPoolAsync(session, query, cancellationToken);
        var capacity = await CalculateTeacherCapacityAsync(session, query, cancellationToken);

        var result = HoursAllocationCalculator.Allocate(
            BuildInputs(rows, pinned), pool, capacity);

        return ToDto(result, rows);
    }

    /// <summary>
    /// Alanın işletme satırları. Temel satırlar (boş alan kodu) işletme düzeyi ortak
    /// bilgileri taşır, dağıtıma girmez (#114).
    /// </summary>
    private static async Task<Dictionary<Guid, BusinessCoordinationView>> LoadBranchRowsAsync(
        IQuerySession session,
        SuggestAssignedHours query,
        CancellationToken cancellationToken)
    {
        var rows = await session.Query<BusinessCoordinationView>()
            .Where(v =>
                v.InstitutionId == query.InstitutionId &&
                v.BranchCode == query.BranchCode &&
                v.AcademicPeriodId == query.AcademicPeriodId)
            .ToListAsync(cancellationToken);

        // Çok-alanlı modele geçmeden önce yazılmış kayıtlar aynı işletme kimliğini
        // paylaşabilir; algoritma satır başına benzersiz kimlik beklediği için ilk satır
        // temsilci seçilir.
        return rows
            .GroupBy(r => r.ResolveBusinessId())
            .ToDictionary(g => g.Key, g => g.First());
    }

    private static List<AllocationInput> BuildInputs(
        Dictionary<Guid, BusinessCoordinationView> rows,
        IReadOnlyList<PinnedHours> pinned)
    {
        var pinnedById = pinned.ToDictionary(p => p.BusinessId, p => p.Hours);

        return rows
            .Select(entry =>
            {
                var isPinned = pinnedById.TryGetValue(entry.Key, out var pinnedHours);
                return new AllocationInput(
                    BusinessId: entry.Key,
                    BranchCode: entry.Value.BranchCode,
                    MaxHours: entry.Value.MaxCoordinationHours,
                    StudentCount: entry.Value.ActiveStudentCount,
                    IsPinned: isPinned,
                    PinnedHours: isPinned ? pinnedHours : 0);
            })
            .ToList();
    }

    /// <summary>Havuz yapılandırması yoksa 0 — algoritma bunu "havuz tanımsız" sayar.</summary>
    private static async Task<int> LoadWorkloadPoolAsync(
        IQuerySession session,
        SuggestAssignedHours query,
        CancellationToken cancellationToken)
    {
        var config = await session.Query<BranchWorkloadConfig>()
            .FirstOrDefaultAsync(c =>
                c.InstitutionId == query.InstitutionId &&
                c.BranchCode == query.BranchCode &&
                c.AcademicPeriodId == query.AcademicPeriodId,
                cancellationToken);

        return config?.TotalWorkloadPool ?? 0;
    }

    /// <summary>
    /// Alan öğretmenlerinin kalan kapasitesi (<c>C</c>).
    ///
    /// <para>Öğretmen kümesi ve boş slot/atanmış saat verisi
    /// <see cref="GetAllTeachersOverviewHandler"/>'ın ürettiği özet satırlardan gelir —
    /// ders programı taraması orada zaten yapılıyor, ikinci bir kopyası tutulmaz.
    /// Bu bir <b>doğrudan statik çağrı</b>dır; mesaj yayınlanmaz, işlem sınırı değişmez.</para>
    ///
    /// <para><see cref="CoordinationConfig"/> yoksa kapasite 0 döner: yapılandırma
    /// okunamadığında "sınırsız kapasite" varsaymak, havuzun tamamını sessizce alan içi
    /// göstermek olurdu.</para>
    /// </summary>
    private static async Task<int> CalculateTeacherCapacityAsync(
        IQuerySession session,
        SuggestAssignedHours query,
        CancellationToken cancellationToken)
    {
        var config = await session.Query<CoordinationConfig>()
            .FirstOrDefaultAsync(c => c.InstitutionId == query.InstitutionId, cancellationToken);

        var maxWeeklyExtraHours = config?.MaxWeeklyExtraHours ?? 0;
        if (maxWeeklyExtraHours <= 0) return 0;

        var teachers = await GetAllTeachersOverviewHandler.Handle(
            new GetAllTeachersOverview(
                query.InstitutionId, query.AcademicPeriodId, query.Semester, query.BranchCode),
            session,
            cancellationToken);

        var inputs = teachers
            .Select(t => new TeacherCapacityInput(
                t.TeacherId,
                FreeSlotTotal: t.FreeSlotsByDay.Values.Sum(),
                AssignedBillableHours: t.AssignedHours))
            .ToList();

        return TeacherCapacityCalculator.Calculate(inputs, maxWeeklyExtraHours);
    }

    private static HoursSuggestionDto ToDto(
        AllocationResult result,
        Dictionary<Guid, BusinessCoordinationView> rows) =>
        new(
            result.Lines.Select(line => new HoursSuggestionLineDto(
                line.BusinessId,
                rows.TryGetValue(line.BusinessId, out var row) ? row.Name : string.Empty,
                line.BranchCode,
                line.MaxHours,
                line.StudentCount,
                line.Weight,
                line.SuggestedHours,
                line.IsPinned,
                line.IsHonoraryVisit,
                line.Bucket.Name,
                line.Bucket.Slug)).ToList(),
            new HoursSuggestionDiagnosticsDto(
                result.Diagnostics.Pool,
                result.Diagnostics.TeacherCapacity,
                result.Diagnostics.SumOfMax,
                result.Diagnostics.TotalAllocated,
                result.Diagnostics.Undistributed,
                result.Diagnostics.HonoraryCount,
                result.Diagnostics.OutOfBranchHours,
                result.Diagnostics.IsPoolUndefined));
}
