import { defineConfig, loadEnv } from 'vite'
import vue from '@vitejs/plugin-vue'
import { quasar, transformAssetUrls } from '@quasar/vite-plugin'
import { fileURLToPath, URL } from 'node:url'

export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd(), '')
  const apiUrl = env.VITE_API_URL || 'http://localhost:5270'

  return {
    plugins: [
      vue({ template: { transformAssetUrls } }),
      quasar({
        sassVariables: fileURLToPath(
          new URL('./src/assets/quasar-variables.sass', import.meta.url),
        ),
      }),
    ],

    resolve: {
      alias: {
        src: fileURLToPath(new URL('./src', import.meta.url)),
        components: fileURLToPath(new URL('./src/components', import.meta.url)),
        pages: fileURLToPath(new URL('./src/pages', import.meta.url)),
        stores: fileURLToPath(new URL('./src/stores', import.meta.url)),
        boot: fileURLToPath(new URL('./src/boot', import.meta.url)),
        composables: fileURLToPath(
          new URL('./src/composables', import.meta.url),
        ),
        api: fileURLToPath(new URL('./src/api', import.meta.url)),
        utils: fileURLToPath(new URL('./src/utils', import.meta.url)),
      },
    },

    server: {
      port: parseInt(env.PORT || '5173'),
      strictPort: true,
      proxy: {
        // API istekleri backend'e yönlendirilir
        '/api': {
          target: apiUrl,
          changeOrigin: true,
        },
        // SSE bildirimleri — uzun bağlantı, buffering kapalı
        '/notifications': {
          target: apiUrl,
          changeOrigin: true,
          // SSE için response buffering'i kapat
          configure: (proxy) => {
            proxy.on('proxyRes', (proxyRes) => {
              proxyRes.headers['x-accel-buffering'] = 'no'
            })
          },
        },
        // Scalar API docs (dev)
        '/scalar': {
          target: apiUrl,
          changeOrigin: true,
        },
      },
    },
  }
})
