import { describe, it, expect } from 'vitest'
import { readFileSync } from 'node:fs'
import { dirname, resolve } from 'node:path'
import { fileURLToPath } from 'node:url'

/**
 * StatusBadge renk haritası — backend slug'ları ile eşleşme kilidi.
 *
 * NEDEN VAR: `STATUS_COLORS` haritasında karşılığı olmayan bir slug hataya düşmez,
 * sessizce `grey-7` fallback'ine düşer. Rozet okunabilir göründüğü için kimse fark etmez;
 * yalnız anlam kaybolur — feshedilmiş bir sözleşme kırmızı yerine gri, ödeme onay
 * zincirinin ara adımları renksiz görünür. Backend'e yeni bir SmartEnum durumu eklendiğinde
 * bu test kırılır ve haritanın güncellenmesi gerektiğini söyler.
 *
 * Yöntem: backend enum dosyaları diskten okunur, SmartEnum ctor'unun 3. parametresindeki
 * Türkçe Slug değerleri çıkarılır ve hepsinin haritada karşılığı olduğu doğrulanır.
 */

// Yol çözümü çalışma dizinine değil, bu dosyanın konumuna dayanır.
// (`new URL(..., import.meta.url)` kullanılmaz — Vite o deseni varlık URL'ine dönüştürür.)
const COMPONENTS_DIR = dirname(fileURLToPath(import.meta.url))
// components -> src -> WebUI -> src -> depo kökü
const REPO_ROOT = resolve(COMPONENTS_DIR, '../../../..')

const STATUS_BADGE_PATH = resolve(COMPONENTS_DIR, 'StatusBadge.vue')

// Backend SmartEnum dosyaları — ekranda StatusBadge ile render edilen tüm durum enum'ları.
const ENUM_FILES: ReadonlyArray<{ enumName: string; path: string }> = [
  { enumName: 'PaymentPhase', path: 'src/Modules/Payment/MESNET.Payment.Core/Enums/PaymentPhase.cs' },
  { enumName: 'ContractStatus', path: 'src/Modules/Contract/MESNET.Contract.Core/Enums/ContractStatus.cs' },
  { enumName: 'InternshipPhase', path: 'src/Modules/Internship/MESNET.Internship.Core/Enums/InternshipPhase.cs' },
  { enumName: 'AbsenceType', path: 'src/Modules/Attendance/MESNET.Attendance.Core/Enums/AbsenceType.cs' },
  { enumName: 'AttendanceStatus', path: 'src/Modules/Attendance/MESNET.Attendance.Core/Enums/AttendanceStatus.cs' },
  { enumName: 'StudentStatus', path: 'src/Modules/Enrollment/MESNET.Enrollment.Core/Enums/StudentStatus.cs' },
]

// `new(nameof(Terminated), 6, "Feshedilmiş");` → "Feshedilmiş"
// Hizalama boşlukları (ContractStatus'ta var) ve değişken sayı genişliği tolere edilir.
const SLUG_PATTERN = /new\(\s*nameof\([^)]+\)\s*,\s*\d+\s*,\s*"([^"]+)"\s*\)/g

function readSlugs(relativePath: string): string[] {
  const source = readFileSync(resolve(REPO_ROOT, relativePath), 'utf8')
  return [...source.matchAll(SLUG_PATTERN)].map((match) => match[1] as string)
}

// StatusBadge.vue'deki STATUS_COLORS haritasının anahtarları.
// Harita dışa aktarılmadığı için bileşen import edilmez, kaynak metin ayrıştırılır.
function readMappedSlugs(): Set<string> {
  const source = readFileSync(STATUS_BADGE_PATH, 'utf8')
  const block = /const STATUS_COLORS[^{]*\{([\s\S]*?)\n\}/.exec(source)
  if (!block) {
    throw new Error(
      'StatusBadge.vue içinde STATUS_COLORS haritası bulunamadı. ' +
        'Harita yeniden adlandırıldıysa bu testteki desen de güncellenmelidir.',
    )
  }
  const keys = [...(block[1] as string).matchAll(/^\s*'([^']+)'\s*:/gm)].map((m) => m[1] as string)
  return new Set(keys)
}

describe('StatusBadge — backend slug kapsaması', () => {
  it.each(ENUM_FILES)('$enumName slug değerlerinin tümü STATUS_COLORS içinde tanımlı', ({ enumName, path }) => {
    // Arrange
    const backendSlugs = readSlugs(path)
    const mappedSlugs = readMappedSlugs()

    // Act
    const missing = backendSlugs.filter((slug) => !mappedSlugs.has(slug))

    // Assert
    expect(backendSlugs.length, `${path} içinde hiç slug bulunamadı — dosya taşınmış olabilir`).toBeGreaterThan(0)
    expect(
      missing,
      `Backend'e yeni durum eklendi; StatusBadge.vue STATUS_COLORS haritasına ekleyin: ` +
        `${missing.map((slug) => `'${slug}'`).join(', ')} (${enumName} — ${path}). ` +
        'Eşleşmeyen slug hata vermez, sessizce gri (grey-7) render edilir.',
    ).toEqual([])
  })
})
