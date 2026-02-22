<template>
  <q-badge :color="color" :label="slug" class="text-body2 q-px-sm q-py-xs" />
</template>

<script setup lang="ts">
import { computed } from 'vue'

interface Props {
  slug: string
}

const props = defineProps<Props>()

const STATUS_COLORS: Record<string, string> = {
  // İşletme durumları
  'Onay Bekliyor': 'amber-8',
  'Aktif': 'green-7',
  'Reddedildi': 'red-7',
  'Pasif': 'grey-6',
  'Kapatılmış': 'grey-9',
  // Öğrenci durumları
  'Kayıtlı': 'blue-grey-6',
  'Başvurdu': 'light-blue-7',
  'Yerleştirildi': 'teal-7',
  'Aktif Staj': 'green-7',
  'Tamamladı': 'deep-purple-6',
  // Sözleşme durumları
  'Taslak': 'grey-6',
  'İmza Bekliyor': 'amber-8',
  'İmzaya Sunuldu': 'amber-8',
  'İmzalandı': 'cyan-7',
  'Askıda': 'orange-8',
  'Fesih Talep Edildi': 'deep-orange-7',
  'Feshedildi': 'red-7',
  'Tamamlandı': 'deep-purple-6',
  // Belge durumları
  'Yüklendi': 'light-blue-7',
  'Onaylandı': 'green-7',
  'Süresi Doldu': 'orange-8',
  // Yerleştirme durumları
  'Eşleştirildi': 'teal-7',
  'Transfer Edildi': 'deep-purple-6',
  'İptal Edildi': 'red-7',
  // Devamsızlık
  'Onay Bekleniyor': 'amber-8',
  'Doğrulandı': 'green-7',
  'Düzeltildi': 'light-blue-7',
  // Ödeme
  'Hesaplandı': 'light-blue-7',
  'Ödendi': 'green-7',
  'İtiraz Edildi': 'orange-8',
}

const color = computed(() => STATUS_COLORS[props.slug] ?? 'grey-7')
</script>
