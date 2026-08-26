<template>
  <!--
    Salt görsel örneklik: iki marka rengini yan yana gösterir.

    aria-hidden ÇÜNKÜ renk burada tek sinyal değildir — bu bileşen her kullanıldığı yerde
    paletin Türkçe adı görünür metin olarak yanında durur (DESIGN.md "Renk Yalnız Kanıt
    Kuralı"). Ekran okuyucuya ikinci kez, üstelik anlamsız bir biçimde okutmanın faydası yok.

    Renkler sunucudan gelen hex'lerdir; bu bileşen renk TANIMLAMAZ, yalnız boyar.
  -->
  <span
    class="brand-swatch"
    aria-hidden="true"
  >
    <span
      class="brand-swatch__half"
      :style="{ backgroundColor: primary }"
    />
    <span
      class="brand-swatch__half"
      :style="{ backgroundColor: secondary }"
    />
  </span>
</template>

<script setup lang="ts">
defineProps<{
  /** "#RRGGBB" — kurum DTO'sundan ya da palet kataloğundan gelir. */
  primary: string
  secondary: string
}>()
</script>

<style scoped>
.brand-swatch {
  display: inline-flex;
  width: 44px;
  height: 28px;
  border-radius: 4px;
  overflow: hidden;
  flex: 0 0 auto;
  /*
   * İnce çerçeve: Antrasit gibi koyu paletlerde gerekmiyor ama açık zeminle sınırı
   * belirsizleşen bir palet eklenirse kutu kaybolmasın. Ton, temanın ayraç değeriyle
   * aynı (quasar-variables.sass $separator-color).
   */
  box-shadow: inset 0 0 0 1px rgb(30 58 95 / 14%);
}

.brand-swatch__half {
  display: block;
  height: 100%;
}

/* Birincil renk ağırlığı taşır; ikincil ondan türetilmiştir, o yüzden dar. */
.brand-swatch__half:first-child {
  width: 60%;
}

.brand-swatch__half:last-child {
  width: 40%;
}
</style>
