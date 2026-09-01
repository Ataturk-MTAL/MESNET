<script setup lang="ts">
withDefaults(
  defineProps<{
    /** Yükleniyor — ortalı spinner gösterir */
    loading?: boolean
    /** Hata var — ortalı hata mesajı (+ opsiyonel tekrar-dene) */
    error?: boolean
    /** Boş — ortalı ikon + mesaj */
    empty?: boolean
    /** Boş durum ikonu */
    emptyIcon?: string
    /** Boş durum metni */
    emptyText?: string
    /** Yükleniyor metni (spinner altında — boşsa gösterilmez) */
    loadingText?: string
    /** Hata metni */
    errorText?: string
    /** Dişli spinner (q-spinner-gears) — varsayılan düz q-spinner */
    gears?: boolean
    /** Spinner boyutu */
    spinnerSize?: string
    /** Dış padding sınıfı (q-pa-md, q-pa-lg, q-pa-xl) */
    padding?: string
    /** Tekrar-dene butonu göster (error durumunda) */
    retryable?: boolean
    /** Yükleniyor — spinner yerine içerik-şekilli skeleton satırlar (layout-shift'siz) */
    skeleton?: boolean
    /** Skeleton satır sayısı */
    skeletonLines?: number
  }>(),
  {
    loading: false,
    error: false,
    empty: false,
    emptyIcon: 'inbox',
    emptyText: 'Kayıt bulunamadı',
    loadingText: undefined,
    errorText: 'Bir hata oluştu',
    gears: false,
    spinnerSize: '2em',
    padding: 'q-pa-lg',
    skeleton: false,
    skeletonLines: 4,
  },
)

const emit = defineEmits<{ retry: [] }>()
</script>

<template>
  <div
    v-if="loading && skeleton"
    :class="padding"
  >
    <q-skeleton
      v-for="i in skeletonLines"
      :key="i"
      type="text"
      height="28px"
      class="q-mb-sm"
    />
  </div>

  <div
    v-else-if="loading"
    :class="`text-center ${padding}`"
  >
    <q-spinner-gears
      v-if="gears"
      color="primary"
      :size="spinnerSize"
    />
    <q-spinner
      v-else
      color="primary"
      :size="spinnerSize"
    />
    <div
      v-if="loadingText"
      class="text-caption text-grey-7 q-mt-sm"
    >
      {{ loadingText }}
    </div>
  </div>

  <div
    v-else-if="error"
    :class="`text-center ${padding} text-grey-7`"
  >
    <slot name="error">
      <q-icon
        name="error_outline"
        size="2em"
        class="q-mb-sm"
      />
      <div class="text-caption">
        {{ errorText }}
      </div>
      <div
        v-if="retryable"
        class="q-mt-sm"
      >
        <q-btn
          flat
          dense
          color="primary"
          label="Tekrar dene"
          @click="emit('retry')"
        />
      </div>
    </slot>
  </div>

  <div
    v-else-if="empty"
    :class="`text-center ${padding} text-grey-7`"
  >
    <slot name="empty">
      <q-icon
        :name="emptyIcon"
        size="2em"
        class="q-mb-sm"
      />
      <div class="text-caption">
        {{ emptyText }}
      </div>
    </slot>
  </div>

  <slot v-else />
</template>
