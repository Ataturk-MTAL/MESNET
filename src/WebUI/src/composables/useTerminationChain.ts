import { ref, type Ref } from 'vue'
import {
  internshipApi,
  type InternshipSummaryDto,
  type TerminationChainStatusDto,
  type TerminationStepDto,
} from 'src/api/internship'
import { useAuthStore } from 'stores/auth'

/** Zincirin kanonik adım sırası — görüntüleme içindir, dayatma değildir. */
const KANONIK = ['Parent', 'Teacher', 'Deputy', 'Director', 'BusinessRep']

const SLUGLAR: Record<string, string> = {
  Parent: 'Veli',
  Teacher: 'Koordinatör Öğretmen',
  Deputy: 'Müdür Yardımcısı',
  Director: 'Müdür',
  BusinessRep: 'İşletme Yetkilisi',
}

/**
 * Fesih onay zinciri okuma ve ilerletme (#191).
 *
 * Hem okul tarafındaki `TerminationsPage` hem veli/işletme tarafındaki `MyApprovalsPage`
 * aynı işi yapar: zinciri oku, kullanıcının yapabildiği adımı sun. Ekran farklı, mantık
 * aynı — iki yere kopyalansaydı biri düzeltilip diğeri unutulurdu.
 *
 * **İzin kararı sunucudan gelir.** Her adım kendi `permission` ve `endpoint` alanını taşır;
 * burada adım→izin eşlemesi tutulmaz (ADR-0001).
 */
export function useTerminationChain() {
  const authStore = useAuthStore()

  /**
   * Staj kimliği → zincir durumu.
   *
   * Liste DTO'su zinciri taşımıyor (zincir saga state'inde yaşıyor, read-model'de yok), bu
   * yüzden görünen satırlar için ayrı ayrı okunuyor. Sayfa başına ~20 küçük istek; liste
   * büyürse doğru çözüm zinciri read-model'e denormalize etmektir.
   */
  const chains = ref<Record<string, TerminationChainStatusDto>>({})
  const acting = ref(false)

  async function loadChains(items: InternshipSummaryDto[]): Promise<void> {
    const sonuclar = await Promise.allSettled(
      items.map((i) =>
        internshipApi.getTerminationChain(i.id).then((r) => [i.id, r.data] as const),
      ),
    )

    const yeni: Record<string, TerminationChainStatusDto> = {}
    for (const s of sonuclar) if (s.status === 'fulfilled') yeni[s.value[0]] = s.value[1]
    chains.value = yeni
  }

  function chainOf(id: string): TerminationChainStatusDto | undefined {
    return chains.value[id]
  }

  function pendingOf(id: string): TerminationStepDto[] {
    return chains.value[id]?.pendingSteps ?? []
  }

  /** Bu kullanıcının **yapabildiği** bekleyen adımlar — buton görünürlüğü buna bakar. */
  function actionableSteps(id: string): TerminationStepDto[] {
    return pendingOf(id).filter((s) => !!s.permission && authStore.hasPermission(s.permission))
  }

  /**
   * Panelde gösterilecek tüm adımlar — onaylananlar dâhil.
   *
   * Sunucu yalnız bekleyenleri gönderir; tamamlananlar ham bayraklardan çıkarılır. Bayrak→ad
   * eşlemesi burada zorunlu, ama **izin bilgisi** yine sunucudan gelen adımdan okunur.
   */
  function allSteps(status: TerminationChainStatusDto | null): TerminationStepDto[] {
    if (!status?.chain) return []

    const c = status.chain
    const onayli: Array<[string, boolean]> = [
      ['Parent', c.parentApproved && status.requiresParentApproval],
      ['Teacher', c.teacherApproved],
      ['Deputy', c.deputyApproved],
      ['Director', c.directorApproved],
      ['BusinessRep', c.businessRepApproved],
    ]

    const tamamlanan: TerminationStepDto[] = onayli
      .filter(([, verildi]) => verildi)
      .map(([name]) => ({ name, slug: SLUGLAR[name] ?? name, endpoint: '', permission: '' }))

    return [...tamamlanan, ...status.pendingSteps].sort(
      (a, b) => KANONIK.indexOf(a.name) - KANONIK.indexOf(b.name),
    )
  }

  function isApproved(status: TerminationChainStatusDto | null, step: TerminationStepDto): boolean {
    return !(status?.pendingSteps ?? []).some((s) => s.name === step.name)
  }

  function canDo(step: TerminationStepDto): boolean {
    return !!step.permission && authStore.hasPermission(step.permission)
  }

  async function refresh(internshipId: string, target: Ref<TerminationChainStatusDto | null>) {
    const res = await internshipApi.getTerminationChain(internshipId)
    target.value = res.data
    chains.value = { ...chains.value, [internshipId]: res.data }
  }

  return {
    chains,
    acting,
    loadChains,
    chainOf,
    pendingOf,
    actionableSteps,
    allSteps,
    isApproved,
    canDo,
    refresh,
  }
}
