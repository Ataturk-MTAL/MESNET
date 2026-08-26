import { describe, it, expect, beforeEach, vi } from 'vitest'
import { defineComponent } from 'vue'
import { mount } from '@vue/test-utils'
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
  // Aşağıdakiler listede YOKTU; dördü (EvaluationResult, ExamResult, ReportStatus,
  // StudentTermGradeStatus) ham q-badge'den StatusBadge'e geçirildiğinde haritada karşılığı
  // olmadığı fark edilmedi — rozetler sessizce griye düştü. Kilit o yüzden genişletildi.
  { enumName: 'EvaluationResult', path: 'src/Modules/Coordination/MESNET.Coordination.Core/Enums/EvaluationResult.cs' },
  { enumName: 'ExamResult', path: 'src/Modules/Coordination/MESNET.Coordination.Core/Enums/ExamResult.cs' },
  { enumName: 'ReportStatus', path: 'src/Modules/Coordination/MESNET.Coordination.Core/Enums/ReportStatus.cs' },
  { enumName: 'StudentTermGradeStatus', path: 'src/Modules/Coordination/MESNET.Coordination.Core/Enums/StudentTermGradeStatus.cs' },
  { enumName: 'VisitStatus', path: 'src/Modules/Coordination/MESNET.Coordination.Core/Enums/VisitStatus.cs' },
  { enumName: 'HealthReportStatus', path: 'src/Modules/Attendance/MESNET.Attendance.Core/Enums/HealthReportStatus.cs' },
  { enumName: 'PaidLeaveStatus', path: 'src/Modules/Attendance/MESNET.Attendance.Core/Enums/PaidLeaveStatus.cs' },
  { enumName: 'BusinessStatus', path: 'src/Modules/Business/MESNET.Business.Core/Enums/BusinessStatus.cs' },
  { enumName: 'DocumentStatus', path: 'src/Modules/Business/MESNET.Business.Core/Enums/DocumentStatus.cs' },
  { enumName: 'AcademicPeriodStatus', path: 'src/Modules/Institution/MESNET.Institution.Core/Enums/AcademicPeriodStatus.cs' },
  { enumName: 'InvitationStatus', path: 'src/Modules/Security/MESNET.Security.Core/Enums/InvitationStatus.cs' },
  // Aşama değil, kategori: InstitutionPage şube rozetinde `branch.typeSlug` olarak render edilir.
  { enumName: 'EducationType', path: 'src/MESNET.Common.Shared/Enums/EducationType.cs' },
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

// ── Render edilen ton — kaynak metni değil, bileşenin gerçek çıktısı ────────────────────
//
// Yukarıdaki kapsama testi "anahtar var mı" der; AŞAMANIN doğruluğunu söylemez. Dört sayfada
// kaybolan şey tam olarak buydu: rozet vardı, rengi anlamsızdı. Bu blok her slug için
// q-badge'e geçen `color` değerini ölçer.

// Logger mock'u bileşenin import ettiği tanımlayıcıyla aynı olmalı ('src/utils/logger').
vi.mock('src/utils/logger', () => ({
  logger: { info: vi.fn(), warn: vi.fn(), error: vi.fn() },
}))

import { logger } from 'src/utils/logger'
import StatusBadge from './StatusBadge.vue'

/** Quasar kurulmadan render edebilmek için q-badge yerine geçen basit stub. */
const BadgeStub = defineComponent({
  name: 'QBadgeStub',
  props: {
    color: { type: String, default: '' },
    label: { type: String, default: '' },
  },
  template: '<span class="q-badge-stub" :data-color="color">{{ label }}</span>',
})

function renderBadge(slug: string) {
  return mount(StatusBadge, {
    props: { slug },
    global: { stubs: { 'q-badge': BadgeStub } },
  })
}

function renderedColor(slug: string): string | undefined {
  return renderBadge(slug).get('.q-badge-stub').attributes('data-color')
}

