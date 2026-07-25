<script setup lang="ts">
import { useRouter } from 'vue-router'

const props = withDefaults(
  defineProps<{
    /** Quasar ikon adı */
    icon: string
    /** Gösterilecek sayı/değer */
    value: number | string
    /** Alt etiket */
    label: string
    /** Renk (ikon + değer) */
    color?: string
    /** Yükleniyor — değer yerine skeleton */
    loading?: boolean
    /** Route — verilirse kart tıklanabilir olur */
    to?: string
    /** Düzen: yatay (ikon solda) veya dikey (ikon üstte, ortalı) */
    orientation?: 'horizontal' | 'vertical'
  }>(),
  {
    color: 'primary',
    loading: false,
    to: undefined,
    orientation: 'horizontal',
  },
)

const router = useRouter()

function onClick() {
  if (props.to) router.push(props.to)
}
</script>

<template>
  <q-card
    flat
    bordered
    :class="['stat-card', to ? 'cursor-pointer' : '']"
    @click="onClick"
  >
    <!-- Dikey: ikon üstte, ortalı -->
    <q-card-section
      v-if="orientation === 'vertical'"
      class="text-center"
    >
      <q-icon
        :name="icon"
        size="40px"
        :color="color"
      />
      <q-skeleton
        v-if="loading"
        type="text"
        width="60px"
        class="q-mt-sm"
        style="margin-inline: auto"
      />
      <div
        v-else
        :class="`stat-value text-h4 text-weight-bold text-${color} q-mt-sm`"
      >
        {{ value }}
      </div>
      <div class="text-caption text-grey">
        {{ label }}
      </div>
    </q-card-section>

    <!-- Yatay: ikon solda -->
    <q-card-section
      v-else
      class="row items-center no-wrap"
    >
      <q-icon
        :name="icon"
        size="40px"
        :color="color"
        class="q-mr-md"
      />
      <div>
        <q-skeleton
          v-if="loading"
          type="text"
          width="60px"
        />
        <div
          v-else
          :class="`stat-value text-h4 text-weight-bold text-${color}`"
        >
          {{ value }}
        </div>
        <div class="text-caption text-grey">
          {{ label }}
        </div>
      </div>
    </q-card-section>
  </q-card>
</template>

<style scoped>
.stat-card.cursor-pointer {
  transition:
    transform 0.2s ease,
    box-shadow 0.2s ease;
}
.stat-card.cursor-pointer:hover {
  transform: translateY(-2px);
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.08);
}
</style>
