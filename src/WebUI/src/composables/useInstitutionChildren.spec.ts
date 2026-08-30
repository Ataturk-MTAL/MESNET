import { describe, it, expect, vi, beforeEach } from 'vitest'
import { ref } from 'vue'
import type { InstitutionDto } from 'src/api/institution'

const list = vi.fn()

vi.mock('src/api/institution', () => ({
  institutionApi: {
    list: (...args: unknown[]) => list(...args),
  },
}))

function kurum(id: string, fullName: string): InstitutionDto {
  return { id, fullName } as InstitutionDto
}

describe('useInstitutionChildren', () => {
  beforeEach(() => {
    list.mockReset()
  })

  it('İl düğümü için çocukları İLÇE tipiyle sorgular', async () => {
    list.mockResolvedValue({ data: { items: [kurum('ilce-1', 'Konak İlçe MEM')] } })
    const { useInstitutionChildren } = await import('./useInstitutionChildren')
    const { load, children } = useInstitutionChildren(ref('il-id'), ref('Province'))

    await load()

    expect(list).toHaveBeenCalledWith(
      expect.objectContaining({ parentId: 'il-id', nodeType: 'District' }),
    )
    expect(children.value).toHaveLength(1)
    expect(children.value[0]?.fullName).toBe('Konak İlçe MEM')
  })

  it('İlçe düğümü için çocukları OKUL tipiyle sorgular', async () => {
    list.mockResolvedValue({ data: { items: [kurum('okul-1', 'Atatürk MTAL')] } })
    const { useInstitutionChildren } = await import('./useInstitutionChildren')
    const { load, children } = useInstitutionChildren(ref('ilce-id'), ref('District'))

    await load()

    expect(list).toHaveBeenCalledWith(
      expect.objectContaining({ parentId: 'ilce-id', nodeType: 'School' }),
    )
    expect(children.value[0]?.fullName).toBe('Atatürk MTAL')
  })

  it('Okul düğümünde hiç istek atılmaz — çocuğu yoktur', async () => {
    const { useInstitutionChildren } = await import('./useInstitutionChildren')
    const { load, children } = useInstitutionChildren(ref('okul-id'), ref('School'))

    await load()

    expect(list).not.toHaveBeenCalled()
    expect(children.value).toEqual([])
  })

  it('sunucu hatasında error true olur ve çocuk listesi boşalır', async () => {
    list.mockRejectedValue(new Error('boom'))
    const { useInstitutionChildren } = await import('./useInstitutionChildren')
    const { load, children, error } = useInstitutionChildren(ref('il-id'), ref('Province'))

    await load()

    expect(error.value).toBe(true)
    expect(children.value).toEqual([])
  })

  it('toggleDistrict ilk açılışta okulları çeker, ikinci açılışta İKİNCİ istek ATMAZ (cache)', async () => {
    list.mockResolvedValue({ data: { items: [kurum('okul-1', 'Atatürk MTAL')] } })
    const { useInstitutionChildren } = await import('./useInstitutionChildren')
    const { toggleDistrict, districtSchools, expandedIds } = useInstitutionChildren(
      ref('il-id'),
      ref('Province'),
    )

    await toggleDistrict('ilce-1')
    expect(expandedIds.value['ilce-1']).toBe(true)
    expect(list).toHaveBeenCalledWith(
      expect.objectContaining({ parentId: 'ilce-1', nodeType: 'School' }),
    )
    expect(districtSchools.value['ilce-1']).toHaveLength(1)

    // Kapat → aç: veri zaten cache'te, ikinci sunucu isteği atılmaz.
    await toggleDistrict('ilce-1')
    expect(expandedIds.value['ilce-1']).toBe(false)
    await toggleDistrict('ilce-1')
    expect(expandedIds.value['ilce-1']).toBe(true)
    expect(list).toHaveBeenCalledTimes(1)
  })
})
