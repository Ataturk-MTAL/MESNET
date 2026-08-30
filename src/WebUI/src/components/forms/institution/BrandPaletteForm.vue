<template>
  <FormDialog
    v-model="open"
    title="Kurum Teması"
    icon="palette"
    color="primary"
    width="520px"
    :saving="saving"
    :save-disabled="!selected || selected === currentPaletteName"
    @save="handleSave"
  >
    <div
      id="marka-paleti-etiket"
      class="text-subtitle2"
    >
      Marka Paleti
    </div>
    <div class="text-caption text-grey-7">
      Renkler önceden ölçülmüş sekiz seçenekten seçilir; serbest renk girilmez. Seçim üst
      barı, birincil butonları ve rozetleri birlikte kaydırır. Durum renkleri (onay, ret,
      uyarı, bilgi) değişmez.
    </div>

    <DataState
      :loading="loadingCatalog"
      :error="!!catalogError"
      :error-text="catalogError ?? undefined"
      retryable
      padding="q-pa-lg"
      @retry="loadCatalog"
    >
      <div
        role="radiogroup"
        aria-labelledby="marka-paleti-etiket"
        class="row q-col-gutter-sm"
      >
        <div
          v-for="palette in palettes"
          :key="palette.name"
          class="col-12 col-sm-6"
        >
          <q-card
            flat
            bordered
            :class="['palette-option', { 'palette-option--selected': selected === palette.name }]"
          >
            <q-radio
              v-model="selected"
              :val="palette.name"
              :disable="saving"
              class="palette-option__radio"
            >
              <div class="row items-center no-wrap q-gutter-sm">
                <BrandPaletteSwatch
                  :primary="palette.primary"
                  :secondary="palette.secondary"
                />
                <div class="col">
                  <!-- Türkçe ad HER ZAMAN görünür: renk tek sinyal değildir. -->
                  <div class="text-body2 text-weight-medium">
                    {{ palette.slug }}
                  </div>
                  <div class="text-caption text-grey-7">
                    <template v-if="palette.isDefault">
                      Varsayılan
                    </template>
                    <template v-else>
                      {{ palette.primary }}
                    </template>
                  </div>
                </div>
              </div>
            </q-radio>
          </q-card>
        </div>
      </div>
    </DataState>
  </FormDialog>
</template>

<script setup lang="ts">
import { ref, watch } from 'vue'
import { institutionApi, type BrandPaletteDto } from 'src/api/institution'
import { useNotify } from 'src/composables/useNotify'
import { applyBrandTheme } from 'utils/brandTheme'
import FormDialog from 'components/FormDialog.vue'
import DataState from 'components/DataState.vue'
import BrandPaletteSwatch from 'components/BrandPaletteSwatch.vue'

const open = defineModel<boolean>({ required: true })

const props = defineProps<{
  institutionId: string
  /** Kurumun yürürlükteki palet ANAHTARI (InstitutionDto.brandPaletteName) — hex değil. */
  currentPaletteName: string
}>()

const emit = defineEmits<{ saved: [] }>()

const notify = useNotify()
const saving = ref(false)

/*
 * Katalog burada yükleniyor, institutionStore'da DEĞİL.
 *
 * Değişmez bir kod sabiti ve tek kullanım noktası var (bu panel); store'a koymak
 * geçersiz kılma yüzeyi ekler ama hiçbir şey kazandırmaz — MEMORY'deki "küçük/tek-kullanım
 * listeler per-instance kalır" kuralı. Aynı sayfadaki alan kataloğu da böyle yükleniyor.
 */
const palettes = ref<BrandPaletteDto[]>([])
const loadingCatalog = ref(false)
const catalogError = ref<string | null>(null)
const selected = ref<string>('')

async function loadCatalog() {
  loadingCatalog.value = true
  catalogError.value = null
  try {
    const { data } = await institutionApi.getBrandPalettes()
    palettes.value = data ?? []
  } catch {
    catalogError.value = 'Palet listesi yüklenemedi.'
  } finally {
    loadingCatalog.value = false
  }
}

watch(open, (isOpen) => {
  if (!isOpen) return
  selected.value = props.currentPaletteName
  if (palettes.value.length === 0) {
    loadCatalog().catch(() => {})
  }
})

async function handleSave() {
  const chosen = palettes.value.find((p) => p.name === selected.value)
  if (!chosen) return

  saving.value = true
  try {
    // Gövde YALNIZ anahtarı taşır; hex gönderilmez, sunucu kabul etmez.
    await institutionApi.setBrandPalette(props.institutionId, { paletteName: chosen.name })

    // Tema ANINDA uygulanır — hex katalog yanıtından gelir, yani hâlâ sunucunun değeridir;
    // frontend renk tanımlamaz. Sayfanın yeniden yüklemesini beklemek, kaydettiği rengi
    // görmek isteyen kullanıcıya bir istek boyu eski temayı gösterirdi.
    applyBrandTheme(chosen.primary, chosen.secondary)

    notify.success('Kurum teması güncellendi.')
    open.value = false
    // Çağıran sayfa kurumu yeniden çeker ve institutionStore'u geçersiz kılar
    // (InstitutionPage.load → institutionStore.clear) — CLAUDE.md cache invalidation kuralı.
    emit('saved')
  } catch (e) {
    notify.apiError(e, 'Tema kaydedilirken hata oluştu.')
  } finally {
    saving.value = false
  }
}
</script>

<style scoped>
.palette-option {
  height: 100%;
}

/*
 * Seçili kart: kenarlık marka rengine döner + yumuşak zemin.
 * Zemin tonu app.css'te bir kez tanımlı `bg-primary-soft` ile AYNI reçetedir
 * (color-mix %8/%12 ailesi); burada yeni bir ton uydurulmuyor.
 *
 * Bu yalnız pekiştirmedir — seçimin taşıyıcı işareti radyo düğmesinin kendisi ve
 * yanındaki Türkçe addır. Bu yüzden kenarlık WCAG 1.4.11 (3:1) kapsamında zorunlu
 * bir gösterge değildir.
 */
.palette-option--selected {
  border-color: var(--q-primary);
  background-color: #e4e7ec;
  background-color: color-mix(in srgb, var(--q-primary) 8%, #fff);
}

/* Tıklama hedefi kartın tamamı olsun — 44px'lik dokunma alanı kutuyu doldurur. */
.palette-option__radio {
  width: 100%;
  padding: 8px 12px 8px 4px;
}

/*
 * QRadio kök öğesi inline-flex'tir ve etiketine büyüme vermez (QRadio.sass); etiket
 * içerik genişliğinde kalır ve iki sütunlu ızgarada kartlar farklı genişlikte durur.
 * min-width:0 ayrıca uzun palet adının taşmak yerine kırpılmasına izin verir.
 */
.palette-option__radio :deep(.q-radio__label) {
  flex: 1 1 auto;
  min-width: 0;
}
</style>
