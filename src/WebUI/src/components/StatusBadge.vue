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
  // Olumlu / aktif (yeşil)
  'Aktif': ACTIVE,
  'Aktif Staj': ACTIVE,
  'Onaylandı': ACTIVE,
  'Doğrulandı': ACTIVE,
  'Ödendi': ACTIVE,
  // Ara aşama (teal)
  'Yerleştirildi': PROGRESS,
  'Eşleştirildi': PROGRESS,
  // Bilgilendirici ara durum (cyan)
  'İmzalandı': INFO,
  'Yüklendi': INFO,
  'Hesaplandı': INFO,
  'Düzeltildi': INFO,
  // Uyarı (turuncu)
  'Askıda': WARNING,
  'Fesih Talep Edildi': WARNING,
  'Süresi Doldu': WARNING,
  'İtiraz Edildi': WARNING,
  // Olumsuz (kırmızı)
  'Reddedildi': NEGATIVE,
  'Feshedildi': NEGATIVE,
  'İptal Edildi': NEGATIVE,
  // Başarıyla tamamlandı (yeşil — aktif yeşilden koyu ton)
  'Tamamladı': SUCCESS,
  'Tamamlandı': SUCCESS,
  'Tamamlanmış': SUCCESS, // ContractStatus.Completed slug'ı (önceden eşlemesizdi → gri)
  // Transfer / aktarılmış (mor)
  'Transfer Edildi': DONE,
  // Nötr / taslak (gri)
  'Kayıtlı': NEUTRAL,
  'Taslak': DRAFT,
  'Pasif': DRAFT,
  'Kapatılmış': CLOSED,
  // Devamsızlık türü (AbsenceType slug'ları) — semantik ayrım
  'Mazeretli': ACTIVE, //       mazeretli: kabul edilebilir (yeşil)
  'Mazeretsiz': WARNING, //     mazeretsiz: dikkat gerektiren (turuncu)
  'Sağlık Raporu': INFO, //     belgeli/sağlık raporu (cyan)
  // Ücretli izin başvurusu (PaidLeaveStatus slug'ları, #177) — zincirin iki adımı ayrı görünür
  'İşletme Onayı Bekliyor': PENDING,
  'Okul Onayı Bekliyor': PENDING,
  'Resmileşti': SUCCESS, //     iki onay tamamlandı, izin günleri kaydedildi
}

const color = computed(() => STATUS_COLORS[props.slug] ?? 'grey-7')
</script>
