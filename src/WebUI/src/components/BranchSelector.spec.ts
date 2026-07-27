/* eslint-disable vue/one-component-per-file --
   Bu bir test dosyası; aşağıdaki iki bileşen Quasar stub'ıdır, üretim bileşeni değildir.
   Adlandırılmış stub kullanılıyor ki findComponent(...) tipli wrapper döndürsün. */
import { describe, it, expect, beforeEach, vi } from 'vitest'
import { defineComponent, ref } from 'vue'
import { mount } from '@vue/test-utils'
import { setActivePinia, createPinia } from 'pinia'
import { useAuthStore, ALL_BRANCHES_PERMISSION, type AuthUser } from 'stores/auth'

// Alan kataloğu ağ/store'a gitmeden sabitlenir — test edilen şey kapsam filtresi.
const branchOptions = [
  { label: 'EET — Elektrik-Elektronik Teknolojisi', value: 'EET' },
  { label: 'MTT — Makine Teknolojisi', value: 'MTT' },
  { label: 'BLS — Bilişim Teknolojileri', value: 'BLS' },
]

vi.mock('src/composables/useEntityOptions', () => ({
  useBranchOptions: () => ({
    options: ref(branchOptions),
    allOptions: ref(branchOptions),
    loading: ref(false),
    load: vi.fn().mockResolvedValue(undefined),
    filter: vi.fn(),
  }),
}))

import BranchSelector from './BranchSelector.vue'

interface BranchOption {
  label: string
  value: string
}

/** Adlandırılmış stub: findComponent(SelectStub) tipli wrapper döndürür. */
const SelectStub = defineComponent({
  name: 'QSelectStub',
  props: {
    options: { type: Array as () => BranchOption[], default: () => [] },
    modelValue: { type: String, default: null },
  },
  template: '<div class="q-select-stub" />',
})

const FieldStub = defineComponent({
  name: 'QFieldStub',
  template: '<div class="q-field-stub"><slot name="control" /></div>',
})

const global = {
  stubs: {
    'q-field': FieldStub,
    'q-select': SelectStub,
    'q-icon': true,
    SelectEmptyOption: true,
  },
}

function makeUser(branchCodes: string[]): AuthUser {
  return {
    id: 'u1',
    username: 'test',
    email: 'test@mesnet.local',
    firstName: 'Test',
    lastName: 'Kullanıcı',
    fullName: 'Test Kullanıcı',
    roles: [],
    institutionId: 'i1',
    branchCode: branchCodes[0] ?? null,
    branchCodes,
  }
}

function signIn(branchCodes: string[], permissions: string[]) {
  const store = useAuthStore()
  store.user = makeUser(branchCodes)
  store.permissions = permissions
  return store
}

const SCOPED_PERMS = ['department:distribution:manage']

describe('BranchSelector — alan kapsamı (#126)', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
  })

  it('muafiyetli kullanıcıya yazma bağlamında TÜM alanlar gösterilir', async () => {
    // Arrange — yöneticinin branşı yok, bu normaldir
    signIn([], [ALL_BRANCHES_PERMISSION])

    // Act
    const wrapper = mount(BranchSelector, { props: { writeContext: true }, global })
    await wrapper.vm.$nextTick()

    // Assert — seçici görünür (salt okunur alana düşmez) ve kısıt yoktur
    const select = wrapper.findComponent(SelectStub)
    expect(select.exists()).toBe(true)
    expect(select.props('options')).toHaveLength(3)
  })

  it('yazma bağlamında yetkisiz alanlar listelenmez', async () => {
    // Arrange — iki alandan sorumlu alan şefi
    signIn(['EET', 'MTT'], SCOPED_PERMS)

    // Act
    const wrapper = mount(BranchSelector, { props: { writeContext: true }, global })
    await wrapper.vm.$nextTick()

    // Assert — BLS listede yok
    const options = wrapper.findComponent(SelectStub).props('options') ?? []
    expect(options.map((o) => o.value)).toEqual(['EET', 'MTT'])
  })

  it('SALT OKUMA bağlamında tüm alanlar görünür — okuma kısıtlanmaz', async () => {
    // Arrange — tek alandan sorumlu alan şefi
    signIn(['EET'], SCOPED_PERMS)

    // Act — writeContext verilmedi (varsayılan false)
    const wrapper = mount(BranchSelector, { global })
    await wrapper.vm.$nextTick()

    // Assert — bilinçli karar: alan şefi başka alanın verisini GÖREBİLİR
    const options = wrapper.findComponent(SelectStub).props('options')
    expect(options).toHaveLength(3)
  })

  it('yazma bağlamında kapsamı tek alan olan kullanıcıya seçici yerine salt okunur alan gösterilir', async () => {
    // Arrange
    signIn(['EET'], SCOPED_PERMS)

    // Act
    const wrapper = mount(BranchSelector, { props: { writeContext: true }, global })
    await wrapper.vm.$nextTick()

    // Assert — q-select yok, salt okunur q-field var
    expect(wrapper.findComponent(SelectStub).exists()).toBe(false)
    expect(wrapper.find('.q-field-stub').exists()).toBe(true)
  })

  it('kapsamı tek alansa o alan otomatik seçilir', async () => {
    // Arrange
    signIn(['EET'], SCOPED_PERMS)

    // Act
    const wrapper = mount(BranchSelector, { props: { writeContext: true }, global })
    await wrapper.vm.$nextTick()
    await wrapper.vm.$nextTick()

    // Assert
    expect(wrapper.emitted('update:modelValue')?.at(-1)).toEqual(['EET'])
  })

  it('branşı olmayan ve muafiyeti olmayan kullanıcıya yazma bağlamında hiç alan listelenmez', async () => {
    // Arrange — branşı girilmemiş alan şefi
    signIn([], SCOPED_PERMS)

    // Act
    const wrapper = mount(BranchSelector, { props: { writeContext: true }, global })
    await wrapper.vm.$nextTick()

    // Assert — yazabileceği alan yok
    const options = wrapper.findComponent(SelectStub).props('options')
    expect(options).toHaveLength(0)
  })

  it('forceSelect ile tek alanlı kullanıcıda bile seçici gösterilir (filtre amaçlı)', async () => {
    // Arrange
    signIn(['EET'], SCOPED_PERMS)

    // Act
    const wrapper = mount(BranchSelector, {
      props: { writeContext: true, forceSelect: true },
      global,
    })
    await wrapper.vm.$nextTick()

    // Assert
    expect(wrapper.findComponent(SelectStub).exists()).toBe(true)
  })
})
