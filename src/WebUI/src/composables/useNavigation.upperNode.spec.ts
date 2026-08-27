import { describe, it, expect } from 'vitest'
import { isNavItemVisible, type NavItem } from './useNavigation'

/**
 * "Kurumlar" menü girdisi okul kullanıcısına GÖSTERİLMEZ.
 *
 * İzin kapısı bunu yapamaz: okul müdürü de `institution:view` taşır (hatta `institution:*`).
 * Ayrım izinde değil, kullanıcının bağlı olduğu düğümün TİPİNDEDİR. Kapı olmasaydı okul
 * müdürü tek satırlık bir "liste" görürdü — bilgi taşımayan, tıklandığında zaten açık olan
 * sayfaya giden bir menü girdisi.
 *
 * NOT: bu bir GÖRÜNÜRLÜK kararıdır, yetki kararı değil. Yetki sunucudadır
 * (`InstitutionScopePolicy`); okul kullanıcısı ucu elle çağırsa da kendi kurumundan
 * fazlasını göremez.
 */
describe('isNavItemVisible', () => {
  const izinliOkuyucu = (perms: string[]) => (required: string[]) =>
    required.length === 0 || required.some((p) => perms.includes(p))

  const kurumlar: NavItem = {
    title: 'Kurumlar',
    icon: 'account_tree',
    to: { name: 'InstitutionList' },
    permissions: ['institution:view'],
    visibleWhen: (ctx) => ctx.isUpperNode,
  }

  it('il/ilçe kullanıcısına gösterilir', () => {
    expect(
      isNavItemVisible(kurumlar, izinliOkuyucu(['institution:view']), { isUpperNode: true }),
    ).toBe(true)
  })

  it('okul kullanıcısına gösterilmez — izni olsa bile', () => {
    expect(
      isNavItemVisible(kurumlar, izinliOkuyucu(['institution:view']), { isUpperNode: false }),
    ).toBe(false)
  })

  it('izni olmayana gösterilmez — düğüm tipi üst düğüm olsa bile', () => {
    expect(isNavItemVisible(kurumlar, izinliOkuyucu([]), { isUpperNode: true })).toBe(false)
  })

  it('koşulu olmayan girdi yalnız izne bakar', () => {
    const kurumBilgileri: NavItem = {
      title: 'Kurum Bilgileri',
      icon: 'account_balance',
      to: { name: 'Institution' },
      permissions: ['institution:view'],
    }

    expect(
      isNavItemVisible(kurumBilgileri, izinliOkuyucu(['institution:view']), { isUpperNode: false }),
    ).toBe(true)
  })
})
