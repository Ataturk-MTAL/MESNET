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
    /** Dolu (outlined) stil */
    outlined?: boolean
    /** Arama ikonu boyutu */
    iconSize?: string
  }>(),
  {
    label: undefined,
    placeholder: undefined,
    dense: true,
    outlined: true,
    iconSize: undefined,
  },
)
</script>

<template>
  <q-input v-model="model" :label="label" :placeholder="placeholder" :dense="dense" :outlined="outlined" clearable>
    <template #prepend>
      <q-icon name="search" :size="iconSize" />
    </template>
  </q-input>
</template>
