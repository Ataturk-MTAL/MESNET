<template>
  <div class="row items-center q-mb-lg">
    <div class="col">
      <div class="text-h5 text-weight-bold">
        {{ title }}
      </div>
      <div
        v-if="subtitle"
        class="text-subtitle2 text-grey-7"
      >
        {{ subtitle }}
      </div>
    </div>
    <div
      v-if="$slots.default"
      class="col-auto q-gutter-sm"
    >
      <slot />
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
