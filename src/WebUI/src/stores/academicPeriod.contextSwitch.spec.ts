import { describe, it, expect, vi, beforeEach } from 'vitest'
import { setActivePinia, createPinia } from 'pinia'
import type { AcademicPeriodDto } from 'src/api/institution'

/**
 * SESSİZ YANLIŞ-OKULA-YAZMA TUZAĞININ KİLİDİ.
 *
 * <p>`loadPeriods()` dönem listesini `authStore.user?.institutionId` ile çekiyor ve hangi
 * kurum için çektiğini HATIRLAMIYOR. Bağlam değişip liste yenilenmezse ekranda A okulunun
 * dönemi seçili kalır ve B okuluna <b>A okulunun dönem kimliğiyle</b> yazılır. Sonuç hata
 * değil; sessizce yanlış döneme düşmüş bir kayıt.</p>
 *
 * <p>`institutionStore` bu tuzağı öngörmüş ve `loadedInstitutionId` alanıyla kapatmış;
 * yorumu "kiracı değişirse bayrak hâlâ true'dur, eski okulun adı ve alanları ekranda kalır"
 * diyor. Aynı koruma buraya da gelir.</p>
 */

const listAcademicPeriods = vi.fn()

vi.mock('src/api/institution', () => ({
  institutionApi: {
    listAcademicPeriods: (...args: unknown[]) => listAcademicPeriods(...args),
  },
}))

// authStore'un tamamı taklit edilir: bu testin konusu dönem store'unun DAVRANIŞI, kimlik
// katmanı değil. `currentInstitutionId` testten değiştirilebilir olmalı.
let currentInstitutionId: string | null = 'okul-a'

vi.mock('./auth', () => ({
  useAuthStore: () => ({
    get currentInstitutionId() {
      return currentInstitutionId
    },
  }),
}))

function donem(id: string): AcademicPeriodDto {
  return { id, status: 'Active' } as AcademicPeriodDto
}

describe('academicPeriodStore — bağlam değişimi', () => {
  beforeEach(async () => {
    setActivePinia(createPinia())
    currentInstitutionId = 'okul-a'
    listAcademicPeriods.mockReset()
    listAcademicPeriods.mockImplementation((institutionId: string) =>
      Promise.resolve({ data: { items: [donem(`${institutionId}-donem`)] } }),
    )
  })

  it('AYNI kurum için ikinci yükleme isteği sunucuya GİTMEZ', async () => {
    const { useAcademicPeriodStore } = await import('./academicPeriod')
    const store = useAcademicPeriodStore()

    await store.loadPeriods()
    await store.loadPeriods()

    expect(listAcademicPeriods).toHaveBeenCalledTimes(1)
  })

  it('BAŞKA kurum için yeniden yükler — bayat dönem listesi kalmamalı', async () => {
    // Koruma yoksa bu test kırmızı olur ve tuzak açıktır.
    const { useAcademicPeriodStore } = await import('./academicPeriod')
    const store = useAcademicPeriodStore()

    await store.loadPeriods()
    currentInstitutionId = 'okul-b'
    await store.loadPeriods()

    expect(listAcademicPeriods).toHaveBeenCalledTimes(2)
    expect(store.periods[0]?.id).toBe('okul-b-donem')
  })

  it('kurum değişince seçili dönem sıfırlanır', async () => {
    // Eski okulun dönem kimliği seçili kalırsa yazma o kimlikle gider.
    const { useAcademicPeriodStore } = await import('./academicPeriod')
    const store = useAcademicPeriodStore()

    await store.loadPeriods()
    expect(store.selectedPeriodId).toBe('okul-a-donem')

    currentInstitutionId = 'okul-b'
    await store.loadPeriods()

    expect(store.selectedPeriodId).toBe('okul-b-donem')
  })

  it('kurum yoksa istek atılmaz', async () => {
    const { useAcademicPeriodStore } = await import('./academicPeriod')
    const store = useAcademicPeriodStore()
    currentInstitutionId = null

    await store.loadPeriods()

    expect(listAcademicPeriods).not.toHaveBeenCalled()
  })
})
