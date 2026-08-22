<template>
  <q-page class="flex flex-center">
    <div class="text-center">
      <div class="text-h1 text-grey-4">
        404
      </div>
      <h1 class="text-h5 q-mt-md q-mb-none">
        Sayfa Bulunamadı
      </h1>
      <div class="text-body1 text-grey q-mt-sm">
        Aradığınız sayfa mevcut değil veya taşınmış olabilir.
      </div>
      <q-btn
        class="q-mt-lg"
        color="primary"
        label="Ana Sayfaya Dön"
        :to="{ name: 'Dashboard' }"
      />
    </div>
  </q-page>
</template>
