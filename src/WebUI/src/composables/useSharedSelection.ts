import { computed, type WritableComputedRef } from 'vue'
import { useSelectionStore } from 'stores/selection'
import { useAuthStore } from 'stores/auth'

export interface UseSharedSelectionOptions {
  /**
   * Sayfa seçilen alana YAZIYOR mu (#126)? Öyleyse kullanıcının yazamadığı bir alan, başka
   * bir okuma sayfasında seçilmiş olsa bile burada seçili gelmez — BranchSelector o alanı
   * listelemez ve kayıt 422 ile düşerdi.
   */
  writeContext?: boolean
}

export interface SharedSelection {
  branchCode: WritableComputedRef<string | null>
  teacherId: WritableComputedRef<string | null>
}

/**
 * Sayfalar arası korunan alan/öğretmen seçimi — `v-model` ve `.value =` ile doğrudan kullanılır.
 * Sayfanın yerel `ref`'inin yerine geçer; handler'lar değişmez.
 */
export function useSharedSelection(options: UseSharedSelectionOptions = {}): SharedSelection {
  const selection = useSelectionStore()
  const authStore = useAuthStore()

  const branchCode = computed<string | null>({
    get: () => {
      const code = selection.branchCode
      if (!code || !options.writeContext) return code
      return authStore.canWriteBranch(code) ? code : null
    },
    set: (code) => selection.setBranch(code),
  })

  const teacherId = computed<string | null>({
    // Alan bu sayfada geçersizse öğretmeni de gösterme — öğretmen o alanın listesinden seçildi.
    get: () => (branchCode.value === selection.branchCode ? selection.teacherId : null),
    set: (id) => selection.setTeacher(id),
  })

  return { branchCode, teacherId }
}
