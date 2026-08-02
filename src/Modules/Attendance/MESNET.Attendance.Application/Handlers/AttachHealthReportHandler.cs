using MESNET.Attendance.Application.Commands;
using MESNET.Attendance.Application.Errors;
using MESNET.Attendance.Core.Aggregates;
using MESNET.Attendance.Core.Enums;
using MESNET.Attendance.Shared.Events;
using MESNET.Common.Infrastructure.Security;
using MESNET.Common.Infrastructure.Storage;
using MESNET.Common.Shared;
using MESNET.Common.Shared.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Wolverine.Marten;

namespace MESNET.Attendance.Application.Handlers;

/// <summary>
/// Sağlık raporu ekleme (#172).
///
/// <para><b>Giriş geniş, hüküm dar.</b> Uç <c>attendance:upload</c> ister; işletme yetkilisi,
/// işletme İK, usta öğretici ve öğrenci de yükleyebilir. Kaydın hüküm doğurup doğurmadığına
/// burada karar verilir: yükleyende <c>attendance:health-report:direct</c> varsa (koordinatör
/// öğretmen, müdür yardımcısı, müdür) rapor doğrudan geçerlidir; yoksa <c>Pending</c> düşer ve
/// koordinatör öğretmen onaylayana kadar devamsızlık türü değişmez — yani ücret kesintisi
/// kalkmaz.</para>
///
/// <para>Karar <b>permission</b> ile verilir, rol adıyla değil (ADR-0001). Rol adı listesi yeni
/// bir işletme rolü eklendiğinde sessizce eksik kalırdı.</para>
/// </summary>
public static class AttachHealthReportHandler
{
    private const long MaxFileSizeBytes = 10 * 1024 * 1024; // 10 MB
    private const string PdfContentType = "application/pdf";
    private static readonly byte[] PdfMagicBytes = "%PDF-"u8.ToArray();

    // Taranmış rapor çoğunlukla fotoğraftır; PDF'e ek olarak JPEG ve PNG kabul edilir.
    // Her tür kendi magic byte imzasıyla doğrulanır — Content-Type başlığı istemciden gelir
    // ve tek başına güvenilmez.
    private static readonly Dictionary<string, byte[]> AllowedTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        [PdfContentType] = PdfMagicBytes,
        ["image/jpeg"] = [0xFF, 0xD8, 0xFF],
        ["image/png"] = [0x89, 0x50, 0x4E, 0x47]
    };

    private static readonly Dictionary<string, string> Extensions = new(StringComparer.OrdinalIgnoreCase)
    {
        [PdfContentType] = "pdf",
        ["image/jpeg"] = "jpg",
        ["image/png"] = "png"
    };

    [AggregateHandler]
    public static async Task<HealthReportAttached> Handle(
        AttachHealthReport command,
        AttendanceRecord? record,
        ICurrentUserService currentUser,
        IFileStorageService fileStorage,
        IOptions<MinioStorageOptions> minioOptions,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        if (record is null)
            throw new DomainException(AttendanceErrors.NotFound(command.AttendanceId));

        // Onay bekleyen rapor varken ikinci dosya yüklenemez: aksi hâlde öğretmenin gördüğü
        // belge ile onayladığı belge farklı olabilirdi.
        if (record.EffectiveReportStatus == HealthReportStatus.Pending)
            throw new DomainException(AttendanceErrors.HealthReportAlreadyPending(record.Id));

        // Veli kapsamı (#174): bağ kaydı olan kullanıcı yalnız kendi öğrencisine rapor
        // yükleyebilir. Öğrenci kimliği istekten değil KAYITTAN okunuyor — istekten alınsaydı
        // veli başka öğrencinin kimliğini göndererek kontrolü aşardı.
        ParentScopeGuard.EnsureCanAccessStudent(currentUser, record.StudentId);

        var contentType = ValidateFile(command.ReportFile);

        var attachedById = currentUser.GetUserId();
        var requiresApproval = !currentUser.HasPermission(Permissions.Attendance.HealthReportDirect);

        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        var objectPath =
            $"health-reports/{record.StudentId:N}/{record.Id:N}_{timestamp}.{Extensions[contentType]}";

        var metadata = new Dictionary<string, string>
        {
            ["x-amz-meta-attendance-id"] = record.Id.ToString(),
            ["x-amz-meta-student-id"] = record.StudentId.ToString(),
            ["x-amz-meta-attached-by"] = attachedById.ToString(),
            ["x-amz-meta-attached-at"] = DateTime.UtcNow.ToString("O"),
            ["x-amz-meta-requires-approval"] = requiresApproval.ToString()
        };

        await using var stream = command.ReportFile.OpenReadStream();
        var uploadResult = await fileStorage.UploadFileAsync(
            minioOptions.Value.DefaultBucket, objectPath, stream, contentType, metadata, cancellationToken);

        if (uploadResult.IsFailure)
        {
            loggerFactory.CreateLogger("AttachHealthReport")
                .LogError("MinIO yükleme başarısız: {Error}", uploadResult.Error.Description);
            throw new DomainException(uploadResult.Error);
        }

        return new HealthReportAttached(
            record.Id, record.StudentId, objectPath, DateTime.UtcNow, attachedById, requiresApproval);
    }

    /// <summary>Boyut, MIME türü ve magic byte doğrulaması. Geçerli içerik türünü döndürür.</summary>
    private static string ValidateFile(Microsoft.AspNetCore.Http.IFormFile file)
    {
        if (file.Length == 0)
            throw new DomainException(FileUploadError.FileNull());

        if (file.Length > MaxFileSizeBytes)
            throw new DomainException(FileUploadError.FileTooLarge(file.Length, MaxFileSizeBytes));

        if (!AllowedTypes.TryGetValue(file.ContentType, out var magicBytes))
            throw new DomainException(FileUploadError.InvalidFileType(file.ContentType));

        using var probe = file.OpenReadStream();
        var buffer = new byte[magicBytes.Length];
        var read = probe.Read(buffer, 0, buffer.Length);

        if (read < magicBytes.Length || !buffer.AsSpan().SequenceEqual(magicBytes))
            throw new DomainException(FileUploadError.InvalidFileContent());

        return file.ContentType;
    }
}
