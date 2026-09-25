import { describe, it, expect, vi, beforeEach } from 'vitest'
import { setActivePinia, createPinia } from 'pinia'

let writable: string[] = ['EET']

vi.mock('stores/auth', () => ({
  useAuthStore: () => ({
    currentInstitutionId: 'okul-a',
    canWriteBranch: (code: string) => writable.includes(code),
  }),
}))

async function setup(writeContext: boolean) {
  const { useSharedSelection } = await import('./useSharedSelection')
  return useSharedSelection({ writeContext })
}

describe('useSharedSelection', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
    writable = ['EET']
  })

  it('bir sayfada yapılan seçim başka sayfada hazır gelir', async () => {
    const listPage = await setup(false)
    listPage.branchCode.value = 'EET'
    listPage.teacherId.value = 't1'

    const otherPage = await setup(false)

    expect(otherPage.branchCode.value).toBe('EET')
    expect(otherPage.teacherId.value).toBe('t1')
  })

  it('yazma sayfası, kullanıcının yazamadığı alanı seçili getirmez', async () => {
    // Alan şefi öğrenci listesinde başka alanı süzebilir (okuma açık); o seçim dağıtım
    // sayfasına taşınsaydı BranchSelector listelemediği bir alanı gösterir, kayıt 422 ile düşerdi.
    const listPage = await setup(false)
    listPage.branchCode.value = 'MTT'
    listPage.teacherId.value = 't9'

    const writePage = await setup(true)

    expect(writePage.branchCode.value).toBeNull()
    expect(writePage.teacherId.value).toBeNull()
  })

  it('yazma sayfası yazılabilir alanı getirir', async () => {
    const listPage = await setup(false)
    listPage.branchCode.value = 'EET'

    const writePage = await setup(true)

    expect(writePage.branchCode.value).toBe('EET')
  })
})
