<template>
  <!-- Dar ekranda (xs) başlık ve aksiyonlar alt alta, sm ve üstünde aynı satırda.
       Boşluk `q-gutter` ile değil `gap` ile verilir: q-gutter col öğesine negatif margin
       basıp grubu yukarı-sola kaydırıyordu. -->
  <div class="page-header row items-center q-mb-lg">
    <div class="col-12 col-sm">
      <h1 class="text-h5 text-weight-bold q-my-none">
        {{ title }}
      </h1>
      <div
        v-if="subtitle"
        class="text-subtitle2 text-grey-7"
      >
        {{ subtitle }}
      </div>
    </div>
    <div
      v-if="$slots.default"
      class="page-header__actions-col col-12 col-sm-auto"
    >
      <div class="page-header__actions row items-center">
        <slot />
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
withDefaults(defineProps<{
  title: string
  subtitle?: string
}>(), {
  subtitle: undefined,
})
</script>

<style lang="scss" scoped>
.page-header {
  gap: $space-y-base * 0.5 $space-x-base; // satır arası 8px, sütun arası 16px
}

// Slot verilmiş ama içerik üretmemişse (ör. yetkisi olmayana PermissionGuard hiçbir şey
// çizmez) kolon gizlenir; yoksa xs'te col-12 boş bir satır + row-gap bırakır. `$slots.default`
// bunu göremez: slotun VERİLDİĞİNE bakar, ürettiğine değil. `:empty` yorum düğümlerini saymaz.
.page-header__actions-col:has(> .page-header__actions:empty) {
  display: none;
}

.page-header__actions {
  gap: $space-x-base * 0.5; // butonlar arası 8px; sığmazsa alt satıra sarar (.row = wrap)
}
</style>
