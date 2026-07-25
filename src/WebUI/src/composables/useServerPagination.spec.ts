import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import { computed, nextTick, ref } from 'vue'
import { useServerPagination } from './useServerPagination'
import type { PagedResponse, PaginationParams } from 'src/types/pagination'

interface Row {
  id: string
}

/**
 * fetchFn taklidi — çağrıldığı parametreleri kaydeder, sabit bir sayfa döner.
 * P, composable'ın fetchFn'e geçirdiği parametre tipi (filtreler + sayfalama).
 */
function makeFetch<P extends object = PaginationParams>(
  overrides: Partial<PagedResponse<Row>> = {},
) {
  const calls: Record<string, unknown>[] = []
  const fetchFn = vi.fn(async (params: P) => {
    calls.push(params as Record<string, unknown>)
    return {
      data: {
        items: [{ id: 'a' }, { id: 'b' }],
        totalCount: 42,
        page: 1,
        pageSize: 20,
        totalPages: 3,
        hasNextPage: true,
        hasPreviousPage: false,
        ...overrides,
      } as PagedResponse<Row>,
    }
  })
  return { fetchFn, calls }
}

describe('useServerPagination', () => {
  beforeEach(() => vi.useFakeTimers())
  afterEach(() => vi.useRealTimers())

  it('ilk yüklemede sayfa ve boyutu gönderir, satırları ve toplam sayıyı doldurur', async () => {
    // Arrange
    const { fetchFn, calls } = makeFetch()
    const { rows, pagination, load } = useServerPagination<Row>({ fetchFn })

    // Act
    await load()

    // Assert
    expect(calls[0]).toMatchObject({ page: 1, pageSize: 20 })
    expect(rows.value).toHaveLength(2)
    expect(pagination.value.rowsNumber).toBe(42)
  })

  it('sortBy tanımlı değilse sıralama parametrelerini hiç göndermez', async () => {
    // Arrange — defaultSortBy verilmedi
    const { fetchFn, calls } = makeFetch()
    const { load } = useServerPagination<Row>({ fetchFn })

    // Act
    await load()

    // Assert — sunucu tarafı varsayılan sıralamasına düşmeli
    expect(calls[0]).not.toHaveProperty('sortBy')
    expect(calls[0]).not.toHaveProperty('descending')
  })

  it('defaultSortBy verildiğinde sıralama parametrelerini gönderir', async () => {
    // Arrange
    const { fetchFn, calls } = makeFetch()
    const { load } = useServerPagination<Row>({
      fetchFn,
      defaultSortBy: 'name',
      defaultDescending: true,
    })

    // Act
    await load()

    // Assert
    expect(calls[0]).toMatchObject({ sortBy: 'name', descending: true })
  })

  it('q-table @request olayındaki sayfa ve sıralamayı bir sonraki isteğe yansıtır', async () => {
    // Arrange
    const { fetchFn, calls } = makeFetch({ page: 3 })
    const { onRequest } = useServerPagination<Row>({ fetchFn })

    // Act — kullanıcı 3. sayfaya geçip "name" sütununa azalan sıralama uyguluyor
    onRequest({ pagination: { page: 3, rowsPerPage: 50, sortBy: 'name', descending: true } })
    await vi.runAllTimersAsync()

    // Assert
    expect(calls[0]).toMatchObject({ page: 3, pageSize: 50, sortBy: 'name', descending: true })
  })

  it('arama debounce süresi dolmadan istek atmaz, dolunca sayfayı 1e döndürür', async () => {
    // Arrange — kullanıcı 3. sayfada
    const { fetchFn, calls } = makeFetch()
    const { onRequest, onSearch } = useServerPagination<Row>({ fetchFn })
    onRequest({ pagination: { page: 3, rowsPerPage: 20, sortBy: null, descending: false } })
    await vi.runAllTimersAsync()
    const callsBeforeSearch = calls.length

    // Act — arama yazılıyor, debounce henüz dolmadı
    onSearch('akdeniz')
    await vi.advanceTimersByTimeAsync(399)

    // Assert — henüz istek yok
    expect(fetchFn).toHaveBeenCalledTimes(callsBeforeSearch)

    // Act — debounce doluyor
    await vi.advanceTimersByTimeAsync(1)

    // Assert — arama terimi gitti ve sayfa başa döndü
    expect(calls.at(-1)).toMatchObject({ page: 1, search: 'akdeniz' })
  })

  it('arka arkaya yazılan aramada yalnız son terim için istek atar', async () => {
    // Arrange
    const { fetchFn, calls } = makeFetch()
    const { onSearch } = useServerPagination<Row>({ fetchFn })

    // Act — hızlı yazım
    onSearch('a')
    await vi.advanceTimersByTimeAsync(100)
    onSearch('ak')
    await vi.advanceTimersByTimeAsync(100)
    onSearch('akd')
    await vi.runAllTimersAsync()

    // Assert
    expect(fetchFn).toHaveBeenCalledTimes(1)
    expect(calls[0]).toMatchObject({ search: 'akd' })
  })

  it('boşluktan ibaret arama terimini parametre olarak göndermez', async () => {
    // Arrange
    const { fetchFn, calls } = makeFetch()
    const { onSearch } = useServerPagination<Row>({ fetchFn })

    // Act
    onSearch('   ')
    await vi.runAllTimersAsync()

    // Assert
    expect(calls[0]).not.toHaveProperty('search')
  })

  it('filtre değişince sayfayı 1e döndürüp yeni filtreyle yeniden yükler', async () => {
    // Arrange — kullanıcı 3. sayfada, status filtresi Active
    const status = ref('Active')
    const filters = computed(() => ({ status: status.value }))
    const { fetchFn, calls } = makeFetch<{ status: string } & PaginationParams>()
    const { onRequest } = useServerPagination<Row, { status: string }>({ fetchFn, filters })
    onRequest({ pagination: { page: 3, rowsPerPage: 20, sortBy: null, descending: false } })
    await vi.runAllTimersAsync()

    // Act — filtre değişiyor
    status.value = 'Closed'
    await nextTick()
    await vi.runAllTimersAsync()

    // Assert — 3. sayfada kalıp boş liste göstermemeli
    expect(calls.at(-1)).toMatchObject({ page: 1, status: 'Closed' })
  })

  it('istek başarısız olsa da loading bayrağını indirir', async () => {
    // Arrange
    const fetchFn = vi.fn(async () => {
      throw new Error('network')
    })
    const { loading, load } = useServerPagination<Row>({
      fetchFn: fetchFn as unknown as Parameters<typeof useServerPagination<Row>>[0]['fetchFn'],
    })

    // Act
    await expect(load()).rejects.toThrow('network')

    // Assert — hata sonrası tablo sonsuza kadar "yükleniyor" kalmamalı
    expect(loading.value).toBe(false)
  })
})
