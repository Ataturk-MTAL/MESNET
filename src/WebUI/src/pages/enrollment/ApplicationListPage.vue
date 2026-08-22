<template>
  <q-page padding>
    <h1 class="text-h5 text-weight-bold q-mb-lg q-mt-none">
      Staj Başvuruları
    </h1>

    <AppNotice
      type="info"
      dense
      message="Bu sayfa öğrenci ve işletme başvurularını listeler. Yerleştirme için Öğrenciler sayfasını kullanın."
      class="q-mb-md"
    />

    <!-- Bu sayfa Phase 1'de pasif — başvurular öğrenci ve işletme sayfaları üzerinden yönetilir -->
    <div class="flex flex-center q-pa-xl text-grey-6 column">
      <q-icon
        name="inbox"
        size="64px"
        class="q-mb-md"
      />
      <div class="text-subtitle1">
        Başvuru yönetimi için ilgili sayfaları kullanın
      </div>
      <div class="row q-gutter-sm q-mt-md">
        <q-btn
          color="primary"
          icon="person"
          label="Öğrenciler"
          :to="{ name: 'StudentList' }"
        />
        <q-btn
          color="secondary"
          icon="business"
          label="İşletmeler"
          :to="{ name: 'CompanyList' }"
        />
      </div>
    </div>
  </q-page>
</template>

<script setup lang="ts">
// Bu sayfa Phase 1'de pasif — başvuru yönetimi öğrenci/işletme sayfaları üzerinden yapılır.
import AppNotice from 'components/AppNotice.vue'
</script>
