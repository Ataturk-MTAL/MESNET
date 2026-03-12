<template>
  <div class="row no-wrap items-center justify-center q-gutter-xs">
    <!-- Atanmış slot sayısı -->
    <span
      v-if="assigned > 0"
      :class="['bg-blue-2', 'text-blue-9', 'q-px-xs', 'rounded-borders', 'text-caption', 'text-weight-bold']"
      style="min-width: 20px; display: inline-block; text-align: center"
    >
      {{ assigned }}
    </span>
    <!-- Boş slot sayısı -->
    <span
      :class="[`bg-${bgColor}`, `text-${textColor}`, 'q-px-xs', 'rounded-borders', 'text-caption', 'text-weight-medium']"
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
  if (props.free === 0) return 'red-2'
  if (props.free <= 1) return 'orange-2'
  return 'green-2'
})

const textColor = computed(() => {
  if (props.free === 0) return 'red-9'
  if (props.free <= 1) return 'orange-9'
  return 'green-9'
})
</script>
