import 'vue-router'

declare module 'vue-router' {
  interface RouteMeta {
    // true ise navigation guard'ı atla
    public?: boolean
    // Route'a erişim için gereken izinlerden en az biri kullanıcıda olmalı
    permissions?: string[]
  }
}
