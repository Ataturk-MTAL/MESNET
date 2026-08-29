import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import { computed } from 'vue'
import { useServerPagination } from 'src/composables/useServerPagination'
import type { PagedResponse } from 'src/types/pagination'
import type { AuditEntryDto } from 'src/api/audit'
import {
  DEFAULT_SORT_BY,
  DEFAULT_DESCENDING,
  DEFAULT_SCOPE,
  buildAuditListFilters,
  resolveAuditEndpoint,
  type AuditScope,
  type AuditApiClient,
} from './auditListQuery'

/**
 * Denetim listesinin SUNUCU SÖZLEŞMESİ.
 *
 * <p>Test bileşeni monte etmez, sayfanın sunucuya ne sorduğunu ölçer. Kırılgan olan kısım
 * şablon değil sözleşmedir: sıralama yönü gitmezse liste en ESKİ işlemle açılır ve "az önce
 * ne oldu" sorusu cevapsız kalır; boş `outcome` süzgeci gönderilirse liste sessizce boşalır.</p>
 *
 * <p>Değerler burada YENİDEN YAZILMAZ — `./auditListQuery`'den import edilir ve sayfa da AYNI
 * dosyayı kullanır. Bu depoda ölçülmüş sahte-yeşil kalıbının kapatılma biçimi budur.</p>
 *
 * <p><b>`useServerPagination` gerçek imzası:</b> `onSearch(term)` ve `onRequest(props)`
 * SENKRONDUR ve `load()`'u fire-and-forget tetikler — Promise DÖNDÜRMEZ. Bu yüzden `await`
 * ile değil `vi.useFakeTimers()` + `vi.runAllTimersAsync()` ile doğrulanır. Composable
 * BURADA DEĞİŞTİRİLMEZ.</p>
 */
describe('AuditLogPage — sunucu sözleşmesi', () => {
  const bosSayfa: PagedResponse<AuditEntryDto> = {
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
  ) => Promise<{ data: PagedResponse<AuditEntryDto> }>

  let fetchFn: ReturnType<typeof vi.fn<FetchFn>>

  const kur = (
    scope: AuditScope = 'mine',
    outcome: string | null = null,
    crossedOnly = false,
  ) =>
    useServerPagination<AuditEntryDto>({
      fetchFn,
      filters: computed(() => buildAuditListFilters(scope, outcome, crossedOnly)),
      defaultSortBy: DEFAULT_SORT_BY,
      defaultDescending: DEFAULT_DESCENDING,
    })

  beforeEach(() => {
    vi.useFakeTimers()
    fetchFn = vi.fn(async () => ({ data: bosSayfa }))
  })

  afterEach(() => {
    vi.useRealTimers()
  })

  it('varsayılan kapsam İŞLEMLERİM — açılışta izin duvarına çarpılmamalı', () => {
    expect(DEFAULT_SCOPE).toBe('mine')
  })

  it('varsayılan sıralama tarihe göre AZALANDIR — en yeni işlem üstte', async () => {
    const { load } = kur()

    await load()

    expect(fetchFn).toHaveBeenCalledWith(
      expect.objectContaining({ sortBy: 'occurredAt', descending: true }),
    )
  })

  it('sonuç süzgeci sunucuya gider', async () => {
    const { load } = kur('mine', 'Rejected')

    await load()

    expect(fetchFn).toHaveBeenCalledWith(expect.objectContaining({ outcome: 'Rejected' }))
  })

  it('boş sonuç süzgeci GÖNDERİLMEZ — gönderilse liste sessizce boşalırdı', async () => {
    const { load } = kur('mine', null)

    await load()

    const params = fetchFn.mock.calls[0]![0]
    expect(params).not.toHaveProperty('outcome')
  })

  it('kurum sınırı süzgeci KURUM kapsamında açıkken gider', async () => {
    const { load } = kur('institution', null, true)

    await load()

    expect(fetchFn).toHaveBeenCalledWith(
      expect.objectContaining({ crossedTenantBoundary: true }),
    )
  })

  it(
    'kurum sınırı süzgeci İŞLEMLERİM kapsamında GÖNDERİLMEZ — ' +
      '`GetMine` ucu bu parametreyi almıyor, göndermek anahtarın açık görünüp hiçbir şey ' +
      'yapmadığı bir yalan üretirdi',
    async () => {
      const { load } = kur('mine', null, true)

      await load()

      const params = fetchFn.mock.calls[0]![0]
      expect(params).not.toHaveProperty('crossedTenantBoundary')
    },
  )

  it('arama terimi sunucuya gider — istemci tarafında süzülmez', async () => {
    const { onSearch } = kur()

    onSearch('Ayşe')
    await vi.runAllTimersAsync()

    expect(fetchFn).toHaveBeenCalledWith(expect.objectContaining({ search: 'Ayşe' }))
  })

  it('sayfa isteği sunucuya gider', async () => {
    const { onRequest } = kur()

    onRequest({ pagination: { page: 4, rowsPerPage: 20, sortBy: 'occurredAt', descending: true } })
    await vi.runAllTimersAsync()

    expect(fetchFn).toHaveBeenCalledWith(expect.objectContaining({ page: 4 }))
  })
})

/**
 * Madde 5 düzeltmesi: `AuditLogPage`'in kapsam→uç eşlemesi eskiden sayfa içinde satır içi bir
 * üçlü operatördü ve yukarıdaki testler `fetchFn`'i doğrudan mock'ladığı için bu satırı HİÇ
 * ÇALIŞTIRMIYORDU — eşleme ters çevrilse (institution ↔ mine) bile 8/8 yeşil kalırdı. Eşleme
 * `resolveAuditEndpoint`'e (`./auditListQuery`) taşındı; sayfa VE bu test AYNI fonksiyonu
 * çağırır. Aşağıdaki testler o fonksiyonu DOĞRUDAN çağırır — `useServerPagination`'ı
 * devreye SOKMAZ.
 */
describe('resolveAuditEndpoint — kapsam → uç eşlemesi', () => {
  const kurSahteApi = (): AuditApiClient => ({
    listMine: vi.fn() as unknown as AuditApiClient['listMine'],
    listForInstitution: vi.fn() as unknown as AuditApiClient['listForInstitution'],
  })

  it('institution kapsamında listForInstitution seçilir', () => {
    const api = kurSahteApi()

    const secilen = resolveAuditEndpoint('institution', api)

    expect(secilen).toBe(api.listForInstitution)
  })

  it('mine kapsamında listMine seçilir', () => {
    const api = kurSahteApi()

    const secilen = resolveAuditEndpoint('mine', api)

    expect(secilen).toBe(api.listMine)
  })
})
