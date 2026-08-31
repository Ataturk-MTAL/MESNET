import { readFileSync } from 'node:fs'
import { fileURLToPath } from 'node:url'
import { dirname, join } from 'node:path'
import { describe, expect, it } from 'vitest'
import { menuDefinition } from '../composables/useNavigation'

/**
 * Rota izinleri, sunucudaki yetkilendirme politikasıyla ELLE eşleniyor ve aralarında hiçbir
 * bağ yok. Bu testin varlık nedeni o eşlemenin sessizce ayrışmasıdır.
 *
 * Ölçüldü (31.08.2026): `internship:approval:override` ve `directorate:institution-bootstrap`
 * sunucuda doğru korunuyordu ama rota metaları onları içermediği için müdürlük rolleri
 * sayfalara HİÇ ulaşamıyordu. Belirti 403 duvarı değil, menüde hiç görünmemekti — yani
 * GÖRÜNMEZ.
 *
 * Kural: bir yeteneğin sunucu politikası birden çok izni kabul ediyorsa (AnyOf), o yeteneğe
 * ulaşan rotanın meta.permissions listesi o izinlerin HEPSİNİ içermelidir.
 *
 * Bu tablo ELLE yazıldı ve bilerek öyle: rota adı ile sunucudaki C# politikası arasında
 * makine ile izlenebilir hiçbir bağ yok (aynı sebep formRoutePermissions.spec.ts'te de
 * geçerli). Tablo değişmeden bir rotanın izin listesi daralırsa test kırmızıya döner —
 * ama tablonun kendisi büyümezse (yeni bir AnyOf politikası eklenip tabloya işlenmezse)
 * bu kilit o yeni ayrışmayı YAKALAMAZ.
 */

const ROUTER = join(dirname(fileURLToPath(import.meta.url)), 'index.ts')

/**
 * Rota adı → sunucu politikasının kabul ettiği ve bu yüzden rotanın meta.permissions
 * listesinde bulunması ZORUNLU olan izinler. Elle bakım gerektirir (bkz. üstteki not).
 */
const ZORUNLU_IZINLER: Record<string, string[]> = {
  InternshipTerminations: ['internship:view', 'internship:manage', 'internship:approval:override'],
  UserManagement: ['user:view', 'user:create', 'directorate:institution-bootstrap'],
  PermissionScope: ['user:roles:manage'],
}

/**
 * `index.ts`'i metin olarak ayrıştırır. Modülü import etmek sayfa bileşenlerini ve
 * Pinia store'larını devreye sokardı; burada aranan şey yalnız yapılandırma.
 *
 * Her `meta: { ... }` bloğu için geriye doğru en yakın `name:` alanını bulur ve bloktaki
 * izin benzeri string literalleri (`'modül:eylem'` biçimi) toplar.
 */
function rotaIzinleriniOku(): Map<string, string[]> {
  const kaynak = readFileSync(ROUTER, 'utf8')
  const rotaIzinleri = new Map<string, string[]>()

  const metaDeseni = /meta:\s*\{([^}]*)\}/g

  for (const eslesme of kaynak.matchAll(metaDeseni)) {
    const meta = eslesme[1]!
    const oncesi = kaynak.slice(0, eslesme.index)
    const adEslesmeleri = [...oncesi.matchAll(/name:\s*'([^']+)'/g)]
    const ad = adEslesmeleri.at(-1)?.[1]

    if (!ad) continue

    const izinler = [...meta.matchAll(/'([a-z]+:[a-z0-9:-]+)'/g)].map((m) => m[1]!)
    rotaIzinleri.set(ad, izinler)
  }

  return rotaIzinleri
}

/**
 * Verilen rota adına işaret eden menü girdisini `menuDefinition`'da bulur — hem tek-child'ı
 * düz link'e terfi ETMEMİŞ hâldeki (child) girdileri hem üst-seviye grup linklerini ('home'
 * gibi `to` taşıyan gruplar) kapsar. `useNavigation.upperNode.spec.ts` ile aynı desen: menü
 * yerel kopyalanmaz, gerçek dışa aktarılan tanımdan okunur — yerel kopya bir sapmayı asla
 * yakalayamaz.
 */
