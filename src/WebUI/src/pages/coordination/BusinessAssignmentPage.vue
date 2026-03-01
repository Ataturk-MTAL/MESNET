<template>
  <q-page padding>
    <div class="text-h5 text-weight-bold q-mb-lg">Isletme Dagitimi</div>

    <!-- Filtreler -->
    <div class="row q-col-gutter-md q-mb-lg">
      <div class="col-12 col-sm-4">
        <q-select
          v-model="branchFilter"
          :options="branchOpts.options.value"
          :loading="branchOpts.loading.value"
          label="Alan"
          filled
          use-input
          input-debounce="0"
          emit-value
          map-options
          option-label="label"
          option-value="value"
          clearable
          @filter="branchOpts.filter"
        >
          <template #prepend>
            <q-icon name="school" />
          </template>
          <template #no-option>
            <q-item>
              <q-item-section class="text-grey">Sonuc bulunamadi</q-item-section>
            </q-item>
          </template>
        </q-select>
      </div>
      <div class="col-12 col-sm-4">
        <q-select
          v-model="teacherFilter"
          :options="teacherOpts.options.value"
          :loading="teacherOpts.loading.value"
          label="Ogretmen"
          filled
          use-input
          input-debounce="0"
          emit-value
          map-options
          option-label="label"
          option-value="value"
          clearable
          @filter="teacherOpts.filter"
        >
          <template #prepend>
            <q-icon name="person" />
          </template>
          <template #no-option>
            <q-item>
              <q-item-section class="text-grey">Sonuc bulunamadi</q-item-section>
            </q-item>
          </template>
        </q-select>
      </div>
      <div class="col-12 col-sm-2">
        <q-select
          v-model="assignedFilter"
          :options="assignedFilterOptions"
          label="Durum"
          filled
          emit-value
          map-options
          clearable
        >
          <template #prepend>
            <q-icon name="filter_alt" />
          </template>
        </q-select>
      </div>
      <div class="col-12 col-sm-2 self-center">
        <q-btn
          color="primary"
          icon="refresh"
          label="Yukle"
          :loading="loading"
          @click="loadData"
          class="full-width"
        />
      </div>
    </div>

    <!-- Bolum A: Ozet Kartlari -->
    <div class="row q-col-gutter-md q-mb-lg">
      <div class="col-12 col-sm-3">
        <q-card flat bordered>
          <q-card-section class="text-center">
            <div class="text-caption text-grey-7">Toplam Verilebilir Saat</div>
            <div class="text-h4 text-green-8">{{ summary.totalAvailableHours }}</div>
          </q-card-section>
        </q-card>
      </div>
      <div class="col-12 col-sm-3">
        <q-card flat bordered>
          <q-card-section class="text-center">
            <div class="text-caption text-grey-7">Toplam Dagitilan Saat</div>
            <div class="text-h4" :class="isOverLimit ? 'text-red-8' : 'text-blue-8'">
              {{ summary.totalAssignedHours }}
            </div>
            <div v-if="isOverLimit" class="text-caption text-red-7 text-weight-bold">
              Limit asildi!
            </div>
          </q-card-section>
        </q-card>
      </div>
      <div class="col-12 col-sm-3">
        <q-card flat bordered>
          <q-card-section class="text-center">
            <div class="text-caption text-grey-7">Kalan Saat</div>
            <div class="text-h4 text-orange-8">{{ summary.remainingHours }}</div>
          </q-card-section>
        </q-card>
      </div>
      <div class="col-12 col-sm-3">
        <q-card flat bordered>
          <q-card-section class="text-center">
            <div class="text-caption text-grey-7">Atanmis / Toplam Isletme</div>
            <div class="text-h4 text-purple-8">
              {{ summary.assignedBusinessCount }} / {{ summary.assignedBusinessCount + summary.unassignedBusinessCount }}
            </div>
          </q-card-section>
        </q-card>
      </div>
    </div>

    <!-- Bolum B: Ogretmen Ozet Tablosu (collapse) -->
    <q-card flat bordered class="q-mb-lg">
      <q-expansion-item
        icon="people"
        label="Ogretmen Ozeti"
        header-class="text-subtitle1 text-weight-medium"
        default-opened
      >
        <q-card-section class="q-pt-none">
          <q-table
            :rows="summary.teacherWorkloads"
            :columns="teacherSummaryColumns"
            row-key="teacherId"
            flat
            bordered
            dense
            hide-pagination
            :rows-per-page-options="[0]"
          >
            <template #body-cell-teacherName="{ row }">
              <q-td>
                <a
                  href="#"
                  class="text-primary cursor-pointer"
                  @click.prevent="filterByTeacher(row.teacherId)"
                >
                  {{ row.teacherName }}
                </a>
              </q-td>
            </template>
            <template #bottom-row>
              <q-tr class="text-weight-bold bg-grey-2">
                <q-td>TOPLAM</q-td>
                <q-td class="text-center">{{ totalTeacherBusinessCount }}</q-td>
                <q-td class="text-center">{{ summary.totalAssignedHours }}</q-td>
              </q-tr>
            </template>
          </q-table>
        </q-card-section>
      </q-expansion-item>
    </q-card>

    <!-- Bolum C: Isletme Dagitim Tablosu -->
    <q-card flat bordered>
      <q-card-section>
        <div class="row items-center q-mb-md">
          <div class="col text-subtitle1 text-weight-medium">
            Isletme Listesi
            <q-badge color="primary" class="q-ml-sm">{{ filteredAssignments.length }}</q-badge>
          </div>
          <div class="col-auto q-gutter-sm">
            <q-btn
              flat
              color="secondary"
              icon="calculate"
              label="Mesafe Hesapla"
              :loading="recalculating"
              @click="recalculateDistances"
            />
            <q-btn
              color="primary"
              icon="save"
              label="Toplu Kaydet"
              :loading="bulkSaving"
              :disable="!hasPendingChanges || isOverLimit"
              @click="bulkSave"
            />
          </div>
        </div>

        <q-table
          :rows="filteredAssignments"
          :columns="assignmentColumns"
          row-key="businessId"
          flat
          bordered
          dense
          :rows-per-page-options="[20, 50, 0]"
          :pagination="tablePagination"
        >
          <!-- Uzaklik -->
          <template #body-cell-distanceToSchoolKm="{ row }">
            <q-td>
              <template v-if="row.isManualDistance || row.distanceToSchoolKm == null">
                <q-input
                  v-model.number="row._editDistance"
                  type="number"
                  dense
                  filled
                  :min="0"
                  :max="999"
                  style="width: 80px"
                  @update:model-value="markDistanceChanged(row)"
                >
                  <template #append>
                    <span class="text-caption text-grey-6">km</span>
                  </template>
                </q-input>
              </template>
              <template v-else>
                {{ row.distanceToSchoolKm?.toFixed(1) }} km
              </template>
            </q-td>
          </template>

          <!-- Verilebilir Saat -->
          <template #body-cell-maxCoordinationHours="{ row }">
            <q-td class="text-center">
              <q-badge
                :color="row.maxCoordinationHours > 0 ? 'green-7' : 'grey'"
                :label="String(row.maxCoordinationHours)"
              />
            </q-td>
          </template>

          <!-- Takdir Edilen Saat -->
          <template #body-cell-assignedHours="{ row }">
            <q-td>
              <q-input
                v-model.number="row._editAssignedHours"
                type="number"
                dense
                filled
                :min="0"
                :max="row.maxCoordinationHours"
                style="width: 70px"
                :class="{ 'bg-red-1': row._editAssignedHours > row.maxCoordinationHours }"
                @update:model-value="markChanged(row)"
              />
            </q-td>
          </template>

          <!-- Ogretmen -->
          <template #body-cell-assignedTeacherName="{ row }">
            <q-td>
              <q-select
                v-model="row._editTeacherId"
                :options="teacherOpts.allOptions.value"
                dense
                filled
                emit-value
                map-options
                option-label="label"
                option-value="value"
                clearable
                style="min-width: 160px"
                @update:model-value="onTeacherChanged(row)"
              />
            </q-td>
          </template>

          <!-- Gun -->
          <template #body-cell-assignedDay="{ row }">
            <q-td>
              <q-select
                v-model="row._editDay"
                :options="dayOptions"
                dense
                filled
                emit-value
                map-options
                clearable
                style="min-width: 110px"
                @update:model-value="markChanged(row)"
              />
            </q-td>
          </template>

          <!-- Islemler -->
          <template #body-cell-actions="{ row }">
            <q-td class="text-right">
              <q-btn
                v-if="row._changed"
                flat
                round
                dense
                icon="save"
                color="primary"
                :loading="row._saving"
                :disable="row._editAssignedHours > row.maxCoordinationHours"
                @click="saveRow(row)"
              >
                <q-tooltip>Kaydet</q-tooltip>
              </q-btn>
            </q-td>
          </template>
        </q-table>
      </q-card-section>
    </q-card>

    <!-- Limit Asildi Uyari -->
    <q-banner
      v-if="isOverLimit"
      rounded
      class="bg-red-1 text-red-9 q-mt-md"
    >
      <template #avatar>
        <q-icon name="error" color="red-7" />
      </template>
      Toplam dagitilan saat ({{ summary.totalAssignedHours }}) toplam verilebilir saati ({{ summary.totalAvailableHours }}) asiyor.
      Mevzuat geregi toplam takdir edilen saat, toplam verilebilir saati asamaz.
    </q-banner>
  </q-page>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import {
  coordinationApi,
  type BusinessAssignmentDto,
  type CoordinationSummaryDto,
} from 'src/api/coordination'
import { useTeacherOptions, useBranchOptions } from 'src/composables/useEntityOptions'
import { useNotify } from 'src/composables/useNotify'
import { useAuthStore } from 'stores/auth'

