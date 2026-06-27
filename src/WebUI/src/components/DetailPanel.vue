<template>
  <q-drawer v-model="open" side="right" bordered :width="numericWidth" overlay>
    <template v-if="hasContent">
      <q-toolbar>
        <q-toolbar-title class="text-subtitle1 text-weight-bold">
          <slot name="title">{{ title }}</slot>
        </q-toolbar-title>
        <slot name="toolbar-actions" />
        <q-btn flat round dense icon="close" aria-label="Kapat" @click="open = false" />
      </q-toolbar>
      <q-separator />
      <q-scroll-area class="fit">
        <div class="q-pa-md">
          <slot />
        </div>
      </q-scroll-area>
    </template>
  </q-drawer>
</template>

<script setup lang="ts">
import { computed } from 'vue'

const open = defineModel<boolean>({ required: true })

const props = withDefaults(defineProps<{
  title?: string
  width?: string | number
  hasContent?: boolean
}>(), {
  title: '',
  width: 480,
  hasContent: true,
})

const numericWidth = computed(() =>
  typeof props.width === 'string' ? parseInt(props.width, 10) : props.width,
)
</script>
