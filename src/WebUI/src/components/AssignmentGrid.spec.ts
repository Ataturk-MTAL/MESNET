import { describe, it, expect, vi } from 'vitest'
import { mount } from '@vue/test-utils'
import { nextTick } from 'vue'
import AssignmentGrid from './AssignmentGrid.vue'
import type { DailyScheduleDto } from 'src/api/coordination'

// Quasar bileşenleri bu testte önemli değil; hepsi stub'lanır.
const global = {
  stubs: {
    'q-icon': true,
    'q-tooltip': true,
    'q-btn': true,
    'q-chip': true,
    'q-badge': true,
  },
}

function makeSchedule(courseName: string | null): DailyScheduleDto[] {
  return [
    {
      day: 'Monday',
      periods: [
        // Boş + atanmamış = drop zone (registerDropZone bu hücrede çalışır)
        { periodNumber: 1, status: 'Free', courseName: null, assignedBusinessId: null },
        // Sadece yeniden render tetiklemek için değişen hücre
        { periodNumber: 2, status: 'Occupied', courseName, assignedBusinessId: null },
      ],
    },
  ]
}

/** jsdom'da gerçek DataTransfer yok; sürükleme verisi için asgari taklit. */
function makeDataTransfer(data: Record<string, string>) {
  return {
    getData: (type: string) => data[type] ?? '',
    setData: () => {},
    dropEffect: '',
    effectAllowed: '',
  }
}

describe('AssignmentGrid drop zone listener kaydı', () => {
  it('yeniden render sonrası drop zone üzerine listener yığmaz', async () => {
    const addSpy = vi.spyOn(HTMLElement.prototype, 'addEventListener')

    const wrapper = mount(AssignmentGrid, {
      props: {
        schedule: makeSchedule('Matematik'),
        periodCount: 2,
        disabled: false,
        businessNameMap: {},
        selected: null,
      },
      global,
    })

    const countDragListeners = () =>
      addSpy.mock.calls.filter(
        ([type]) => type === 'dragover' || type === 'dragleave' || type === 'drop',
      ).length

    // 5 gün x 2 ders saati = 10 boş hücre, her biri 3 olay = 30.
    const afterMount = countDragListeners()
    expect(afterMount).toBe(30)

    // Sayfanın gerçek davranışı: her atama/saat düzenlemesi schedule prop'unu yeniler.
    for (let i = 0; i < 5; i++) {
      await wrapper.setProps({ schedule: makeSchedule(`Ders ${i}`) })
      await nextTick()
    }

    // Aynı DOM düğümü, aynı tek drop zone. Yeni listener eklenmemeli.
    expect(countDragListeners()).toBe(afterMount)

    addSpy.mockRestore()
    wrapper.unmount()
  })

  it('bırakma işlemi business-dropped olayını TEK kez yayar', async () => {
    const wrapper = mount(AssignmentGrid, {
      props: {
        schedule: makeSchedule('Matematik'),
        periodCount: 2,
        disabled: false,
        businessNameMap: {},
        selected: null,
      },
      global,
    })

    // Sayfada gerçekte olan: her atama sonrası schedule prop'u yenilenir.
    for (let i = 0; i < 5; i++) {
      await wrapper.setProps({ schedule: makeSchedule(`Ders ${i}`) })
      await nextTick()
    }

    const zone = wrapper.find('.drop-zone')
    expect(zone.exists()).toBe(true)

    await zone.trigger('drop', { dataTransfer: makeDataTransfer({ 'application/business-id': 'isletme-1' }) })

    // Listener yığılsaydı aynı bırakma birden çok kez yayılırdı.
    expect(wrapper.emitted('business-dropped')).toHaveLength(1)
    expect(wrapper.emitted('business-dropped')![0]![0]).toMatchObject({
      businessId: 'isletme-1',
      periodNumber: 1,
    })
  })

  it('grid içi taşımada önce eski konumu kaldırır', async () => {
    const wrapper = mount(AssignmentGrid, {
      props: {
        schedule: makeSchedule('Matematik'),
        periodCount: 2,
        disabled: false,
        businessNameMap: {},
        selected: null,
      },
      global,
    })

    await wrapper.find('.drop-zone').trigger('drop', {
      dataTransfer: makeDataTransfer({
        'application/business-id': 'isletme-1',
        'application/from-day': 'Tuesday',
        'application/from-period': '2',
      }),
    })

    expect(wrapper.emitted('business-removed')![0]![0]).toMatchObject({
      businessId: 'isletme-1',
      day: 'Tuesday',
      periodNumber: 2,
    })
    expect(wrapper.emitted('business-dropped')).toHaveLength(1)
  })

  it('hücreden hücreye geçerken yeni hücrenin vurgusunu söndürmez', async () => {
    const wrapper = mount(AssignmentGrid, {
      props: {
        schedule: makeSchedule('Matematik'),
        periodCount: 2,
        disabled: false,
        businessNameMap: {},
        selected: null,
      },
      global,
    })

    const zones = wrapper.findAll('.drop-zone')
    expect(zones.length).toBeGreaterThan(1)

    // Tarayıcı sırası: yeni hücrede dragover, ARDINDAN eski hücrede dragleave.
    await zones[0]!.trigger('dragover', { dataTransfer: makeDataTransfer({}) })
    await zones[1]!.trigger('dragover', { dataTransfer: makeDataTransfer({}) })
    await zones[0]!.trigger('dragleave')

    expect(zones[1]!.classes()).toContain('drop-zone--active')
    expect(zones[0]!.classes()).not.toContain('drop-zone--active')
  })

  it('salt okunur modda (disabled) bırakmayı yok sayar', async () => {
    const wrapper = mount(AssignmentGrid, {
      props: {
        schedule: makeSchedule('Matematik'),
        periodCount: 2,
        disabled: true,
        businessNameMap: {},
        selected: null,
      },
      global,
    })

    await wrapper.find('.drop-zone').trigger('drop', {
      dataTransfer: makeDataTransfer({ 'application/business-id': 'isletme-1' }),
    })

    // Kapalı dönem = salt okunur; hiçbir yazma olayı çıkmamalı.
    expect(wrapper.emitted('business-dropped')).toBeUndefined()
    expect(wrapper.emitted('business-removed')).toBeUndefined()
  })
})
