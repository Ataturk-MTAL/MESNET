<template>
  <q-badge
    :color="color"
    :label="slug"
    class="text-body2 q-px-sm q-py-xs"
  />
</template>

<script lang="ts">
/*
 * Modül kapsamlı blok: bir kez çalışır, örnek başına DEĞİL.
 *
 * `<script setup>` her rozet örneği için yeniden koşar; renk haritası ile "bu slug için
 * zaten uyardık" kaydı orada dursaydı tablo satırı başına yeniden kurulur, uyarı da satır
 * sayısı kadar tekrarlanırdı.
 */
import { logger } from 'src/utils/logger'

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
// Gri tonlar tema türevi DEĞİL, ham Quasar palet sabitidir. Hex'ler
// node_modules/quasar/src/css/variables.sass'tan doğrulandı — Quasar'ın gri ölçeği
// Material'ınkinden BİR BASAMAK KAYIKTIR (grey-6 = #9e9e9e, grey-7 = #757575,
// grey-8 = #616161; Material'da #616161 grey-700'dür). Ezberden yazma, dosyadan doğrula.
// q-badge metni her zaman beyazdır (QBadge.sass: `color: #fff`), bu yüzden zemin tonu
// beyaz metinle 4,5:1 metin eşiğini geçmek ZORUNDADIR.
const DRAFT = 'grey-8' //  taslak/pasif — #616161, beyaz metinle 6,19:1 (eşik 4,5:1).
//                         Önceki grey-6 (#9e9e9e) 2,68:1 ile eşiği GEÇMİYORDU. grey-7
//                         (#757575) 4,61:1 verip eşiğe teğet kalırdı; depodaki anlamsal
//                         tonlar 4,8–6,6 aralığında olduğu için rahat geçen ton seçildi.
const CLOSED = 'grey-9' // kapatılmış — #424242, beyaz metinle 10,05:1; zaten geçiyordu.
//                         DRAFT ile arası ölçüldü: 1,62:1 (grey-6 döneminde 3,75:1 idi).
//                         Ton farkı hâlâ gözle seçilir ama artık zayıftır ve WCAG 1.4.11
//                         grafik eşiğini (3:1) karşılamaz — karşılamak zorunda da değildir:
//                         rozet her zaman etiket metni taşır ("Taslak"/"Pasif" ↔
//                         "Kapatılmış"), DESIGN.md "Renk Yalnız Kanıt Kuralı" gereği renk
//                         ikincil sinyaldir. İki tonu daha da yaklaştırma.

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
  'Uygun': ACTIVE, // EvaluationResult.Suitable — işletme değerlendirmesi olumlu sonuçlandı
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
  'Gönderildi': INFO, // ReportStatus / VisitStatus / StudentTermGradeStatus.Submitted — onay bekleyen ara adım
  // Uyarı (turuncu)
  'Askıda': WARNING,
  'Fesih Talep Edildi': WARNING,
  'Süresi Doldu': WARNING,
  'İtiraz Edildi': WARNING,
  'Fesih Sürecinde': WARNING, // InternshipPhase.TerminationInProgress
  'Şartlı Uygun': WARNING, // EvaluationResult.Conditional slug'ı — koşullu uygunluk, dikkat gerektirir
  'Şartlı': WARNING, //       BusinessEvaluationsPage kısaltılmış etiketi; aynı sonucun ekrandaki yazımı
  // Olumsuz (kırmızı)
  'Reddedildi': NEGATIVE,
  'Feshedildi': NEGATIVE,
  'Feshedilmiş': NEGATIVE, //  ContractStatus.Terminated — "Feshedildi"nin sözleşmedeki farklı yazımı
  'İptal Edildi': NEGATIVE,
  'Kayıt Silindi': NEGATIVE, // StudentStatus.Deregistered
  'Uygun Değil': NEGATIVE, //  EvaluationResult.Unsuitable — işletme staja uygun bulunmadı
  'Başarısız': NEGATIVE, //    ExamResult.Failed — beceri sınavı sonucu
  // Başarıyla tamamlandı (yeşil — aktif yeşilden koyu ton)
  'Tamamladı': SUCCESS,
  'Tamamlandı': SUCCESS,
  'Tamamlanmış': SUCCESS, // ContractStatus.Completed slug'ı (önceden eşlemesizdi → gri)
  'Başarılı': SUCCESS, //     ExamResult.Passed — beceri sınavı geçildi (terminal başarı)
  'Kesinleşti': SUCCESS, //   StudentTermGradeStatus.Finalized — not fişi kesinleşti, geri alınmaz
  // Transfer / aktarılmış (mor)
  'Transfer Edildi': DONE,
  // Nötr / taslak (gri)
  'Kayıtlı': NEUTRAL,
  'Kaydedildi': NEUTRAL, // AttendanceStatus.Recorded
  'Yok': NEUTRAL, //        HealthReportStatus.None — rapor girilmemiş; durum değil, yokluk bildirimi
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
  // Kategori rozeti (EducationType slug'ları) — aşama DEĞİL, sınıflandırma.
  // DESIGN.md "Chips / Badges": anlamsal durum taşımayan etiket nötr tonu kullanır;
  // positive/info gibi bir role bağlamak yanlış anlam yükler.
  'Örgün': NEUTRAL,
  'MESEM': NEUTRAL,
}

/**
 * Eşleşmeyen slug SESSİZCE gri düşer: rozet okunur görünmeye devam eder, yalnız ANLAM
 * kaybolur (feshedilmiş sözleşme kırmızı yerine gri, başarısız sınav nötr). Tam bu sessizlik
 * yüzünden dört sayfada renk anlamı fark edilmeden yitirilmişti. Düşüşü geliştirme
 * ortamında görünür kılıyoruz; üretimde konsola tek satır bile yazılmaz.
 *
 * Slug başına bir kez uyarılır — tablo satırları aynı slug'ı yüzlerce kez render eder.
 */
const warnedSlugs = new Set<string>()

function warnUnmappedSlug(slug: string): void {
  if (!import.meta.env.DEV) return
  // Boş slug bir eşleme hatası değildir: seçim yokken `selected?.statusSlug ?? ''` beklenen yol.
  if (slug.trim() === '' || warnedSlugs.has(slug)) return

  warnedSlugs.add(slug)
  logger.warn(
    `[StatusBadge] '${slug}' STATUS_COLORS haritasında yok — gri (grey-7) render edilecek, ` +
      'renk anlamı kaybolur. Backend SmartEnum Slug değeri eklendiyse haritayı güncelleyin.',
  )
}
</script>

<script setup lang="ts">
import { computed } from 'vue'

interface Props {
  slug: string
}

const props = defineProps<Props>()

const color = computed(() => {
  const mapped = STATUS_COLORS[props.slug]
  if (mapped !== undefined) return mapped

  warnUnmappedSlug(props.slug)
  return 'grey-7'
})
</script>
