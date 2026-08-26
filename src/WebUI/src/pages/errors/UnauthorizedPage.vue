<template>
  <q-page
    padding
    class="flex flex-center"
  >
    <div class="text-center">
      <q-icon
        name="lock"
        size="64px"
        color="negative"
      />
      <h1 class="text-h5 q-mt-md q-mb-none">
        Erişim Yetkisi Yok
      </h1>
      <div class="text-body2 text-grey-7 q-mt-sm">
        Bu sayfayı görüntülemek için gerekli izniniz bulunmamaktadır.
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
