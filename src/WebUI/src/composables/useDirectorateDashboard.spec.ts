import { describe, it, expect, vi } from 'vitest'
import { useDirectorateDashboard } from './useDirectorateDashboard'

function makeNotify() {
  return {
    success: vi.fn(),
    error: vi.fn(),
    apiError: vi.fn(),
    warning: vi.fn(),
    info: vi.fn(),
  }
}

const okDistricts = () => Promise.resolve(12)
const okSchools = () => Promise.resolve(148)
const okUnmanaged = () =>
  Promise.resolve({ total: 3, names: ['Atatürk MTAL', 'Gazi MTAL', 'Cumhuriyet MTAL'] })
const okStuck = () =>
  Promise.resolve({
    totalCount: 7,
    thresholdDays: 14,
    byInstitution: [
      { institutionId: 'a', institutionName: null, count: 5, oldestDays: 40 },
      { institutionId: 'b', institutionName: null, count: 2, oldestDays: null },
    ],
  })

describe('useDirectorateDashboard', () => {
  it('üç kart da dolduğunda değerleri yayar', async () => {
    // Arrange
    const dash = useDirectorateDashboard({
      fetchDistrictCount: okDistricts,
      fetchSchoolCount: okSchools,
      fetchUnmanaged: okUnmanaged,
      fetchStuck: okStuck,
      notify: makeNotify(),
    })

    // Act
    await dash.load()

    // Assert
    expect(dash.districtCount.value).toBe(12)
    expect(dash.schoolCount.value).toBe(148)
    expect(dash.unmanagedCount.value).toBe(3)
    expect(dash.unmanagedNames.value).toEqual([
      'Atatürk MTAL',
      'Gazi MTAL',
      'Cumhuriyet MTAL',
    ])
    expect(dash.stuckCount.value).toBe(7)
    expect(dash.stuckThresholdDays.value).toBe(14)
    expect(dash.loading.value).toBe(false)
    expect(dash.districtFailed.value).toBe(false)
    expect(dash.schoolFailed.value).toBe(false)
    expect(dash.unmanagedFailed.value).toBe(false)
    expect(dash.stuckFailed.value).toBe(false)
  })

  /**
   * Bir kartın verisi gelmezse pano TÜMDEN boşalmamalı. Aksi hâlde tek bir uç arızası üç
   * kartı birden söndürür ve kullanıcı hiçbir şey göremez.
   */
  it('bir çağrı patlarsa diğer kartlar yine dolar', async () => {
    const notify = makeNotify()
    const dash = useDirectorateDashboard({
      fetchDistrictCount: okDistricts,
      fetchSchoolCount: okSchools,
      fetchUnmanaged: okUnmanaged,
      fetchStuck: () => Promise.reject(new Error('403')),
      notify,
    })

    await dash.load()

    expect(dash.districtCount.value).toBe(12)
    expect(dash.unmanagedCount.value).toBe(3)
    expect(dash.stuckCount.value).toBe(0)
    expect(notify.apiError).toHaveBeenCalled()
  })

  /**
   * "Veri yok" (ör. bekleyen onay yok) ile "veri hiç yüklenemedi" ayrımı yalnız toast'a
   * bağlıysa kart bunu göremez — bayrak kaynak başınadır ve BAŞKA kaynağı kirletmemelidir.
   */
  it('fetchStuck patlarsa yalnız stuckFailed set edilir, diğerleri false kalır', async () => {
    const dash = useDirectorateDashboard({
      fetchDistrictCount: okDistricts,
      fetchSchoolCount: okSchools,
      fetchUnmanaged: okUnmanaged,
      fetchStuck: () => Promise.reject(new Error('403')),
      notify: makeNotify(),
    })

    await dash.load()

    expect(dash.stuckFailed.value).toBe(true)
    expect(dash.districtFailed.value).toBe(false)
    expect(dash.schoolFailed.value).toBe(false)
    expect(dash.unmanagedFailed.value).toBe(false)
  })

  it('fetchUnmanaged patlarsa yalnız unmanagedFailed set edilir, diğerleri false kalır', async () => {
    const dash = useDirectorateDashboard({
      fetchDistrictCount: okDistricts,
      fetchSchoolCount: okSchools,
      fetchUnmanaged: () => Promise.reject(new Error('500')),
      fetchStuck: okStuck,
      notify: makeNotify(),
    })

    await dash.load()

    expect(dash.unmanagedFailed.value).toBe(true)
    expect(dash.districtFailed.value).toBe(false)
    expect(dash.schoolFailed.value).toBe(false)
    expect(dash.stuckFailed.value).toBe(false)
  })

  it('bütün çağrılar patlarsa dördü de kendi bayrağını set eder', async () => {
    const dash = useDirectorateDashboard({
      fetchDistrictCount: () => Promise.reject(new Error('500')),
      fetchSchoolCount: () => Promise.reject(new Error('500')),
      fetchUnmanaged: () => Promise.reject(new Error('500')),
      fetchStuck: () => Promise.reject(new Error('500')),
      notify: makeNotify(),
    })

    await dash.load()

    expect(dash.districtFailed.value).toBe(true)
    expect(dash.schoolFailed.value).toBe(true)
    expect(dash.unmanagedFailed.value).toBe(true)
    expect(dash.stuckFailed.value).toBe(true)
  })

  it('yükleme bittiğinde loading kapanır — hata olsa bile', async () => {
    const dash = useDirectorateDashboard({
      fetchDistrictCount: () => Promise.reject(new Error('500')),
      fetchSchoolCount: () => Promise.reject(new Error('500')),
      fetchUnmanaged: () => Promise.reject(new Error('500')),
      fetchStuck: () => Promise.reject(new Error('500')),
      notify: makeNotify(),
    })

    await dash.load()

    expect(dash.loading.value).toBe(false)
  })
})
