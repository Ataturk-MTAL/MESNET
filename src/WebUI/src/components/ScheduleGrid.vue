<template>
  <div class="schedule-grid">
    <table
      class="full-width tabular-nums"
      aria-label="Ders programı tablosu. Satırlar ders saatini, sütunlar haftanın gününü gösterir; her hücre dolu veya boş durumundadır."
    >
      <thead>
        <tr>
          <th
            scope="col"
            class="text-left text-caption text-weight-medium text-grey-8"
            style="width: 60px"
          >
            Saat
          </th>
          <th
            v-for="day in days"
            :key="day.value"
            scope="col"
            class="text-center text-caption text-weight-medium text-grey-8"
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
          <th
            scope="row"
            class="text-center text-weight-medium text-grey-8"
          >
            {{ period }}.
          </th>
          <td
            v-for="day in days"
            :key="day.value"
            class="schedule-cell"
            :class="cellClass(day.value, period)"
            @click="onCellClick(day.value, period)"
          >
            <!-- Hücre klavyeyle de çevrilebilir (WCAG 2.1.1): Enter/Space fare
              tıklamasıyla AYNI handler'ı çağırır. Düzenleme kipi dışında
              aria-disabled ile bildirilir; onCellClick zaten erken döner. -->
            <div
              role="button"
              tabindex="0"
              :aria-disabled="!editing"
              :aria-pressed="isOccupied(day.value, period)"
              :aria-label="cellLabel(day.label, day.value, period)"
              @keydown.enter.prevent="onCellClick(day.value, period)"
              @keydown.space.prevent="onCellClick(day.value, period)"
            >
              <div
                v-if="isOccupied(day.value, period)"
                class="text-caption text-grey-8"
              >
                Dolu
              </div>
              <div
                v-else
                class="text-caption text-positive-strong"
              >
                Boş
              </div>
            </div>
          </td>
        </tr>
      </tbody>
    </table>

    <!-- Özet -->
    <div class="row q-mt-md q-gutter-md tabular-nums">
      <!-- Ham palet tonu (grey-3 #eeeeee) tema değişince yerinde donardı. Çip, özetlediği
           "Dolu" hücresiyle AYNI tonu kullanır: bg-neutral-soft (#edeff2) + grey-8 (#616161)
           = 5,38:1 (ölçüldü, eşik 4,5:1). Kardeş çipler de tema türevi. -->
      <q-chip
        icon="event_busy"
        color="neutral-soft"
        text-color="grey-8"
        dense
      >
        Dolu: {{ occupiedCount }}
      </q-chip>
      <q-chip
        icon="event_available"
        color="positive-soft"
        text-color="positive-strong"
        dense
      >
        Boş: {{ freeCount }}
      </q-chip>
      <q-chip
        icon="calendar_today"
        color="info-soft"
        text-color="info-strong"
        dense
      >
        Toplam: {{ totalSlots }}
      </q-chip>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import type { DailyScheduleInput, PeriodSlotInput } from 'src/api/coordination'

const props = defineProps<{
  schedule: DailyScheduleInput[]
  periodCount: number
  editing: boolean
}>()

const emit = defineEmits<{
  (e: 'update:schedule', value: DailyScheduleInput[]): void
}>()

const days = [
  { label: 'Pazartesi', value: 'Monday' },
  { label: 'Salı', value: 'Tuesday' },
  { label: 'Çarşamba', value: 'Wednesday' },
  { label: 'Perşembe', value: 'Thursday' },
  { label: 'Cuma', value: 'Friday' },
]

function findSlot(dayValue: string, periodNumber: number): PeriodSlotInput | undefined {
  const day = props.schedule.find((d) => d.day === dayValue)
  return day?.periods.find((p) => p.periodNumber === periodNumber)
}

function isOccupied(dayValue: string, periodNumber: number): boolean {
  const slot = findSlot(dayValue, periodNumber)
  return slot?.status === 'Occupied'
}

function cellLabel(dayLabel: string, dayValue: string, periodNumber: number): string {
  const durum = isOccupied(dayValue, periodNumber) ? 'dolu' : 'boş'
  return `${dayLabel} ${periodNumber}. ders: ${durum}`
}

// Dolu hücre zemini ham Quasar tonu (`bg-grey-2`, #f5f5f5) DEĞİL, tema türevi nötr:
// `bg-neutral-soft` (app.css) = color-mix(in srgb, var(--q-primary) 8%, #fff), düz hex
// yedeği #edeff2 — kardeş AssignmentGrid'in `.cell-occupied` zeminiyle birebir aynı.
// Ham palet tonu kiracı teması değişince yerinde donardı, türetilmiş ton birlikte kayar.
// ÖLÇÜLDÜ: hücre metni text-grey-8 (#616161) bu zemin üzerinde 5,38:1 (eşik 4,5:1).
function cellClass(dayValue: string, periodNumber: number): string {
  const occupied = isOccupied(dayValue, periodNumber)
  const base = props.editing ? 'cursor-pointer ' : ''
  return base + (occupied ? 'bg-neutral-soft' : 'bg-positive-soft')
}

function onCellClick(dayValue: string, periodNumber: number) {
  if (!props.editing) return

  const newSchedule = props.schedule.map((day) => {
    if (day.day !== dayValue) return day
    return {
      ...day,
      periods: day.periods.map((p) => {
        if (p.periodNumber !== periodNumber) return p
        return {
          ...p,
          status: p.status === 'Occupied' ? 'Free' : 'Occupied',
          courseName: undefined,
        }
      }),
    }
  })
  emit('update:schedule', newSchedule)
}

const totalSlots = computed(() => props.periodCount * 5)
const occupiedCount = computed(() => {
  let count = 0
  for (const day of props.schedule) {
    for (const p of day.periods) {
      if (p.status === 'Occupied') count++
    }
  }
  return count
})
const freeCount = computed(() => totalSlots.value - occupiedCount.value)
</script>

<style scoped>
.schedule-grid table {
  border-collapse: collapse;
}

.schedule-grid th,
.schedule-grid td {
  border: 1px solid rgba(30, 58, 95, 0.14);
  border: 1px solid color-mix(in srgb, var(--q-primary) 14%, transparent);
  padding: 6px 8px;
}

/* Başlık bandı dolu hücreden BİR BASAMAK koyu: ikisi de primary türevi olduğu için tema
   değişince birlikte kayar, ama rolleri ayrışır (bant %12 = #e4e7ec, dolu hücre
   .bg-neutral-soft %8 = #edeff2). Eskiden ikisi de #edeff2 idi ve aynı punto/ağırlık/
   hizalamayla birlikte tümü dolu bir satır ikinci bir başlık satırı gibi okunuyordu.
   Ayrım tek başına tonla bırakılmıyor; başlık ayrıca text-weight-medium taşır.
   ÖLÇÜLDÜ: text-grey-8 (#616161) bu bant üzerinde 4,996:1 (eşik 4,5:1). */
.schedule-grid thead th {
  background: #e4e7ec;
  background: color-mix(in srgb, var(--q-primary) 12%, #fff);
}

.schedule-cell {
  min-width: 100px;
  min-height: 36px;
  text-align: center;
  transition: background-color 0.15s;
}

.schedule-cell.cursor-pointer:hover {
  filter: brightness(0.95);
}

[role="button"]:focus-visible {
  outline: 2px solid var(--q-primary);
  outline-offset: -2px;
}
</style>
