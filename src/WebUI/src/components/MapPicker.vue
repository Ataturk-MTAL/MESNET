<template>
  <div>
    <!-- Adres arama çubuğu -->
    <div v-if="!readonly" class="q-mb-sm" style="position: relative">
      <q-input
        v-model="searchQuery"
        label="Adres veya işletme ara..."
        filled
        dense
        clearable
        @keyup.enter="searchAddress"
        @clear="searchResults = []"
      >
        <template #prepend>
          <q-icon name="search" />
        </template>
        <template #append>
          <q-btn flat dense round icon="search" :loading="searching" @click="searchAddress" />
        </template>
      </q-input>
      <q-list
        v-if="searchResults.length > 0"
        bordered
        dense
        class="bg-white"
        style="position: absolute; z-index: 1000; width: 100%; max-height: 200px; overflow-y: auto"
      >
        <q-item
          v-for="result in searchResults"
          :key="result.place_id"
          clickable
          v-close-popup
          @click="selectResult(result)"
        >
          <q-item-section avatar>
            <q-icon name="place" size="sm" color="primary" />
          </q-item-section>
          <q-item-section>
            <q-item-label lines="2" class="text-body2">{{ result.display_name }}</q-item-label>
          </q-item-section>
        </q-item>
      </q-list>
    </div>

    <!-- Harita -->
    <div :style="{ height, width: '100%' }">
      <l-map
        ref="mapRef"
        v-model:zoom="zoom"
        :center="center"
        :use-global-leaflet="false"
        @click="onMapClick"
        @ready="onMapReady"
      >
        <l-tile-layer
          url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png"
          attribution="&copy; <a href='https://www.openstreetmap.org/copyright'>OpenStreetMap</a> contributors"
          layer-type="base"
          name="OpenStreetMap"
        />
        <l-marker
          v-if="markerPosition"
          :lat-lng="markerPosition"
          :draggable="!readonly"
          @moveend="onMarkerDrag"
        />
      </l-map>
    </div>
    <div v-if="modelValue" class="text-caption text-grey q-mt-xs">
      Enlem: {{ modelValue.latitude.toFixed(6) }}, Boylam: {{ modelValue.longitude.toFixed(6) }}
    </div>
  </div>
</template>

<script setup lang="ts">
import 'leaflet/dist/leaflet.css'
import { ref, computed, watch } from 'vue'
import { LMap, LTileLayer, LMarker } from '@vue-leaflet/vue-leaflet'
import type { LeafletMouseEvent } from 'leaflet'
import type { GeoLocation } from 'src/api/institution'

interface NominatimResult {
  place_id: number
  display_name: string
  lat: string
  lon: string
}

const props = withDefaults(defineProps<{
  modelValue: GeoLocation | null
  readonly?: boolean
  height?: string
}>(), {
  modelValue: null,
  readonly: false,
  height: '300px',
})

const emit = defineEmits<{
  'update:modelValue': [value: GeoLocation | null]
}>()

const TURKEY_CENTER: [number, number] = [39.0, 35.0]
const DEFAULT_ZOOM = 6
const SELECTED_ZOOM = 15

const zoom = ref(props.modelValue ? SELECTED_ZOOM : DEFAULT_ZOOM)
const mapRef = ref<InstanceType<typeof LMap> | null>(null)

// Arama durumu
const searchQuery = ref('')
const searchResults = ref<NominatimResult[]>([])
const searching = ref(false)

const center = computed<[number, number]>(() =>
  props.modelValue
    ? [props.modelValue.latitude, props.modelValue.longitude]
    : TURKEY_CENTER,
)

const markerPosition = computed<[number, number] | null>(() =>
  props.modelValue
    ? [props.modelValue.latitude, props.modelValue.longitude]
    : null,
)

function onMapClick(event: LeafletMouseEvent) {
  if (props.readonly) return
  const { lat, lng } = event.latlng
  emit('update:modelValue', { latitude: lat, longitude: lng })
  searchResults.value = []
}

function onMarkerDrag(event: { target: { getLatLng: () => { lat: number; lng: number } } }) {
  if (props.readonly) return
  const { lat, lng } = event.target.getLatLng()
  emit('update:modelValue', { latitude: lat, longitude: lng })
}

function onMapReady() {
  if (props.modelValue) {
    zoom.value = SELECTED_ZOOM
  }
}

async function searchAddress() {
  const q = searchQuery.value.trim()
  if (!q) return

  searching.value = true
  try {
    const params = new URLSearchParams({
      q,
      format: 'json',
      limit: '5',
      countrycodes: 'tr',
      addressdetails: '1',
    })
    const response = await fetch(`https://nominatim.openstreetmap.org/search?${params}`, {
      headers: { 'Accept-Language': 'tr' },
    })
    searchResults.value = await response.json()
  } catch {
    searchResults.value = []
  } finally {
    searching.value = false
  }
}

function selectResult(result: NominatimResult) {
  const lat = parseFloat(result.lat)
  const lng = parseFloat(result.lon)
  emit('update:modelValue', { latitude: lat, longitude: lng })
  zoom.value = SELECTED_ZOOM
  searchResults.value = []
  searchQuery.value = result.display_name
}

// Dışarıdan modelValue değiştiğinde haritayı konuma odakla
watch(() => props.modelValue, (newVal) => {
  if (newVal && zoom.value < SELECTED_ZOOM) {
    zoom.value = SELECTED_ZOOM
  }
})
</script>
