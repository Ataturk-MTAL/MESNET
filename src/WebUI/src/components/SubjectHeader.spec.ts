import { describe, it, expect } from 'vitest'
import { mount } from '@vue/test-utils'
import SubjectHeader from './SubjectHeader.vue'

const global = { stubs: { 'q-icon': true } }

/**
 * Seçime bağlı veri düzenlenen kartın başlığı: verinin sahibi başlığın kendisidir. Seçici
 * sayfanın tepesinde kalır; kullanıcı ızgarada çalışırken yanlış öğretmenin/alanın verisini
 * değiştirdiğini ancak kayıttan sonra fark ediyordu.
 */
describe('SubjectHeader', () => {
  it('sahibi başlık olarak, kartın konusunu üstünde gösterir', () => {
    const wrapper = mount(SubjectHeader, {
      props: { title: 'Haftalık Program', name: 'Ayşe Çelik', context: 'EET' },
      global,
    })

    expect(wrapper.find('.text-h6').text()).toBe('Ayşe Çelik')
    expect(wrapper.text()).toContain('Haftalık Program')
    expect(wrapper.text()).toContain('EET')
  })

  it('düzenleme modunda uyarı şeridi ve kenar vurgusu çıkar', () => {
    const wrapper = mount(SubjectHeader, {
      props: { title: 'Haftalık Program', name: 'Ayşe Çelik', editing: true },
      global,
    })

    expect(wrapper.find('[role="status"]').text()).toContain('Ayşe Çelik — düzenleniyor')
    expect(wrapper.classes()).toContain('subject-header--editing')
  })

  it('düzenleme yokken şerit çizilmez', () => {
    const wrapper = mount(SubjectHeader, {
      props: { title: 'Haftalık Program', name: 'Ayşe Çelik' },
      global,
    })

    expect(wrapper.find('[role="status"]').exists()).toBe(false)
  })
})
