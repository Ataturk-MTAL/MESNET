import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import { computed, ref } from 'vue'
import { useServerPagination } from 'src/composables/useServerPagination'
import type { PagedResponse } from 'src/types/pagination'
import type { InstitutionDto } from 'src/api/institution'
import {
  DEFAULT_NODE_TYPE_FILTER,
  DEFAULT_SORT_BY,
  buildInstitutionListFilters,
} from './institutionListQuery'

/**
 * Liste sayfasının sunucu sözleşmesi.
 *
 * <p>Test bileşeni monte etmez, <b>sayfanın sunucuya ne sorduğunu</b> ölçer. Kırılgan olan
 * kısım şablon değil sözleşmedir: `nodeType` süzgeci gitmezse liste il/ilçe müdürlüklerini
 * okul gibi gösterir, `page`/`search` gitmezse sayfalama ve arama sessizce istemci tarafına
 * düşer ve yalnız ilk 20 satır aranır.</p>
 *
 * <p><b>Sahte-yeşil riski kapatıldı (Engelleyici 2):</b> eski sürüm bu değerleri (`'School'`
 * varsayılanı, `nodeType` gövde şekli, `'fullName'` sıralaması) burada YENİDEN yazıyordu —
 * `InstitutionListPage.vue`'yu hiç import etmiyordu. Sayfanın varsayılanı değişse (ör.
 * `'Province'`e) test bunu göremiyordu (ölçüldü, aşağıdaki kanıt). Bu sürüm `./institutionListQuery`
 * içindeki sabitleri ve kurucuyu import eder — sayfa da AYNI dosyayı kullanır (bkz.
 * `InstitutionListPage.vue`), yani test artık sayfanın gerçek sözleşmesine bağlıdır.</p>
 *
 * <p><b>`useServerPagination` gerçek imzası (composable dosyasından):</b> `onSearch(term)`
 * senkrondur ve 400ms debounce ile `load()`'u tetikler — Promise DÖNMEZ. `onRequest(props)`
 * de senkrondur, `{ pagination: { page, rowsPerPage, sortBy, descending } }` alır ve `load()`'u
 * fire-and-forget çağırır. Bu yüzden ikisi de `await` ile değil, `vi.useFakeTimers()` +
 * `vi.runAllTimersAsync()` ile doğrulanır — composable'ın kendi testindeki (`useServerPagination.spec.ts`)
 * desenin birebir aynısı. Composable BURADA DEĞİŞTİRİLMEDİ.</p>
 */
describe('InstitutionListPage — sunucu sözleşmesi', () => {
  const bosSayfa: PagedResponse<InstitutionDto> = {
    items: [],
    totalCount: 0,
    page: 1,
    pageSize: 20,
    totalPages: 0,
    hasNextPage: false,
    hasPreviousPage: false,
  }

  type FetchFn = (
    params: Record<string, unknown>,
  ) => Promise<{ data: PagedResponse<InstitutionDto> }>

  let fetchFn: ReturnType<typeof vi.fn<FetchFn>>

  beforeEach(() => {
    vi.useFakeTimers()
    fetchFn = vi.fn(async () => ({ data: bosSayfa }))
  })

  afterEach(() => {
    vi.useRealTimers()
  })

  it('varsayılan süzgeç OKUL — üst düğümler okul listesinde görünmemeli', async () => {
    // Arrange — sayfanın KENDİ varsayılanı; burada yeniden yazılmaz.
    const nodeType = ref(DEFAULT_NODE_TYPE_FILTER)
    const { load } = useServerPagination<InstitutionDto>({
      fetchFn,
      filters: computed(() => buildInstitutionListFilters(nodeType.value)),
      defaultSortBy: DEFAULT_SORT_BY,
    })

    // Act
    await load()

    // Assert
    expect(fetchFn).toHaveBeenCalledWith(expect.objectContaining({ nodeType: 'School' }))
  })

  it('kurum türü süzgeci sunucuya gider', async () => {
    const nodeType = ref('District')
    const { load } = useServerPagination<InstitutionDto>({
      fetchFn,
      filters: computed(() => buildInstitutionListFilters(nodeType.value)),
      defaultSortBy: DEFAULT_SORT_BY,
    })

    await load()

    expect(fetchFn).toHaveBeenCalledWith(expect.objectContaining({ nodeType: 'District' }))
  })

  it('arama terimi sunucuya gider — istemci tarafında süzülmez', async () => {
    const { onSearch } = useServerPagination<InstitutionDto>({
      fetchFn,
      filters: computed(() => buildInstitutionListFilters(DEFAULT_NODE_TYPE_FILTER)),
      defaultSortBy: DEFAULT_SORT_BY,
    })

    // onSearch senkron — debounce sonrası load() tetiklenir, Promise döndürmez.
    onSearch('Atatürk')
    await vi.runAllTimersAsync()

    expect(fetchFn).toHaveBeenCalledWith(expect.objectContaining({ search: 'Atatürk' }))
  })

  it('sayfa isteği sunucuya gider', async () => {
    const { onRequest } = useServerPagination<InstitutionDto>({
      fetchFn,
      filters: computed(() => buildInstitutionListFilters(DEFAULT_NODE_TYPE_FILTER)),
      defaultSortBy: DEFAULT_SORT_BY,
    })

    // onRequest senkron — load()'u beklemeden tetikler.
    onRequest({ pagination: { page: 3, rowsPerPage: 20, sortBy: 'fullName', descending: false } })
    await vi.runAllTimersAsync()

    expect(fetchFn).toHaveBeenCalledWith(expect.objectContaining({ page: 3 }))
  })

  it('varsayılan sıralama kurum adıdır — sıralamasız liste her yazmadan sonra kayardı', async () => {
    const { load } = useServerPagination<InstitutionDto>({
      fetchFn,
      filters: computed(() => buildInstitutionListFilters(DEFAULT_NODE_TYPE_FILTER)),
      defaultSortBy: DEFAULT_SORT_BY,
    })

    await load()

    expect(fetchFn).toHaveBeenCalledWith(expect.objectContaining({ sortBy: 'fullName' }))
  })
})
