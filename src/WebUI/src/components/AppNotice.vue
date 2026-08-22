<script setup lang="ts">
import { ref, computed } from 'vue'

type NoticeType = 'info' | 'warning' | 'error' | 'success' | 'readonly'

const props = withDefaults(
  defineProps<{
    /** Bildirim türü — renk + varsayılan ikonu belirler */
    type?: NoticeType
    /** Mesaj metni (default slot ile de verilebilir) */
    message?: string
    /** İkonu override et (varsayılan: türe göre) */
    icon?: string
    /** Yoğun (dar) görünüm */
    dense?: boolean
    /** Kapatılabilir — sağda × butonu gösterir */
    dismissible?: boolean
  }>(),
  {
    type: 'info',
    message: undefined,
    icon: undefined,
    dense: false,
    dismissible: false,
  },
)

const emit = defineEmits<{ close: [] }>()

const STYLES: Record<NoticeType, { bg: string; text: string; icon: string; iconColor: string }> = {
  info: { bg: 'bg-info-soft', text: 'text-info-strong', icon: 'info', iconColor: 'info' },
  warning: { bg: 'bg-warning-soft', text: 'text-warning-strong', icon: 'warning', iconColor: 'warning' },
  error: { bg: 'bg-negative-soft', text: 'text-negative-strong', icon: 'error', iconColor: 'negative' },
  success: { bg: 'bg-positive-soft', text: 'text-positive-strong', icon: 'check_circle', iconColor: 'positive' },
  readonly: { bg: 'bg-grey-2', text: 'text-grey-8', icon: 'lock', iconColor: 'grey-6' },
}

const visible = ref(true)
const style = computed(() => STYLES[props.type])

function dismiss() {
  visible.value = false
  emit('close')
}
</script>

<template>
  <q-banner
    v-if="visible"
    rounded
    :dense="dense"
    :class="`${style.bg} ${style.text} rounded-borders`"
  >
    <template #avatar>
      <q-icon
        :name="icon ?? style.icon"
        :color="style.iconColor"
      />
    </template>

    <slot>{{ message }}</slot>

    <template
      v-if="dismissible"
      #action
    >
      <q-btn
        flat
        dense
        round
        icon="close"
        aria-label="Kapat"
        :color="style.iconColor"
        @click="dismiss"
      />
    </template>
  </q-banner>
</template>
