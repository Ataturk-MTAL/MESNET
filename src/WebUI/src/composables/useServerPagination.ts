import { ref, watch, onUnmounted, getCurrentInstance, type Ref, type ComputedRef } from 'vue'
import type { QTableProps } from 'quasar'
import type { PagedResponse, PaginationParams } from 'src/types/pagination'

type QTablePagination = NonNullable<QTableProps['pagination']>

export interface UseServerPaginationOptions<T, F extends Record<string, unknown> = Record<string, unknown>> {
  /** API çağrısı — filter + pagination parametreleri alır, PagedResponse döner. */
  fetchFn: (params: F & PaginationParams) => Promise<{ data: PagedResponse<T> }>
  /** Reaktif filtre nesnesi (status, sector vb.). Değişince sayfa 1'e döner ve yeniden yükler. */
  filters?: ComputedRef<F> | Ref<F>
  /** Varsayılan sıralama alanı. */
  defaultSortBy?: string
  /** Varsayılan sıralama yönü. */
  defaultDescending?: boolean
  /** Varsayılan sayfa boyutu. */
  defaultPageSize?: number
}

export function useServerPagination<T, F extends Record<string, unknown> = Record<string, unknown>>(
  options: UseServerPaginationOptions<T, F>,
) {
  const {
    fetchFn,
    filters,
    defaultSortBy = '',
    defaultDescending = false,
    defaultPageSize = 20,
  } = options

  const rows = ref<T[]>([]) as Ref<T[]>
  const loading = ref(false)
  const search = ref('')

  const pagination = ref<QTablePagination>({
    page: 1,
    rowsPerPage: defaultPageSize,
    rowsNumber: 0,
    sortBy: defaultSortBy || null,
    descending: defaultDescending,
  })

  let debounceTimer: ReturnType<typeof setTimeout> | null = null

  // Sıra dışı yanıt koruması. Hızlı sayfa tıklaması, filtre değişiminin hemen ardından
  // sayfa değişimi ya da debounce dolmadan yeni arama — birden fazla istek uçuşta olur ve
  // eski/yavaş olanı sonra dönerse yeni sonucun üstüne bayat veri yazar. Her istek bir sıra
  // numarası alır; yalnız en son başlatılan yazma hakkına sahiptir.
  let latestRequestId = 0

  async function load() {
    const requestId = ++latestRequestId
    loading.value = true
    try {
      const p = pagination.value
      const filterValues = (filters?.value ?? {}) as F
      const params: F & PaginationParams = {
        ...filterValues,
        page: p.page,
        pageSize: p.rowsPerPage,
        ...(p.sortBy ? { sortBy: p.sortBy, descending: p.descending ?? false } : {}),
        ...(search.value.trim() ? { search: search.value.trim() } : {}),
      }
      const { data } = await fetchFn(params)
      if (requestId !== latestRequestId) return // daha yeni bir istek başladı, bu sonuç bayat
      rows.value = data.items
      pagination.value = {
        ...p,
        page: data.page,
        rowsPerPage: data.pageSize,
        rowsNumber: data.totalCount,
        sortBy: p.sortBy,
        descending: p.descending,
      }
    } finally {
      // Yükleniyor göstergesi yalnız en son istek bitince kapanır; yoksa erken dönen bayat
      // istek, hâlâ süren yeni isteğin göstergesini söndürür.
      if (requestId === latestRequestId) loading.value = false
    }
  }

  /**
   * Quasar q-table @request event handler.
   * Tablo sayfa değiştirme, sıralama tıklaması vb. tetikler.
   */
  function onRequest(props: { pagination: QTablePagination }) {
    const { page, rowsPerPage, sortBy, descending } = props.pagination
    pagination.value.page = page ?? 1
    pagination.value.rowsPerPage = rowsPerPage ?? defaultPageSize
    pagination.value.sortBy = sortBy ?? null
    pagination.value.descending = descending ?? false
    load()
  }

  /** Arama terimi değiştiğinde debounce ile çağrılır. */
  function onSearch(term: string) {
    search.value = term
    if (debounceTimer) clearTimeout(debounceTimer)
    debounceTimer = setTimeout(() => {
      pagination.value.page = 1
      load()
    }, 400)
  }

  // Bekleyen debounce zamanlayıcısı bileşen sökülürken iptal edilir: kullanıcı yazıp
  // 400 ms dolmadan başka sayfaya geçerse, zamanlayıcı artık var olmayan bir bileşenin
  // ref'leri üzerinde load() çağırırdı. getCurrentInstance kontrolü, composable'ın
  // birim testlerinde bileşen dışında da çağrılabilmesi içindir.
  if (getCurrentInstance()) {
    onUnmounted(() => {
      if (debounceTimer) clearTimeout(debounceTimer)
    })
  }

  // Filtreler değiştiğinde sayfa 1'e dön ve yeniden yükle
  if (filters) {
    watch(filters, () => {
      pagination.value.page = 1
      load()
    }, { deep: true })
  }

  return {
    rows,
    loading,
    search,
    pagination,
    load,
    onRequest,
    onSearch,
  }
}
