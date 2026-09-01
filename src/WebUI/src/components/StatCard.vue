<script setup lang="ts">
import { computed } from 'vue'
import { useRouter } from 'vue-router'

const props = withDefaults(
  defineProps<{
    /** Quasar ikon adı */
    icon: string
    /** Gösterilecek sayı/değer */
    value: number | string
    /** Alt etiket */
    label: string
    /** Renk (ikon + değer) */
    color?: string
    /**
     * Anlam tonu — `color`dan AYRI bir eksendir ve tek bir değeri vardır.
     *
     * `'accent'` = **"SIRA SİZDE"**: kartın saydığı iş, ekrana bakan kullanıcının KENDİ
     * eylemini bekliyor. Genel "bekliyor" durumu DEĞİLDİR (o `StatusBadge`in
     * `status-pending` tonudur) ve bir ekranda en çok bir bağlamda görünür — nadirliği
     * sinyalin kendisidir.
     *
     * Neden `color="accent"` DEĞİL de ayrı bir prop: `color` şablonda `text-${color}`
     * üretir, yani `color="accent"` saf hardal METİN demek olurdu. #C9A227 beyaz üzerinde
     * 2,42:1 verir — metin eşiğini (4,5:1) de WCAG 1.4.11 grafik nesnesi eşiğini (3:1) de
     * geçemez. `tone` bu yüzden değeri `text-${color}` yerine ölçülmüş türev olan
     * `.text-accent-strong` (#796117) ile yazar. İkon da aynı yerden
     * sürülür (`iconColorClass`), yani accent görünümü için `color` prop'unu ayrıca
     * vermek GEREKMEZ — iki prop'un elle senkron tutulması bileşenin işidir.
     *
     * `tone` verilmediğinde bileşenin çıktısı birebir eskisi gibidir.
     */
    tone?: 'accent'
    /** Yükleniyor — değer yerine skeleton */
    loading?: boolean
    /** Route — verilirse kart tıklanabilir olur */
    to?: string
    /** Düzen: yatay (ikon solda) veya dikey (ikon üstte, ortalı) */
    orientation?: 'horizontal' | 'vertical'
  }>(),
  {
    color: 'primary',
    tone: undefined,
    loading: false,
    to: undefined,
    orientation: 'horizontal',
  },
)

const router = useRouter()

const isAccent = computed(() => props.tone === 'accent')

/*
 * Değerin rengi tek yerden gelir ki iki yerleşim dalı (dikey/yatay) ayrışmasın.
 * tone yokken BUGÜNKÜ davranış korunur: `text-${color}`.
 * tone === 'accent' iken `color` prop'u değere HİÇ karışmaz — yalnız ikonu sürer.
 */
const valueColorClass = computed(() =>
  isAccent.value ? 'text-accent-strong' : `text-${props.color}`,
)

/*
 * İkon rengi de AYNI tek yerden sürülür — önceden ham `color` prop'una bağlıydı ve
 * `tone` ile senkron kalması çağıran tarafın işiydi. `tone="accent"` gelip `color`
 * unutulsaydı ikon eski tonunda kalırdı: ör. `warning` (#9A6B00) hardal `bg-accent-soft`
 * (#f7f2e1) zemininde 4,19:1 verir — WCAG 1.4.11 grafik eşiğini (3:1) geçtiği için
 * erişilebilirlik hatası DEĞİL, ama iki farklı hardal yan yana durur ve Tek Ses okuması
 * bulanır. Artık çağıran yalnız `tone` verir; senkron yükü bileşendedir.
 */
const iconColorClass = computed(() => (isAccent.value ? 'accent-strong' : props.color))

/*
 * Renk Yalnız Kanıt Kuralı: hardal zemin tek başına sinyal olamaz (renk körlüğünde
 * kaybolur). Etiket metni bileşenin İÇİNDE üretilir — çağıran taraf unutamasın diye.
 * Metin sabittir çünkü tonun tek anlamı budur.
 */
const ACCENT_LABEL = 'Sıra sizde'

// Rozet yalnız değer BİLİNİYORKEN çıkar: skeleton sırasında "sıra sizde" vaat edilmez.
const showAccentBadge = computed(() => isAccent.value && !props.loading)

/*
 * Tıklanabilir kartta içerik aria-label ile ezilir; sinyal metni orada da bulunmalı,
 * yoksa ekran okuyucu rozeti hiç duymaz.
 */
const cardAriaLabel = computed(() => {
  if (!props.to) return undefined
  return isAccent.value
    ? `${props.label}: ${props.value} — ${ACCENT_LABEL}`
    : `${props.label}: ${props.value}`
})

function onClick() {
  if (props.to) router.push(props.to)
}
</script>

