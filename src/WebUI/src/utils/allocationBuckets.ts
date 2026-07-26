import type { AllocationBucketName } from 'src/api/coordination'

/**
 * Saat dağıtım önerisindeki üç kovanın ekran gösterimi (#116/#118).
 *
 * Renkler #104'teki anlamsal tonlardan gelir (bkz. `src/assets/app.css`); ham Quasar
 * palet renkleri (orange-8, blue-6 ...) KULLANILMAZ.
 *
 * Ayrım **yalnız renkle taşınmaz**: her rozet Türkçe etiketini de basar, açıklaması
 * `q-tooltip` ile verilir. Renk körü bir koordinatör kovaları yine ayırt edebilmelidir.
 */
export interface AllocationBucketPresentation {
  /** Rozet metni — ayrımın renkten bağımsız taşıyıcısı. */
  label: string
  /** `q-badge color` — `.bg-*-soft` sınıfına karşılık gelir. */
  color: string
  /** `q-badge text-color` — soft zemin üzerinde >= 4,5:1 kontrast verir. */
  textColor: string
  /** Rozetin `q-tooltip` açıklaması. */
  hint: string
}

const PRESENTATIONS: Record<AllocationBucketName, AllocationBucketPresentation> = {
  InBranchPaid: {
    label: 'Alan içi',
    color: 'info-soft',
    textColor: 'info-strong',
    hint: 'Alan içi ücretli — alanın kendi öğretmeninin boş saati bu işletmeye yetiyor',
  },
  OutOfBranchSuggested: {
    label: 'Alan dışı öneri',
    color: 'warning-soft',
    textColor: 'warning-strong',
    hint:
      'Havuzda saat var ama alanın öğretmeninde boş saat kalmadı — ' +
      'bu işletme alan dışı bir öğretmene önerilir',
  },
  Honorary: {
    label: 'Fahri',
    color: 'neutral-soft',
    textColor: 'neutral-strong',
    hint: 'Havuz yetmedi — fahri ziyaret: öğretmen gider, ek ders ücreti doğmaz',
  },
}

/** Bilinmeyen kova adı geldiğinde nötr rozet — sessizce boş bırakılmaz. */
const UNKNOWN: AllocationBucketPresentation = {
  label: 'Bilinmeyen kova',
  color: 'neutral-soft',
  textColor: 'neutral-strong',
  hint: 'Sunucu tanınmayan bir kova adı döndürdü.',
}

export function bucketPresentation(
  bucket: AllocationBucketName | undefined | null,
): AllocationBucketPresentation | null {
  if (!bucket) return null
  return PRESENTATIONS[bucket] ?? UNKNOWN
}
