using Marten;
using MESNET.Common.Infrastructure.Storage;
using MESNET.Common.Shared;
using MESNET.Common.Shared.Security;
using MESNET.Reporting.Application.Commands;
using MESNET.Reporting.Application.Errors;
using MESNET.Reporting.Core.Entities;
using MESNET.Reporting.Core.Enums;

namespace MESNET.Reporting.Application.Handlers;

public static class DocumentLifecycleHandler
{
    private const string BucketName = "meb-forms";

    // ─── Yazdırıldı ───
    public static async Task<(Result, NotifyDocumentStatusChanged?)> Handle(
        MarkDocumentAsPrinted command, IDocumentSession session)
    {
        var doc = await session.LoadAsync<GeneratedDocument>(command.DocumentId);
        if (doc is null)
            return (Result.Failure(ReportingErrors.DocumentNotFound(command.DocumentId)), null);

        if (!doc.Status.CanTransitionTo(PhysicalDocumentStatus.Printed))
            return (Result.Failure(ReportingErrors.InvalidStatusTransition(
                doc.Status.Slug, PhysicalDocumentStatus.Printed.Slug)), null);

        doc.MarkAsPrinted(command.User.FullName, command.User.UserId);
        session.Store(doc);

        return (Result.Success(), BuildStatusNotification(doc, PhysicalDocumentStatus.Printed, command.User));
    }

    // ─── İmzalanıp Teslim Edildi ───
    public static async Task<(Result, NotifyDocumentStatusChanged?)> Handle(
        MarkDocumentAsSignedAndReturned command, IDocumentSession session)
    {
        var doc = await session.LoadAsync<GeneratedDocument>(command.DocumentId);
        if (doc is null)
            return (Result.Failure(ReportingErrors.DocumentNotFound(command.DocumentId)), null);

        if (!doc.Status.CanTransitionTo(PhysicalDocumentStatus.SignedAndReturned))
            return (Result.Failure(ReportingErrors.InvalidStatusTransition(
                doc.Status.Slug, PhysicalDocumentStatus.SignedAndReturned.Slug)), null);

        doc.MarkAsSignedAndReturned(command.User.FullName, command.User.UserId);
        session.Store(doc);

        return (Result.Success(), BuildStatusNotification(doc, PhysicalDocumentStatus.SignedAndReturned, command.User));
    }

    // ─── Arşivlendi ───
    public static async Task<(Result, NotifyDocumentStatusChanged?)> Handle(
        MarkDocumentAsArchived command, IDocumentSession session)
    {
        var doc = await session.LoadAsync<GeneratedDocument>(command.DocumentId);
        if (doc is null)
            return (Result.Failure(ReportingErrors.DocumentNotFound(command.DocumentId)), null);

        if (!doc.Status.CanTransitionTo(PhysicalDocumentStatus.Archived))
            return (Result.Failure(ReportingErrors.InvalidStatusTransition(
                doc.Status.Slug, PhysicalDocumentStatus.Archived.Slug)), null);

        doc.MarkAsArchived(command.User.FullName, command.User.UserId);
        session.Store(doc);

        return (Result.Success(), BuildStatusNotification(doc, PhysicalDocumentStatus.Archived, command.User));
    }

    // ─── Tekil Silme ───
    public static async Task<(Result, NotifyDocumentDeleted?)> Handle(
        DeleteDocument command, IDocumentSession session, IFileStorageService storage)
    {
        var doc = await session.LoadAsync<GeneratedDocument>(command.DocumentId);
        if (doc is null)
            return (Result.Failure(ReportingErrors.DocumentNotFound(command.DocumentId)), null);

        if (!string.IsNullOrEmpty(doc.PdfStoragePath))
            await storage.DeleteFileAsync(BucketName, doc.PdfStoragePath);

        session.Delete(doc);

        return (Result.Success(), new NotifyDocumentDeleted(
            doc.Id, doc.FormType.Name, doc.InstitutionId, doc.TeacherId, command.User.FullName));
    }

    // ─── Toplu Silme ───
    public static async Task<(Result, NotifyDocumentDeleted?)> Handle(
        DeleteDocumentsBatch command, IDocumentSession session, IFileStorageService storage)
    {
        var docs = await session.LoadManyAsync<GeneratedDocument>(command.DocumentIds.ToArray());
        var notFoundIds = command.DocumentIds
            .Except(docs.Select(d => d.Id))
            .ToList();

        if (notFoundIds.Count > 0)
            return (Result.Failure(ReportingErrors.DocumentDeleteFailed(
                notFoundIds.First(),
                $"{notFoundIds.Count} doküman bulunamadı.")), null);

        foreach (var doc in docs)
        {
            if (!string.IsNullOrEmpty(doc.PdfStoragePath))
                await storage.DeleteFileAsync(BucketName, doc.PdfStoragePath);

            session.Delete(doc);
        }

        // Toplu silme için ilk dokümanın bilgileriyle notification gönder
        var first = docs.First();
        return (Result.Success(), new NotifyDocumentDeleted(
            first.Id, $"Toplu ({docs.Count} doküman)", first.InstitutionId, first.TeacherId, command.User.FullName));
    }

    private static NotifyDocumentStatusChanged BuildStatusNotification(
        GeneratedDocument doc, PhysicalDocumentStatus newStatus, UserContext user) =>
        new(doc.Id, doc.FormType.Name, newStatus.Name, newStatus.Slug,
            doc.InstitutionId, doc.TeacherId, doc.StudentId, user.FullName);
}
