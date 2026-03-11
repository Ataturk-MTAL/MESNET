<template>
  <div class="cluster-map-wrapper">
    <!-- Küme Özeti Chips -->
    <div v-if="clusterSummary.length > 0" class="row q-gutter-xs q-mb-sm">
      <q-chip
        v-for="c in clusterSummary"
        :key="c.id ?? -1"
        dense
        :style="{ backgroundColor: c.color, color: '#fff' }"
        icon="place"
        size="sm"
      >
        {{ c.id === null ? 'Tekil' : `Küme ${c.id + 1}` }}: {{ c.count }} işletme
      </q-chip>
      <q-chip v-if="selectedRoute" dense color="blue-7" text-color="white" icon="route" size="sm">
        {{ selectedRoute.distanceKm.toFixed(1) }} km · {{ formatDuration(selectedRoute.durationSec) }}
      </q-chip>
      <q-btn
        v-if="selectedRoute"
        flat
        dense
        round
        icon="close"
        size="xs"
        color="grey-6"
        @click="clearRoute"
      />
    </div>

    <!-- Harita -->
    <div :style="{ height, width: '100%' }">
      <l-map
        ref="mapRef"
        v-model:zoom="zoom"
        :center="mapCenter"
        :use-global-leaflet="false"
        @ready="onMapReady"
      >
        <l-tile-layer
          url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png"
          attribution="&copy; <a href='https://www.openstreetmap.org/copyright'>OpenStreetMap</a>"
          layer-type="base"
          name="OpenStreetMap"
        />

        <!-- Okul marker -->
        <l-marker
          v-if="schoolLocation"
          :lat-lng="[schoolLocation.latitude, schoolLocation.longitude]"
        >
          <l-icon
            :icon-size="[32, 32]"
            :icon-anchor="[16, 32]"
            :icon-url="SCHOOL_ICON_URL"
          />
          <l-popup><strong>Okul</strong></l-popup>
        </l-marker>

        <!-- Seçili işletme yol çizgisi -->
        <l-polyline
          v-if="selectedRoute"
          :lat-lngs="selectedRoute.polyline"
          color="#1565C0"
          :weight="4"
          :opacity="0.8"
          :dash-array="undefined"
        />

        <!-- İşletme marker'ları — kümeye göre renkli -->
        <l-circle-marker
          v-for="biz in businesses"
          :key="biz.businessId"
          :lat-lng="[biz.latitude, biz.longitude]"
          :radius="selectedRoute?.businessId === biz.businessId ? 10 : 7"
          :color="selectedRoute?.businessId === biz.businessId ? '#1565C0' : clusterColor(biz.clusterId)"
          :fill-color="selectedRoute?.businessId === biz.businessId ? '#1565C0' : clusterColor(biz.clusterId)"
          :fill-opacity="0.85"
          :weight="selectedRoute?.businessId === biz.businessId ? 3 : 1.5"
          @click="onMarkerClick(biz)"
        >
          <l-popup>
            <div style="min-width: 180px">
              <div class="text-weight-bold q-mb-xs">{{ biz.businessName }}</div>
              <div v-if="biz.district" class="text-caption text-grey-7">{{ biz.district }}</div>
              <q-separator class="q-my-xs" />
              <div class="text-caption">
                <span class="text-grey-6">Alan:</span> {{ biz.branchName }}
              </div>
              <div v-if="biz.distanceToSchoolKm != null" class="text-caption">
                <span class="text-grey-6">Uzaklık:</span> {{ biz.distanceToSchoolKm.toFixed(1) }} km
              </div>
              <div class="text-caption">
                <span class="text-grey-6">Öğrenci:</span> {{ biz.activeStudentCount }}
              </div>
              <div class="text-caption">
                <span class="text-grey-6">Verilebilir Maks:</span>
                <strong class="text-green-8">{{ biz.maxCoordinationHours }} saat</strong>
              </div>
              <q-separator class="q-my-xs" />
              <div v-if="biz.isAssigned" class="text-caption text-positive">
                <q-icon name="check_circle" size="12px" /> {{ biz.assignedTeacherName }}
              </div>
              <div v-else class="text-caption text-negative">
                <q-icon name="warning" size="12px" /> Atanmamış
              </div>
              <div class="text-caption text-grey-6 q-mt-xs">
                {{ biz.clusterId === null ? 'Tekil nokta' : `Küme ${biz.clusterId + 1}` }}
              </div>
              <!-- Takdir edilen saat girişi -->
              <template v-if="editable">
                <q-separator class="q-my-xs" />
                <div class="row items-center q-gutter-xs">
                  <q-input
                    :model-value="getPopupHours(biz.businessId)"
                    type="number"
                    dense
                    outlined
                    label="Takdir Edilen Saat"
                    :min="1"
                    :max="biz.maxCoordinationHours"
                    :hint="`Maks: ${biz.maxCoordinationHours}`"
                    style="width: 120px"
                    @update:model-value="v => setPopupHours(biz.businessId, Number(v))"
                  />
                  <q-btn
                    flat
                    dense
                    round
                    icon="check"
                    color="positive"
                    size="sm"
                    @click.stop="savePopupHours(biz.businessId)"
                  >
                    <q-tooltip>Kaydet ve kapat</q-tooltip>
                  </q-btn>
                </div>
              </template>
              <template v-else-if="assignedHours[biz.businessId]">
                <div class="text-caption">
                  <span class="text-grey-6">Takdir Edilen:</span> {{ assignedHours[biz.businessId] }} saat
                </div>
              </template>

              <q-separator class="q-my-xs" />
              <q-btn
                v-if="schoolLocation"
                flat
                dense
                size="xs"
                color="primary"
                icon="route"
                label="Yolu Göster"
                :loading="routeLoading === biz.businessId"
                @click.stop="fetchRoute(biz)"
              />
            </div>
          </l-popup>
        </l-circle-marker>
      </l-map>
    </div>
  </div>
