<template>
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
        attribution="&copy; <a href='https://www.openstreetmap.org/copyright'>OpenStreetMap</a> contributors"
        layer-type="base"
        name="OpenStreetMap"
      />

      <!-- Okul marker (buyuk mavi) -->
      <l-marker
        v-if="schoolLocation"
        :lat-lng="[schoolLocation.latitude, schoolLocation.longitude]"
      >
        <l-icon
          :icon-size="[32, 32]"
          :icon-anchor="[16, 32]"
          icon-url="data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' width='32' height='32' viewBox='0 0 24 24' fill='%231565C0'%3E%3Cpath d='M12 2L1 7l11 5 9-4.09V17h2V7L12 2zm0 14.18L3 12.09V17l9 5 9-5v-4.91l-9 4.09z'/%3E%3C/svg%3E"
        />
        <l-popup>
          <strong>Okul</strong>
        </l-popup>
      </l-marker>

      <!-- Mesafe cemberleri -->
      <template v-if="schoolLocation && showCircles">
        <l-circle
          v-for="circle in distanceCircles"
          :key="circle.radius"
          :lat-lng="[schoolLocation.latitude, schoolLocation.longitude]"
          :radius="circle.radius"
          :color="circle.color"
          :fill-color="circle.color"
          :fill-opacity="0.05"
          :weight="1"
          :dash-array="'5,5'"
        >
          <l-popup>{{ circle.label }}</l-popup>
        </l-circle>
      </template>

      <!-- Isletme marker'lari -->
      <l-marker
        v-for="biz in businessesWithLocation"
        :key="biz.businessId"
        :lat-lng="[biz.latitude, biz.longitude]"
      >
        <l-icon
          :icon-size="[20, 20]"
          :icon-anchor="[10, 20]"
          :icon-url="markerIcon(biz)"
        />
        <l-popup>
          <div>
            <strong>{{ biz.businessName }}</strong>
            <br />
            <span v-if="biz.distanceToSchoolKm != null">
              Uzaklik: {{ biz.distanceToSchoolKm.toFixed(1) }} km
            </span>
            <br />
            <span v-if="biz.assignedTeacherName">
              Ogretmen: {{ biz.assignedTeacherName }}
            </span>
            <span v-else class="text-red">Atanmamis</span>
          </div>
        </l-popup>
      </l-marker>
    </l-map>
  </div>
</template>

<script setup lang="ts">
import 'leaflet/dist/leaflet.css'
import { ref, computed } from 'vue'
import { LMap, LTileLayer, LMarker, LIcon, LPopup, LCircle } from '@vue-leaflet/vue-leaflet'
import type { BusinessAssignmentDto } from 'src/api/coordination'

interface GeoLocation {
  latitude: number
  longitude: number
}

interface BusinessWithLocation extends BusinessAssignmentDto {
  latitude: number
  longitude: number
}

const props = withDefaults(defineProps<{
  schoolLocation: GeoLocation | null
  businesses: BusinessAssignmentDto[]
  height?: string
  showCircles?: boolean
}>(), {
  schoolLocation: null,
  height: '400px',
  showCircles: true,
})

const TURKEY_CENTER: [number, number] = [39.0, 35.0]
const zoom = ref(13)
const mapRef = ref(null)

const mapCenter = computed<[number, number]>(() =>
  props.schoolLocation
    ? [props.schoolLocation.latitude, props.schoolLocation.longitude]
    : TURKEY_CENTER,
)

const distanceCircles = [
  { radius: 1000, color: '#4CAF50', label: '1 km — 2 saat' },
  { radius: 3000, color: '#FF9800', label: '3 km — 4 saat' },
  { radius: 5000, color: '#F44336', label: '5 km — 6 saat' },
]

const businessesWithLocation = computed<BusinessWithLocation[]>(() => {
  // Sadece adres/lokasyonu olanlari goster
  // BusinessAssignmentDto'da lat/lng yok — bu bilgi ileride eklenebilir
  // Simdilik distanceToSchoolKm > 0 olanlari goster (lokasyon mevcutsa)
  return [] // Placeholder — BusinessAssignmentDto'ya lat/lng eklendikten sonra doldurulacak
})

function markerIcon(biz: BusinessWithLocation): string {
  const color = biz.assignedTeacherName ? '%2343A047' : '%23E53935'
  return `data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' width='20' height='20' viewBox='0 0 24 24' fill='${color}'%3E%3Cpath d='M12 2C8.13 2 5 5.13 5 9c0 5.25 7 13 7 13s7-7.75 7-13c0-3.87-3.13-7-7-7zm0 9.5c-1.38 0-2.5-1.12-2.5-2.5s1.12-2.5 2.5-2.5 2.5 1.12 2.5 2.5-1.12 2.5-2.5 2.5z'/%3E%3C/svg%3E`
}

function onMapReady() {
  if (!props.schoolLocation) {
    zoom.value = 6
  }
}
</script>
