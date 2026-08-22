import { ref, computed, unref, type MaybeRef } from 'vue'
import { watchThrottled } from '@vueuse/core'
import { enrollmentApi } from 'src/api/enrollment'
import { useInstitutionStore } from 'stores/institution'
import { useEntityOptionsStore } from 'stores/entityOptions'
import type { PagedResponse } from 'src/types/pagination'
import { logger } from '../utils/logger'

export interface SelectOption {
  label: string
  value: string
  caption?: string
}

/**
 * Sayfalı bir listeleme endpoint'inin TÜM kayıtlarını sayfa sayfa çeker.
 * Seçim listelerinin tek sayfa (pageSize:100) ile sessizce kırpılmasını önler;
 * label/lookup çözümlemesi için tüm kayıtlar yerelde tutulur (q-select use-input + map-options).
 */
async function fetchAllItems<T>(
  fetchPage: (page: number, pageSize: number) => Promise<{ data: PagedResponse<T> }>,
): Promise<T[]> {
  const pageSize = 100
  const all: T[] = []
  let page = 1
  // Güvenlik sınırı: en çok 100 sayfa — sonsuz döngü koruması
  for (let i = 0; i < 100; i++) {
    const res = await fetchPage(page, pageSize)
    const items = res.data?.items ?? []
    all.push(...items)
    if (!res.data?.hasNextPage || items.length === 0) break
    page++
  }
  return all
}

export interface BusinessOption extends SelectOption {
  /** İşletmenin öğrenci alabildiği AKTİF alan kodları (#119). Boş liste = hiçbir alandan alamaz. */
  authorizedBranches: string[]
}

/**
 * Öğrencinin alanından öğrenci almaya yetkili işletmeleri süzer (#119).
 * `branchCode` verilmezse liste olduğu gibi döner (yetki bağlamı olmayan ekranlar).
 */
export function filterBusinessesByBranch(
  businesses: BusinessOption[],
  branchCode?: string | null,
): BusinessOption[] {
  if (!branchCode) return businesses
  const needle = branchCode.trim().toLocaleLowerCase('tr')
  if (!needle) return businesses
  return businesses.filter((b) =>
    b.authorizedBranches.some((c) => c.trim().toLocaleLowerCase('tr') === needle),
  )
}

export interface UseBusinessOptionsOptions {
  /** Verilirse yalnız bu alandan öğrenci almaya yetkili işletmeler listelenir. */
  branchCode?: MaybeRef<string | null | undefined>
}

// ── İşletme Seçimi ── (store-backed cache, per-component filtrelenmiş görünüm)
export function useBusinessOptions(opts: UseBusinessOptionsOptions = {}) {
  const store = useEntityOptionsStore()
  const options = ref<BusinessOption[]>([])
  const allOptions = computed(() => filterBusinessesByBranch(store.businesses, unref(opts.branchCode)))
  const loading = computed(() => store.businessesLoading)

  async function load() {
    await store.loadBusinesses()
    options.value = allOptions.value
  }

  function filter(val: string, update: (fn: () => void) => void) {
    update(() => {
      const needle = val.toLowerCase()
      options.value = needle
        ? allOptions.value.filter(
            (o) =>
              o.label.toLowerCase().includes(needle) ||
              (o.caption?.toLowerCase().includes(needle) ?? false),
          )
        : allOptions.value
    })
  }

  function reset() {
    options.value = []
  }

  return { options, allOptions, loading, load, filter, reset }
}

// ── Öğrenci Seçimi ── (store-backed cache, per-component filtrelenmiş görünüm)
export function useStudentOptions() {
  const store = useEntityOptionsStore()
  const options = ref<SelectOption[]>([])
  const allOptions = computed(() => store.students)
  const loading = computed(() => store.studentsLoading)

  // params imzası korunur (tüketici uyumluluğu) ancak store sabit tam-listeyi çeker — params yok sayılır
  async function load(_params?: { institutionId?: string; branchCode?: string }) {
    await store.loadStudents()
    options.value = store.students
  }

  function filter(val: string, update: (fn: () => void) => void) {
    update(() => {
      const needle = val.toLowerCase()
      options.value = needle
        ? store.students.filter(
            (o) =>
              o.label.toLowerCase().includes(needle) ||
              (o.caption?.toLowerCase().includes(needle) ?? false),
          )
        : store.students
    })
  }

  function reset() {
    options.value = []
  }

  return { options, allOptions, loading, load, filter, reset }
}

// ── Yerleştirme Bazlı Öğrenci Seçimi (Devamsızlık Dialogu) ──
export interface PlacementOption {
  label: string
  value: string
  businessId: string
  businessName: string
  caption?: string
}

export function usePlacementOptions() {
  const options = ref<PlacementOption[]>([])
  const allOptions = ref<PlacementOption[]>([])
  const loading = ref(false)
  let loaded = false

  async function load(params?: { academicPeriodId?: string; status?: string }) {
    if (loaded) return
    loading.value = true
    try {
      const items = await fetchAllItems((page, pageSize) =>
        enrollmentApi.listPlacements({
          ...params,
          status: params?.status ?? 'Matched',
          page,
          pageSize,
        }),
      )
      allOptions.value = items.map((p) => ({
        label: p.studentName,
        value: p.studentId,
        businessId: p.businessId,
        businessName: p.businessName,
        caption: p.businessName,
      }))
      options.value = allOptions.value
      loaded = true
    } finally {
      loading.value = false
    }
  }

  function filter(val: string, update: (fn: () => void) => void) {
    update(() => {
      const needle = val.toLowerCase()
      options.value = needle
        ? allOptions.value.filter(
            (o) =>
              o.label.toLowerCase().includes(needle) ||
              (o.caption?.toLowerCase().includes(needle) ?? false),
          )
        : allOptions.value
    })
  }

  function getBusinessForStudent(studentId: string): { businessId: string; businessName: string } | null {
    const opt = allOptions.value.find((o) => o.value === studentId)
    return opt ? { businessId: opt.businessId, businessName: opt.businessName } : null
  }

  function reset() {
    loaded = false
    options.value = []
    allOptions.value = []
  }

  return { options, allOptions, loading, load, filter, getBusinessForStudent, reset }
}

