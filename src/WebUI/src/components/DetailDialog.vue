<script setup lang="ts">
const open = defineModel<boolean>()

withDefaults(
  defineProps<{
    /** Başlık */
    title: string
    /** Başlık ikonu (opsiyonel) */
    icon?: string
    /** Tam ekran */
    maximized?: boolean
    /** Konum: standard | right | left | top | bottom */
    position?: 'standard' | 'right' | 'left' | 'top' | 'bottom'
    /** Tam yükseklik (right/left konumlarında) */
    fullHeight?: boolean
    /** q-card style override (örn. genişlik) */
    cardStyle?: string
  }>(),
  {
    icon: undefined,
    maximized: false,
    position: 'standard',
    fullHeight: false,
    cardStyle: undefined,
  },
)
</script>

<template>
  <q-dialog
    v-model="open"
    :maximized="maximized"
    :position="position === 'standard' ? undefined : position"
    :full-height="fullHeight"
  >
    <q-card :style="cardStyle">
      <q-card-section class="row items-center q-pb-none">
        <q-icon
          v-if="icon"
          :name="icon"
          class="q-mr-sm"
        />
        <div class="text-h6">
          {{ title }}
        </div>
        <q-space />
        <slot name="toolbar-actions" />
        <q-btn
          flat
          round
          dense
          icon="close"
          aria-label="Kapat"
          @click="open = false"
        />
      </q-card-section>

      <!-- Alt başlık / separator / içerik tüketici tarafından -->
      <slot />
    </q-card>
  </q-dialog>
</template>
