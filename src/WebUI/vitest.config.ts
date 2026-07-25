import { defineConfig } from 'vitest/config'
import vue from '@vitejs/plugin-vue'
import { fileURLToPath, URL } from 'node:url'

// Vitest, vite.config.ts'i kullanmıyor: oradaki quasar() eklentisi sass değişkenleri ve
// tarayıcı bağlamı gerektiriyor, birim testlerinde gereksiz. Alias'lar birebir kopyalandı —
// vite.config.ts'te alias eklenirse buraya da eklenmeli.
export default defineConfig({
  plugins: [vue()],
  resolve: {
    alias: {
      src: fileURLToPath(new URL('./src', import.meta.url)),
      components: fileURLToPath(new URL('./src/components', import.meta.url)),
      pages: fileURLToPath(new URL('./src/pages', import.meta.url)),
      stores: fileURLToPath(new URL('./src/stores', import.meta.url)),
      boot: fileURLToPath(new URL('./src/boot', import.meta.url)),
      composables: fileURLToPath(new URL('./src/composables', import.meta.url)),
      api: fileURLToPath(new URL('./src/api', import.meta.url)),
      utils: fileURLToPath(new URL('./src/utils', import.meta.url)),
    },
  },
  test: {
    environment: 'jsdom',
    include: ['src/**/*.{spec,test}.ts'],
  },
})
