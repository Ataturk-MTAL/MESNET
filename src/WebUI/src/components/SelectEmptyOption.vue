<script setup lang="ts">
withDefaults(
  defineProps<{
    /** Boş sonuç metni */
    text?: string
  }>(),
  { text: 'Sonuç bulunamadı' },
)
</script>

<template>
  <q-item>
    <q-item-section class="text-grey">
      {{ text }}
    </q-item-section>
  </q-item>
</template>
