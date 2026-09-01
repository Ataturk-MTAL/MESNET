<template>
  <q-page padding>
    <PageHeader title="Staj Başvuruları" />

    <AppNotice
      type="info"
      dense
      message="Bu sayfa öğrenci ve işletme başvurularını listeler. Yerleştirme için Öğrenciler sayfasını kullanın."
      class="q-mb-md"
    />

    <!-- Bu sayfa Phase 1'de pasif — başvurular öğrenci ve işletme sayfaları üzerinden yönetilir -->
    <!--
      Sarmalayıcı gri: içindeki text-subtitle1 metni 1rem = 16px normal ağırlıktadır,
      yani büyük-metin istisnasına (>=24px, ya da >=18,66px kalın) girmez ve 4,5:1 ister.
      grey-6 (#9e9e9e) beyaz zeminde 2,68:1 idi; grey-7 (#757575) 4,61:1. 64px inbox
      ikonu aynı sınıfı alır ama metin aynı bilgiyi verdiği için dekoratiftir.
    -->
    <div class="flex flex-center q-pa-xl text-grey-7 column">
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
          unelevated
          color="primary"
          icon="person"
          label="Öğrenciler"
          :to="{ name: 'StudentList' }"
        />
        <q-btn
          unelevated
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
import PageHeader from 'components/PageHeader.vue'
</script>