const notify = useNotify()
const authStore = useAuthStore()
const teacherOpts = useTeacherOptions()
const branchOpts = useBranchOptions()

// ── Filters ──
const branchFilter = ref<string | null>(null)
const teacherFilter = ref<string | null>(null)
const assignedFilter = ref<boolean | null>(null)
const loading = ref(false)
const recalculating = ref(false)
const bulkSaving = ref(false)

const assignedFilterOptions = [
  { label: 'Atanmis', value: true },
  { label: 'Atanmamis', value: false },
]

const dayOptions = [
  { label: 'Pazartesi', value: 'Monday' },
  { label: 'Sali', value: 'Tuesday' },
  { label: 'Carsamba', value: 'Wednesday' },
  { label: 'Persembe', value: 'Thursday' },
  { label: 'Cuma', value: 'Friday' },
]

const tablePagination = ref({ rowsPerPage: 20 })

// ── Data ──
interface EditableAssignment extends BusinessAssignmentDto {
  _editAssignedHours: number
  _editTeacherId: string | null
  _editTeacherName: string | null
  _editDay: string | null
  _editDistance: number | null
  _changed: boolean
  _distanceChanged: boolean
  _saving: boolean
}

const assignments = ref<EditableAssignment[]>([])

const summary = ref<CoordinationSummaryDto>({
  totalAvailableHours: 0,
  totalAssignedHours: 0,
  remainingHours: 0,
  assignedBusinessCount: 0,
  unassignedBusinessCount: 0,
  teacherWorkloads: [],
})

