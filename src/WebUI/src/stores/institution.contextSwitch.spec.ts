import { describe, it, expect, vi, beforeEach } from 'vitest'
import { setActivePinia, createPinia } from 'pinia'
import type { InstitutionDto } from 'src/api/institution'

/**
 * SESSİZ YANLIŞ-OKULA-YAZMA TUZAĞININ İKİNCİ KOPYASININ KİLİDİ (Görev 10 ön düzeltmesi).
 *
 * <p>`institutionStore`'un yerel `currentInstitutionId()` yardımcısı `useAuthStore().user?.
 * institutionId` (EV kurumu) okuyordu, Görev 8'in ürettiği `authStore.currentInstitutionId`
 * (aktif bağlam varsa o, yoksa ev kurumu) DEĞİL. Sonuç: bağlam değiştirildikten sonra
 * `useInstitutionContext.switchTo()` `institutionStore.clear()` çağırıyor ama bir sonraki
 * `loadInstitution()` yine EV kurumunun verisini çekiyordu — kurum profili sayfası sessizce
 * YANLIŞ okulu gösteriyordu. `academicPeriodStore.loadPeriods()`'ta Görev 9'un kapattığı
 * tuzağın (bkz. `academicPeriod.contextSwitch.spec.ts`) birebir aynı deseni — bu dosya da
 * aynı deseni izler.</p>
 */

const get = vi.fn()

vi.mock('src/api/institution', () => ({
  institutionApi: {
    get: (...args: unknown[]) => get(...args),
  },
}))

// authStore'un tamamı taklit edilir: bu testin konusu institution store'unun DAVRANIŞI,
// kimlik katmanı değil. `currentInstitutionId` testten değiştirilebilir olmalı.
let currentInstitutionId: string | null = 'okul-a'

vi.mock('./auth', () => ({
  useAuthStore: () => ({
    get currentInstitutionId() {
      return currentInstitutionId
    },
  }),
}))

function kurum(id: string): InstitutionDto {
  return { id, fullName: `${id}-ad` } as InstitutionDto
}

describe('institutionStore — bağlam değişimi', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
    currentInstitutionId = 'okul-a'
    get.mockReset()
    get.mockImplementation((institutionId: string) =>
      Promise.resolve({ data: kurum(institutionId) }),
    )
  })

  it('loadInstitution AKTİF BAĞLAMDAKİ kurum kimliğiyle çağrılır — ev kurumuyla değil', async () => {
    currentInstitutionId = 'okul-b'
    const { useInstitutionStore } = await import('./institution')
    const store = useInstitutionStore()

    await store.loadInstitution()

    expect(get).toHaveBeenCalledWith('okul-b')
    expect(store.institution?.id).toBe('okul-b')
  })

  it('bağlam değişince yeniden yükler — bayat kurum verisi kalmamalı', async () => {
    const { useInstitutionStore } = await import('./institution')
    const store = useInstitutionStore()

    await store.loadInstitution()
    expect(store.institution?.id).toBe('okul-a')

    currentInstitutionId = 'okul-b'
    await store.loadInstitution()

    expect(get).toHaveBeenCalledTimes(2)
    expect(store.institution?.id).toBe('okul-b')
  })

  it('kurum yoksa istek atılmaz', async () => {
    const { useInstitutionStore } = await import('./institution')
    const store = useInstitutionStore()
    currentInstitutionId = null

    await store.loadInstitution()

    expect(get).not.toHaveBeenCalled()
  })
})
