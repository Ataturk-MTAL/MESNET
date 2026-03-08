<template>
  <span
    :class="[`bg-${bgColor}`, `text-${textColor}`, 'q-px-xs', 'rounded-borders', 'text-caption', 'text-weight-medium']"
    style="min-width: 24px; display: inline-block; text-align: center"
  >
    {{ free > 0 ? free : '—' }}
  </span>
</template>

<script setup lang="ts">
import { computed } from 'vue'

const props = withDefaults(defineProps<{
  free: number
  total?: number
}>(), {
  free: 0,
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
