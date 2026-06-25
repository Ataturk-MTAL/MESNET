<template>
  <q-page padding>
    <div class="text-h5 text-weight-bold q-mb-lg">Belgeler</div>

    <AppNotice
      type="info"
      dense
      message="Modüle özgü belgelere (işletme belgeleri, sözleşmeler) ilgili sayfalardan erişin."
      class="q-mb-lg"
    />

    <div class="row q-col-gutter-md">
      <div class="col-12 col-md-4">
        <q-card flat bordered clickable v-ripple :to="{ name: 'CompanyList' }">
          <q-card-section class="text-center q-pa-lg">
            <q-icon name="business" size="48px" color="primary" />
            <div class="text-subtitle1 q-mt-sm">İşletme Belgeleri</div>
            <div class="text-caption text-grey">Ustalık belgesi, usta öğretici belgesi</div>
          </q-card-section>
        </q-card>
      </div>
      <div class="col-12 col-md-4">
        <q-card flat bordered clickable v-ripple :to="{ name: 'ContractList' }">
          <q-card-section class="text-center q-pa-lg">
            <q-icon name="description" size="48px" color="teal" />
            <div class="text-subtitle1 q-mt-sm">Sözleşme Belgeleri</div>
            <div class="text-caption text-grey">Islak imzalı sözleşmeler ve fesih belgeleri</div>
          </q-card-section>
        </q-card>
      </div>
      <div class="col-12 col-md-4">
        <q-card flat bordered clickable v-ripple :to="{ name: 'Reporting' }">
          <q-card-section class="text-center q-pa-lg">
            <q-icon name="bar_chart" size="48px" color="purple" />
            <div class="text-subtitle1 q-mt-sm">Raporlar</div>
            <div class="text-caption text-grey">PDF rapor üretimi</div>
          </q-card-section>
        </q-card>
      </div>
    </div>
  </q-page>
</template>

<script setup lang="ts">
import AppNotice from 'components/AppNotice.vue'
</script>