</template>

<script setup lang="ts">
import 'leaflet/dist/leaflet.css'
import { ref, computed } from 'vue'
import { LMap, LTileLayer, LMarker, LIcon, LPopup, LCircleMarker, LPolyline } from '@vue-leaflet/vue-leaflet'
import type { BusinessClusterDto } from 'src/api/coordination'
import type { GeoLocation } from 'src/api/institution'
import { SCHOOL_ICON_URL } from 'src/utils/mapConstants'

interface RouteState {
  businessId: string
  polyline: [number, number][]
  distanceKm: number
  durationSec: number
}

const props = withDefaults(defineProps<{
  businesses: BusinessClusterDto[]
  schoolLocation: GeoLocation | null
  height?: string
  assignedHours?: Record<string, number>
  editable?: boolean
}>(), {
  height: '500px',
  schoolLocation: null,
  assignedHours: () => ({}),
  editable: false,
})

const emit = defineEmits<{
  'update:hours': [businessId: string, hours: number]
}>()

// ── Popup saat düzenleme ──
const popupHoursInput = ref<Record<string, number>>({})

function getPopupHours(businessId: string): number {
  return popupHoursInput.value[businessId] ?? props.assignedHours[businessId] ?? 0
}

function setPopupHours(businessId: string, val: number) {
  popupHoursInput.value[businessId] = val
}

function savePopupHours(businessId: string) {
  const val = popupHoursInput.value[businessId]
  if (val != null && val > 0) {
    emit('update:hours', businessId, val)
    // Popup'ı kapat
    if (mapRef.value) {
      ;(mapRef.value as { leafletObject?: { closePopup?: () => void } }).leafletObject?.closePopup?.()
    }
  }
}

// ── Renk Paleti ──
const CLUSTER_PALETTE = [
  '#1E88E5', '#43A047', '#FB8C00', '#E53935', '#8E24AA',
  '#00ACC1', '#F4511E', '#6D4C41', '#00897B', '#C0CA33',
  '#3949AB', '#D81B60', '#039BE5', '#7CB342', '#FFB300',
  '#5E35B1', '#0097A7', '#2E7D32', '#BF360C', '#4527A0',
]
const NOISE_COLOR = '#9E9E9E'

function clusterColor(clusterId: number | null): string {
  if (clusterId === null) return NOISE_COLOR
  return CLUSTER_PALETTE[clusterId % CLUSTER_PALETTE.length] ?? NOISE_COLOR
}

// ── Küme Özeti ──
const clusterSummary = computed(() => {
  const map = new Map<number | null, number>()
  for (const b of props.businesses) {
    map.set(b.clusterId, (map.get(b.clusterId) ?? 0) + 1)
  }
  return Array.from(map.entries())
    .sort(([a], [b]) => {
      if (a === null) return 1
      if (b === null) return -1
      return a - b
    })
    .map(([id, count]) => ({ id, count, color: clusterColor(id) }))
})

// ── Harita Merkezi ──
const MERSIN_CENTER: [number, number] = [36.8, 34.63]
const zoom = ref(13)
const mapRef = ref(null)

const mapCenter = computed<[number, number]>(() => {
  if (props.schoolLocation) return [props.schoolLocation.latitude, props.schoolLocation.longitude]
  if (props.businesses.length > 0) {
    const avgLat = props.businesses.reduce((s, b) => s + b.latitude, 0) / props.businesses.length
    const avgLon = props.businesses.reduce((s, b) => s + b.longitude, 0) / props.businesses.length
    return [avgLat, avgLon]
  }
  return MERSIN_CENTER
})


function onMapReady() {
  if (!props.schoolLocation && props.businesses.length === 0) zoom.value = 11
}

// ── Rota ──
const OSRM_URL = import.meta.env.VITE_OSRM_URL ?? 'http://localhost:5002'

const selectedRoute = ref<RouteState | null>(null)
const routeLoading = ref<string | null>(null)

function onMarkerClick(biz: BusinessClusterDto) {
  // Aynı marker'a tekrar tıklanınca rotayı temizle
  if (selectedRoute.value?.businessId === biz.businessId) {
    clearRoute()
  }
}

function clearRoute() {
  selectedRoute.value = null
}

async function fetchRoute(biz: BusinessClusterDto) {
  if (!props.schoolLocation) return

  routeLoading.value = biz.businessId
  try {
    const school = props.schoolLocation
    // OSRM koordinat sırası: longitude,latitude
    const coords = `${school.longitude},${school.latitude};${biz.longitude},${biz.latitude}`
    const url = `${OSRM_URL}/route/v1/driving/${coords}?overview=full&geometries=geojson`

    const res = await fetch(url)
    const data = await res.json() as {
      code: string
      routes: Array<{
        distance: number
        duration: number
        geometry: { coordinates: [number, number][] }
      }>
    }

    if (data.code !== 'Ok' || !data.routes.length) return

    const route = data.routes[0]!
    // GeoJSON: [lon, lat] → Leaflet: [lat, lon]
    const polyline = route.geometry.coordinates.map(
      ([lon, lat]) => [lat, lon] as [number, number],
    )

    selectedRoute.value = {
      businessId: biz.businessId,
      polyline,
      distanceKm: route.distance / 1000,
      durationSec: route.duration,
    }
  } finally {
    routeLoading.value = null
  }
}

function formatDuration(seconds: number): string {
  const m = Math.round(seconds / 60)
  if (m < 60) return `${m} dk`
  return `${Math.floor(m / 60)} sa ${m % 60} dk`
}
</script>

<style scoped>
.cluster-map-wrapper {
  width: 100%;
}
</style>
