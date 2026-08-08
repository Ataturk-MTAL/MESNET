import { ref, type Ref } from 'vue'
import {
  internshipApi,
  type InternshipSummaryDto,
  type TerminationChainStatusDto,
  type TerminationStepDto,
} from 'src/api/internship'
import { useAuthStore } from 'stores/auth'

/**
 * Zincirin adım sırası — **dayatma** sırasıdır, sunucu da aynısını uygular (#218).
 *
 * Veli ve işletme yetkilisi burada yoktur: onlar fesih **talep eder**, onaylamaz.
 */
const STEP_ORDER = ['Teacher', 'Deputy', 'Director'] as const

const STEP_LABELS: Record<string, string> = {
  Teacher: 'Koordinatör Öğretmen',
  Deputy: 'Müdür Yardımcısı',
  Director: 'Müdür',
}

/** Panelde gösterilen tek bir adım — onaylandı mı, şimdi onaylanabilir mi. */
export interface ChainStepView {
  name: string
  label: string
  approved: boolean
  /** Sıradaki adım mı — yalnız bunun butonu etkindir. */
  isNext: boolean
  /** Sunucudan gelen adım tanımı; yalnız sıradaki adımda dolu olur. */
  step: TerminationStepDto | null
}

/**
 * Fesih onay zinciri okuma ve ilerletme (#191, #218).
 *
 * **İzin kararı sunucudan gelir.** Sıradaki adım kendi `permission` ve `endpoint` alanını
 * taşır; burada adım→izin eşlemesi tutulmaz (ADR-0001).
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
    const results = await Promise.allSettled(
      items.map((i) =>
        internshipApi.getTerminationChain(i.id).then((r) => [i.id, r.data] as const),
      ),
    )

    const next: Record<string, TerminationChainStatusDto> = {}
    for (const r of results) if (r.status === 'fulfilled') next[r.value[0]] = r.value[1]
    chains.value = next
  }

  function chainOf(id: string): TerminationChainStatusDto | undefined {
    return chains.value[id]
  }

  function nextStepOf(id: string): TerminationStepDto | null {
    return chains.value[id]?.nextStep ?? null
  }

  /** Sıradaki adımı bu kullanıcı yapabiliyor mu — buton görünürlüğü buna bakar. */
  function canActOn(id: string): boolean {
    const step = nextStepOf(id)
    return !!step && authStore.hasPermission(step.permission)
  }

  /** Panelde gösterilecek üç adım: onaylananlar işaretli, sıradaki etkin. */
  function stepViews(status: TerminationChainStatusDto | null): ChainStepView[] {
    if (!status?.chain) return []

    const c = status.chain
    const approvedByName: Record<string, boolean> = {
      Teacher: c.teacherApproved,
      Deputy: c.deputyApproved,
      Director: c.directorApproved,
    }

    return STEP_ORDER.map((name) => ({
      name,
      label: STEP_LABELS[name] ?? name,
      approved: approvedByName[name] ?? false,
      isNext: status.nextStep?.name === name,
      step: status.nextStep?.name === name ? status.nextStep : null,
    }))
  }

  function canDo(step: TerminationStepDto | null): boolean {
    return !!step && authStore.hasPermission(step.permission)
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
    nextStepOf,
    canActOn,
    stepViews,
    canDo,
    refresh,
  }
}