export interface TeacherOption extends SelectOption {
  branchCode?: string | null
}

// ── Öğretmen Seçimi ──
export function useTeacherOptions() {
  const options = ref<TeacherOption[]>([])
  const allOptions = ref<TeacherOption[]>([])
  const loading = ref(false)
  let loaded = false

  async function load(params?: { institutionId?: string; academicPeriodId?: string }) {
    if (loaded) return
    await reload(params)
  }

  /** Branş değişince veya force yenileme gerektiğinde çağır — her zaman API isteği atar */
  async function reload(params?: { institutionId?: string; academicPeriodId?: string; branchCode?: string }) {
    loading.value = true
    try {
      const items = await fetchAllItems((page, pageSize) =>
        enrollmentApi.listTeachers({ ...params, page, pageSize }),
      )
      allOptions.value = items.map((t) => ({
        label: t.fullName,
        value: t.id,
        branchCode: t.branchCode ?? null,
      }))
      options.value = allOptions.value
      loaded = true
    } finally {
      loading.value = false
    }
  }

  function filter(val: string, update: (fn: () => void) => void) {
    update(() => {
      const needle = val.toLowerCase()
      options.value = needle
        ? allOptions.value.filter((o) => o.label.toLowerCase().includes(needle))
        : allOptions.value
    })
  }

  function reset() {
    loaded = false
    options.value = []
    allOptions.value = []
  }

  return { options, allOptions, loading, load, reload, filter, reset }
}

// ── Keycloak Kullanıcı Seçimi ── (store-backed cache, per-component filtrelenmiş görünüm)
export function useKeycloakUserOptions() {
  const store = useEntityOptionsStore()
  const options = ref<SelectOption[]>([])
  const allOptions = computed(() => store.keycloakUsers)
  const loading = computed(() => store.keycloakUsersLoading)

  // params imzası korunur (tüketici uyumluluğu) ancak store sabit tam-listeyi çeker — params yok sayılır
  async function load(_params?: { role?: string; institutionId?: string }) {
    await store.loadKeycloakUsers()
    options.value = store.keycloakUsers
  }

  function filter(val: string, update: (fn: () => void) => void) {
    update(() => {
      const needle = val.toLowerCase()
      options.value = needle
        ? store.keycloakUsers.filter(
            (o) =>
              o.label.toLowerCase().includes(needle) ||
              (o.caption?.toLowerCase().includes(needle) ?? false),
          )
        : store.keycloakUsers
    })
  }

  function reset() {
    options.value = []
  }

  return { options, allOptions, loading, load, filter, reset }
}

// ── Alan (Branch) + Dal (Specialization) Seçimi ──
export interface SpecOption {
  label: string
  value: string
}

export function useBranchOptions() {
  const options = ref<SelectOption[]>([])
  const allOptions = ref<SelectOption[]>([])
  const loading = ref(false)
  const searchQuery = ref('')
  let loaded = false

  const institutionStore = useInstitutionStore()

  async function load() {
    if (loaded) return
    loading.value = true
    try {
      await Promise.all([
        institutionStore.loadInstitution(),
        institutionStore.loadFieldCatalog(),
      ])
      allOptions.value = institutionStore.activeBranches.map((b) => ({
        label: `${b.fieldCode} — ${b.fieldName}`,
        value: b.fieldCode,
      }))
      options.value = allOptions.value
      loaded = true
    } catch (err) {
      logger.error('[useBranchOptions] Alan seçenekleri yüklenirken hata:', err)
    } finally {
      loading.value = false
    }
  }

  // Throttled arama — her tuşa basmada değil, 300ms aralıklarla filtrele
  watchThrottled(searchQuery, (needle) => {
    const q = needle.toLocaleLowerCase('tr')
    options.value = q
      ? allOptions.value.filter((o) => o.label.toLocaleLowerCase('tr').includes(q))
      : allOptions.value
  }, { throttle: 300 })

  function filter(val: string, update: (fn: () => void) => void) {
    searchQuery.value = val
    update(() => {})
  }

  /** Seçili branch code'a göre dal (specialization) seçenekleri döndürür */
  function getSpecializations(branchCode: string): SpecOption[] {
    const branch = institutionStore.activeBranches.find((b) => b.fieldCode === branchCode)
    if (!branch || !branch.activeSpecializations.length) return []
    const field = institutionStore.fieldCatalog.find((f) => f.code === branchCode)
    if (!field) return []
    return branch.activeSpecializations.map((specCode) => {
      const spec = field.specializations.find((s) => s.code === specCode)
      return { label: spec?.name ?? specCode, value: specCode }
    })
  }

  /** Branch code'dan fieldName döndürür */
  function getFieldName(branchCode: string): string {
    return institutionStore.activeBranches.find((b) => b.fieldCode === branchCode)?.fieldName ?? ''
  }

  function reset() {
    loaded = false
    searchQuery.value = ''
    options.value = []
    allOptions.value = []
  }

  return { options, allOptions, loading, searchQuery, load, filter, getSpecializations, getFieldName, reset }
}
