<template>
  <div class="assignment-grid">
    <table
      class="full-width"
      aria-label="Ders programı ve işletme atama tablosu. Hücreler gün ve saate göre düzenlenmiştir; her hücre dolu (ders), boş veya atanmış işletme durumunu gösterir."
    >
      <thead>
        <tr>
          <th class="period-header text-caption text-grey-7">
            Saat
          </th>
          <th
            v-for="day in days"
            :key="day.value"
            class="day-header text-caption text-grey-7"
          >
            {{ day.label }}
          </th>
        </tr>
      </thead>
      <tbody>
        <tr
          v-for="period in periodCount"
          :key="period"
        >
          <td class="period-label text-weight-medium text-grey-8">
            {{ period }}.
          </td>
          <td
            v-for="day in days"
            :key="day.value"
            class="grid-cell"
            :class="cellClass(day.value, period)"
            :aria-label="cellLabel(day.label, day.value, period)"
          >
            <!-- Dolu hücre — sürükleme kabul etmez -->
            <div
              v-if="isOccupied(day.value, period)"
              class="occupied-cell"
            >
              <q-icon
                name="menu_book"
                size="14px"
                color="grey-5"
              />
              <span class="text-caption text-grey-6">
                {{ getCourseName(day.value, period) || 'Ders' }}
              </span>
            </div>

            <!-- Boş + atanmamış — Drop Zone -->
            <div
              v-else-if="!getAssignedBusinessId(day.value, period)"
              :ref="(el) => registerDropZone(el as HTMLElement, day.value, period)"
              class="drop-zone"
              :class="{ 'drop-zone--disabled': disabled, 'drop-zone--target': !disabled && selected !== null }"
              role="button"
              tabindex="0"
              :aria-disabled="disabled"
              :aria-label="dropZoneLabel(day.label, period)"
              @keydown.enter.prevent="onDropZoneKey(day.value, period)"
              @keydown.space.prevent="onDropZoneKey(day.value, period)"
              @keydown.esc.prevent="emit('selection-cancelled')"
            >
              <q-icon
                name="add_circle_outline"
                size="16px"
                color="green-4"
              />
              <span class="text-caption text-green-6">Boş</span>
            </div>

            <!-- Boş + atanmış — İşletme chip -->
            <!-- Fare için sürükle-bırak, klavye için seç-taşı akışı (#88): Enter/Space seçer,
              hedef hücrede Enter bırakır, Escape iptal eder. -->
            <div
              v-else
              class="assigned-slot"
              :class="{ 'assigned-slot--selected': isSelected(day.value, period) }"
              role="button"
              tabindex="0"
              :aria-disabled="disabled"
              :aria-pressed="isSelected(day.value, period)"
              :aria-label="assignedSlotLabel(day.value, day.label, period)"
              :draggable="!disabled"
              :data-business-id="getAssignedBusinessId(day.value, period)"
              :data-from-day="day.value"
              :data-from-period="period"
              @dragstart="onDragStart($event, day.value, period)"
              @keydown.enter.prevent="onAssignedKey(day.value, period)"
              @keydown.space.prevent="onAssignedKey(day.value, period)"
              @keydown.esc.prevent="emit('selection-cancelled')"
            >
              <div class="business-chip">
                <q-icon
                  name="business"
                  size="14px"
                  color="blue-7"
                />
                <span class="text-caption text-blue-9 ellipsis chip-label">
                  {{ getBusinessName(day.value, period) }}
                </span>
                <q-btn
                  v-if="!disabled"
                  flat
                  round
                  dense
                  icon="close"
                  size="8px"
                  color="red-5"
                  class="remove-btn"
                  :aria-label="`${getBusinessName(day.value, period)} atamasını kaldır`"
                  @click.stop="onRemoveClick(day.value, period)"
                >
                  <q-tooltip>Atamayı kaldır</q-tooltip>
                </q-btn>
              </div>
            </div>
          </td>
        </tr>
      </tbody>
    </table>

    <!-- Özet Chip'leri -->
    <div class="row q-mt-md q-gutter-sm">
      <q-chip
        icon="menu_book"
        color="grey-2"
        text-color="grey-8"
        dense
        size="sm"
      >
        Dolu: {{ occupiedCount }}
      </q-chip>
      <q-chip
        icon="event_available"
        color="green-1"
        text-color="green-8"
        dense
        size="sm"
      >
        Boş: {{ freeCount }}
      </q-chip>
      <q-chip
        icon="business"
        color="blue-1"
        text-color="blue-8"
        dense
        size="sm"
      >
        Atanmış: {{ assignedCount }}
      </q-chip>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed, onBeforeUnmount } from 'vue'
