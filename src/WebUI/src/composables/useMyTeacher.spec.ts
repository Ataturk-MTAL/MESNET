import { describe, it, expect, vi, beforeEach } from 'vitest'

const getMyTeacher = vi.fn()

vi.mock('src/api/enrollment', () => ({
  enrollmentApi: { getMyTeacher: () => getMyTeacher() },
}))

/** axios hatasının `response.status` biçimi — composable yalnız ona bakar. */
function httpError(status: number) {
  return Object.assign(new Error(`HTTP ${status}`), { response: { status } })
}

describe('useMyTeacher', () => {
  beforeEach(() => {
    getMyTeacher.mockReset()
  })

  it('öğretmen kaydını yükler', async () => {
    getMyTeacher.mockResolvedValue({ data: { id: 't1', fullName: 'Ayşe Kaya' } })
    const { useMyTeacher } = await import('./useMyTeacher')
    const me = useMyTeacher()

    await me.load()

    expect(me.teacher.value?.id).toBe('t1')
    expect(me.loaded.value).toBe(true)
  })

  it('kaydı olmayan kullanıcıda (404) hata değil, boş sonuç verir', async () => {
    getMyTeacher.mockImplementation(() => Promise.reject(httpError(404)))
    const { useMyTeacher } = await import('./useMyTeacher')
    const me = useMyTeacher()

    await me.load()

    expect(me.teacher.value).toBeNull()
    expect(me.loaded.value).toBe(true)
  })

  it('404 dışındaki hatayı yutmaz', async () => {
    getMyTeacher.mockImplementation(() => Promise.reject(httpError(500)))
    const { useMyTeacher } = await import('./useMyTeacher')
    const me = useMyTeacher()

    await expect(me.load()).rejects.toThrow('HTTP 500')
    expect(me.loaded.value).toBe(false)
  })
})
