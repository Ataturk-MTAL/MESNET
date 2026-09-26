/* eslint-disable vue/one-component-per-file --
   Bu bir test dosyası; aşağıdaki bileşenler Quasar stub'ıdır, üretim bileşeni değildir. */
import { describe, it, expect } from 'vitest'
import { defineComponent, h } from 'vue'
import { mount } from '@vue/test-utils'
import AppTable from './AppTable.vue'

/**
 * Boş liste ile yüklenemeyen liste ayrı görünmeli.
 *
 * NEDEN VAR: 403/500 dönen liste eskiden "Kayıt bulunamadı" gösteriyordu — kullanıcı verinin
 * olmadığını sanıyordu. Hata `useServerPagination.error`'dan AppTable'a `error` prop'u ile gelir.
 */
const QTableStub = defineComponent({
  name: 'QTable',
  setup(_, { slots }) {
    return () => h('div', slots['no-data']?.())
  },
})

const QBtnStub = defineComponent({
  name: 'QBtn',
  props: { label: { type: String, default: '' } },
  emits: ['click'],
  setup(props, { emit }) {
    return () => h('button', { onClick: () => emit('click') }, props.label)
  },
})

function mountTable(props: Record<string, unknown>) {
  return mount(AppTable, {
    props: { rows: [], columns: [], ...props },
    global: {
      stubs: { 'q-table': QTableStub, 'q-btn': QBtnStub, 'q-icon': true, 'q-inner-loading': true },
    },
  })
}

describe('AppTable boş/hata durumu', () => {
  it('hata yokken boş listede noDataLabel gösterir', () => {
    const wrapper = mountTable({})

    expect(wrapper.text()).toContain('Kayıt bulunamadı')
    expect(wrapper.find('[role="alert"]').exists()).toBe(false)
  })

  it('hata varsa "Kayıt bulunamadı" yerine hata iletisi gösterir', () => {
    const wrapper = mountTable({ error: new Error('403') })

    expect(wrapper.find('[role="alert"]').text()).toContain('Liste yüklenemedi.')
    expect(wrapper.text()).not.toContain('Kayıt bulunamadı')
  })

  it('"Yeniden dene" retry olayı yayar', async () => {
    const wrapper = mountTable({ error: new Error('500') })

    await wrapper.find('button').trigger('click')

    expect(wrapper.emitted('retry')).toHaveLength(1)
  })
})
