import { describe, it, expect } from 'vitest'
import { isNavItemVisible, menuDefinition, type NavItem } from './useNavigation'

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
 *
 * NOT 2: `kurumlar` burada yerel kurulmaz — gerçek `menuDefinition`'dan bulunur. Yerel bir
 * kopya, "Kurumlar" girdisinden `visibleWhen` silinse bile testi yeşil tutardı; bu dosya o
 * sahte-yeşil riskini kapatmak için gerçek tanıma bağlıdır.
 */
describe('isNavItemVisible', () => {
  const izinliOkuyucu = (perms: string[]) => (required: string[]) =>
    required.length === 0 || required.some((p) => perms.includes(p))

  const institutionGroup = menuDefinition.find((group) => group.key === 'institution')
  const kurumlar = institutionGroup?.children.find((item) => item.to.name === 'InstitutionList')

  it('menü tanımında "Kurumlar" girdisi var ve visibleWhen taşıyor', () => {
    expect(kurumlar).toBeDefined()
    expect(kurumlar?.visibleWhen).toBeTypeOf('function')
  })

  it('il/ilçe kullanıcısına gösterilir', () => {
    expect(
      isNavItemVisible(kurumlar as NavItem, izinliOkuyucu(['institution:view']), {
        isUpperNode: true,
      }),
    ).toBe(true)
  })

  it('okul kullanıcısına gösterilmez — izni olsa bile', () => {
    expect(
      isNavItemVisible(kurumlar as NavItem, izinliOkuyucu(['institution:view']), {
        isUpperNode: false,
      }),
    ).toBe(false)
  })

  it('izni olmayana gösterilmez — düğüm tipi üst düğüm olsa bile', () => {
    expect(
      isNavItemVisible(kurumlar as NavItem, izinliOkuyucu([]), { isUpperNode: true }),
    ).toBe(false)
  })

  it('koşulu olmayan girdi yalnız izne bakar', () => {
    const kurumBilgileri: NavItem = {
      title: 'Kurum Bilgileri',
      icon: 'account_balance',
      to: { name: 'Institution' },
      permissions: ['institution:view'],
    }

    expect(
      isNavItemVisible(kurumBilgileri, izinliOkuyucu(['institution:view']), {
        isUpperNode: false,
      }),
    ).toBe(true)
  })
})
