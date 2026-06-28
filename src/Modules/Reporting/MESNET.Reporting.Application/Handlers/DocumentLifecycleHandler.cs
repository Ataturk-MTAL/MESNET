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
    public static async Task<NotifyDocumentStatusChanged> Handle(
        MarkDocumentAsPrinted command, IDocumentSession session)
    {
        var doc = await session.LoadAsync<GeneratedDocument>(command.DocumentId);
        if (doc is null)
            throw new DomainException(ReportingErrors.DocumentNotFound(command.DocumentId));

        if (!doc.Status.CanTransitionTo(PhysicalDocumentStatus.Printed))
            throw new DomainException(ReportingErrors.InvalidStatusTransition(
                doc.Status.Slug, PhysicalDocumentStatus.Printed.Slug));

        doc.MarkAsPrinted(command.User.FullName, command.User.UserId);
        session.Store(doc);

        return BuildStatusNotification(doc, PhysicalDocumentStatus.Printed, command.User);
    }

    // ─── İmzalanıp Teslim Edildi ───
    public static async Task<NotifyDocumentStatusChanged> Handle(
        MarkDocumentAsSignedAndReturned command, IDocumentSession session)
    {
        var doc = await session.LoadAsync<GeneratedDocument>(command.DocumentId);
        if (doc is null)
            throw new DomainException(ReportingErrors.DocumentNotFound(command.DocumentId));

        if (!doc.Status.CanTransitionTo(PhysicalDocumentStatus.SignedAndReturned))
            throw new DomainException(ReportingErrors.InvalidStatusTransition(
                doc.Status.Slug, PhysicalDocumentStatus.SignedAndReturned.Slug));

        doc.MarkAsSignedAndReturned(command.User.FullName, command.User.UserId);
        session.Store(doc);

        return BuildStatusNotification(doc, PhysicalDocumentStatus.SignedAndReturned, command.User);
    }

    // ─── Arşivlendi ───
    public static async Task<NotifyDocumentStatusChanged> Handle(
        MarkDocumentAsArchived command, IDocumentSession session)
    {
        var doc = await session.LoadAsync<GeneratedDocument>(command.DocumentId);
        if (doc is null)
            throw new DomainException(ReportingErrors.DocumentNotFound(command.DocumentId));

        if (!doc.Status.CanTransitionTo(PhysicalDocumentStatus.Archived))
            throw new DomainException(ReportingErrors.InvalidStatusTransition(
                doc.Status.Slug, PhysicalDocumentStatus.Archived.Slug));

        doc.MarkAsArchived(command.User.FullName, command.User.UserId);
        session.Store(doc);

        return BuildStatusNotification(doc, PhysicalDocumentStatus.Archived, command.User);
    }

    // ─── Tekil Silme ───
    public static async Task<NotifyDocumentDeleted> Handle(
        DeleteDocument command, IDocumentSession session, IFileStorageService storage)
    {
        var doc = await session.LoadAsync<GeneratedDocument>(command.DocumentId);
        if (doc is null)
            throw new DomainException(ReportingErrors.DocumentNotFound(command.DocumentId));

        if (!string.IsNullOrEmpty(doc.PdfStoragePath))
            await storage.DeleteFileAsync(BucketName, doc.PdfStoragePath);

        session.Delete(doc);

        return new NotifyDocumentDeleted(
            doc.Id, doc.FormType.Name, doc.InstitutionId, doc.TeacherId, command.User.FullName);
    }

    // ─── Toplu Silme ───
    public static async Task<NotifyDocumentDeleted> Handle(
        DeleteDocumentsBatch command, IDocumentSession session, IFileStorageService storage)
    {
        // Null/boş koleksiyon koruması: eksik veya boş gövdeyle gelen istek NRE yerine 422 döner.
        if (command.DocumentIds is null || command.DocumentIds.Count == 0)
            throw new DomainException(ReportingErrors.EmptyDocumentList());

        var docs = await session.LoadManyAsync<GeneratedDocument>(command.DocumentIds.ToArray());
        var notFoundIds = command.DocumentIds
            .Except(docs.Select(d => d.Id))
            .ToList();

        if (notFoundIds.Count > 0)
            throw new DomainException(ReportingErrors.DocumentDeleteFailed(
                notFoundIds.First(),
                $"{notFoundIds.Count} doküman bulunamadı."));

        foreach (var doc in docs)
        {
            if (!string.IsNullOrEmpty(doc.PdfStoragePath))
                await storage.DeleteFileAsync(BucketName, doc.PdfStoragePath);

            session.Delete(doc);
        }

        // Toplu silme için ilk dokümanın bilgileriyle notification gönder
        var first = docs.First();
        return new NotifyDocumentDeleted(
            first.Id, $"Toplu ({docs.Count} doküman)", first.InstitutionId, first.TeacherId, command.User.FullName);
    }

    private static NotifyDocumentStatusChanged BuildStatusNotification(
        GeneratedDocument doc, PhysicalDocumentStatus newStatus, UserContext user) =>
        new(doc.Id, doc.FormType.Name, newStatus.Name, newStatus.Slug,
            doc.InstitutionId, doc.TeacherId, doc.StudentId, user.FullName);
}
