import { readFileSync } from 'node:fs'
import { fileURLToPath } from 'node:url'
import { dirname, join } from 'node:path'
import { describe, expect, it } from 'vitest'

/**
 * Form rotaları YAZMA izniyle korunmalıdır.
 *
 * Neden test: `meta.formRoute` taşıyan bir rota, tek amacı veri yazmak olan bir sayfaya
 * açılır. Rota OKUMA izniyle (`x:view`) korunursa, yalnız görüntüleme yetkisi olan
 * kullanıcı formu açar, doldurur ve **Kaydet'te 403** duvarına çarpar — hata sunucudan
 * gelir, arayüz hiçbir şey söylememiş olur.
 *
 * Bu gerçekten yaşandı: dört rota (`institution/edit`, `students/new`,
 * `students/:id/edit`, `attendance/new`) okuma izniyle korunuyordu. Liste sayfalarındaki
 * butonlar doğru izinle gizlendiği için normal yolda görünmüyordu; ama doğrudan URL,
 * yer imi ve geri tuşu formu açıyordu. Alan şefi (`attendance:view` var,
 * `attendance:manage` yok) devamsızlık formunu doldurup gönderemiyordu.
 *
 * Kural: form rotasının izni, o formun çağırdığı ucun izniyle AYNI olmalı.
 * Bu test daha zayıf ama otomatik doğrulanabilir bir değişmezi kilitler: form rotası
 * hiçbir koşulda salt-okuma izniyle korunamaz.
 */

const ROUTER = join(dirname(fileURLToPath(import.meta.url)), 'index.ts')

/** Salt-okuma anlamı taşıyan izin sonekleri. */
const OKUMA_SONEKLERI = [':view', ':view-own']

interface FormRotasi {
  ad: string
  izinler: string[]
}

/**
 * `index.ts`'i metin olarak ayrıştırır. Modülü import etmek sayfa bileşenlerini ve
 * Pinia store'larını devreye sokardı; burada aranan şey yalnız yapılandırma.
 */
function formRotalariniOku(): FormRotasi[] {
  const kaynak = readFileSync(ROUTER, 'utf8')
  const rotalar: FormRotasi[] = []

  // meta satırındaki formRoute: true işaretinden geriye doğru en yakın `name:` alanını bul.
  const metaDeseni = /meta:\s*\{([^}]*formRoute:\s*true[^}]*)\}/g

  for (const eslesme of kaynak.matchAll(metaDeseni)) {
    const meta = eslesme[1]!
    const oncesi = kaynak.slice(0, eslesme.index)
    const adEslesmeleri = [...oncesi.matchAll(/name:\s*'([^']+)'/g)]
    const ad = adEslesmeleri.at(-1)?.[1] ?? '(adsız)'

    const izinler = [...meta.matchAll(/'([a-z]+:[a-z0-9:-]+)'/g)].map((m) => m[1]!)
    rotalar.push({ ad, izinler })
  }

  return rotalar
}

describe('Form rotaları — yazma izni kilidi', () => {
  const formRotalari = formRotalariniOku()

  it('router/index.ts ayrıştırılabiliyor ve form rotası bulunuyor', () => {
    // Ayrıştırma sessizce boş dönerse aşağıdaki testler hiçbir şey doğrulamaz.
    expect(formRotalari.length).toBeGreaterThan(0)
  })

  it('her form rotasının en az bir izni var', () => {
    const izinsiz = formRotalari.filter((r) => r.izinler.length === 0).map((r) => r.ad)

    expect(izinsiz, `İzinsiz form rotası: ${izinsiz.join(', ')}`).toEqual([])
  })

  it('hiçbir form rotası salt-okuma izniyle korunmuyor', () => {
    const ihlaller = formRotalari
      .filter((r) => r.izinler.some((izin) => OKUMA_SONEKLERI.some((s) => izin.endsWith(s))))
      .map((r) => `${r.ad} → ${r.izinler.join(', ')}`)

    expect(
      ihlaller,
      'Form rotası YAZMA izniyle korunmalı. Salt-okuma izniyle korunan rota, yalnız ' +
        'görüntüleme yetkisi olan kullanıcıyı formu doldurup Kaydet\'te 403 almaya ' +
        'gönderir. Rotanın iznini, formun çağırdığı ucun izniyle aynı yapın:\n  ' +
        ihlaller.join('\n  '),
    ).toEqual([])
  })
})
