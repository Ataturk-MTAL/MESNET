---
title: Ana Sayfa
slug: /
---

# MESNET — Mimari Dokümantasyon

**Mesleki Eğitim Stajları Nitelikli, Eşgüdümlü Takip Sistemi**

MESNET, mesleki eğitim staj süreçlerinin uçtan uca dijitalleştirilmesini hedefleyen modüler monolit bir .NET uygulamasıdır.

## Teknoloji Yığını

| Katman | Teknoloji |
| --- | --- |
| Runtime | .NET 10.0 |
| Veritabanı | PostgreSQL (JSONB document storage + event store) |
| ORM / Document DB | Marten |
| Messaging / CQRS | Wolverine |
| Frontend | Vue 3 + TypeScript + Pinia |
| Kimlik Doğrulama | Keycloak (OAuth2 / OIDC) |
| Mimari | Modüler Monolit + CQRS + Event Sourcing |

## Modüller

| Modül | Sorumluluk |
| --- | --- |
| [Institution](modules/institution) | Kurum bilgileri, şube/dal yönetimi |
| [Business](modules/business) | İşletme kayıt, onay, sektör yönetimi |
| [Enrollment](modules/enrollment) | Öğrenci/öğretmen kayıt, yerleştirme |
| [Contract](modules/contract) | Staj sözleşmeleri, imza, fesih |
| [Attendance](modules/attendance) | Devamsızlık takibi, iş takvimi |
| [Payment](modules/payment) | Dekont/maaş süreçleri |
| [Coordination](modules/coordination) | Ziyaret programı, departman dağılımı |
| [Internship](modules/internship) | Staj yaşam döngüsü orkestrasyonu (saga) |
| [Reporting](modules/reporting) | PDF rapor üretimi (QuestPDF) |

## Hızlı Bağlantılar

- [Proje Kapsamı](architecture/project-scope) — Phase 1 ve Phase 2 ayrımı
- [Modül Tasarımı](architecture/module-design) — Modül sınırları ve mimari kararlar
- [Senaryolar](scenarios) — Aktör bazlı iş senaryoları
- [Aktörler ve İzinler](./actors/) — Rol tanımları
- [C4 Diyagramlar](modules/c4-diagrams) — Sistem ve container diyagramları
