<template>
  <q-dialog
    v-model="open"
    persistent
    :maximized="$q.screen.lt.sm"
    transition-show="slide-up"
    transition-hide="slide-down"
  >
    <q-card :style="$q.screen.gt.xs ? `width: ${width}; max-width: 95vw` : ''">
      <q-toolbar :class="`bg-${color} text-white`">
        <q-icon v-if="icon" :name="icon" class="q-mr-sm" />
        <q-toolbar-title>{{ title }}</q-toolbar-title>
        <q-btn flat round dense icon="close" color="white" v-close-popup />
      </q-toolbar>

      <q-card-section class="q-pt-lg q-gutter-md">
        <slot />
      </q-card-section>

      <q-separator />
      <q-card-actions align="right" class="q-pa-md">
        <slot name="actions">
          <q-btn flat label="İptal" color="grey-7" v-close-popup />
          <q-btn
            unelevated
            :color="saveColor ?? color"
            :label="saveLabel"
            :loading="saving"
            :disable="saveDisabled"
            @click="emit('save')"
          />
        </slot>
      </q-card-actions>
    </q-card>
  </q-dialog>
</template>

<script setup lang="ts">
import { useQuasar } from 'quasar'

const open = defineModel<boolean>({ required: true })
const $q = useQuasar()

withDefaults(defineProps<{
  title: string
  icon?: string
  color?: string
  width?: string
  saveLabel?: string
  saveColor?: string
  saving?: boolean
  saveDisabled?: boolean
}>(), {
  icon: undefined,
  color: 'primary',
  width: '480px',
  saveLabel: 'Kaydet',
  saveColor: undefined,
  saving: false,
  saveDisabled: false,
})

const emit = defineEmits<{
  save: []
}>()
</script>
