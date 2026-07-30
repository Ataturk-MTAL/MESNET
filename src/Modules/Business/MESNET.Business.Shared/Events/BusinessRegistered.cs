using MESNET.Common.Shared;

namespace MESNET.Business.Shared.Events;

public sealed record BusinessRegistered(
    Guid BusinessId,
    Guid InstitutionId,
    string Name,
    string? Address,
    Location? Location,
    // Modüller arası event: SmartEnum yerine Name string'i taşınır (RegistrationSource.Name)
    string Source,
    int TotalSlots = 0,
    List<string>? Sectors = null,
    string? PhoneNumber = null,
    string? Email = null,
    string? MasterInstructorName = null,
    // 3308 Madde 25: staj ücreti işletmenin personel sayısına göre değişiyor (20 altı/üstü).
    // Payment modülü bu bilgiyi başka modülün şemasından okuyamaz, olayla taşınmalı (#64).
    int PersonnelCount = 0,
    // İşletme Yetkilisi (BusinessRepresentative.FullName) — Dönem Not Fişi'nin (Form 8) imza
    // bloğunda basılır. Reporting başka modülün şemasını okuyamaz, olayla taşınmalı (#99).
    string? RepresentativeName = null,
    // 3308 Geçici Madde 12: kamu kurum ve kuruluşlarına Devlet katkısı ödenmez (#157).
    // Payment bu bilgiyi başka modülün şemasından okuyamaz, olayla taşınmalı.
    // Varsayılan false: bu alandan önce yazılmış olaylar özel işletme olarak deserialize olur.
    bool IsPublicInstitution = false);