// Aşama sabitlerinin karşılığı olan CSS ton adları (StatusBadge.vue ile birebir).
const ACTIVE = 'status-active'
const SUCCESS = 'status-success'
const INFO = 'status-info'
const WARNING = 'status-warning'
const NEGATIVE = 'status-negative'
const NEUTRAL = 'status-neutral'
const FALLBACK = 'grey-7'

describe('StatusBadge — aşama tonu', () => {
  it.each([
    // Denetimde griye düşerken yakalanan dört sayfa (#işletme değerlendirme, beceri sınavı,
    // etkinlik raporu, dönem not fişi) — her satır o sayfanın rozetidir.
    ['Uygun', ACTIVE], //           EvaluationResult.Suitable — olumlu
    ['Şartlı', WARNING], //         BusinessEvaluationsPage etiketi — dikkat
    ['Şartlı Uygun', WARNING], //   EvaluationResult.Conditional slug'ı — aynı sonuç
    ['Uygun Değil', NEGATIVE], //   EvaluationResult.Unsuitable
    ['Başarılı', SUCCESS], //       ExamResult.Passed — terminal başarı
    ['Başarısız', NEGATIVE], //     ExamResult.Failed
    ['Gönderildi', INFO], //        ReportStatus/VisitStatus/StudentTermGradeStatus.Submitted
    ['Kesinleşti', SUCCESS], //     StudentTermGradeStatus.Finalized — terminal
    ['Yok', NEUTRAL], //            HealthReportStatus.None — yokluk bildirimi
    ['Örgün', NEUTRAL], //          EducationType.Formal — kategori rozeti (DESIGN.md)
    ['MESEM', NEUTRAL], //          EducationType.Mesem — kategori rozeti
  ])('%s slug\'ı %s tonuyla render edilir', (slug, expected) => {
    // Act
    const color = renderedColor(slug as string)

    // Assert
    expect(color, `'${slug}' yanlış tonda render edildi — anlam kayması`).toBe(expected)
  })

  it('rozet etiketi her zaman slug metnidir — renk yalnız ikincil sinyal', () => {
    // Arrange & Act
    const wrapper = renderBadge('Başarısız')

    // Assert
    expect(wrapper.get('.q-badge-stub').text()).toBe('Başarısız')
  })

  it('eşleşmeyen slug grey-7 fallbackine düşer', () => {
    // Act
    const color = renderedColor('Tanımsız Bir Durum')

    // Assert
    expect(color).toBe(FALLBACK)
  })
})

describe('StatusBadge — eşleşmeyen slug uyarısı', () => {
  beforeEach(() => {
    vi.mocked(logger.warn).mockClear()
  })

  it('eşleşmeyen slug için geliştirme ortamında uyarı loglanır', () => {
    // Act
    renderBadge('Haritada Olmayan Durum')

    // Assert
    expect(logger.warn).toHaveBeenCalledTimes(1)
    expect(vi.mocked(logger.warn).mock.calls[0]?.[0]).toContain('Haritada Olmayan Durum')
  })

  it('aynı slug tekrar render edilse de uyarı bir kez yazılır', () => {
    // Arrange — modül kapsamındaki kayıt sayesinde ikinci render sessiz kalmalı.
    renderBadge('Tekrarlayan Bilinmeyen')
    vi.mocked(logger.warn).mockClear()

    // Act
    renderBadge('Tekrarlayan Bilinmeyen')
    renderBadge('Tekrarlayan Bilinmeyen')

    // Assert
    expect(logger.warn).not.toHaveBeenCalled()
  })

  it('boş slug uyarı üretmez — seçim yokken beklenen yoldur', () => {
    // Act
    renderBadge('')

    // Assert
    expect(logger.warn).not.toHaveBeenCalled()
  })

  it('haritada olan slug uyarı üretmez', () => {
    // Act
    renderBadge('Başarılı')

    // Assert
    expect(logger.warn).not.toHaveBeenCalled()
  })
})

