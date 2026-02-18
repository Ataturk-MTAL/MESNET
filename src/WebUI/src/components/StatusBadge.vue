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
  'Onay Bekliyor': 'orange',
  'Aktif': 'positive',
  'Reddedildi': 'negative',
  'Pasif': 'grey',
  'Kapatılmış': 'dark',
  // Öğrenci durumları
  'Kayıtlı': 'blue-grey',
  'Başvurdu': 'info',
  'Yerleştirildi': 'teal',
  'Aktif Staj': 'positive',
  'Tamamladı': 'purple',
  // Sözleşme durumları
  'Taslak': 'grey',
  'İmzaya Sunuldu': 'orange',
  'İmzalandı': 'cyan',
  'Askıda': 'deep-orange',
  'Feshedildi': 'negative',
  'Tamamlandı': 'purple',
  // Belge durumları
  'Yüklendi': 'info',
  'Onaylandı': 'positive',
  'Süresi Doldu': 'warning',
  // Yerleştirme durumları
  'Eşleştirildi': 'teal',
  'Transfer Edildi': 'deep-purple',
  'İptal Edildi': 'negative',
  // Devamsızlık
  'Onay Bekleniyor': 'orange',
  'Excused': 'blue',
  // Ödeme
  'Hesaplandı': 'info',
  'Ödendi': 'positive',
  'İtiraz Edildi': 'warning',
}

const color = computed(() => STATUS_COLORS[props.slug] ?? 'grey-7')
</script>