// ── Computed ──
const filteredAssignments = computed(() => {
  let list = assignments.value
  if (branchFilter.value) {
    list = list.filter((a) => a.branchCode === branchFilter.value)
  }
  if (teacherFilter.value) {
    list = list.filter((a) => a._editTeacherId === teacherFilter.value)
  }
  if (assignedFilter.value === true) {
    list = list.filter((a) => a._editTeacherId != null)
  } else if (assignedFilter.value === false) {
    list = list.filter((a) => a._editTeacherId == null)
  }
  return list
})

const isOverLimit = computed(
  () => summary.value.totalAssignedHours > summary.value.totalAvailableHours,
)

const hasPendingChanges = computed(() => assignments.value.some((a) => a._changed))

const totalTeacherBusinessCount = computed(() =>
  summary.value.teacherWorkloads.reduce((sum, tw) => sum + tw.businessCount, 0),
)

// ── Recalculate summary from local edits ──
function recalcSummaryFromLocal() {
  const totalAvailable = assignments.value.reduce((s, a) => s + a.maxCoordinationHours, 0)
  const totalAssigned = assignments.value.reduce((s, a) => s + (a._editAssignedHours || 0), 0)
  const assignedCount = assignments.value.filter((a) => a._editTeacherId != null).length
  const unassignedCount = assignments.value.length - assignedCount

  // Teacher workloads — group by teacher
  const teacherMap = new Map<string, { name: string; hours: number; count: number }>()
  for (const a of assignments.value) {
    if (!a._editTeacherId) continue
    const existing = teacherMap.get(a._editTeacherId)
    if (existing) {
      existing.hours += a._editAssignedHours || 0
      existing.count += 1
    } else {
      teacherMap.set(a._editTeacherId, {
        name: a._editTeacherName ?? teacherNameById(a._editTeacherId),
        hours: a._editAssignedHours || 0,
        count: 1,
      })
    }
  }

  summary.value = {
    totalAvailableHours: totalAvailable,
    totalAssignedHours: totalAssigned,
    remainingHours: totalAvailable - totalAssigned,
    assignedBusinessCount: assignedCount,
    unassignedBusinessCount: unassignedCount,
    teacherWorkloads: Array.from(teacherMap.entries()).map(([id, v]) => ({
      teacherId: id,
      teacherName: v.name,
      assignedHours: v.hours,
      businessCount: v.count,
    })),
  }
}

