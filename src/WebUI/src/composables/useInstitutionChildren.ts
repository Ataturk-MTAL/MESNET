import { ref, type Ref } from 'vue'
import { institutionApi, type InstitutionDto } from 'src/api/institution'
import { childNodeTypeFor } from 'utils/institutionTree'

const CHILDREN_PAGE_SIZE = 100

/**
 * Bir il/ilçe müdürlüğü düğümünün altındaki kurum ağacını yükler.
 *
 * <p><b>Neden burada:</b> `InstitutionChildrenTree.vue` bu mantığı doğrudan taşısaydı sayfanın
 * 300 satır sınırı (depo kuralı) aşılırdı ve yükleme/genişletme durumu bileşen testinden
 * bağımsız koşamazdı. İki seviyeli ağaç (İl → İlçe → Okul) tek uçtan (`GET /api/institutions
 * ?parentId&nodeType`) iki adımda kurulur: kök çocuklar `load()` ile, her ilçenin okulları
 * yalnız o ilçe AÇILDIĞINDA (`toggleDistrict`) — bir il müdürlüğünün onlarca ilçesi olabilir,
 * hepsinin okullarını baştan çekmek gereksiz yük olurdu.</p>
 */
export function useInstitutionChildren(institutionId: Ref<string>, nodeType: Ref<string>) {
  const loading = ref(false)
  const error = ref(false)
  // Ham hata — `notify.apiError` sunucunun 4xx mesajını göstermek için buna ihtiyaç duyar;
  // `error` (boolean) yalnız DataState'in hata dalını tetikler.
  const lastError = ref<unknown>(null)
  const children = ref<InstitutionDto[]>([])

  const expandedIds = ref<Record<string, boolean>>({})
  const districtSchools = ref<Record<string, InstitutionDto[]>>({})
  const districtSchoolsLoading = ref<Record<string, boolean>>({})

  async function load(): Promise<void> {
    const childType = childNodeTypeFor(nodeType.value)
    if (!childType || !institutionId.value) {
      children.value = []
      return
    }

    loading.value = true
    error.value = false
    lastError.value = null
    try {
      const { data } = await institutionApi.list({
        parentId: institutionId.value,
        nodeType: childType,
        pageSize: CHILDREN_PAGE_SIZE,
      })
      children.value = data?.items ?? []
    } catch (e) {
      error.value = true
      lastError.value = e
      children.value = []
    } finally {
      loading.value = false
    }
  }

  /** Bir ilçenin okullarını (yalnız ilk açılışta) yükler ve genişletme durumunu değiştirir. */
  async function toggleDistrict(districtId: string): Promise<void> {
    const wasExpanded = !!expandedIds.value[districtId]
    expandedIds.value = { ...expandedIds.value, [districtId]: !wasExpanded }
    if (wasExpanded || districtSchools.value[districtId]) return

    districtSchoolsLoading.value = { ...districtSchoolsLoading.value, [districtId]: true }
    try {
      const { data } = await institutionApi.list({
        parentId: districtId,
        nodeType: 'School',
        pageSize: CHILDREN_PAGE_SIZE,
      })
      districtSchools.value = { ...districtSchools.value, [districtId]: data?.items ?? [] }
    } catch {
      districtSchools.value = { ...districtSchools.value, [districtId]: [] }
    } finally {
      districtSchoolsLoading.value = { ...districtSchoolsLoading.value, [districtId]: false }
    }
  }

  return {
    loading,
    error,
    lastError,
    children,
    expandedIds,
    districtSchools,
    districtSchoolsLoading,
    load,
    toggleDistrict,
  }
}
