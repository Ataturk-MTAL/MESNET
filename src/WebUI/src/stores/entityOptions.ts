import { ref } from 'vue'
import { defineStore } from 'pinia'
import { enrollmentApi } from 'src/api/enrollment'
import { businessApi } from 'src/api/business'
import { securityApi } from 'src/api/security'
import type { PagedResponse } from 'src/types/pagination'
import type { SelectOption } from 'src/composables/useEntityOptions'

/**
 * Seçim listeleri için merkezi cache (öğrenci / işletme / Keycloak kullanıcı).
 * Tüm tam-liste seçenekleri bir kez yüklenip component'ler arası paylaşılır (loaded guard) —
 * formlar her açılışta tüm listeyi yeniden çekmez. İlgili entity oluşturulduğunda/değiştiğinde
 * invalidate* ile cache geçersiz kılınır → bir sonraki erişim taze çeker (bayat dropdown olmaz).
 *
 * NOT: Öğretmen seçimi (param-bağımlı branş filtresi + küçük liste) ve placement (döneme bağlı)
 * kasıtlı olarak burada DEĞİL — useEntityOptions içinde per-instance kalır.
 */

async function fetchAllItems<T>(
  fetchPage: (page: number, pageSize: number) => Promise<{ data: PagedResponse<T> }>,
): Promise<T[]> {
  const pageSize = 100
  const all: T[] = []
  let page = 1
  for (let i = 0; i < 100; i++) {
    const res = await fetchPage(page, pageSize)
    const items = res.data?.items ?? []
    all.push(...items)
    if (!res.data?.hasNextPage || items.length === 0) break
    page++
  }
  return all
}

export const useEntityOptionsStore = defineStore('entityOptions', () => {
  // ── Öğrenciler ──
  const students = ref<SelectOption[]>([])
  const studentsLoading = ref(false)
  const studentsLoaded = ref(false)

  async function loadStudents(force = false): Promise<void> {
    if (studentsLoaded.value && !force) return
    studentsLoading.value = true
    try {
      const items = await fetchAllItems((page, pageSize) =>
        enrollmentApi.listStudents({ page, pageSize }),
      )
      students.value = items.map((s) => ({
        label: s.fullName,
        value: s.id,
        caption: `${s.branchCode} · ${s.classYear}/${s.section ?? '—'}`,
      }))
      studentsLoaded.value = true
    } finally {
      studentsLoading.value = false
    }
  }

  function invalidateStudents(): void {
    studentsLoaded.value = false
  }

  // ── İşletmeler (onaylı) ──
  const businesses = ref<SelectOption[]>([])
  const businessesLoading = ref(false)
  const businessesLoaded = ref(false)

  async function loadBusinesses(force = false): Promise<void> {
    if (businessesLoaded.value && !force) return
    businessesLoading.value = true
    try {
      const items = await fetchAllItems((page, pageSize) =>
        businessApi.list({ status: 'Approved', page, pageSize }),
      )
      businesses.value = items.map((b: { name: string; id: string; address: string }) => ({
        label: b.name,
        value: b.id,
        caption: b.address,
      }))
      businessesLoaded.value = true
    } finally {
      businessesLoading.value = false
    }
  }

  function invalidateBusinesses(): void {
    businessesLoaded.value = false
  }

  // ── Keycloak kullanıcıları ──
  const keycloakUsers = ref<SelectOption[]>([])
  const keycloakUsersLoading = ref(false)
  const keycloakUsersLoaded = ref(false)

  async function loadKeycloakUsers(force = false): Promise<void> {
    if (keycloakUsersLoaded.value && !force) return
    keycloakUsersLoading.value = true
    try {
      const items = await fetchAllItems((page, pageSize) =>
        securityApi.listUsers({ page, pageSize }),
      )
      keycloakUsers.value = items.map((u) => ({
        label: u.fullName,
        value: u.keycloakUserId,
        caption: `${u.email} (${u.username})`,
      }))
      keycloakUsersLoaded.value = true
    } finally {
      keycloakUsersLoading.value = false
    }
  }

  function invalidateKeycloakUsers(): void {
    keycloakUsersLoaded.value = false
  }

  function clear(): void {
    students.value = []
    studentsLoaded.value = false
    businesses.value = []
    businessesLoaded.value = false
    keycloakUsers.value = []
    keycloakUsersLoaded.value = false
  }

  return {
    students,
    studentsLoading,
    loadStudents,
    invalidateStudents,
    businesses,
    businessesLoading,
    loadBusinesses,
    invalidateBusinesses,
    keycloakUsers,
    keycloakUsersLoading,
    loadKeycloakUsers,
    invalidateKeycloakUsers,
    clear,
  }
})
