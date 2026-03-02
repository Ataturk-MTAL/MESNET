<template>
  <div class="schedule-grid">
    <table class="full-width">
      <thead>
        <tr>
          <th class="text-left text-caption text-grey-7" style="width: 60px">Saat</th>
          <th
            v-for="day in days"
            :key="day.value"
            class="text-center text-caption text-grey-7"
          >
            {{ day.label }}
          </th>
        </tr>
      </thead>
      <tbody>
        <tr v-for="period in periodCount" :key="period">
          <td class="text-center text-weight-medium text-grey-8">{{ period }}.</td>
          <td
            v-for="day in days"
            :key="day.value"
            class="schedule-cell"
            :class="cellClass(day.value, period)"
            @click="onCellClick(day.value, period)"
          >
            <div v-if="isOccupied(day.value, period)" class="text-caption text-grey-8">
              Dolu
            </div>
            <div v-else class="text-caption text-green-8">
              Boş
            </div>
          </td>
        </tr>
      </tbody>
    </table>

    <!-- Özet -->
    <div class="row q-mt-md q-gutter-md">
      <q-chip icon="event_busy" color="grey-3" text-color="grey-8" dense>
        Dolu: {{ occupiedCount }}
      </q-chip>
      <q-chip icon="event_available" color="green-1" text-color="green-8" dense>
        Boş: {{ freeCount }}
      </q-chip>
      <q-chip icon="calendar_today" color="blue-1" text-color="blue-8" dense>
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

function cellClass(dayValue: string, periodNumber: number): string {
  const occupied = isOccupied(dayValue, periodNumber)
  const base = props.editing ? 'cursor-pointer ' : ''
  return base + (occupied ? 'bg-grey-2' : 'bg-green-1')
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
  border: 1px solid #e0e0e0;
  padding: 6px 8px;
}

.schedule-grid th {
  background: #fafafa;
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
</style>
