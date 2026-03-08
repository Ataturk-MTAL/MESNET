<template>
  <span :class="[`text-${color}`, 'text-weight-bold', 'text-caption']">
    {{ assignedHours }}s
  </span>
</template>

<script setup lang="ts">
import { computed } from 'vue'

const props = withDefaults(defineProps<{
  assignedHours: number
  availableHours?: number
}>(), {
  assignedHours: 0,
  availableHours: 0,
})

const color = computed(() => {
  if (props.assignedHours === 0) return 'grey-4'
  if (props.assignedHours <= 4) return 'green-7'
  if (props.assignedHours <= 8) return 'orange-7'
  return 'red-7'
})
</script>
