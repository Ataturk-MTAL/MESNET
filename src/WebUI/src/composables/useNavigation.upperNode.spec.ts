import { describe, it, expect } from 'vitest'
import { isNavItemVisible, menuDefinition, resolveIsUpperNode, type NavItem } from './useNavigation'

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
        isActingAsDirectorate: false,
      }),
    ).toBe(true)
  })

  it('okul kullanıcısına gösterilmez — izni olsa bile', () => {
    expect(
      isNavItemVisible(kurumlar as NavItem, izinliOkuyucu(['institution:view']), {
        isUpperNode: false,
        isActingAsDirectorate: false,
      }),
    ).toBe(false)
  })

  it('izni olmayana gösterilmez — düğüm tipi üst düğüm olsa bile', () => {
    expect(
      isNavItemVisible(kurumlar as NavItem, izinliOkuyucu([]), {
        isUpperNode: true,
        isActingAsDirectorate: false,
      }),
    ).toBe(false)
  })

  /**
   * Görev 10 (B parçası) sonrası ölçülen hata: `institutionStore` artık aktif bağlama bağlı —
   * il yetkilisi bir okula geçtiğinde `institutionStore.institution.nodeType === 'School'`
   * olur ve `nodeType` TEK BAŞINA kontrol edilirse "Kurumlar" menü girdisi KAYBOLUR. Aktif
   * bağlam DOLU olması aktörün üst düğüm olduğunun KANITIDIR (okul kullanıcısı bağlam
   * SEÇEMEZ), o yüzden `resolveIsUpperNode` ikisini OR'lar.
   */
  it('aktif bağlam varken üst düğüm sayılır — nodeType School olsa bile', () => {
    expect(resolveIsUpperNode('School', 'okul-x-id')).toBe(true)
  })

  it('aktif bağlam varken "Kurumlar" görünür kalır', () => {
    const ctx = {
      isUpperNode: resolveIsUpperNode('School', 'okul-x-id'),
      isActingAsDirectorate: false,
    }

    expect(isNavItemVisible(kurumlar as NavItem, izinliOkuyucu(['institution:view']), ctx)).toBe(
      true,
    )
  })

  it('aktif bağlam yokken nodeType School ise üst düğüm SAYILMAZ', () => {
    expect(resolveIsUpperNode('School', null)).toBe(false)
    expect(resolveIsUpperNode('School', undefined)).toBe(false)
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
        isActingAsDirectorate: false,
      }),
    ).toBe(true)
  })
})

/**
 * Müdürlük bağlamında "Staj Yönetimi" grubunun okul-işi girdileri gizlenir.
 *
 * Gerekçe: müdürlük rollerine `internship:view` verildi (D1) — bu izin `Staj Yönetimi`
 * grubunun TÜM çocuklarını açar, ama müdürlüğün KENDİ bağlamında (aktif bağlam yok, kiracı
 * müdürlüğün kendisi) bu sayfaların hiçbiri veri döndürmez: liste boş görünür. `Fesihler`
 * istisnadır — müdürlük oraya tam olarak bir okulun bağlamına GEÇMEK için gider, girdi onun
 * giriş kapısıdır ve gizlenmez.
 *
 * `isActingAsDirectorate` ile `isUpperNode` KARIŞTIRILMAZ: `isUpperNode` aktif bağlam açıkken
 * de true kalır (Kurumlar ağacı erişilebilir kalmalı), `isActingAsDirectorate` ise aktif bağlam
 * açıkken FALSE olur (kiracı artık o okuldur, okul menüleri doğrudur).
 *
 * NOT: bu testler de `menuDefinition`'ı GERÇEK tanımdan bulur — yerel bir kopya, bir girdiden
 * `visibleWhen` silinse bile testi yeşil tutardı.
 */
describe('isNavItemVisible — Staj Yönetimi grubunun müdürlük bağlamı kapısı', () => {
  const izinliOkuyucu = (perms: string[]) => (required: string[]) =>
    required.length === 0 || required.some((p) => perms.includes(p))

  const internshipGroup = menuDefinition.find((group) => group.key === 'internship')
  const children = internshipGroup?.children ?? []
  const fesihler = children.find((item) => item.to.name === 'InternshipTerminations')
  const digerCocuklar = children.filter((item) => item.to.name !== 'InternshipTerminations')

  it('grup tanımlı ve Fesihler dışında en az bir çocuk taşıyor', () => {
    expect(internshipGroup).toBeDefined()
    expect(fesihler).toBeDefined()
    expect(digerCocuklar.length).toBeGreaterThan(0)
  })

  it('Fesihler dışındaki her çocuk visibleWhen koşulu taşıyor', () => {
    for (const item of digerCocuklar) {
      expect(item.visibleWhen, `${item.title} için visibleWhen bekleniyordu`).toBeTypeOf(
        'function',
      )
    }
  })

  it('Fesihler herhangi bir visibleWhen koşulu TAŞIMAZ — müdürlüğün giriş kapısı', () => {
    expect(fesihler?.visibleWhen).toBeUndefined()
  })

  it('müdürlük bağlamında (isActingAsDirectorate: true) Fesihler dışındaki çocuklar görünmez', () => {
    const ctx = { isUpperNode: true, isActingAsDirectorate: true }
    const tumIzinler = digerCocuklar.flatMap((item) => item.permissions)

    for (const item of digerCocuklar) {
      expect(
        isNavItemVisible(item, izinliOkuyucu(tumIzinler), ctx),
        `${item.title} müdürlük bağlamında görünmemeliydi`,
      ).toBe(false)
    }
  })

  it('müdürlük bağlamında Fesihler görünür — izni varsa', () => {
    const ctx = { isUpperNode: true, isActingAsDirectorate: true }

    expect(
      isNavItemVisible(fesihler as NavItem, izinliOkuyucu(['internship:approval:override']), ctx),
    ).toBe(true)
  })

  it('okul bağlamında (isActingAsDirectorate: false) grubun tüm çocukları izinliyse görünür', () => {
    const ctx = { isUpperNode: false, isActingAsDirectorate: false }

    for (const item of children) {
      expect(
        isNavItemVisible(item, izinliOkuyucu(item.permissions), ctx),
        `${item.title} okul bağlamında görünmeliydi`,
      ).toBe(true)
    }
  })

  it('Kurum Yönetimi grubunun girdileri müdürlük bağlamında da görünür kalır', () => {
    const institutionGroup = menuDefinition.find((group) => group.key === 'institution')
    const izinsizGirdiler = institutionGroup?.children ?? []
    const directorateCtx = { isUpperNode: true, isActingAsDirectorate: true }

    for (const item of izinsizGirdiler) {
      expect(
        isNavItemVisible(item, izinliOkuyucu(item.permissions), directorateCtx),
        `${item.title} müdürlük bağlamında görünmeliydi (Kurum Yönetimi müdürlüğün kendi işi)`,
      ).toBe(true)
    }
  })
})
