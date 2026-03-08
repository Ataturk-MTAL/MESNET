<template>
  <q-item dense>
    <q-item-section avatar><q-icon :name="icon" /></q-item-section>
    <q-item-section>
      <q-item-label caption>{{ label }}</q-item-label>
      <q-item-label><slot>{{ value ?? '—' }}</slot></q-item-label>
    </q-item-section>
  </q-item>
</template>

<script setup lang="ts">
defineProps<{
  icon: string
  label: string
  value?: string | number | null
}>()
</script>
