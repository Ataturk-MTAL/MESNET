<template>
  <q-card
    flat
    bordered
  >
    <q-card-section>
      <div class="text-subtitle1 text-weight-medium q-mb-md">
        Kurum Bilgileri
      </div>
      <div class="row q-col-gutter-md info-items">
        <div class="col-12 col-md-6">
          <InfoItem
            icon="location_on"
            label="Adres"
            :value="institution.address"
          />
        </div>
        <div class="col-12 col-md-6">
          <InfoItem
            icon="map"
            label="İl / İlçe"
          >
            <!-- Ad görüntü, kod yetkili (#147) — ikisi birlikte gösterilir ki
                 kaydın hangi il koduyla saklandığı ekrandan doğrulanabilsin. -->
            <template v-if="institution.provinceName">
              {{ institution.provinceName }} ({{ institution.provinceCode }})
              <template v-if="institution.districtName">
                / {{ institution.districtName }}
              </template>
            </template>
            <span v-else>—</span>
          </InfoItem>
        </div>
        <div class="col-12 col-md-6">
          <InfoItem
            icon="phone"
            label="Telefon"
            :value="institution.phoneNumber"
          />
        </div>
        <div class="col-12 col-md-6">
          <InfoItem
            icon="email"
            label="E-posta"
            :value="institution.email"
          />
        </div>
        <div class="col-12 col-md-6">
          <InfoItem
            icon="language"
            label="Web Sitesi"
          >
            <!-- href yalnız süzülmüş http(s) URL'i alır: serbest metin alanına
              yazılmış javascript:/data: adresi tıklayanın oturumunda çalışırdı.
              rel="noopener noreferrer" ters sekme ele geçirmesini kapatır. -->
            <a
              v-if="safeWebUrl"
              :href="safeWebUrl"
              target="_blank"
              rel="noopener noreferrer"
              class="text-primary"
            >
              {{ institution.webUrl }}
            </a>
            <span v-else-if="institution.webUrl">{{ institution.webUrl }}</span>
            <span v-else>—</span>
          </InfoItem>
        </div>
        <div class="col-12 col-md-6">
          <InfoItem
            icon="my_location"
            label="Konum"
          >
            <template v-if="institution.location">
              {{ institution.location.latitude.toFixed(6) }}, {{ institution.location.longitude.toFixed(6) }}
            </template>
            <span
              v-else
              class="text-grey-7"
            >Konum eklenmemiş</span>
          </InfoItem>
        </div>
      </div>
    </q-card-section>
  </q-card>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import type { InstitutionDto } from 'src/api/institution'
import { toSafeUrl } from 'utils/safeUrl'
import InfoItem from 'components/InfoItem.vue'

const props = defineProps<{
  institution: InstitutionDto
}>()

// Bağlantı olarak SADECE http(s) adres verilir; güvenli değilse metin olarak gösterilir.
const safeWebUrl = computed(() => toSafeUrl(props.institution.webUrl))
</script>

<style scoped>
/*
 * InfoItem ikonu ikincil tondadır — değer metni birincil kalır.
 *
 * Neden bir kural gerekiyor: Quasar `.q-item__section--side` için $grey-7 atar,
 * ama `.q-item__section--avatar` bunu `color: inherit` ile geri alır
 * (quasar/src/components/item/QItem.sass) — InfoItem ikonu gövde metni rengine
 * yükseliyordu.
 *
 * Ton ESKİDEN (çağrı yerindeki `color="grey-6"`) #9e9e9e idi ve InfoItem'a
 * inerken düştü. Birebir geri GETİRİLMEDİ, bilerek bir kademe koyulaştırıldı:
 * ikon DEKORATİFTİR — InfoItem şablonu (components/InfoItem.vue) ikonun hemen
 * yanına koşulsuz bir `q-item-label caption` basar (Adres, Telefon, E-posta,
 * Konum …) ve aynı bilgiyi görünür veriyor. Bu yüzden WCAG 1.4.11 muafiyeti
 * geçerlidir ve 3:1 grafik nesnesi eşiği YÜRÜRLÜKTE DEĞİLDİR.
 * #757575 bir erişilebilirlik zorunluluğu değil, bilinçli hiyerarşi kararıdır.
 *
 * Ölçüm (WCAG 2.x, sRGB relative luminance; beyaz zemin #FFFFFF):
 *   grey-6 #9e9e9e → 2,68:1  — muafiyet olmasaydı 3:1'i geçemezdi
 *   grey-7 #757575 → 4,61:1  — muafiyete güvenmeden de her iki eşiği geçer
 * Muafiyet varken bile koyu ton seçildi: bu ikonlar bir gün etiketsiz bir
 * bağlama taşınırsa (etiket InfoItem'dan kalkarsa) muafiyet düşer ve ton
 * sessizce eşik altına inerdi.
 *
 * Hex Quasar'ın $grey-7 token'ıdır (quasar/src/css/variables.sass:360) — aynı
 * zamanda Quasar'ın kendi `.q-item__section--side` rengi, uydurma bir ton değil.
 * Değeri değiştirmeden önce kontrastı yeniden ölç.
 */
.info-items :deep(.q-item__section--avatar .q-icon) {
  color: #757575;
}
</style>
