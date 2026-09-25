<template>
  <div
    v-if="name"
    class="editing-subject row no-wrap items-center rounded-borders"
    :class="editing ? 'bg-warning-soft text-warning-strong' : 'bg-info-soft text-info-strong'"
    role="status"
    aria-live="polite"
  >
    <!-- "Bu veri KİMİN?" etiketi — seçime bağlı veri düzenlenen her kartta başlığın yanında durur.
         Neden: seçici sayfanın tepesinde kalır; kullanıcı ızgarada çalışırken hangi öğretmenin /
         alanın verisini değiştirdiğini gözden kaçırır ve yanlış kaydı ancak kayıttan sonra fark eder.
         Düzenleme modunda uyarı tonuna geçer: dikkat en çok o anda gerekir.
         role="status": seçim değişince ekran okuyucu yeni sahibi duyurur. -->
    <q-icon
      :name="editing ? 'edit' : icon"
      size="20px"
      class="q-mr-sm"
    />
    <div class="ellipsis">
      <span class="text-caption text-weight-medium q-mr-xs">
        {{ editing ? 'Düzenleniyor:' : `${kind}:` }}
      </span>
      <span class="text-subtitle1 text-weight-bold">{{ name }}</span>
      <span
        v-if="context"
        class="text-caption q-ml-xs"
      >
        · {{ context }}
      </span>
    </div>
    <q-tooltip v-if="context">
      {{ name }} · {{ context }}
    </q-tooltip>
  </div>
</template>

<script setup lang="ts">
withDefaults(defineProps<{
  /** Verinin sahibi (öğretmen adı, alan adı…). Boşsa etiket çizilmez. */
  name: string | null
  /** Sahibin türü — "Öğretmen", "Alan". */
  kind?: string
  /** İkincil bağlam (ör. öğretmenin alanı). */
  context?: string | null
  icon?: string
  /** Kart düzenleme modunda mı — uyarı tonu. */
  editing?: boolean
}>(), {
  kind: 'Öğretmen',
  context: null,
  icon: 'person',
  editing: false,
})
</script>

<style lang="scss" scoped>
.editing-subject {
  padding: $space-y-base * 0.5 $space-x-base * 0.75;
  max-width: 100%;
  min-width: 0;
}
</style>