// ── Gri tonların kontrastı — WCAG metin eşiği kilidi ───────────────────────────────────
//
// NEDEN VAR: `q-badge` metni HER ZAMAN beyazdır (`quasar/src/components/badge/QBadge.sass`
// → `color: #fff`), bileşen yalnız zemin tonunu seçer. Zemin yeterince koyu değilse rozet
// okunur GÖRÜNÜR ama beyaz metin eşiği geçmez. `DRAFT` tam bu yüzden `grey-6` (#9e9e9e,
// 2,68:1) ile kalmıştı — 'Taslak' ve 'Pasif' rozetleri WCAG 1.4.3'ü karşılamıyordu.
//
// NEDEN HEX'LER DOSYADAN OKUNUYOR: Quasar'ın gri ölçeği Material'ınkinden BİR BASAMAK
// KAYIKTIR — Material'da grey-700 = #616161, Quasar'da o değer grey-8'dir. Bu depoda iki
// ayrı denetim aynı sınıf için farklı hex bildirdi. Ezberden yazılmış bir tablo aynı hatayı
// tekrarlar; bu yüzden tablo Quasar'ın kendi `variables.sass` dosyasıyla karşılaştırılır ve
// bir Quasar yükseltmesi ölçeği kaydırırsa test kırılır.
const QUASAR_VARIABLES_PATH = resolve(
  REPO_ROOT,
  'src/WebUI/node_modules/quasar/src/css/variables.sass',
)

/** `$grey-8 : #616161 !default` → `#616161`. `$grey` ile `$grey-8` birbirine karışmaz. */
function readQuasarGrey(name: string): string {
  const source = readFileSync(QUASAR_VARIABLES_PATH, 'utf8')
  const match = new RegExp(`^\\$${name}\\s*:\\s*(#[0-9a-fA-F]{6})`, 'm').exec(source)
  if (!match) {
    throw new Error(
      `Quasar variables.sass içinde $${name} bulunamadı — paket düzeni değişmiş olabilir: ` +
        QUASAR_VARIABLES_PATH,
    )
  }
  return (match[1] as string).toLowerCase()
}

/** sRGB relative luminance (WCAG 2.x). */
function relativeLuminance(hex: string): number {
  const channel = (value: number): number => {
    const v = value / 255
    return v <= 0.03928 ? v / 12.92 : ((v + 0.055) / 1.055) ** 2.4
  }
  const [r, g, b] = [1, 3, 5].map((offset) => parseInt(hex.slice(offset, offset + 2), 16)) as [
    number,
    number,
    number,
  ]
  return 0.2126 * channel(r) + 0.7152 * channel(g) + 0.0722 * channel(b)
}

function contrastRatio(a: string, b: string): number {
  const [light, dark] = [relativeLuminance(a), relativeLuminance(b)].sort((x, y) => y - x) as [
    number,
    number,
  ]
  return (light + 0.05) / (dark + 0.05)
}

const WHITE = '#ffffff'
/** WCAG 1.4.3 — gövde/etiket metni (rozet etiketi 14px normal ağırlık). */
const TEXT_CONTRAST_THRESHOLD = 4.5

/** StatusBadge.vue'deki `const DRAFT = 'grey-8'` gibi ton sabitlerini kaynaktan okur. */
function readColorConstant(name: string): string {
  const source = readFileSync(STATUS_BADGE_PATH, 'utf8')
  const match = new RegExp(`^const ${name} = '([^']+)'`, 'm').exec(source)
  if (!match) {
    throw new Error(
      `StatusBadge.vue içinde '${name}' ton sabiti bulunamadı — yeniden adlandırıldıysa ` +
        'bu testteki desen de güncellenmelidir.',
    )
  }
  return match[1] as string
}

