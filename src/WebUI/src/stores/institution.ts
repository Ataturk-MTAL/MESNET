import { ref, computed } from 'vue'
import { defineStore } from 'pinia'
import {
  institutionApi,
  type InstitutionDto,
  type FieldOfStudyDto,
  type ScheduleConfigDto,
} from 'src/api/institution'
import { useAuthStore } from './auth'
import { applyBrandTheme } from 'utils/brandTheme'

/**
 * Kurum referans/katalog verisi için merkezi cache.
 * Kurum profili (branches dahil), MEB alan/dal kataloğu ve ders programı config'i
 * bir kez yüklenip tüm sayfalarda paylaşılır (isLoaded guard). academicPeriod store deseni.
 * Mutasyon sonrası ilgili load*(true) ile tazelenir → tüm tüketicilere reaktif yansır.
 */
export const useInstitutionStore = defineStore('institution', () => {
  /**
   * Bağlamda DAVRANILAN kurum. `user?.institutionId` (EV kurumu) DEĞİL,
   * `authStore.currentInstitutionId` (Görev 8: aktif bağlam varsa o, yoksa ev kurumu) okunur.
   *
   * <p>Denetimden gelen düzeltme: bu yardımcı eskiden ev kurumunu okuyordu. Bağlam
   * `useInstitutionContext().switchTo()` ile değiştirildiğinde `institutionStore.clear()`
   * çağrılıyor ama bir sonraki `loadInstitution()`/`loadFieldCatalog()` yine ev kurumunun
   * verisini çekiyordu — kurum profili sayfası sessizce YANLIŞ okulu gösteriyordu. Bu,
   * `academicPeriodStore`'da Görev 9'un kapattığı tuzağın (bkz. `academicPeriod.ts`
   * `loadPeriods`) birebir ikinci kopyasıydı. `loadedInstitutionId` koruması zaten kiracı
   * değişimini kimlik karşılaştırmasıyla yakalıyor; sorun yalnız BURADA hangi kimliğin
   * okunduğuydu.</p>
   */
  function currentInstitutionId(): string | null {
    return useAuthStore().currentInstitutionId
  }

  // ── Kurum profili (branches + staff dahil) ──
  const institution = ref<InstitutionDto | null>(null)
  const isLoaded = ref(false)

  /**
   * Cache'teki kaydın HANGİ kuruma ait olduğu.
   *
   * `isLoaded` tek başına yetmez: kiracı değişirse (kullanıcının kurumu değiştirilirse)
   * bayrak hâlâ true'dur ve `loadInstitution()` erken döner — eski okulun adı, alanları VE
   * marka teması ekranda kalırdı. Kimlik karşılaştırması bu sessiz bayatlamayı kapatır.
   */
  const loadedInstitutionId = ref<string | null>(null)

  const branches = computed(() => institution.value?.branches ?? [])
  const activeBranches = computed(() => branches.value.filter((b) => b.isActive))

  // ── MEB alan/dal kataloğu (tam katalog — educationType filtreli sorgular lokal kalır) ──
  const fieldCatalog = ref<FieldOfStudyDto[]>([])
  const isFieldCatalogLoaded = ref(false)

  // ── Ders programı config ──
  const scheduleConfig = ref<ScheduleConfigDto | null>(null)
  const isScheduleConfigLoaded = ref(false)

  const periodCount = computed(() =>
    scheduleConfig.value?.configured && scheduleConfig.value.dailyPeriodCount
      ? scheduleConfig.value.dailyPeriodCount
      : 0,
  )
  const scheduleConfigMissing = computed(
    () => isScheduleConfigLoaded.value && periodCount.value === 0,
  )

  /**
   * Kurum profilini yükler ve <b>kiracının marka temasını uygular</b>.
   *
   * <p>Tema burada uygulanır çünkü <c>InstitutionDto</c> uygulamaya TEK bu kapıdan girer:
   * palet değiştikten sonra çağrılan <c>loadInstitution(true)</c> ile tema kendiliğinden
   * tazelenir, kiracı değişince yeni renkle yeniden kurulur. Uygulamayı ayrı bir yerde
   * (boot dosyası, bileşen) tetiklemek ikinci bir doğruluk kaynağı yaratır ve o kaynak er
   * geç cache ile ayrışır.</p>
   *
   * <p>Hex sunucudan gelir; frontend'de ikinci kez tanımlanmaz. Değer bozuksa
   * <c>applyBrandTheme</c> sessizce derleme zamanı varsayılanına (Mührü Lacivert) düşer —
   * kontrast hiçbir durumda ölçülmemiş bir renge bırakılmaz.</p>
   */
  async function loadInstitution(force = false): Promise<void> {
    const id = currentInstitutionId()
    if (!id) return
    if (isLoaded.value && !force && loadedInstitutionId.value === id) return
    try {
      const { data } = await institutionApi.get(id)
      institution.value = data
      loadedInstitutionId.value = id
      applyBrandTheme(data?.brandPrimary, data?.brandSecondary)
    } finally {
      isLoaded.value = true
    }
  }

  async function loadFieldCatalog(force = false): Promise<void> {
    if (isFieldCatalogLoaded.value && !force) return
    try {
      const { data } = await institutionApi.getFieldCatalog()
      fieldCatalog.value = data ?? []
    } finally {
      isFieldCatalogLoaded.value = true
    }
  }

  async function loadScheduleConfig(force = false): Promise<void> {
    const id = currentInstitutionId()
    if (!id) return
    if (isScheduleConfigLoaded.value && !force) return
    try {
      const { data } = await institutionApi.getScheduleConfig(id)
      scheduleConfig.value = data
    } catch {
      scheduleConfig.value = { configured: false }
    } finally {
      isScheduleConfigLoaded.value = true
    }
  }

  /**
   * Cache'i geçersiz kılar; bir sonraki erişim taze veri çeker.
   *
   * <b>Temayı SIFIRLAMAZ:</b> bu fonksiyon mutasyon sonrası tazeleme için çağrılıyor
   * (InstitutionPage her kaydetmede çağırır) ve tema sıfırlansaydı ekran her kayıtta
   * varsayılan laciverte düşüp yeniden kiracı rengine dönerdi. Tema yalnız yeni kurum
   * verisi geldiğinde değişir.
   */
  function clear(): void {
    institution.value = null
    isLoaded.value = false
    loadedInstitutionId.value = null
    fieldCatalog.value = []
    isFieldCatalogLoaded.value = false
    scheduleConfig.value = null
    isScheduleConfigLoaded.value = false
  }

  return {
    // Kurum profili
    institution,
    isLoaded,
    branches,
    activeBranches,
    loadInstitution,
    // Katalog
    fieldCatalog,
    isFieldCatalogLoaded,
    loadFieldCatalog,
    // Ders programı config
    scheduleConfig,
    periodCount,
    scheduleConfigMissing,
    isScheduleConfigLoaded,
    loadScheduleConfig,
    // Yardımcı
    clear,
  }
})
