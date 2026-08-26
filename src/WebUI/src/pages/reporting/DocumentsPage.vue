<template>
  <q-page padding>
    <PageHeader title="Belgeler" />

    <AppNotice
      type="info"
      dense
      message="Modüle özgü belgelere (işletme belgeleri, sözleşmeler) ilgili sayfalardan erişin."
      class="q-mb-lg"
    />

    <div class="row q-col-gutter-md">
      <div class="col-12 col-md-4">
        <router-link
          :to="{ name: 'CompanyList' }"
          class="block doc-card-link"
        >
          <q-card
            v-ripple
            flat
            bordered
          >
            <q-card-section class="text-center q-pa-lg">
              <q-icon
                name="business"
                size="48px"
                color="primary"
              />
              <div class="text-subtitle1 q-mt-sm">
                İşletme Belgeleri
              </div>
              <!-- Beyaz flat bordered q-card üzerinde caption: .doc-card-link yalnız
                   text-decoration/color:inherit tanımlar, zemini değiştirmez.
                   text-grey (#9e9e9e) 2,68:1 idi; text-grey-7 (#757575) 4,61:1. -->
              <div class="text-caption text-grey-7">
                Ustalık belgesi, usta öğretici belgesi
              </div>
            </q-card-section>
          </q-card>
        </router-link>
      </div>
      <div class="col-12 col-md-4">
        <router-link
          :to="{ name: 'ContractList' }"
          class="block doc-card-link"
        >
          <q-card
            v-ripple
            flat
            bordered
          >
            <q-card-section class="text-center q-pa-lg">
              <q-icon
                name="description"
                size="48px"
                color="positive"
              />
              <div class="text-subtitle1 q-mt-sm">
                Sözleşme Belgeleri
              </div>
              <!-- Beyaz flat bordered q-card üzerinde caption: .doc-card-link yalnız
                   text-decoration/color:inherit tanımlar, zemini değiştirmez.
                   text-grey (#9e9e9e) 2,68:1 idi; text-grey-7 (#757575) 4,61:1. -->
              <div class="text-caption text-grey-7">
                Islak imzalı sözleşmeler ve fesih belgeleri
              </div>
            </q-card-section>
          </q-card>
        </router-link>
      </div>
      <div class="col-12 col-md-4">
        <router-link
          :to="{ name: 'Reporting' }"
          class="block doc-card-link"
        >
          <q-card
            v-ripple
            flat
            bordered
          >
            <q-card-section class="text-center q-pa-lg">
              <q-icon
                name="bar_chart"
                size="48px"
                color="secondary"
              />
              <div class="text-subtitle1 q-mt-sm">
                Raporlar
              </div>
              <!-- Beyaz flat bordered q-card üzerinde caption: .doc-card-link yalnız
                   text-decoration/color:inherit tanımlar, zemini değiştirmez.
                   text-grey (#9e9e9e) 2,68:1 idi; text-grey-7 (#757575) 4,61:1. -->
              <div class="text-caption text-grey-7">
                PDF rapor üretimi
              </div>
            </q-card-section>
          </q-card>
        </router-link>
      </div>
    </div>
  </q-page>
</template>

<script setup lang="ts">
import AppNotice from 'components/AppNotice.vue'
import PageHeader from 'components/PageHeader.vue'
</script>

<style scoped>
.doc-card-link {
  text-decoration: none;
  color: inherit;
}
</style>