function menuIzinleriniBul(rotaAdi: string): string[] | undefined {
  for (const grup of menuDefinition) {
    if (grup.to?.name === rotaAdi) return grup.permissions
    for (const cocuk of grup.children) {
      if (cocuk.to.name === rotaAdi) return cocuk.permissions
    }
  }
  return undefined
}

describe('Rota/politika hizası kilidi', () => {
  const rotaIzinleri = rotaIzinleriniOku()

  it('router/index.ts ayrıştırılabiliyor ve kilitli rotaların hepsi bulunuyor', () => {
    const bulunamayan = Object.keys(ZORUNLU_IZINLER).filter((ad) => !rotaIzinleri.has(ad))

    expect(
      bulunamayan,
      `Kilitli rota router/index.ts içinde bulunamadı: ${bulunamayan.join(', ')}. ` +
        'Rota yeniden adlandırıldıysa bu dosyadaki ZORUNLU_IZINLER tablosunu da güncelleyin.',
    ).toEqual([])
  })

  it('her kilitli rota, sunucu politikasının kabul ettiği izinlerin HEPSİNİ meta.permissions içinde taşıyor', () => {
    const ihlaller: string[] = []

    for (const [rotaAdi, zorunluIzinler] of Object.entries(ZORUNLU_IZINLER)) {
      const mevcutIzinler = rotaIzinleri.get(rotaAdi) ?? []
      const eksikIzinler = zorunluIzinler.filter((izin) => !mevcutIzinler.includes(izin))

      for (const eksikIzin of eksikIzinler) {
        ihlaller.push(
          `${rotaAdi} rotası '${eksikIzin}' iznini meta.permissions listesinde içermiyor — ` +
            'sunucu politikası bu izni de kabul ediyor (AnyOf). Sonuç: bu izne sahip aktör ' +
            'işlemi sunucuda yapabilir ama sayfaya HİÇ ulaşamaz (403 değil, menüde görünmeme).',
        )
      }
    }

    expect(ihlaller, `\n  ${ihlaller.join('\n  ')}`).toEqual([])
  })

  /**
   * Rota metasının doğru olması yetmez: aynı yeteneğe giden MENÜ girdisi farklı bir izin
   * listesi taşırsa aktör rotaya URL'den ulaşsa bile menüde hiç göremez (ya da tersi — menü
   * geniş, rota dar kalırsa 403'e giden bir bağlantı gösterilir). İki dosyadaki liste
   * (`router/index.ts` meta.permissions ↔ `useNavigation.ts` menuDefinition) bugüne kadar
   * yalnız YORUM SATIRIYLA "aynı" tutuluyordu — yorum kilit değildir, sessiz sapmayı
   * yakalamaz. Karşılaştırma SIRAYA değil KÜMEYE bakar: iki listenin yazılış sırası farklı
   * olabilir, önemli olan aynı izin kümesini taşımalarıdır.
   */
  it('her kilitli rotanın menü girdisi, rotanın meta.permissions listesiyle KÜME olarak eşleşiyor', () => {
    const ihlaller: string[] = []

    for (const rotaAdi of Object.keys(ZORUNLU_IZINLER)) {
      const rotaninIzinleri = rotaIzinleri.get(rotaAdi) ?? []
      const menuIzinleri = menuIzinleriniBul(rotaAdi)

      if (menuIzinleri === undefined) {
        ihlaller.push(
          `${rotaAdi} rotasına işaret eden bir menü girdisi (\`to: { name: '${rotaAdi}' }\`) ` +
            'menuDefinition içinde bulunamadı.',
        )
        continue
      }

      const rotaKumesi = new Set(rotaninIzinleri)
      const menuKumesi = new Set(menuIzinleri)
      const kumeEsit =
        rotaKumesi.size === menuKumesi.size &&
        [...rotaKumesi].every((izin) => menuKumesi.has(izin))

      if (!kumeEsit) {
        ihlaller.push(
          `${rotaAdi}: rota izinleri [${[...rotaKumesi].join(', ')}] ile menü izinleri ` +
            `[${[...menuKumesi].join(', ')}] KÜME olarak eşleşmiyor.`,
        )
      }
    }

    expect(ihlaller, `\n  ${ihlaller.join('\n  ')}`).toEqual([])
  })
})
