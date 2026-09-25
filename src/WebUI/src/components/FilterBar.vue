<template>
  <div
    class="filter-bar"
    :class="{ 'filter-bar--dense': dense }"
  >
    <!-- Sayfa/tablo üstü filtre satırı — tek düzen kuralı burada yaşar.
         Neden grid kolonu (col-sm-3) değil: sabit yüzde, içeriği bilmez; "EET — Elektrik-Elektronik
         Teknolojisi" gibi uzun etiketler dar kolonda kesiliyordu. Burada her alan okunabilir bir
         en az genişlikle esner, sığmazsa alt satıra geçer. Boşluk `gap` ile verilir: q-gutter
         çocuklara negatif/pozitif margin basıp butonları alanlardan kaydırıyordu. -->
    <slot />
    <div
      v-if="$slots.actions"
      class="filter-bar__actions"
    >
      <slot name="actions" />
    </div>
  </div>
</template>

<script setup lang="ts">
withDefaults(defineProps<{
  /** Alanlar `dense` ise true verin — eylem butonları alan yüksekliğine (40px) eşitlenir. */
  dense?: boolean
}>(), {
  dense: false,
})
</script>

<style lang="scss" scoped>
.filter-bar {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: $space-base;
  margin-bottom: $space-base * 1.5;
}

// Doğrudan her filtre alanı (eylem kutusu hariç). :deep — slot içeriği bu bileşenin
// kapsam özniteliğini taşımaz.
.filter-bar > :deep(:not(.filter-bar__actions)) {
  flex: 1 1 18rem;
  min-width: min(100%, 14rem);
  max-width: 26rem;
}

// Kısa değerli alanlar (ay, yıl, tarih) için: `class="filter-bar__narrow"`.
.filter-bar > :deep(.filter-bar__narrow) {
  flex: 0 1 10rem;
  min-width: min(100%, 8rem);
}

// Filtre işlevi gören buton (ör. hafta seçici) esnemez; içeriği kadar yer kaplar.
.filter-bar > :deep(.q-btn) {
  flex: 0 0 auto;
  min-width: 0;
  max-width: none;
}

// Eylemler (Kaydet, Ara, Oluştur…) satırın sonuna yaslanır; sarınca da sağda kalır.
.filter-bar__actions {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: $space-base * 0.5;
  margin-left: auto;
}

// Buton, yanındaki outlined alanla aynı boyda dursun (Quasar alan yüksekliği: 56px / dense 40px).
.filter-bar > :deep(.q-btn),
.filter-bar__actions > :deep(.q-btn) {
  min-height: 56px;
}

.filter-bar--dense > :deep(.q-btn),
.filter-bar--dense .filter-bar__actions > :deep(.q-btn) {
  min-height: 40px;
}
</style>