function teacherNameById(id: string): string {
  return teacherOpts.allOptions.value.find((o) => o.value === id)?.label ?? '—'
}

// ── Table Columns ──
const teacherSummaryColumns = [
  { name: 'teacherName', label: 'Ogretmen', field: 'teacherName', align: 'left' as const, sortable: true },
  { name: 'businessCount', label: 'Isletme Sayisi', field: 'businessCount', align: 'center' as const, sortable: true },
  { name: 'assignedHours', label: 'Atanan Saat', field: 'assignedHours', align: 'center' as const, sortable: true },
]

const assignmentColumns = [
  { name: 'businessName', label: 'Isletme', field: 'businessName', align: 'left' as const, sortable: true },
  { name: 'district', label: 'Ilce', field: 'district', align: 'left' as const, sortable: true },
  { name: 'branchName', label: 'Alan', field: 'branchName', align: 'left' as const, sortable: true },
  { name: 'activeStudentCount', label: 'Ogrenci', field: 'activeStudentCount', align: 'center' as const, sortable: true },
  { name: 'distanceToSchoolKm', label: 'Uzaklik', field: 'distanceToSchoolKm', align: 'center' as const, sortable: true },
  { name: 'maxCoordinationHours', label: 'Verilebilir', field: 'maxCoordinationHours', align: 'center' as const, sortable: true },
  { name: 'assignedHours', label: 'Takdir Edilen', field: 'assignedHours', align: 'center' as const },
  { name: 'assignedTeacherName', label: 'Ogretmen', field: 'assignedTeacherName', align: 'left' as const },
  { name: 'assignedDay', label: 'Gun', field: 'assignedDay', align: 'center' as const },
  { name: 'actions', label: '', field: 'actions', align: 'right' as const },
]

// ── Event Handlers ──
function markChanged(row: EditableAssignment) {
  row._changed = true
  recalcSummaryFromLocal()
}

function markDistanceChanged(row: EditableAssignment) {
  row._distanceChanged = true
  row._changed = true
}

function onTeacherChanged(row: EditableAssignment) {
  const opt = teacherOpts.allOptions.value.find((o) => o.value === row._editTeacherId)
  row._editTeacherName = opt?.label ?? null
  markChanged(row)
}

function filterByTeacher(teacherId: string) {
  teacherFilter.value = teacherId
}

function toEditable(dto: BusinessAssignmentDto): EditableAssignment {
  return {
    ...dto,
    _editAssignedHours: dto.assignedHours,
    _editTeacherId: dto.assignedTeacherId,
    _editTeacherName: dto.assignedTeacherName,
    _editDay: dto.assignedDay,
    _editDistance: dto.distanceToSchoolKm,
    _changed: false,
    _distanceChanged: false,
    _saving: false,
  }
}

