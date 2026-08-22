import { readdirSync, readFileSync, statSync } from 'node:fs'
import { fileURLToPath } from 'node:url'
import { dirname, join, relative } from 'node:path'
import { describe, expect, it } from 'vitest'

/**
 * Kapsam ve görünürlük kararı rol adına bakamaz — ADR-0001'in frontend kilidi (#192).
 *
 * Neden dosya taraması: bu kural tek bir bileşenin davranışı değil, kod tabanı geneli bir
 * yasaktır. Davranış testi yalnız bugünkü çağrı yerlerini korur; yarın başka bir sayfaya
 * eklenen `roles.includes('DepartmentHead')` hiçbir birim testinde görünmez.
 *
 * Gerçekten yaşandı: `TeacherSchedulePage` alan ön-seçimi `isDepartmentHead && user.branchCode`
 * koşuluna bakıyordu. `branchCode` #126 ile deprecate edilip `null` atanınca koşul HİÇ tutmaz
 * oldu — özellik sessizce çalışmadı, kimse fark etmedi. Rol adına bakan kontrol yalnız kural
 * ihlali değil, kırılganlık kaynağıdır.
 *
 * Doğru desen: `writableBranchCodes` / `canManageAllBranches` / `hasPermission(Permissions.X)`.
 */

const SRC = join(dirname(fileURLToPath(import.meta.url)), '..')

/** Rol adı ile kapsam/görünürlük kararı veren desenler. */
const YASAK_DESENLER = [
  /\broles\s*\.\s*includes\s*\(/,
  /\broles\s*\.\s*some\s*\(/,
  /\bisDepartmentHead\b/,
  /\bisManager\b/,
]

/** Rol adı listesini meşru biçimde tutan yerler — karar vermezler. */
const MUAF = ['utils/permissions.ts', 'types/', '.spec.ts']

function* tsVeVueDosyalari(dizin: string): Generator<string> {
  for (const ad of readdirSync(dizin)) {
    const yol = join(dizin, ad)
    if (statSync(yol).isDirectory()) {
      if (ad === 'node_modules' || ad === 'dist') continue
      yield* tsVeVueDosyalari(yol)
    } else if (/\.(ts|vue)$/.test(ad)) {
      yield yol
    }
  }
}

describe('Rol adına bakan kapsam kararı — ADR-0001 kilidi', () => {
  const ihlaller: string[] = []

  for (const dosya of tsVeVueDosyalari(SRC)) {
    const goreli = relative(SRC, dosya)
    if (MUAF.some((m) => goreli.includes(m))) continue

    const satirlar = readFileSync(dosya, 'utf8').split('\n')
    satirlar.forEach((satir, i) => {
      const kod = satir.trim()
      if (kod.startsWith('//') || kod.startsWith('*')) return
      if (YASAK_DESENLER.some((d) => d.test(satir))) {
        ihlaller.push(`${goreli}:${i + 1}  ${kod.slice(0, 90)}`)
      }
    })
  }

  it('tarama gerçekten dosya okuyor', () => {
    // Tarama sessizce boş dönerse aşağıdaki test hiçbir şey doğrulamaz.
    expect([...tsVeVueDosyalari(SRC)].length).toBeGreaterThan(50)
  })

  it('hiçbir yerde rol adına bakan kapsam/görünürlük kararı yok', () => {
    expect(
      ihlaller,
      'Kapsam ve görünürlük kararı izne ya da kapsam kaydına bakar, rol adına DEĞİL ' +
        '(ADR-0001). Rol adı organizasyon şemasının bugünkü fotoğrafıdır; şema kayınca ' +
        'kontrol sessizce yanlış çalışır. Desen: writableBranchCodes / canManageAllBranches / ' +
        'hasPermission(Permissions.X).\n  ' + ihlaller.join('\n  '),
    ).toEqual([])
  })
})
