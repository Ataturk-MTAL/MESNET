import { ref, computed, type Ref } from 'vue'
import {
  coordinationApi,
  type BusinessAssignmentDto,
  type BranchWorkloadConfigDto,
} from 'src/api/coordination'
import type { useNotify } from 'src/composables/useNotify'

export interface UseAssignedHoursOptions {
  assignments: Ref<BusinessAssignmentDto[]>
  workloadConfig: Ref<BranchWorkloadConfigDto | null>
  notify: ReturnType<typeof useNotify>
  loadData: () => Promise<void>
}

export function useAssignedHours(options: UseAssignedHoursOptions) {
  const { assignments, workloadConfig, notify, loadData } = options

  const hoursSaving = ref(false)
  const editedHours = ref<Record<string, number>>({})

  function initEditedHours() {
    const map: Record<string, number> = {}
    for (const a of assignments.value) {
      map[a.businessId] = a.assignedHours > 0 ? a.assignedHours : a.maxCoordinationHours
    }
    editedHours.value = map
  }

  const hoursTotalAvailable = computed(() =>
    assignments.value.reduce((sum, a) => sum + a.maxCoordinationHours, 0),
  )

  const hoursTotalAssigned = computed(() =>
    Object.values(editedHours.value).reduce((sum, h) => sum + h, 0),
  )

  const hoursRemaining = computed(() => hoursTotalAvailable.value - hoursTotalAssigned.value)

  const hoursOverLimit = computed(() => hoursTotalAssigned.value > hoursTotalAvailable.value)

  const hoursNearLimit = computed(() =>
    !hoursOverLimit.value && hoursTotalAssigned.value > hoursTotalAvailable.value * 0.9,
  )

  const hoursPoolOverLimit = computed(() => {
    if (!workloadConfig.value) return false
    return hoursTotalAssigned.value > workloadConfig.value.totalWorkloadPool
  })

  const changedHoursCount = computed(() => {
    let count = 0
    for (const a of assignments.value) {
      const current = a.assignedHours > 0 ? a.assignedHours : a.maxCoordinationHours
      if (editedHours.value[a.businessId] !== current) count++
    }
    return count
  })

  async function saveHours() {
    hoursSaving.value = true
    let successCount = 0
    const errors: string[] = []

    for (const a of assignments.value) {
      const current = a.assignedHours > 0 ? a.assignedHours : a.maxCoordinationHours
      const edited = editedHours.value[a.businessId]
      if (edited === undefined || edited === current) continue

      try {
        await coordinationApi.updateAssignedHours(a.businessId, { assignedHours: edited })
        successCount++
      } catch (e: unknown) {
        const msg = e instanceof Error ? e.message : 'Bilinmeyen hata'
        errors.push(`${a.businessName}: ${msg}`)
      }
    }

    hoursSaving.value = false

    if (successCount > 0) {
      notify.success(`${successCount} işletmenin takdir edilen saati güncellendi.`)
      await loadData()
      initEditedHours()
    }
    if (errors.length > 0) {
      notify.warning(`Hatalar: ${errors.join(', ')}`)
    }
  }

  return {
    editedHours,
    hoursSaving,
    hoursTotalAvailable,
    hoursTotalAssigned,
    hoursRemaining,
    hoursOverLimit,
    hoursNearLimit,
    hoursPoolOverLimit,
    changedHoursCount,
    initEditedHours,
    saveHours,
  }
}
