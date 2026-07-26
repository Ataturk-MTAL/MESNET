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
              class="drop-zone"
              :class="{
                'drop-zone--disabled': disabled,
                'drop-zone--target': !disabled && selected !== null,
                'drop-zone--active': activeDropZone === dropZoneKey(day.value, period),
              }"
              role="button"
              tabindex="0"
              :aria-disabled="disabled"
              :aria-label="dropZoneLabel(day.label, period)"
              @dragover="onDropZoneDragOver($event, day.value, period)"
              @dragleave="onDropZoneDragLeave(day.value, period)"
              @drop="onDropZoneDrop($event, day.value, period)"
              @keydown.enter.prevent="onDropZoneKey(day.value, period)"
              @keydown.space.prevent="onDropZoneKey(day.value, period)"
              @keydown.esc.prevent="emit('selection-cancelled')"
            >
              <q-icon
                name="add_circle_outline"
                size="16px"
                color="positive"
              />
              <span class="text-caption text-positive">Boş</span>
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
                  color="info"
                />
                <span class="text-caption text-info-strong ellipsis chip-label">
                  {{ getBusinessName(day.value, period) }}
                </span>
                <q-btn
                  v-if="!disabled"
                  flat
                  round
                  dense
                  icon="close"
                  size="8px"
                  color="negative"
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
        color="positive-soft"
        text-color="positive-strong"
        dense
        size="sm"
      >
        Boş: {{ freeCount }}
      </q-chip>
      <q-chip
        icon="business"
        color="info-soft"
        text-color="info-strong"
        dense
        size="sm"
      >
        Atanmış: {{ assignedCount }}
      </q-chip>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed, ref } from 'vue'
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

// Drop zone olayları şablonda bildirimsel olarak bağlanır (@dragover/@dragleave/@drop).
//
// Daha önce burada bir `:ref="(el) => registerDropZone(el, ...)"` geri çağrısı vardı ve
// içinde el.addEventListener çağrılıyordu. Vue, FONKSİYON tipindeki template ref'i her
// yamada yeniden çalıştırır ve satır içi ok fonksiyonu her render'da yeni kimlik aldığı
// için eskisi hiç null ile çağrılmaz — yani her render aynı DOM düğümüne 3 listener daha
// ekliyordu, temizlik ise yalnız onBeforeUnmount'ta yapılıyordu. Ölçüm: mount'ta 30
// listener, 5 prop güncellemesinden sonra 165. Sonuç: tek bırakma işleminde
// business-dropped/business-removed olayları defalarca yayılabiliyordu.
// Şablon bağlaması bu riski tamamen ortadan kaldırır — Vue listener kimliğini kendi yönetir.

/** Sürükleme sırasında vurgulanan hücre; classList yerine reaktif durum. */
const activeDropZone = ref<string | null>(null)

function dropZoneKey(day: string, period: number): string {
  return `${day}-${period}`
}

function onDropZoneDragOver(event: DragEvent, day: string, period: number) {
  if (props.disabled) return
  // preventDefault YALNIZ etkin hücrede: aksi halde devre dışı hücre de bırakmayı kabul eder.
  event.preventDefault()
  if (event.dataTransfer) event.dataTransfer.dropEffect = 'move'
  activeDropZone.value = dropZoneKey(day, period)
}

function onDropZoneDragLeave(day: string, period: number) {
  // Yalnız AYRILAN hücre hâlâ vurguluysa temizle. Hücreden hücreye geçerken tarayıcı
  // dragover(yeni) olayını dragleave(eski) olayından önce yayabilir; koşulsuz silmek
  // yeni hücrenin vurgusunu anında söndürür (eski classList'li kodda bu olmuyordu,
  // çünkü her hücre kendi sınıfını yönetiyordu).
  if (activeDropZone.value === dropZoneKey(day, period)) {
    activeDropZone.value = null
  }
}

function onDropZoneDrop(event: DragEvent, day: string, period: number) {
  event.preventDefault()
  activeDropZone.value = null
  if (props.disabled) return

  const businessId = event.dataTransfer?.getData('application/business-id')
  const fromDay = event.dataTransfer?.getData('application/from-day')
  const fromPeriod = event.dataTransfer?.getData('application/from-period')

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

/* Ham Material tonları yerine tema türevi (#104): tema değişince hücre zeminleri de kayar. */
.cell-free {
  background: #e2ede8;
  background: color-mix(in srgb, var(--q-positive) 14%, #fff);
}

.cell-assigned {
  background: #e8edf1;
  background: color-mix(in srgb, var(--q-info) 12%, #fff);
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
  /* transition: all yerine değişen özellikler — tarayıcı her özelliği izlemesin,
     layout özellikleri yanlışlıkla animasyonlanmasın. */
  transition-property: border-color, background-color, opacity;
  transition-duration: 0.2s;
  transition-timing-function: ease-out;
  opacity: 0.6;
}

.drop-zone:not(.drop-zone--disabled):hover {
  border-color: #a1c4b5;
  border-color: color-mix(in srgb, var(--q-positive) 45%, #fff);
  opacity: 1;
}

.drop-zone--active {
  border-color: #77aa94 !important;
  border-color: color-mix(in srgb, var(--q-positive) 65%, #fff) !important;
  background: #cbded6 !important;
  background: color-mix(in srgb, var(--q-positive) 25%, #fff) !important;
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
  background: #d5dee5;
  background: color-mix(in srgb, var(--q-info) 22%, #fff);
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
  /* İkon 8px kalıyor (hücre dar) ama dokunma hedefi WCAG 2.2 SC 2.5.8'in istediği
     24x24 CSS px'e çıkarılıyor. Görünmez pseudo-element yerine butonun kendisi
     büyütüldü: genişletilmiş hedef chip etiketiyle çakışıp yanlış tıklama üretmesin. */
  min-width: 24px;
  min-height: 24px;
}
</style>
