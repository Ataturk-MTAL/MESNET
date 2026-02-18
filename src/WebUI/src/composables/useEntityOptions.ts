import { ref } from 'vue'
import { enrollmentApi } from 'src/api/enrollment'
import { businessApi } from 'src/api/business'
import { securityApi } from 'src/api/security'

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
        caption: `${s.branchName} - ${s.classYear}. Sınıf`,
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

// ── Öğretmen Seçimi ──
export function useTeacherOptions() {
  const options = ref<SelectOption[]>([])
  const allOptions = ref<SelectOption[]>([])
  const loading = ref(false)
  let loaded = false

  async function load(institutionId?: string) {
    if (loaded) return
    loading.value = true
    try {
      const res = await enrollmentApi.listTeachers(institutionId)
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
