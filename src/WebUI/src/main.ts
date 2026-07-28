import { createApp } from 'vue'
import { createPinia } from 'pinia'
import { Quasar, Notify, Loading, Dialog } from 'quasar'
import quasarTR from 'quasar/lang/tr'
import '@quasar/extras/material-icons/material-icons.css'
import 'quasar/src/css/index.sass'
// Quasar CSS'inden SONRA: uygulama geneli kurallar (hareket azaltma tercihi) öncelik kazansın
import './assets/app.css'

import App from './App.vue'
import router from './router'

// Boot sırası önemli: önce auth, sonra axios, sonra app mount
import { bootAuth } from './boot/auth'

const pinia = createPinia()
const app = createApp(App)

app.use(pinia)
app.use(router)
app.use(Quasar, {
  lang: quasarTR,
  plugins: { Notify, Loading, Dialog },
  config: {
    notify: { position: 'top-right' },
  },
})

// Keycloak başlatılmadan app mount edilmez.
//
// bootAuth() yeniden giriş yaptığında dönmez (login/logout tam sayfa yönlendirmedir ve
// döndükleri promise ASLA settle olmaz) — bu normaldir, mount beklenmez. Ama boot GERÇEK
// bir hata ile reddederse yakalanmazsa sayfa kalıcı beyaz kalır: #136'da index.html'deki
// #app boştur, yani mount'tan önce ekranda hiçbir şey yoktur.
bootAuth()
  .then(() => {
    app.mount('#app')
  })
  .catch((err: unknown) => {
    console.error('[Auth] Başlatma başarısız:', err)
    import('./boot/sessionExpiredScreen')
      .then(({ showSessionExpiredScreen }) => {
        showSessionExpiredScreen({
          detail: 'Uygulama başlatılamadı.',
          onLogout: () => {
            import('./boot/auth')
              .then(({ logout }) => logout())
              .catch(() => {})
          },
        })
      })
      .catch(() => {})
  })
