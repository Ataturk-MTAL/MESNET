import { ref } from 'vue'
import { watchThrottled } from '@vueuse/core'
import { enrollmentApi } from 'src/api/enrollment'
import { businessApi } from 'src/api/business'
import { securityApi } from 'src/api/security'
import { institutionApi, type FieldOfStudyDto, type InstitutionBranchDto } from 'src/api/institution'
import { useAuthStore } from 'stores/auth'

export interface SelectOption {
  label: string
  value: string
  caption?: string
}

// ── İşletme Seçimi ──
export function useBusinessOptions() {
  const options = ref<SelectOption[]>([])
  const allOptions = ref<SelectOption[]>([])
  const loading = ref(false)
  let loaded = false

  async function load() {
    if (loaded) return
    loading.value = true
    try {
      const res = await businessApi.list('Approved')
      allOptions.value = (res.data ?? []).map((b) => ({
        label: b.name,
        value: b.id,
        caption: b.address,
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

  function reset() {
    loaded = false
    options.value = []
    allOptions.value = []
  }

  return { options, allOptions, loading, load, filter, reset }
}

// ── Öğrenci Seçimi ──
export function useStudentOptions() {
  const options = ref<SelectOption[]>([])
  const allOptions = ref<SelectOption[]>([])
  const loading = ref(false)
  let loaded = false

  async function load(params?: { institutionId?: string; branchCode?: string }) {
    if (loaded) return
    loading.value = true
    try {
      const res = await enrollmentApi.listStudents(params)
      allOptions.value = (res.data ?? []).map((s) => ({
        label: s.fullName,
        value: s.id,
        caption: `${s.branchCode} · ${s.classYear}/${s.section ?? '—'}`,
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

  function reset() {
    loaded = false
    options.value = []
    allOptions.value = []
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
      const res = await enrollmentApi.listPlacements({
        ...params,
        status: params?.status ?? 'Matched',
      })
      allOptions.value = (res.data ?? []).map((p) => ({
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

// ── Öğretmen Seçimi ──
export function useTeacherOptions() {
  const options = ref<SelectOption[]>([])
  const allOptions = ref<SelectOption[]>([])
  const loading = ref(false)
  let loaded = false

  async function load(params?: { institutionId?: string; academicPeriodId?: string }) {
    if (loaded) return
    loading.value = true
    try {
      const res = await enrollmentApi.listTeachers(params)
      allOptions.value = (res.data ?? []).map((t) => ({
        label: t.fullName,
        value: t.id,
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

  return { options, allOptions, loading, load, filter, reset }
}

// ── Keycloak Kullanıcı Seçimi ──
export function useKeycloakUserOptions() {
  const options = ref<SelectOption[]>([])
  const allOptions = ref<SelectOption[]>([])
  const loading = ref(false)
  let loaded = false

  async function load(params?: { role?: string; institutionId?: string }) {
    if (loaded) return
    loading.value = true
    try {
      const res = await securityApi.listUsers(params)
      allOptions.value = (res.data ?? []).map((u) => ({
        label: u.fullName,
        value: u.keycloakUserId,
        caption: `${u.email} (${u.username})`,
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

  function reset() {
    loaded = false
    options.value = []
    allOptions.value = []
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

  // Field catalog — specialization isim çözümlemesi için
  let _fields: FieldOfStudyDto[] = []
  let _branches: InstitutionBranchDto[] = []

  async function load() {
    if (loaded) return
    const authStore = useAuthStore()
    const instId = authStore.user?.institutionId
    if (!instId) {
      console.warn('[useBranchOptions] institutionId bulunamadı.')
      return
    }
    loading.value = true
    try {
      const [instRes, catalogRes] = await Promise.all([
        institutionApi.get(instId),
        institutionApi.getFieldCatalog(),
      ])
      _branches = instRes.data.branches?.filter((b) => b.isActive) ?? []
      _fields = catalogRes.data ?? []
      allOptions.value = _branches.map((b) => ({
        label: `${b.fieldCode} — ${b.fieldName}`,
        value: b.fieldCode,
      }))
      options.value = allOptions.value
      loaded = true
    } catch (err) {
      console.error('[useBranchOptions] Alan seçenekleri yüklenirken hata:', err)
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
    const branch = _branches.find((b) => b.fieldCode === branchCode)
    if (!branch || !branch.activeSpecializations.length) return []
    const field = _fields.find((f) => f.code === branchCode)
    if (!field) return []
    return branch.activeSpecializations.map((specCode) => {
      const spec = field.specializations.find((s) => s.code === specCode)
      return { label: spec?.name ?? specCode, value: specCode }
    })
  }

  /** Branch code'dan fieldName döndürür */
  function getFieldName(branchCode: string): string {
    return _branches.find((b) => b.fieldCode === branchCode)?.fieldName ?? ''
  }

  function reset() {
    loaded = false
    searchQuery.value = ''
    options.value = []
    allOptions.value = []
    _fields = []
    _branches = []
  }

  return { options, allOptions, loading, searchQuery, load, filter, getSpecializations, getFieldName, reset }
}