import type { DailyScheduleDto, PeriodSlotDto } from 'src/api/coordination'
import type { AssignmentSelection } from 'src/composables/useKeyboardAssignment'

const props = defineProps<{
  schedule: DailyScheduleDto[]
  periodCount: number
  disabled: boolean
  businessNameMap: Record<string, string>
  /** Klavye akışında seçili olan işletme (#88); sayfa sahibi tutar. */
  selected: AssignmentSelection | null
}>()

const emit = defineEmits<{
  (e: 'business-dropped', payload: { businessId: string; day: string; periodNumber: number }): void
  (e: 'business-removed', payload: { businessId: string; day: string; periodNumber: number }): void
  (e: 'business-selected', payload: AssignmentSelection): void
  (e: 'selection-cancelled'): void
}>()

const days = [
  { label: 'Pazartesi', value: 'Monday' },
  { label: 'Salı', value: 'Tuesday' },
  { label: 'Çarşamba', value: 'Wednesday' },
  { label: 'Perşembe', value: 'Thursday' },
  { label: 'Cuma', value: 'Friday' },
]

// ── Slot yardımcıları ──

function findSlot(day: string, period: number): PeriodSlotDto | undefined {
  return props.schedule.find((d) => d.day === day)?.periods.find((p) => p.periodNumber === period)
}

function isOccupied(day: string, period: number): boolean {
  return findSlot(day, period)?.status === 'Occupied'
}

function getAssignedBusinessId(day: string, period: number): string | null {
  return findSlot(day, period)?.assignedBusinessId ?? null
}

function getBusinessName(day: string, period: number): string {
  const id = getAssignedBusinessId(day, period)
  return id ? (props.businessNameMap[id] ?? 'İşletme') : ''
}

function getCourseName(day: string, period: number): string | null {
  return findSlot(day, period)?.courseName ?? null
}

function cellClass(day: string, period: number): string {
  if (isOccupied(day, period)) return 'cell-occupied'
  if (getAssignedBusinessId(day, period)) return 'cell-assigned'
  return 'cell-free'
}

// Ekran okuyucu için hücre durumunu açıklar (gün, saat ve dolu/boş/atanmış durumu).
function cellLabel(dayLabel: string, day: string, period: number): string {
  if (isOccupied(day, period)) return `${dayLabel}, ${period}. saat: ${getCourseName(day, period) || 'ders'}`
  if (getAssignedBusinessId(day, period)) return `${dayLabel}, ${period}. saat: ${getBusinessName(day, period)} atanmış`
  return `${dayLabel}, ${period}. saat: boş`
}

// ── Özet computed'ları ──

const occupiedCount = computed(() => {
  let count = 0
  for (const day of props.schedule) {
    for (const p of day.periods) {
      if (p.status === 'Occupied') count++
    }
  }
  return count
})

const assignedCount = computed(() => {
  let count = 0
  for (const day of props.schedule) {
    for (const p of day.periods) {
      if (p.status !== 'Occupied' && p.assignedBusinessId) count++
    }
  }
  return count
})

const freeCount = computed(() => {
  let count = 0
  for (const day of props.schedule) {
    for (const p of day.periods) {
      if (p.status !== 'Occupied' && !p.assignedBusinessId) count++
    }
  }
  return count
})

// ── Native HTML5 Drag & Drop ──

function onDragStart(event: DragEvent, day: string, period: number) {
  const businessId = getAssignedBusinessId(day, period)
  if (!businessId || !event.dataTransfer) return

  event.dataTransfer.setData('application/business-id', businessId)
  event.dataTransfer.setData('application/from-day', day)
  event.dataTransfer.setData('application/from-period', String(period))
  event.dataTransfer.effectAllowed = 'move'
}

// Drop zone'ları kaydetmek için native event listener kullanıyoruz
const dropZoneCleanups: (() => void)[] = []

