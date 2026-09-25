import { ref, computed } from 'vue'
import { defineStore } from 'pinia'
import { useAuthStore } from './auth'

interface Selection {
  /** Seçimin yapıldığı okul — bağlam değişince seçim geçersizleşir. */
  institutionId: string | null
  branchCode: string | null
  teacherId: string | null
}

const EMPTY: Selection = { institutionId: null, branchCode: null, teacherId: null }

/**
 * Sayfalar arası korunan alan (branş) ve öğretmen seçimi.
 *
 * <p><b>Yerel filtre kuralının bilinçli istisnası:</b> filtre durumu normalde sayfada kalır.
 * Alan ve öğretmen ise koordinasyon akışının ekseni: dağıtım → ders programı → saat ayarı
 * arasında gezen kullanıcı her ekranda aynı seçimi yeniden yapmak zorunda kalıyordu.</p>
 *
 * <p><b>Seçim okula aittir.</b> Kiminle yapıldığı saklanır; aktif bağlam başka okula geçince
 * seçim görünmez. Aksi hâlde önceki okulun öğretmen kimliği yeni okulda seçili kalır ve
 * istekler boş/yanlış sonuç döner.</p>
 */
export const useSelectionStore = defineStore('selection', () => {
  const authStore = useAuthStore()
  const state = ref<Selection>(EMPTY)

  const isCurrent = computed(() => state.value.institutionId === authStore.currentInstitutionId)

  const branchCode = computed(() => (isCurrent.value ? state.value.branchCode : null))
  const teacherId = computed(() => (isCurrent.value ? state.value.teacherId : null))

  /** Alan değişirse öğretmen düşer — öğretmen listesi alana göre süzülür. */
  function setBranch(code: string | null): void {
    const isSameBranch = isCurrent.value && state.value.branchCode === code
    state.value = {
      institutionId: authStore.currentInstitutionId,
      branchCode: code,
      teacherId: isSameBranch ? state.value.teacherId : null,
    }
  }

  function setTeacher(id: string | null): void {
    state.value = {
      institutionId: authStore.currentInstitutionId,
      branchCode: branchCode.value,
      teacherId: id,
    }
  }

  function clear(): void {
    state.value = EMPTY
  }

  return { branchCode, teacherId, setBranch, setTeacher, clear }
})
