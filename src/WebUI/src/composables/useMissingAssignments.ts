import { ref, computed, type Ref } from 'vue'
import {
  coordinationApi,
  type BusinessAssignmentDto,
  type WeeklyVisitAssignmentDto,
  type AddWeeklyVisitAssignmentRequest,
} from 'src/api/coordination'

/** Koordinasyon atamasında olup planda bulunmayan tekil eksik ziyaret kaydı */
export interface MissingAssignment {
  businessId: string
  businessName: string
  teacherId: string
  teacherName: string
  branchCode: string
  branchName: string
  day: string
  periodCount: number
}

/** Eksik atamaların alan bazında gruplanmış hali */
export interface MissingGroup {
  branchCode: string
  branchName: string
  items: MissingAssignment[]
}

export interface UseMissingAssignmentsOptions {
  /** Seçili akademik dönem — koordinasyon atamalarını çekerken kullanılır */
  academicPeriodId: Ref<string | null>
  /** Mevcut plandaki atamalar — eksik kayıtları tespit etmek için referans */
  assignments: Ref<WeeklyVisitAssignmentDto[]>
  /** Eksik atama ekleme dialog'unun açık/kapalı durumu (sayfa ile paylaşılır) */
  addDialogOpen: Ref<boolean>
  /** Tekil atama ekleme aksiyonu — useWeeklyVisits'ten gelir */
  addAssignment: (data: AddWeeklyVisitAssignmentRequest) => Promise<void>
}

/**
 * "Eksik Atama Yönetimi" concern'i: koordinasyon atamalarında olup mevcut
 * haftalık ziyaret planında bulunmayan kayıtları tespit eder, alan bazında
 * gruplar ve tekil/alan/tümü olarak plana eklemeyi sağlar.
 */
export function useMissingAssignments(options: UseMissingAssignmentsOptions) {
  const { academicPeriodId, assignments, addDialogOpen, addAssignment } = options

  const missingLoading = ref(false)
  const missingAssignments = ref<MissingAssignment[]>([])
  const bulkAdding = ref(false)

  /** Eksik atamaları alan bazında grupla */
  const missingGrouped = computed<MissingGroup[]>(() => {
    const map = new Map<string, MissingGroup>()
    for (const item of missingAssignments.value) {
      let group = map.get(item.branchCode)
      if (!group) {
        group = { branchCode: item.branchCode, branchName: item.branchName, items: [] }
        map.set(item.branchCode, group)
      }
      group.items.push(item)
    }
    return [...map.values()].sort((a, b) => a.branchName.localeCompare(b.branchName, 'tr'))
  })

  /**
   * Eksik atama ekleme dialog'unu açar ve koordinasyon atamalarını çekerek
   * planda olmayan (işletme-gün) çiftlerini hesaplar.
   */
  async function openAddDialog() {
    addDialogOpen.value = true
    missingLoading.value = true
    missingAssignments.value = []

    try {
      const res = await coordinationApi.listAssignments({
        assignedOnly: true,
        academicPeriodId: academicPeriodId.value ?? undefined,
      })
      const coordData = res.data as unknown as BusinessAssignmentDto[]

      // Koordinasyon atamalarından işletme-gün çiftlerini çıkar
      const allPairs: MissingAssignment[] = []
      for (const biz of coordData) {
        if (!biz.assignedTeacherId || biz.assignedSlots.length === 0) continue

        // Gün bazında grupla (1 işletme + 1 gün = 1 ziyaret)
        const slotsByDay = new Map<string, number>()
        for (const slot of biz.assignedSlots) {
          slotsByDay.set(slot.day, (slotsByDay.get(slot.day) ?? 0) + 1)
        }

        for (const [day, count] of slotsByDay) {
          allPairs.push({
            businessId: biz.businessId,
            businessName: biz.businessName,
            teacherId: biz.assignedTeacherId,
            teacherName: biz.assignedTeacherName ?? '',
            branchCode: biz.branchCode,
            branchName: biz.branchName,
            day,
            periodCount: count,
          })
        }
      }

      // Mevcut plandaki atamaları set olarak tut
      const existingKeys = new Set(
        assignments.value.map(a => `${a.businessId}::${a.day}`),
      )

      // Eksik olanları filtrele
      missingAssignments.value = allPairs.filter(
        p => !existingKeys.has(`${p.businessId}::${p.day}`),
      )
    } catch {
      missingAssignments.value = []
    } finally {
      missingLoading.value = false
    }
  }

  /** Tekil eksik atamayı plana ekle ve listeden kaldır */
  async function submitMissingAssignment(item: MissingAssignment) {
    await addAssignment({
      teacherId: item.teacherId,
      teacherName: item.teacherName,
      businessId: item.businessId,
      businessName: item.businessName,
      branchCode: item.branchCode,
      branchName: item.branchName,
      day: item.day,
      periodCount: item.periodCount,
    }).catch(() => {})

    // Eklenen kaydı listeden kaldır
    missingAssignments.value = missingAssignments.value.filter(
      m => !(m.businessId === item.businessId && m.day === item.day),
    )
  }

  /** Belirli bir alanın tüm eksik atamalarını sırayla ekle */
  async function addBranchMissing(branchCode: string) {
    const items = missingAssignments.value.filter(m => m.branchCode === branchCode)
    if (items.length === 0) return

    bulkAdding.value = true
    try {
      for (const item of items) {
        await addAssignment({
          teacherId: item.teacherId,
          teacherName: item.teacherName,
          businessId: item.businessId,
          businessName: item.businessName,
          branchCode: item.branchCode,
          branchName: item.branchName,
          day: item.day,
          periodCount: item.periodCount,
        }).catch(() => {})
      }
      // Eklenen alanı listeden kaldır
      missingAssignments.value = missingAssignments.value.filter(m => m.branchCode !== branchCode)
    } finally {
      bulkAdding.value = false
    }
  }

  /** Tüm eksik atamaları sırayla ekle */
  async function addAllMissing() {
    if (missingAssignments.value.length === 0) return

    bulkAdding.value = true
    try {
      const items = [...missingAssignments.value]
      for (const item of items) {
        await addAssignment({
          teacherId: item.teacherId,
          teacherName: item.teacherName,
          businessId: item.businessId,
          businessName: item.businessName,
          branchCode: item.branchCode,
          branchName: item.branchName,
          day: item.day,
          periodCount: item.periodCount,
        }).catch(() => {})
      }
      missingAssignments.value = []
    } finally {
      bulkAdding.value = false
    }
  }

  return {
    // State
    missingLoading,
    missingAssignments,
    bulkAdding,
    // Türetilmiş
    missingGrouped,
    // Aksiyonlar
    openAddDialog,
    submitMissingAssignment,
    addBranchMissing,
    addAllMissing,
  }
}