<template>
  <!-- role="button", "link" değil: kart gerçek bir bağlantı değildir (href yok; yeni
       sekmede aç / bağlantıyı kopyala çalışmaz) ve eylemi router.push'tur. Bağlantı
       olarak duyurmak hem bu vaadi boşa çıkarır hem de Space ile etkinleştirmeyi
       ARIA/APG'ye aykırı kılardı (bağlantı yalnız Enter ile açılır, Space sayfayı
       kaydırır). Buton rolünde Space etkinleştirme beklenen davranıştır; kaydırmanın
       bastırılması da öyle. -->
  <q-card
    flat
    bordered
    :class="[
      'stat-card',
      to ? 'cursor-pointer' : '',
      isAccent ? 'stat-card--accent bg-accent-soft' : '',
    ]"
    :role="to ? 'button' : undefined"
    :tabindex="to ? 0 : undefined"
    :aria-label="cardAriaLabel"
    @click="onClick"
    @keydown.enter.prevent="onClick"
    @keydown.space.prevent="onClick"
  >
    <!-- Alt etiket grey-8 (#616161), grey-7 DEĞİL — ÖLÇÜLDÜ: tıklanabilir kartın hover
         zemini #f6f7f9 (color-mix(in srgb, var(--q-primary) 4%, #fff)) üzerinde grey-7
         yalnız 4,30:1 verir ve metin eşiğinin (4,5:1) altında kalır; grey-8 aynı zeminde
         5,78:1, dinlenen beyaz kartta 6,19:1. Eski `text-grey` (#9e9e9e) 2,68:1 / 2,50:1 idi.
         İki yerleşim dalı (dikey/yatay) aynı tonu paylaşır.
         tone="accent" zeminlerinde de ÖLÇÜLDÜ: dinlenen #f7f2e1 üzerinde 5,53:1,
         hover #f3ebcf üzerinde 5,19:1 — ikisi de eşiği geçiyor, ton değişmedi. -->
    <!-- Dikey: ikon üstte, ortalı -->
    <q-card-section
      v-if="orientation === 'vertical'"
      class="text-center"
    >
      <q-icon
        :name="icon"
        size="40px"
        :color="iconColorClass"
      />
      <q-skeleton
        v-if="loading"
        type="text"
        width="60px"
        class="q-mt-sm"
        style="margin-inline: auto"
      />
      <div
        v-else
        :class="['stat-value', 'text-h4', 'text-weight-bold', valueColorClass, 'q-mt-sm']"
      >
        {{ value }}
      </div>
      <!-- Rozet kendi satırında DEĞİL, etiketin YANINDA duruyor ve satır yüksekliği
           .stat-label-row ile sabitlendi: rozet yüklemeden SONRA belirdiği için kart
           aksi hâlde yerinde büyür (bkz. style bloğundaki ölçüm).
           Dolu rozet: bg-accent-strong (#796117) + q-badge'in varsayılan beyaz metni
           (QBadge.sass) = 5,94:1. Craft-floor yasağı gereği sinyalin biçimi zemin +
           dolu rozettir, kalın renkli kenarlık DEĞİL. -->
      <div class="stat-label-row row items-center justify-center">
        <div class="text-caption text-grey-8">
          {{ label }}
        </div>
        <q-badge
          v-if="showAccentBadge"
          color="accent-strong"
          :label="ACCENT_LABEL"
          class="text-body2 q-px-sm q-py-xs q-ml-xs"
        />
      </div>
    </q-card-section>

    <!-- Yatay: ikon solda -->
    <q-card-section
      v-else
      class="row items-center no-wrap"
    >
      <q-icon
        :name="icon"
        size="40px"
        :color="iconColorClass"
        class="q-mr-md"
      />
      <div>
        <q-skeleton
          v-if="loading"
          type="text"
          width="60px"
        />
        <div
          v-else
          :class="['stat-value', 'text-h4', 'text-weight-bold', valueColorClass]"
        >
          {{ value }}
        </div>
        <div class="stat-label-row row items-center">
          <div class="text-caption text-grey-8">
            {{ label }}
          </div>
          <q-badge
            v-if="showAccentBadge"
            color="accent-strong"
            :label="ACCENT_LABEL"
            class="text-body2 q-px-sm q-py-xs q-ml-xs"
          />
        </div>
      </div>
    </q-card-section>
  </q-card>
</template>

<style scoped>
/*
 * Etiket satırı rozetin yüksekliğine sabitlenir — DÜZEN KAYMAZ.
 *
 * Sorun: `tone` yalnız yükleme bittikten SONRA 'accent' olur (DashboardPage sayaç
 * verisine bakar), yani rozet skeleton çözülünce belirir. Önceki hâlinde kendi satırında
 * (`<div class="q-mt-xs">`) duruyordu; kart o anda rozet + üst boşluk kadar, yani
 * 28px + 4px uzardı: (1) sayaç kartı yüklendikten sonra düzen kayardı — DESIGN.md'nin
 * skeleton kuralı ("44px skeleton satır — düzen kaymaz") tam bunu önlemek için var;
 * (2) aynı satırdaki kartlardan yalnız biri uzayacağı için satır tırtıklı görünürdü
 * (q-card yüksekliği esnemiyor).
 *
 * `v-if="isAccent"` ile yer tutucu render etmek bu vakada İŞE YARAMAZDI: yükleme
 * sırasında `tone` undefined, yani `isAccent` zaten false — ayrılacak yer hiç
 * oluşmazdı. Bu yüzden yer, tondan BAĞIMSIZ olarak burada ayrılıyor.
 *
 * ÖLÇÜLDÜ (quasar 2, dist/quasar.css + QBadge.sass):
 *   .text-body2 line-height 1,25rem = 20px, .q-py-xs 4px + 4px  → rozet 28px
 *   (.q-badge padding 2px'i q-py-xs eziyor; min-height 12px bağlayıcı değil)
 *   .text-caption line-height 1,25rem = 20px                    → etiket satırı 20px
 * Fark tüm StatCard'larda tekdüze +8px'tir ve ilk boyamada oluşur — yüklemeyle
 * değişmez, dolayısıyla kayma da tırtıklanma da olmaz.
 */
.stat-label-row {
  min-height: 28px;
}

/* Tonal Derinlik Kuralı: dinlenen kart yüzmez. Hover geri bildirimi gölge/kaldırma
   yerine tema türevi zemin tonu + koyulaşan kenarlıkla verilir — kiracı rengi
   değişince geri bildirim de birlikte kayar. */
.stat-card.cursor-pointer {
  transition:
    background-color 0.2s ease,
    border-color 0.2s ease;
}
.stat-card.cursor-pointer:hover {
  background-color: #f6f7f9;
  background-color: color-mix(in srgb, var(--q-primary) 4%, #fff);
  border-color: rgba(30, 58, 95, 0.28);
  border-color: color-mix(in srgb, var(--q-primary) 28%, transparent);
}
.stat-card.cursor-pointer:focus-visible {
  outline: 2px solid var(--q-primary);
  outline-offset: -2px;
}

/*
 * tone="accent" hover'ı: lacivert türevi hover zemini (#f6f7f9) hardal zeminin ÜSTÜNE
 * gelseydi kart fareyle üzerine gelindiğinde sinyali kaybederdi. Bu yüzden hover tonu da
 * hardal ailesinden türetilir — dinlenen %14 karışımdan %22'ye koyulaşır.
 *
 * Seçici bilerek üç sınıflı: varsayılan `.stat-card.cursor-pointer:hover` ile aynı
 * özgüllükte kalsaydı sonuç kaynak sırasına bağlı olurdu.
 *
 * ÖLÇÜLDÜ (sRGB relative luminance, WCAG 2.x) — hover zemini #f3ebcf (L=0,8298):
 *   text-accent-strong (#796117, L=0,1268) ....... 4,98:1  ✓ metin eşiği 4,5:1
 *   text-grey-8        (#616161, L=0,1195) ....... 5,19:1  ✓ metin eşiği 4,5:1
 *   ikon color="accent-strong" (#796117) ......... 4,98:1  ✓ grafik eşiği 3:1
 * Dinlenen zemin #f7f2e1 (L=0,8871) ile karşılaştırma 1,065:1 — varsayılan hover'ın
 * beyazdan #f6f7f9'a farkı (1,072:1) ile pratik olarak AYNI, yani geri bildirim
 * varsayılan kartla eş güçte; hardal zeminde zayıflamıyor.
 *
 * TAVAN — %22 bir eşik değil, marj payıdır. Ölçüldü (eşiği geçen EN KOYU karışım):
 *   #796117 (text-accent-strong) ... %33 (#ede0b8) 4,51:1 — %34'te düşer
 *   #616161 (text-grey-8, alt etiket) %38 (#eadcad) 4,52:1 — %39'da düşer
 * Bağlayıcı olan değerin rengidir (accent-strong), alt etiket değil; gerçek tavan %33.
 * %22 bilerek onun altında tutuldu: hover tonu sinyali taşımalı ama kartı bir uyarı
 * yüzeyine çevirmemeli. Koyulaştırmadan önce ölç.
 */
.stat-card.stat-card--accent.cursor-pointer:hover {
  background-color: #f3ebcf;
  background-color: color-mix(in srgb, var(--q-accent) 22%, #fff);
  /* 1px kenarlık; kontrol kimliğini taşımaz (kart zaten metniyle tanınır), bu yüzden
     dinlenen {colors.ayrac} gibi 3:1'e tabi değildir — yalnız aileyi korur. */
  border-color: rgba(201, 162, 39, 0.45);
  border-color: color-mix(in srgb, var(--q-accent) 45%, transparent);
}
</style>
