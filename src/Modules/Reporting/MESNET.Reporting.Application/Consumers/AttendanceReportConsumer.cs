using Marten;
using MESNET.Attendance.Shared.Events;
using MESNET.Reporting.Core.ReadModels;

namespace MESNET.Reporting.Application.Consumers;

/// <summary>
/// Attendance modülü event'lerini dinleyerek StudentAttendanceReportView read model'ini günceller.
/// Aylık devamsızlık formu PDF üretimi için veri kaynağı oluşturur.
/// </summary>
public static class AttendanceReportConsumer
{
    public static async Task Consume(AttendanceMarked @event, IDocumentSession session)
    {
        var date = @event.Date;
        var year = date.Year;
        var month = date.Month;
        var day = date.Day;

        var view = await FindOrCreate(session, @event.StudentId, @event.InstitutionId,
            @event.AcademicPeriodId, year, month);

        // Aynı gün için zaten kayıt varsa güncelle
        var existing = view.AbsentDays.FindIndex(a => a.Day == day);
        if (existing >= 0)
            view.AbsentDays[existing] = new AbsentDayEntry(day, @event.AbsenceType);
        else
            view.AbsentDays.Add(new AbsentDayEntry(day, @event.AbsenceType));

        session.Store(view);
    }

    public static async Task Consume(AttendanceCorrected @event, IDocumentSession session)
    {
        // AttendanceCorrected'da tarih bilgisi yok, sadece AttendanceId ve StudentId var.
        // StudentId + yeni AbsenceType bilgisi ile tüm aylık kayıtları güncelle.
        // Not: Bu consumer AttendanceId'den tarihi çıkaramaz — bu aşamada bir sınırlama.
        // Gerçek uygulamada AttendanceCorrected event'ine tarih bilgisi eklenmeli.
    }

    public static async Task Consume(AttendanceDeleted @event, IDocumentSession session)
    {
        // AttendanceDeleted'da tarih bilgisi yok — benzer sınırlama.
        // Gelecekte event genişletilmeli.
    }

    private static async Task<StudentAttendanceReportView> FindOrCreate(
        IDocumentSession session, Guid studentId, Guid institutionId,
        Guid academicPeriodId, int year, int month)
    {
        // Deterministic ID: StudentId + Year + Month
        var id = GenerateDeterministicId(studentId, year, month);

        var view = await session.LoadAsync<StudentAttendanceReportView>(id);
        if (view is not null) return view;

        return new StudentAttendanceReportView
        {
            Id = id,
            StudentId = studentId,
            InstitutionId = institutionId,
            AcademicPeriodId = academicPeriodId,
            Year = year,
            Month = month,
            AbsentDays = []
        };
    }

    private static Guid GenerateDeterministicId(Guid studentId, int year, int month)
    {
        var bytes = studentId.ToByteArray();
        bytes[0] ^= (byte)(year & 0xFF);
        bytes[1] ^= (byte)((year >> 8) & 0xFF);
        bytes[2] ^= (byte)(month & 0xFF);
        return new Guid(bytes);
    }
}
