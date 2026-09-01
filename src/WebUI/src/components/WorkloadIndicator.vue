<template>
  <span :class="[`text-${color}`, 'text-weight-bold', 'text-caption', 'tabular-nums']">
    {{ assignedHours }}s
  </span>
</template>

<script setup lang="ts">
import { computed } from 'vue'

const props = withDefaults(defineProps<{
  assignedHours: number
  availableHours?: number
}>(), {
  assignedHours: 0,
  availableHours: 0,
})

// Sıfır saat geçerli bir veri değeridir, devre dışı bir bileşen değil — WCAG 1.4.3
// muafiyeti geçmez ve metin eşiği 4,5:1 uygulanır. ÖLÇÜLDÜ: eski grey-4 (#e0e0e0)
// beyaz q-td üzerinde 1,32:1 ile pratikte görünmezdi; grey-7 (#757575) 4,61:1.
//
// Quasar'ın satır üstü gezinme örtüsü (quasar.css: `.q-table tbody td:before` zemini
// rgba(0,0,0,.03), `tr:hover` ile content kazanır) yalnız zemini karartmaz: konumlandırılmış
// ve z-index'i auto olan bir soyundan gelen olduğu için CSS boyama sırasında satır içi
// metinden SONRA boyanır, yani METNİ DE karartır. Efektif kompozisyonla ÖLÇÜLDÜ —
// zemin #ffffff → #f7f7f7, grey-7 #757575 → #717171 ⇒ 4,556:1; warning #9A6B00 → #956800
// ⇒ 4,604:1. İki değer de eşiğin üstünde kalır ve düşüş bileşenin tümü için ortaktır,
// yani tek başına daha koyu bir ton seçmeyi haklı çıkarmaz (koyulaştırmak "sıfır"ı
// uyarıdan daha vurgulu yapardı).
const color = computed(() => {
  if (props.assignedHours === 0) return 'grey-7'
  if (props.assignedHours <= 4) return 'positive'
  if (props.assignedHours <= 8) return 'warning'
  return 'negative'
})
</script>
