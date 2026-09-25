import { describe, it, expect, vi, beforeEach } from 'vitest'
import { ref } from 'vue'
import { setActivePinia, createPinia } from 'pinia'

/**
 * Alan/öğretmen seçimi sayfalar arasında korunur (kullanıcı isteği: her ekran değişiminde
 * yeniden seçmek zorunda kalınıyordu). Ama seçim OKULA aittir: bağlam değişince önceki okulun
 * öğretmen kimliği yeni okulda seçili görünmemeli.
 */

const currentInstitutionId = ref<string | null>('okul-a')

vi.mock('./auth', () => ({
  useAuthStore: () => ({
    get currentInstitutionId() {
      return currentInstitutionId.value
    },
  }),
}))

async function store() {
  const { useSelectionStore } = await import('./selection')
  return useSelectionStore()
}

describe('selectionStore', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
    currentInstitutionId.value = 'okul-a'
  })

  it('seçim store ömrü boyunca korunur', async () => {
    const s = await store()

    s.setBranch('EET')
    s.setTeacher('t1')

    expect(s.branchCode).toBe('EET')
    expect(s.teacherId).toBe('t1')
  })

  it('alan değişince öğretmen sıfırlanır — öğretmen alana bağlıdır', async () => {
    const s = await store()
    s.setBranch('EET')
    s.setTeacher('t1')

    s.setBranch('MTT')

    expect(s.teacherId).toBeNull()
  })

  it('aynı alan yeniden verilirse öğretmen korunur', async () => {
    // Tek alanlı kullanıcıda sayfalar açılışta alanı yeniden atar; öğretmen düşmemeli.
    const s = await store()
    s.setBranch('EET')
    s.setTeacher('t1')

    s.setBranch('EET')

    expect(s.teacherId).toBe('t1')
  })

  it('okul bağlamı değişince seçim görünmez', async () => {
    const s = await store()
    s.setBranch('EET')
    s.setTeacher('t1')

    currentInstitutionId.value = 'okul-b'

    expect(s.branchCode).toBeNull()
    expect(s.teacherId).toBeNull()
  })

  it('başka okulda seçilen öğretmen, alan aynı olsa da taşınmaz', async () => {
    const s = await store()
    s.setBranch('EET')
    s.setTeacher('t1')
    currentInstitutionId.value = 'okul-b'

    s.setBranch('EET')

    expect(s.teacherId).toBeNull()
  })
})
