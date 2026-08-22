using JasperFx;
using MESNET.Attendance.Application.Guards;
using Microsoft.AspNetCore.Http;

namespace MESNET.Attendance.Application.Commands;

/// <summary>
/// Devamsızlık kaydına sağlık raporu ekle (#172).
///
/// <para>Belge artık dışarıdan hazır bir URL olarak GELMEZ; dosya yüklenir ve MinIO'ya yazılır.
/// Önceki hâlinde uç serbest metin <c>ReportUrl</c> alıyordu — yani rapor dosyası sistemde hiç
/// tutulmuyordu ve onaylayacak öğretmenin göreceği bir belge yoktu.</para>
/// </summary>
public sealed record AttachHealthReport(
    [property: Identity] Guid AttendanceId,
    IFormFile ReportFile) : IAttendancePeriodScoped;
