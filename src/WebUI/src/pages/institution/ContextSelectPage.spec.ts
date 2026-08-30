import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import { computed } from 'vue'
import { useServerPagination } from 'src/composables/useServerPagination'
import type { PagedResponse } from 'src/types/pagination'
import type { InstitutionDto } from 'src/api/institution'
import {
  DEFAULT_NODE_TYPE_FILTER,
  DEFAULT_SORT_BY,
  buildContextSelectFilters,
} from './contextSelectQuery'

describe('ContextSelectPage — sunucu sözleşmesi', () => {
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

  const kur = () =>
    useServerPagination<InstitutionDto>({
      fetchFn,
      filters: computed(() => buildContextSelectFilters(DEFAULT_NODE_TYPE_FILTER)),
      defaultSortBy: DEFAULT_SORT_BY,
    })

  beforeEach(() => {
    vi.useFakeTimers()
    fetchFn = vi.fn(async () => ({ data: bosSayfa }))
  })

  afterEach(() => {
    vi.useRealTimers()
  })

  it('yalnız OKULLAR listelenir — il/ilçe düğümü seçilebilir bağlam değildir', async () => {
    const { load } = kur()
    await load()
    expect(fetchFn).toHaveBeenCalledWith(expect.objectContaining({ nodeType: 'School' }))
  })

  it('kurum adına göre sıralanır', async () => {
    const { load } = kur()
    await load()
    expect(fetchFn).toHaveBeenCalledWith(expect.objectContaining({ sortBy: 'fullName' }))
  })

  it('arama terimi sunucuya gider — istemci tarafında süzülmez', async () => {
    const { onSearch } = kur()
    onSearch('Atatürk')
    await vi.runAllTimersAsync()
    expect(fetchFn).toHaveBeenCalledWith(expect.objectContaining({ search: 'Atatürk' }))
  })

  it('sayfa isteği sunucuya gider', async () => {
    const { onRequest } = kur()
    onRequest({ pagination: { page: 2, rowsPerPage: 20, sortBy: 'fullName', descending: false } })
    await vi.runAllTimersAsync()
    expect(fetchFn).toHaveBeenCalledWith(expect.objectContaining({ page: 2 }))
  })
})
