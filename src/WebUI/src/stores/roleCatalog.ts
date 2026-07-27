import { ref, computed } from 'vue'
import { defineStore } from 'pinia'
import { securityApi, type RolePermissionsDto } from 'src/api/security'
import type { SelectOption } from 'src/composables/useEntityOptions'

/**
 * Rol kataloğu — **arayüzün tek doğruluk kaynağı** (#129).
 *
 * Rol listesi ve Türkçe etiketler `GET /api/security/roles` ucundan gelir; hiçbir sayfa
 * kendi rol listesini veya etiket haritasını tutmaz. Önceden UserManagementPage elle
 * yazılmış bir liste taşıyordu ve o liste gerçek rollerle eşleşmiyordu: karşılığı olmayan
 * adlar (`deputy_director`, `coordinator_teacher`, `master_trainer`) sunucuya gidiyor,
 * Keycloak'ta çözülemediği için kullanıcı sıfır realm rolüyle — hiçbir izin almadan,
 * hiçbir hata görmeden — açılıyordu.
 *
 * Katalog statiktir (sunucuda sabit liste); bir kez yüklenir ve paylaşılır.
 */
export const useRoleCatalogStore = defineStore('roleCatalog', () => {
  const roles = ref<RolePermissionsDto[]>([])
  const loading = ref(false)
  const loaded = ref(false)

  async function load(force = false): Promise<void> {
    if (loaded.value && !force) return
    loading.value = true
    try {
      const { data } = await securityApi.listRoles()
      roles.value = data ?? []
      loaded.value = true
    } finally {
      loading.value = false
    }
  }

  /** q-select / q-option-group için hazır seçenekler. `value` = backend rol adı. */
  const options = computed<SelectOption[]>(() =>
    roles.value.map((r) => ({
      label: r.label,
      value: r.roleName,
      caption: r.description,
    })),
  )

  const labelByName = computed<Record<string, string>>(() =>
    Object.fromEntries(roles.value.map((r) => [r.roleName, r.label])),
  )

  /**
   * Rol adının Türkçe etiketi. Katalog henüz yüklenmemişse veya rol tanınmıyorsa ham ad
   * döner — bilinmeyen bir rolü gizlemek yerine göstermek doğrudur (bozuk kayıt belirtisi).
   */
  function labelFor(roleName: string): string {
    return labelByName.value[roleName] ?? roleName
  }

  function invalidate(): void {
    loaded.value = false
  }

  return { roles, loading, loaded, options, labelFor, load, invalidate }
})