describe('StatusBadge — Quasar gri ölçeği', () => {
  // Hex tablosu Quasar'ın kendi dosyasıyla, oranlar yukarıdaki formülle doğrulanır.
  // Beklenen oranlar bu satırlar yazılırken hesaplandı (2 ondalık).
  it.each([
    ['grey-5', '#bdbdbd', 1.88], //   metin olarak KULLANILAMAZ
    ['grey-6', '#9e9e9e', 2.68], //   metin olarak KULLANILAMAZ — DRAFT'ın eski tonu
    ['grey-7', '#757575', 4.61], //   eşiğe teğet; fallback tonu
    ['grey-8', '#616161', 6.19], //   DRAFT
    ['grey-9', '#424242', 10.05], //  CLOSED
  ])('Quasar %s tonu %s hex\'idir ve beyaz metinle %s:1 verir', (name, expectedHex, expectedRatio) => {
    // Arrange & Act
    const hex = readQuasarGrey(name as string)
    const ratio = contrastRatio(hex, WHITE)

    // Assert
    expect(hex, `Quasar ölçeği kaymış: $${name} artık ${hex}`).toBe(expectedHex)
    expect(ratio).toBeCloseTo(expectedRatio as number, 2)
  })
})

describe('StatusBadge — gri rozet tonları metin eşiğini geçer', () => {
  it.each([
    ['DRAFT', 'grey-8', '#616161', 6.19], //   'Taslak' / 'Pasif'
    ['CLOSED', 'grey-9', '#424242', 10.05], // 'Kapatılmış'
  ])(
    '%s sabiti %s tonundadır ve beyaz metinle 4,5:1 eşiğini geçer',
    (constantName, expectedTone, expectedHex, expectedRatio) => {
      // Arrange
      const tone = readColorConstant(constantName as string)
      const hex = readQuasarGrey(expectedTone as string)

      // Act
      const ratio = contrastRatio(hex, WHITE)

      // Assert
      expect(tone, `${constantName} tonu değiştirildi — kontrastı yeniden ölçün`).toBe(expectedTone)
      expect(hex).toBe(expectedHex)
      expect(ratio).toBeCloseTo(expectedRatio as number, 2)
      expect(
        ratio,
        `${constantName} (${expectedTone} = ${hex}) beyaz rozet metniyle ${ratio.toFixed(2)}:1 ` +
          'veriyor; WCAG 1.4.3 metin eşiği 4,5:1.',
      ).toBeGreaterThanOrEqual(TEXT_CONTRAST_THRESHOLD)
    },
  )

  it.each([
    ['Taslak', 'grey-8'],
    ['Pasif', 'grey-8'],
    ['Kapatılmış', 'grey-9'],
  ])('%s slug\'ı %s tonuyla render edilir', (slug, expected) => {
    // Act
    const color = renderedColor(slug as string)

    // Assert
    expect(color, `'${slug}' yanlış tonda render edildi — kontrast kilidi delinir`).toBe(expected)
  })

  it('DRAFT ile CLOSED ayrı tonlardır — ölçülen ayrım 1,62:1', () => {
    // Arrange
    const draft = readQuasarGrey('grey-8') //  #616161
    const closed = readQuasarGrey('grey-9') // #424242

    // Act
    const separation = contrastRatio(draft, closed)

    // Assert — DRAFT grey-6 iken ayrım 3,75:1 idi; eşiği geçen tona taşımak onu 1,62:1'e
    // indirdi. Fark hâlâ gözle seçilir ama WCAG 1.4.11 grafik eşiğinin (3:1) altındadır ve
    // olmak zorunda değildir: rozet her zaman etiket metni taşır ('Taslak'/'Pasif' ↔
    // 'Kapatılmış'), renk ikincil sinyaldir (DESIGN.md "Renk Yalnız Kanıt Kuralı").
    // Bu satır ayrımı KAYDA GEÇİRİR: iki tonu daha da yaklaştıran değişiklik testi kırar.
    expect(draft).not.toBe(closed)
    expect(separation).toBeCloseTo(1.62, 2)
  })
})