// ── API Calls ──
async function loadData() {
  loading.value = true
  try {
    const params: Record<string, string | boolean | undefined> = {}
    if (branchFilter.value) params.branchCode = branchFilter.value
    if (assignedFilter.value != null) params.assignedOnly = assignedFilter.value

    const [assignRes, summaryRes] = await Promise.all([
      coordinationApi.listAssignments(params as { branchCode?: string; assignedOnly?: boolean }),
      coordinationApi.getCoordinationSummary(
        branchFilter.value ? { branchCode: branchFilter.value } : undefined,
      ),
    ])

    assignments.value = (assignRes.data ?? []).map(toEditable)
    summary.value = summaryRes.data ?? summary.value
  } catch (e) {
    notify.apiError(e, 'Isletme listesi yuklenirken hata olustu.')
  } finally {
    loading.value = false
  }
}

async function saveRow(row: EditableAssignment) {
  row._saving = true
  try {
    // Manuel mesafe varsa once onu kaydet
    if (row._distanceChanged && row._editDistance != null && row._editDistance > 0) {
      await coordinationApi.setManualDistance(row.businessId, {
        distanceKm: row._editDistance,
      })
    }

    // Ogretmen atama
    if (row._editTeacherId && row._editDay) {
      await coordinationApi.assignBusiness({
        businessId: row.businessId,
        teacherId: row._editTeacherId,
        teacherName: row._editTeacherName ?? '',
        assignedHours: row._editAssignedHours || 0,
        assignedDay: row._editDay,
        assignedBy: authStore.user?.fullName ?? '',
      })
    }

    row._changed = false
    row._distanceChanged = false
    // Sync original values
    row.assignedHours = row._editAssignedHours
    row.assignedTeacherId = row._editTeacherId
    row.assignedTeacherName = row._editTeacherName
    row.assignedDay = row._editDay
    row.distanceToSchoolKm = row._editDistance

    notify.success('Isletme atamasi kaydedildi.')
  } catch (e) {
    notify.apiError(e, 'Isletme atamasi kaydedilirken hata olustu.')
  } finally {
    row._saving = false
  }
}

async function bulkSave() {
  bulkSaving.value = true
  const changed = assignments.value.filter((a) => a._changed)
  let successCount = 0

  for (const row of changed) {
    try {
      if (row._distanceChanged && row._editDistance != null && row._editDistance > 0) {
        await coordinationApi.setManualDistance(row.businessId, {
          distanceKm: row._editDistance,
        })
      }
      if (row._editTeacherId && row._editDay) {
        await coordinationApi.assignBusiness({
          businessId: row.businessId,
          teacherId: row._editTeacherId,
          teacherName: row._editTeacherName ?? '',
          assignedHours: row._editAssignedHours || 0,
          assignedDay: row._editDay,
          assignedBy: authStore.user?.fullName ?? '',
        })
      }
      row._changed = false
      row._distanceChanged = false
      row.assignedHours = row._editAssignedHours
      row.assignedTeacherId = row._editTeacherId
      row.assignedTeacherName = row._editTeacherName
      row.assignedDay = row._editDay
      row.distanceToSchoolKm = row._editDistance
      successCount++
    } catch {
      // bireysel hata — devam et
    }
  }

  bulkSaving.value = false
  if (successCount === changed.length) {
    notify.success(`${successCount} isletme basariyla kaydedildi.`)
  } else {
    notify.warning(`${successCount}/${changed.length} isletme kaydedildi, bazilari basarisiz.`)
  }
}

async function recalculateDistances() {
  recalculating.value = true
  try {
    await coordinationApi.recalculateDistances()
    notify.success('Mesafeler yeniden hesaplandi.')
    await loadData()
  } catch (e) {
    notify.apiError(e, 'Mesafe hesaplama sirasinda hata olustu.')
  } finally {
    recalculating.value = false
  }
}

// ── Init ──
onMounted(async () => {
  await Promise.all([
    teacherOpts.load(),
    branchOpts.load(),
  ])
  await loadData()
})
</script>
