<template>
  <q-badge
    :color="color"
    :label="slug"
    class="text-body2 q-px-sm q-py-xs"
  />
</template>

<script setup lang="ts">
import { computed } from 'vue'

interface Props {
  slug: string
}

const props = defineProps<Props>()

// Anlamsal renk paleti — aynı AŞAMADAKİ durumlar aynı tonu paylaşır, aşamalar arası ayrışır.
// (Etiket metni renk-körü kullanıcı için ayırt ediciliği zaten sağlar; renk ikincil sinyaldir.)
const PENDING = 'status-pending' //   bekleyen: onay/imza/başvuru bekliyor
const ACTIVE = 'status-active' //     olumlu/aktif: aktif, onaylandı, doğrulandı, ödendi
const SUCCESS = 'status-success' //   başarıyla tamamlandı (aktif yeşilden koyu — terminal başarı)
const PROGRESS = 'status-progress' // ara aşama: yerleştirildi, eşleştirildi
const INFO = 'status-info' //         bilgilendirici ara durum: imzalandı, yüklendi, hesaplandı
const WARNING = 'status-warning' //   uyarı: askıda, fesih talebi, süre doldu, itiraz
const NEGATIVE = 'status-negative' // olumsuz: red, fesih, iptal
const DONE = 'status-done' //         tamamlanmış: tamamlandı, transfer
const NEUTRAL = 'status-neutral' //   nötr: kayıtlı
const DRAFT = 'grey-6' //        taslak/pasif
const CLOSED = 'grey-9' //       kapatılmış

const STATUS_COLORS: Record<string, string> = {
  // Bekleyen (amber)
  'Onay Bekliyor': PENDING,
  'Onay Bekleniyor': PENDING,
  'İmza Bekliyor': PENDING,
  'İmzaya Sunuldu': PENDING,
  'Başvurdu': PENDING,
  'Dekont Bekleniyor': PENDING, //  PaymentPhase.AwaitingReceipt
  'Sözleşme Bekleniyor': PENDING, // InternshipPhase.AwaitingContract
  // Olumlu / aktif (yeşil)
  'Aktif': ACTIVE,
  'Aktif Staj': ACTIVE,
  'Onaylandı': ACTIVE,
  'Doğrulandı': ACTIVE,
  'Ödendi': ACTIVE,
  // Ara aşama (teal)
  'Yerleştirildi': PROGRESS,
  'Eşleştirildi': PROGRESS,
  'Yerleşti': PROGRESS, //                    InternshipPhase.Placed
  'Öğrenci Onayladı': PROGRESS, //            PaymentPhase.StudentConfirmed — onay zinciri sürüyor
  'Öğretmen Onayladı': PROGRESS, //           PaymentPhase.TeacherApproved — onay zinciri sürüyor
  'Müdür Yardımcısı Onayladı': PROGRESS, //   PaymentPhase.DeputyApproved — onay zinciri sürüyor
  // Bilgilendirici ara durum (cyan)
  'İmzalandı': INFO,
  'Yüklendi': INFO,
  'Dekont Yüklendi': INFO, // PaymentPhase.ReceiptUploaded
  'Hesaplandı': INFO,
  'Düzeltildi': INFO,
  // Uyarı (turuncu)
  'Askıda': WARNING,
  'Fesih Talep Edildi': WARNING,
  'Süresi Doldu': WARNING,
  'İtiraz Edildi': WARNING,
  'Fesih Sürecinde': WARNING, // InternshipPhase.TerminationInProgress
  // Olumsuz (kırmızı)
  'Reddedildi': NEGATIVE,
  'Feshedildi': NEGATIVE,
  'Feshedilmiş': NEGATIVE, //  ContractStatus.Terminated — "Feshedildi"nin sözleşmedeki farklı yazımı
  'İptal Edildi': NEGATIVE,
  'Kayıt Silindi': NEGATIVE, // StudentStatus.Deregistered
  // Başarıyla tamamlandı (yeşil — aktif yeşilden koyu ton)
  'Tamamladı': SUCCESS,
  'Tamamlandı': SUCCESS,
  'Tamamlanmış': SUCCESS, // ContractStatus.Completed slug'ı (önceden eşlemesizdi → gri)
  // Transfer / aktarılmış (mor)
  'Transfer Edildi': DONE,
  // Nötr / taslak (gri)
  'Kayıtlı': NEUTRAL,
  'Kaydedildi': NEUTRAL, // AttendanceStatus.Recorded
  'Taslak': DRAFT,
  'Pasif': DRAFT,
  'Kapatılmış': CLOSED,
  // Devamsızlık türü (AbsenceType slug'ları) — semantik ayrım
  'Mazeretli': ACTIVE, //       mazeretli: kabul edilebilir (yeşil)
  'Mazeretsiz': WARNING, //     mazeretsiz: dikkat gerektiren (turuncu)
  'Sağlık Raporu': INFO, //     belgeli/sağlık raporu (cyan)
  'Ücretli İzin': ACTIVE, //    AbsenceType.PaidLeave: kesinti doğurmaz (yeşil)
  'Ücretsiz İzin': WARNING, //  AbsenceType.UnpaidLeave: ücret kesilir (turuncu)
  // Ücretli izin başvurusu (PaidLeaveStatus slug'ları, #177) — zincirin iki adımı ayrı görünür
  'İşletme Onayı Bekliyor': PENDING,
  'Okul Onayı Bekliyor': PENDING,
  'Resmileşti': SUCCESS, //     iki onay tamamlandı, izin günleri kaydedildi
}

const color = computed(() => STATUS_COLORS[props.slug] ?? 'grey-7')
</script>
