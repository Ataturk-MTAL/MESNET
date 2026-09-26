<template>
  <div
    class="subject-header"
    :class="{ 'subject-header--editing': editing }"
  >
    <!-- Seçime bağlı veri düzenlenen kartın başlığı: verinin SAHİBİ başlığın kendisidir.
         Neden rozet değil: seçici sayfanın tepesinde kalır, kullanıcı ızgarada çalışırken hangi
         öğretmenin/alanın verisini değiştirdiğini unutur. Yanına eklenen rozet başlıkla yer
         için yarışıyordu; sahibi başlık yapmak dikkati tek noktaya toplar.
         Düzenleme modunda şerit + sol kenar uyarı tonuna geçer — dikkat en çok o anda gerekir. -->
    <div
      v-if="editing && name"
      class="subject-header__strip row items-center no-wrap bg-warning-soft text-warning-strong"
      role="status"
    >
      <q-icon
        name="edit"
        size="18px"
        class="q-mr-sm"
      />
      <span class="ellipsis">
        <strong>{{ name }}</strong> — düzenleniyor
      </span>
    </div>

    <div class="subject-header__body row items-start no-wrap">
      <div class="col subject-header__text">
        <div class="subject-header__overline row items-center text-grey-7">
          <span>{{ title }}</span>
          <slot name="badges" />
        </div>
        <div
          class="text-h6 text-weight-bold ellipsis"
          aria-live="polite"
        >
          {{ name ?? placeholder }}
        </div>
        <div
          v-if="context || $slots.meta"
          class="text-caption text-grey-7"
        >
          <span v-if="context">{{ context }}</span>
          <slot name="meta" />
        </div>
      </div>
      <div
        v-if="$slots.actions"
        class="col-auto row items-center no-wrap subject-header__actions"
      >
        <slot name="actions" />
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
withDefaults(defineProps<{
  /** Kartın konusu — "Haftalık Program", "Ders Programı". Başlığın üstünde küçük yazılır. */
  title: string
  /** Verinin sahibi (öğretmen adı, alan adı). Başlık olarak büyük yazılır. */
  name: string | null
  /** İkincil bağlam — ör. öğretmenin alanı. */
  context?: string | null
  /** Sahip yokken başlık yerine gösterilecek metin. */
  placeholder?: string
  /** Kaydedilmemiş değişiklik / düzenleme modu. */
  editing?: boolean
}>(), {
  context: null,
  placeholder: '—',
  editing: false,
})
</script>

<style lang="scss" scoped>
.subject-header {
  margin-bottom: $space-base;
  border-left: 3px solid transparent;
  padding-left: $space-x-base * 0.75;
  margin-left: -$space-x-base * 0.75 - 3px; // kenar çizgisi kart iç boşluğuna taşsın, metin hizası kaymasın
  transition: border-color 0.2s;
}

.subject-header--editing {
  border-left-color: var(--q-warning);
}

.subject-header__strip {
  padding: $space-y-base * 0.25 $space-x-base * 0.5;
  margin-bottom: $space-y-base * 0.5;
  border-radius: 4px;
  font-size: 0.8125rem;
}

.subject-header__text {
  min-width: 0;
}

.subject-header__overline {
  gap: $space-x-base * 0.5;
  font-size: 0.75rem;
  font-weight: 500;
  letter-spacing: 0.06em;
  text-transform: uppercase; // Türkçe i/İ doğru büyür: index.html lang="tr"
}

.subject-header__actions {
  gap: $space-x-base * 0.5;
  margin-left: $space-x-base;
}
</style>
