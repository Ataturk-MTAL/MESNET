<template>
  <div class="row no-wrap items-center justify-center q-gutter-xs">
    <!-- Atanmış slot sayısı -->
    <span
      v-if="assigned > 0"
      :class="['bg-info-soft', 'text-info-strong', 'q-px-xs', 'rounded-borders', 'text-caption', 'text-weight-bold', 'tabular-nums']"
      style="min-width: 20px; display: inline-block; text-align: center"
    >
      {{ assigned }}
    </span>
    <!-- Boş slot sayısı -->
    <span
      :class="[`bg-${bgColor}`, `text-${textColor}`, 'q-px-xs', 'rounded-borders', 'text-caption', 'text-weight-medium', 'tabular-nums']"
      style="min-width: 20px; display: inline-block; text-align: center"
    >
      {{ free > 0 ? free : '—' }}
    </span>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue'

const props = withDefaults(defineProps<{
  free: number
  assigned?: number
  total?: number
}>(), {
  free: 0,
  assigned: 0,
  total: 0,
})

const bgColor = computed(() => {
  if (props.free === 0) return 'negative-soft'
  if (props.free <= 1) return 'warning-soft'
  return 'positive-soft'
})

const textColor = computed(() => {
  if (props.free === 0) return 'negative-strong'
  if (props.free <= 1) return 'warning-strong'
  return 'positive-strong'
})
</script>
