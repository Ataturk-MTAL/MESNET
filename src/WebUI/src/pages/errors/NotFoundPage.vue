<template>
  <q-page
    padding
    class="flex flex-center"
  >
    <div class="text-center">
      <div class="text-h4 text-grey-7">
        404
      </div>
      <h1 class="text-h5 q-mt-md q-mb-none">
        Sayfa Bulunamadı
      </h1>
      <div class="text-body2 text-grey-7 q-mt-sm">
        Aradığınız sayfa mevcut değil veya taşınmış olabilir.
      </div>
      <q-btn
        unelevated
        class="q-mt-lg"
        color="primary"
        label="Ana Sayfaya Dön"
        :to="{ name: 'Dashboard' }"
      />
    </div>
  </q-page>
</template>
