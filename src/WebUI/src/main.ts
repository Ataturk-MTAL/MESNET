import { createApp } from 'vue'
import { createPinia } from 'pinia'
import { Quasar, Notify, Loading, Dialog } from 'quasar'
import '@quasar/extras/material-icons/material-icons.css'
import 'quasar/src/css/index.sass'

import App from './App.vue'
import router from './router'

// Boot sırası önemli: önce auth, sonra axios, sonra app mount
import { bootAuth } from './boot/auth'

const pinia = createPinia()
const app = createApp(App)

app.use(pinia)
app.use(router)
app.use(Quasar, {
  plugins: { Notify, Loading, Dialog },
  config: {
    notify: { position: 'top-right' },
  },
})

// Keycloak başlatılmadan app mount edilmez
bootAuth().then(() => {
  app.mount('#app')
})