function registerDropZone(el: HTMLElement | null, day: string, period: number) {
  if (!el) return

  const onDragOver = (e: DragEvent) => {
    if (props.disabled) return
    e.preventDefault()
    e.dataTransfer!.dropEffect = 'move'
    el.classList.add('drop-zone--active')
  }

  const onDragLeave = () => {
    el.classList.remove('drop-zone--active')
  }

  const onDrop = (e: DragEvent) => {
    e.preventDefault()
    el.classList.remove('drop-zone--active')
    if (props.disabled) return

    const businessId = e.dataTransfer?.getData('application/business-id')
    const fromDay = e.dataTransfer?.getData('application/from-day')
    const fromPeriod = e.dataTransfer?.getData('application/from-period')

    if (!businessId) return

    // Eski konumdan kaldır (grid'den grid'e taşıma)
    if (fromDay && fromPeriod) {
      emit('business-removed', {
        businessId,
        day: fromDay,
        periodNumber: parseInt(fromPeriod),
      })
    }

    // Yeni konuma ata
    emit('business-dropped', { businessId, day, periodNumber: period })
  }

  el.addEventListener('dragover', onDragOver)
  el.addEventListener('dragleave', onDragLeave)
  el.addEventListener('drop', onDrop)

  dropZoneCleanups.push(() => {
    el.removeEventListener('dragover', onDragOver)
    el.removeEventListener('dragleave', onDragLeave)
    el.removeEventListener('drop', onDrop)
  })
}

// ── Klavye erişilebilirliği (#88) ──

function isSelected(day: string, period: number): boolean {
  return props.selected?.fromDay === day && props.selected?.fromPeriod === period
}

function assignedSlotLabel(day: string, dayLabel: string, period: number): string {
  return `${getBusinessName(day, period)}, ${dayLabel} ${period}. ders. Taşımak için Enter'a basın.`
}

function dropZoneLabel(dayLabel: string, period: number): string {
  return props.selected
    ? `Boş: ${dayLabel} ${period}. ders. ${props.selected.businessName} işletmesini buraya atamak için Enter'a basın.`
    : `Boş: ${dayLabel} ${period}. ders. Önce bir işletme seçin.`
}

function onAssignedKey(day: string, period: number) {
  if (props.disabled) return
  const businessId = getAssignedBusinessId(day, period)
  if (!businessId) return

  emit('business-selected', {
    businessId,
    businessName: getBusinessName(day, period),
    fromDay: day,
    fromPeriod: period,
  })
}

function onDropZoneKey(day: string, period: number) {
  if (props.disabled || !props.selected) return

  // Izgaradan alındıysa önce eski konumdan kaldır — sürükle-bırak yolundaki davranışın aynısı.
  if (props.selected.fromDay !== undefined && props.selected.fromPeriod !== undefined) {
    emit('business-removed', {
      businessId: props.selected.businessId,
      day: props.selected.fromDay,
      periodNumber: props.selected.fromPeriod,
    })
  }

  emit('business-dropped', { businessId: props.selected.businessId, day, periodNumber: period })
}

function onRemoveClick(day: string, period: number) {
  const businessId = getAssignedBusinessId(day, period)
  if (businessId) {
    emit('business-removed', { businessId, day, periodNumber: period })
  }
}

onBeforeUnmount(() => {
  dropZoneCleanups.forEach((fn) => fn())
})
</script>

<style scoped>
.assignment-grid table {
  border-collapse: collapse;
}

.assignment-grid th,
.assignment-grid td {
  border: 1px solid #e0e0e0;
  padding: 4px 6px;
}

.period-header {
  width: 50px;
  background: #fafafa;
  text-align: center;
}

.day-header {
  text-align: center;
  background: #fafafa;
  min-width: 120px;
}

.period-label {
  text-align: center;
  color: #555;
}

.grid-cell {
  min-height: 48px;
  vertical-align: middle;
}

.cell-occupied {
  background: #f5f5f5;
}

.cell-free {
  background: #e8f5e9;
}

.cell-assigned {
  background: #e3f2fd;
}

.occupied-cell {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 4px;
  opacity: 0.6;
}

/* Klavyeyle seçili chip ve seçim varken vurgulanan boş hücreler (#88) */
.assigned-slot--selected {
  outline: 2px solid var(--q-primary);
  outline-offset: 1px;
}

.drop-zone--target {
  border-style: dashed;
  border-color: var(--q-primary);
}

.drop-zone {
  min-height: 40px;
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 4px;
  border: 2px dashed transparent;
  border-radius: 4px;
  transition: all 0.2s;
  opacity: 0.6;
}

.drop-zone:not(.drop-zone--disabled):hover {
  border-color: #a5d6a7;
  opacity: 1;
}

.drop-zone--active {
  border-color: #66bb6a !important;
  background: #c8e6c9 !important;
  opacity: 1 !important;
}

.drop-zone--disabled {
  cursor: not-allowed;
}

.assigned-slot {
  cursor: grab;
}

.assigned-slot:active {
  cursor: grabbing;
}

.business-chip {
  display: flex;
  align-items: center;
  gap: 4px;
  background: #bbdefb;
  border-radius: 6px;
  padding: 4px 8px;
  min-height: 32px;
}

.chip-label {
  max-width: 80px;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.remove-btn {
  margin-left: auto;
}
</style>
