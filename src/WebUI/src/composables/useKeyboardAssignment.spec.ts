import { describe, it, expect } from 'vitest'
import { useKeyboardAssignment, dayLabel } from './useKeyboardAssignment'

const CARD = { businessId: 'b1', businessName: 'Mezitli Elektrik' }
const GRID_ITEM = { ...CARD, fromDay: 'Monday', fromPeriod: 3 }

describe('useKeyboardAssignment', () => {
  it('başlangıçta seçim yok ve duyuru boş', () => {
    const kb = useKeyboardAssignment()

    expect(kb.selected.value).toBeNull()
    expect(kb.hasSelection.value).toBe(false)
    expect(kb.announcement.value).toBe('')
  })

  it('atanmamış kart seçilince duyuru atama yönergesi verir', () => {
    const kb = useKeyboardAssignment()

    kb.select(CARD)

    expect(kb.hasSelection.value).toBe(true)
    expect(kb.announcement.value).toContain('Mezitli Elektrik seçildi')
    expect(kb.announcement.value).toContain('Enter ile atayın')
  })

  it('ızgaradan seçilince duyuru kaynak gün ve saati Türkçe söyler', () => {
    const kb = useKeyboardAssignment()

    kb.select(GRID_ITEM)

    expect(kb.announcement.value).toContain('Pazartesi 3. ders')
    expect(kb.announcement.value).toContain('Enter ile taşıyın')
  })

  it('aynı öğeye tekrar basınca seçim kalkar', () => {
    const kb = useKeyboardAssignment()

    kb.toggle(CARD)
    expect(kb.hasSelection.value).toBe(true)

    kb.toggle(CARD)

    expect(kb.hasSelection.value).toBe(false)
    expect(kb.announcement.value).toContain('seçimi iptal edildi')
  })

  it('farklı öğeye basınca seçim yeni öğeye geçer', () => {
    const kb = useKeyboardAssignment()

    kb.toggle(CARD)
    kb.toggle({ businessId: 'b2', businessName: 'Akdeniz Enerji' })

    expect(kb.selected.value?.businessId).toBe('b2')
  })

  it('aynı işletmenin ızgaradaki kopyası ayrı öğe sayılır', () => {
    const kb = useKeyboardAssignment()

    kb.toggle(CARD)
    kb.toggle(GRID_ITEM)

    // Aynı businessId ama kaynak konum farklı — seçim kalkmamalı, ızgaradaki öğeye geçmeli
    expect(kb.hasSelection.value).toBe(true)
    expect(kb.selected.value?.fromDay).toBe('Monday')
  })

  it('aynı gün farklı ders saatindeki öğe ayrı sayılır', () => {
    const kb = useKeyboardAssignment()

    kb.toggle(GRID_ITEM)                                   // Pazartesi 3. ders
    kb.toggle({ ...GRID_ITEM, fromPeriod: 5 })             // Pazartesi 5. ders

    // Yalnız ders saati farklı — seçim kalkmamalı, 5. derse geçmeli
    expect(kb.hasSelection.value).toBe(true)
    expect(kb.selected.value?.fromPeriod).toBe(5)
  })

  it('bırakma seçimi temizler ve hedefi duyurur', () => {
    const kb = useKeyboardAssignment()
    kb.select(CARD)

    kb.completeDrop('Wednesday', 5)

    expect(kb.selected.value).toBeNull()
    expect(kb.announcement.value).toBe('Mezitli Elektrik, Çarşamba 5. derse atandı.')
  })

  it('seçim yokken iptal duyuru üretmez', () => {
    const kb = useKeyboardAssignment()

    kb.cancel()

    expect(kb.announcement.value).toBe('')
  })

  it('seçim yokken bırakma duyuru üretmez', () => {
    const kb = useKeyboardAssignment()

    kb.completeDrop('Friday', 1)

    expect(kb.announcement.value).toBe('')
  })

  it('gün adları Türkçe karşılıklarıyla döner, bilinmeyen değer olduğu gibi kalır', () => {
    expect(dayLabel('Tuesday')).toBe('Salı')
    expect(dayLabel('Thursday')).toBe('Perşembe')
    expect(dayLabel('Saturday')).toBe('Saturday')
  })
})
