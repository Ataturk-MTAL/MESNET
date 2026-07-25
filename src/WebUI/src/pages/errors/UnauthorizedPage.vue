<template>
  <q-page class="flex flex-center">
    <div class="text-center">
      <q-icon
        name="lock"
        size="64px"
        color="negative"
      />
      <div class="text-h5 q-mt-md">
        Erişim Yetkisi Yok
      </div>
      <div class="text-body1 text-grey q-mt-sm">
        Bu sayfayı görüntülemek için gerekli izniniz bulunmamaktadır.
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
