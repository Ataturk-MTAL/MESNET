import { ref, computed, type Ref, type ComputedRef } from 'vue'
import {
  coordinationApi,
  type BusinessAssignmentDto,
  type DailyScheduleDto,
} from 'src/api/coordination'
import type { useNotify } from 'src/composables/useNotify'
import { billableTargetHours, slotTargetHours, isHonorary } from 'src/utils/coordinationHours'

export interface PendingChange {
  type: 'assign' | 'unassign'
  businessId: string
  businessName: string
  day: string
  periodNumber: number
}

export interface UseAssignmentDnDOptions {
  assignments: Ref<BusinessAssignmentDto[]>
  rawSchedule: Ref<DailyScheduleDto[]>
  selectedTeacherId: Ref<string | null>
  selectedTeacherName: ComputedRef<string>
  /** Seçili akademik dönem — koordinasyon satırı alan+dönem bazlıdır (#114) */
  academicPeriodId: Ref<string | null>
  notify: ReturnType<typeof useNotify>
  loadData: () => Promise<void>
  loadTeacherSchedule: (teacherId: string) => Promise<void>
}

export function useAssignmentDnD(options: UseAssignmentDnDOptions) {
  const {
    assignments, rawSchedule, selectedTeacherId, selectedTeacherName, academicPeriodId,
    notify, loadData, loadTeacherSchedule,
  } = options

  const pendingChanges = ref<PendingChange[]>([])
  const saving = ref(false)

  const effectiveSchedule = computed((): DailyScheduleDto[] => {
    // Vue reactive proxy'leri structuredClone ile klonlanamaz — elle deep copy
    const schedule: DailyScheduleDto[] = rawSchedule.value.map((day) => ({
      day: day.day,
      periods: day.periods.map((p) => ({
        periodNumber: p.periodNumber,
        status: p.status,
        courseName: p.courseName ?? null,
        assignedBusinessId: p.assignedBusinessId ?? null,
      })),
    }))

    for (const change of pendingChanges.value) {
      const daySchedule = schedule.find((d) => d.day === change.day)
      if (!daySchedule) continue
      const slot = daySchedule.periods.find((p) => p.periodNumber === change.periodNumber)
      if (!slot) continue

      if (change.type === 'assign') {
        slot.assignedBusinessId = change.businessId
      } else if (change.type === 'unassign') {
        slot.assignedBusinessId = null
      }
    }

    return schedule
  })

  const unassignedBusinesses = computed(() => {
    const result: BusinessAssignmentDto[] = []

    for (const biz of assignments.value) {
      // Fahri ziyaret tek slot ister; tavana düşmez (#115)
      const targetHours = slotTargetHours(biz)
      const backendSlots = biz.assignedSlots?.length ?? 0

      const pendingAssigns = pendingChanges.value.filter(
        (c) => c.businessId === biz.businessId && c.type === 'assign',
      ).length
      const pendingUnassigns = pendingChanges.value.filter(
        (c) => c.businessId === biz.businessId && c.type === 'unassign',
      ).length
      const effectiveSlots = backendSlots + pendingAssigns - pendingUnassigns

      if (effectiveSlots < targetHours) {
        result.push(biz)
      }
    }

    return result
  })

  const assignedToTeacher = computed(() => {
    if (!selectedTeacherId.value) return []

    const pendingUnassignIds = new Set(
      pendingChanges.value.filter((c) => c.type === 'unassign').map((c) => c.businessId),
    )

    const base = assignments.value.filter(
      (a) =>
        a.assignedTeacherId === selectedTeacherId.value && !pendingUnassignIds.has(a.businessId),
    )

    for (const pc of pendingChanges.value) {
      if (pc.type === 'assign' && !base.find((b) => b.businessId === pc.businessId)) {
        const original = assignments.value.find((a) => a.businessId === pc.businessId)
        if (original) {
          base.push({
            ...original,
            assignedTeacherId: selectedTeacherId.value,
            assignedDay: pc.day,
            assignedPeriodNumber: pc.periodNumber,
          })
        }
      }
    }

    return base
  })

  function slotProgress(biz: BusinessAssignmentDto): { current: number; target: number } {
    const target = slotTargetHours(biz)
    const backendSlots = biz.assignedSlots?.length ?? 0
    const pendingAssigns = pendingChanges.value.filter(
      (c) => c.businessId === biz.businessId && c.type === 'assign',
    ).length
    const pendingUnassigns = pendingChanges.value.filter(
      (c) => c.businessId === biz.businessId && c.type === 'unassign',
    ).length
    return { current: backendSlots + pendingAssigns - pendingUnassigns, target }
  }

  function onBusinessDragStart(event: DragEvent, biz: BusinessAssignmentDto) {
    if (!event.dataTransfer) return
    event.dataTransfer.setData('application/business-id', biz.businessId)
    event.dataTransfer.effectAllowed = 'move'

    const target = event.target as HTMLElement
    target.classList.add('business-card--dragging')
    setTimeout(() => target.classList.remove('business-card--dragging'), 0)
  }

  function onBusinessDropped(payload: { businessId: string; day: string; periodNumber: number }) {
    const biz = assignments.value.find((a) => a.businessId === payload.businessId)
    if (!biz) return

    const existing = pendingChanges.value.find(
      (c) =>
        c.businessId === payload.businessId &&
        c.type === 'assign' &&
        c.day === payload.day &&
        c.periodNumber === payload.periodNumber,
    )
    if (existing) return

    const targetHours = slotTargetHours(biz)
    const backendSlots = biz.assignedSlots?.length ?? 0
    const pendingAssigns = pendingChanges.value.filter(
      (c) => c.businessId === biz.businessId && c.type === 'assign',
    ).length
    const pendingUnassigns = pendingChanges.value.filter(
      (c) => c.businessId === biz.businessId && c.type === 'unassign',
    ).length
    const currentSlots = backendSlots + pendingAssigns - pendingUnassigns

    if (currentSlots >= targetHours) {
      const limitLabel = isHonorary(biz)
        ? `Fahri ziyaret slotu dolu (${currentSlots}/${targetHours}).`
        : `Tüm saatler atanmış (${currentSlots}/${targetHours}).`
      notify.warning(`${biz.businessName}: ${limitLabel}`)
      return
    }

    pendingChanges.value.push({
      type: 'assign',
      businessId: payload.businessId,
      businessName: biz.businessName,
      day: payload.day,
      periodNumber: payload.periodNumber,
    })
  }

  function onBusinessRemoved(payload: { businessId: string; day: string; periodNumber: number }) {
    const pendingAssignIdx = pendingChanges.value.findIndex(
      (c) =>
        c.businessId === payload.businessId &&
        c.type === 'assign' &&
        c.day === payload.day &&
        c.periodNumber === payload.periodNumber,
    )

    if (pendingAssignIdx >= 0) {
      pendingChanges.value.splice(pendingAssignIdx, 1)
      return
    }

    const biz = assignments.value.find((a) => a.businessId === payload.businessId)
    if (!biz) return

    pendingChanges.value.push({
      type: 'unassign',
      businessId: payload.businessId,
      businessName: biz.businessName,
      day: payload.day,
      periodNumber: payload.periodNumber,
    })
  }

  function removeAssignment(biz: BusinessAssignmentDto) {
    if (biz.assignedSlots?.length > 0) {
      for (const slot of biz.assignedSlots) {
        onBusinessRemoved({
          businessId: biz.businessId,
          day: slot.day,
          periodNumber: slot.periodNumber,
        })
      }
    } else if (biz.assignedDay) {
      onBusinessRemoved({
        businessId: biz.businessId,
        day: biz.assignedDay,
        periodNumber: biz.assignedPeriodNumber ?? 0,
      })
    }
  }

  async function saveAll() {
    if (pendingChanges.value.length === 0 || !selectedTeacherId.value) return

    saving.value = true
    let successCount = 0
    const total = pendingChanges.value.length
    const errors: string[] = []

    for (const change of [...pendingChanges.value]) {
      try {
        // Hedef satır alan bazlıdır — alan kodu listedeki işletme satırından okunur (#114)
        const biz = assignments.value.find((a) => a.businessId === change.businessId)
        const branchRow = {
          branchCode: biz?.branchCode ?? '',
          academicPeriodId: academicPeriodId.value ?? '',
        }

        if (change.type === 'assign') {
          // Fahri satırda 0 gider — tavanı göndermek atamayı ücretliye çevirirdi (#115)
          const hours = biz ? billableTargetHours(biz) : 0
          await coordinationApi.assignBusiness({
            businessId: change.businessId,
            teacherId: selectedTeacherId.value!,
            teacherName: selectedTeacherName.value,
            assignedHours: hours,
            assignedDay: change.day,
            periodNumber: change.periodNumber,
            // assignedBy gönderilmez — aktör token'dan damgalanır (#137)
            ...branchRow,
          })
        } else {
          await coordinationApi.unassignBusinessSlot(
            change.businessId, change.day, change.periodNumber, branchRow,
          )
        }
        successCount++
      } catch (e: unknown) {
        const msg = e instanceof Error ? e.message : 'Bilinmeyen hata'
        errors.push(`${change.businessName}: ${msg}`)
      }
    }

    saving.value = false
    pendingChanges.value = []

    if (successCount === total) {
      notify.success(`${successCount} işlem başarıyla kaydedildi.`)
    } else {
      notify.warning(`${successCount}/${total} işlem kaydedildi. Hatalar: ${errors.join(', ')}`)
    }

    await Promise.all([
      loadData(),
      selectedTeacherId.value ? loadTeacherSchedule(selectedTeacherId.value) : Promise.resolve(),
    ])
  }

  function clearPending() {
    pendingChanges.value = []
  }

  return {
    pendingChanges,
    saving,
    effectiveSchedule,
    unassignedBusinesses,
    assignedToTeacher,
    slotProgress,
    onBusinessDragStart,
    onBusinessDropped,
    onBusinessRemoved,
    removeAssignment,
    saveAll,
    clearPending,
  }
}
