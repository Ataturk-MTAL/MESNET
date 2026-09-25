import { describe, it, expect } from 'vitest'
import { mount } from '@vue/test-utils'
import EditingSubject from './EditingSubject.vue'

const global = { stubs: { 'q-icon': true, 'q-tooltip': true } }

/**
 * "Bu veri kimin?" etiketi. Düzenleme sırasında seçici ekranın tepesinde kalır; kullanıcı
 * ızgarada çalışırken yanlış öğretmenin/alanın verisini değiştirdiğini fark etmez.
 */
describe('EditingSubject', () => {
  it('sahibin adını ve türünü gösterir', () => {
    const wrapper = mount(EditingSubject, { props: { name: 'Ayşe Çelik', context: 'EET' }, global })

    expect(wrapper.text()).toContain('Öğretmen:')
    expect(wrapper.text()).toContain('Ayşe Çelik')
    expect(wrapper.text()).toContain('EET')
  })

  it('düzenleme modunda uyarı tonuna geçer ve "Düzenleniyor" der', () => {
    const wrapper = mount(EditingSubject, { props: { name: 'Ayşe Çelik', editing: true }, global })

    expect(wrapper.text()).toContain('Düzenleniyor:')
    expect(wrapper.classes()).toContain('bg-warning-soft')
  })

  it('seçim yokken hiçbir şey çizmez', () => {
    const wrapper = mount(EditingSubject, { props: { name: null }, global })

    expect(wrapper.find('[role="status"]').exists()).toBe(false)
  })
})
