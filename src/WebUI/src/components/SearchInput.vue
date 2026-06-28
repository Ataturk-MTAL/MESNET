<script setup lang="ts">
const model = defineModel<string | null>()

withDefaults(
  defineProps<{
    /** Etiket (üstte) */
    label?: string
    /** Placeholder (iç) */
    placeholder?: string
    /** Yoğun görünüm */
    dense?: boolean
    /** Dolu (filled) stil */
    filled?: boolean
    /** Arama ikonu boyutu */
    iconSize?: string
  }>(),
  {
    label: undefined,
    placeholder: undefined,
    dense: true,
    filled: true,
    iconSize: undefined,
  },
)
</script>

<template>
  <q-input v-model="model" :label="label" :placeholder="placeholder" :dense="dense" :filled="filled" clearable>
    <template #prepend>
      <q-icon name="search" :size="iconSize" />
    </template>
  </q-input>
</template>
