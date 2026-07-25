<template>
  <!-- login-required modunda Keycloak bu sayfaya düşmez.
       Yanlışlıkla /auth/callback'e gelirse dashboard'a yönlendir. -->
  <q-page class="flex flex-center">
    <q-spinner
      size="48px"
      color="primary"
    />
  </q-page>
</template>

<script setup lang="ts">
import { onMounted } from 'vue'
import { useRouter } from 'vue-router'

const router = useRouter()

onMounted(() => {
  router.replace({ name: 'Dashboard' })
})
</script>
