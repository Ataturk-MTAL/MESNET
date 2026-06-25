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
  info: { bg: 'bg-blue-1', text: 'text-blue-9', icon: 'info', iconColor: 'blue-7' },
  warning: { bg: 'bg-orange-1', text: 'text-orange-9', icon: 'warning', iconColor: 'orange-7' },
  error: { bg: 'bg-red-1', text: 'text-red-9', icon: 'error', iconColor: 'red-7' },
  success: { bg: 'bg-teal-1', text: 'text-teal-10', icon: 'check_circle', iconColor: 'teal-7' },
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
      <q-icon :name="icon ?? style.icon" :color="style.iconColor" />
    </template>

    <slot>{{ message }}</slot>

    <template v-if="dismissible" #action>
      <q-btn flat dense round icon="close" :color="style.iconColor" @click="dismiss" />
    </template>
  </q-banner>
</template>
