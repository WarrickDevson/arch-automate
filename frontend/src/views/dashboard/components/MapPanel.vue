<script setup>
import { computed, nextTick, onBeforeUnmount, onMounted, ref, watch } from 'vue'

const props = defineProps({
  blocks: {
    type: Array,
    default: () => [],
  },
  mapTransform: {
    type: Object,
    default: null,
  },
  monitoringCircles: {
    type: Array,
    default: () => [],
  },
  harvestCircles: {
    type: Array,
    default: () => [],
  },
  selectedBlockId: {
    type: String,
    default: '',
  },
})

const emit = defineEmits(['select-block', 'select-monitoring', 'select-harvest'])

const mapEl = ref(null)

let map
let blockPolygons = []
let blockLabelMarkers = []
let monitoringOverlays = []
let harvestOverlays = []

const mapLabel = computed(() => (props.blocks.length ? 'Satellite' : 'No results'))

const DEFAULT_MAP_TRANSFORM = {
  centerLat: -33.9249,
  centerLng: 18.4241,
  spanLat: 0.12,
  spanLng: 0.16,
}

const xyToLatLng = (point) => ({
  lat: (props.mapTransform?.centerLat ?? DEFAULT_MAP_TRANSFORM.centerLat) +
    ((50 - Number(point.y || 0)) / 100) * (props.mapTransform?.spanLat ?? DEFAULT_MAP_TRANSFORM.spanLat),
  lng: (props.mapTransform?.centerLng ?? DEFAULT_MAP_TRANSFORM.centerLng) +
    ((Number(point.x || 0) - 50) / 100) * (props.mapTransform?.spanLng ?? DEFAULT_MAP_TRANSFORM.spanLng),
})

const scriptId = 'google-maps-js-sdk'
let mapsLoaderPromise

const loadGoogleMaps = () => {
  if (window.google?.maps) return Promise.resolve(window.google.maps)
  if (mapsLoaderPromise) return mapsLoaderPromise

  mapsLoaderPromise = new Promise((resolve, reject) => {
    const existing = document.getElementById(scriptId)
    if (existing) {
      existing.addEventListener('load', () => resolve(window.google.maps))
      existing.addEventListener('error', reject)
      return
    }

    const key = import.meta.env.VITE_GOOGLE_MAPS_API_KEY
    if (!key) {
      reject(new Error('Missing VITE_GOOGLE_MAPS_API_KEY'))
      return
    }

    const script = document.createElement('script')
    script.id = scriptId
    script.src = `https://maps.googleapis.com/maps/api/js?key=${key}`
    script.async = true
    script.defer = true
    script.onload = () => resolve(window.google.maps)
    script.onerror = reject
    document.head.appendChild(script)
  })

  return mapsLoaderPromise
}

const clearOverlays = (items) => {
  items.forEach((item) => item.setMap(null))
  items.length = 0
}

const getLatLngCentroid = (latLngPath) => {
  if (!latLngPath?.length) return null

  const sum = latLngPath.reduce(
    (acc, p) => ({
      lat: acc.lat + Number(p.lat || 0),
      lng: acc.lng + Number(p.lng || 0),
    }),
    { lat: 0, lng: 0 }
  )

  return {
    lat: sum.lat / latLngPath.length,
    lng: sum.lng / latLngPath.length,
  }
}

const renderBlocks = () => {
  if (!map || !window.google?.maps) return
  clearOverlays(blockPolygons)
  clearOverlays(blockLabelMarkers)

  const bounds = new window.google.maps.LatLngBounds()

  props.blocks.forEach((block) => {
    const path = (block.paths || []).map(xyToLatLng)
    path.forEach((entry) => bounds.extend(entry))

    const isSelected = block.id === props.selectedBlockId
    const strokeColor = block.color || '#DCE557'
    const polygon = new window.google.maps.Polygon({
      paths: path,
      strokeColor,
      strokeOpacity: 1,
      strokeWeight: 2,
      // Keep selection visually distinct while still showing each block's configured color.
      fillColor: strokeColor,
      fillOpacity: isSelected ? 0.42 : 0.22,
      map,
    })

    polygon.addListener('click', () => emit('select-block', block))
    blockPolygons.push(polygon)

    // Informational label at the polygon centroid.
    const centroid = getLatLngCentroid(path)
    if (centroid) {
      const labelMarker = new window.google.maps.Marker({
        position: centroid,
        map,
        clickable: false,
        zIndex: 10,
        // Hide the red pin while keeping only the text label.
        icon: {
          path: window.google.maps.SymbolPath.CIRCLE,
          scale: 0,
          fillOpacity: 0,
          strokeOpacity: 0,
        },
        label: {
          text: block.name || '',
          color: '#ffffff',
          fontWeight: '700',
          fontSize: '12px',
        },
      })

      blockLabelMarkers.push(labelMarker)
    }
  })

  if (props.blocks.length) {
    map.fitBounds(bounds)
  }
}

const renderCircles = () => {
  if (!map || !window.google?.maps) return
  clearOverlays(monitoringOverlays)
  clearOverlays(harvestOverlays)

  props.monitoringCircles.forEach((circle) => {
    const overlay = new window.google.maps.Circle({
      center: xyToLatLng({ x: circle.x, y: circle.y }),
      radius: Math.max(50, Number(circle.radius || 0) * 20),
      strokeColor: '#ffffff',
      strokeOpacity: 1,
      strokeWeight: 1,
      fillColor: circle.color || '#22c55e',
      fillOpacity: 0.85,
      map,
    })
    overlay.addListener('click', () => emit('select-monitoring', circle))
    monitoringOverlays.push(overlay)
  })

  props.harvestCircles.forEach((circle) => {
    const overlay = new window.google.maps.Circle({
      center: xyToLatLng({ x: circle.x, y: circle.y }),
      radius: Math.max(50, Number(circle.radius || 0) * 20),
      strokeColor: '#111827',
      strokeOpacity: 1,
      strokeWeight: 1,
      fillColor: circle.color || '#f59e0b',
      fillOpacity: 0.8,
      map,
    })
    overlay.addListener('click', () => emit('select-harvest', circle))
    harvestOverlays.push(overlay)
  })
}

const bootMap = async () => {
  await loadGoogleMaps()
  await nextTick()

  if (!mapEl.value || map) return

  const t = props.mapTransform ?? DEFAULT_MAP_TRANSFORM
  map = new window.google.maps.Map(mapEl.value, {
    center: { lat: t.centerLat, lng: t.centerLng },
    zoom: 13,
    // Use a labeled base layer so users can see location/road/place names.
    mapTypeId: 'hybrid',
    fullscreenControl: false,
    streetViewControl: false,
    mapTypeControl: false,
  })

  renderBlocks()
  renderCircles()
}

watch(
  () => [props.blocks, props.selectedBlockId, props.mapTransform],
  () => {
    renderBlocks()
  },
  { deep: true }
)

watch(
  () => [props.monitoringCircles, props.harvestCircles, props.mapTransform],
  () => {
    renderCircles()
  },
  { deep: true }
)

onMounted(() => {
  bootMap().catch(() => {})
})

onBeforeUnmount(() => {
  clearOverlays(blockPolygons)
  clearOverlays(monitoringOverlays)
  clearOverlays(harvestOverlays)
  map = null
})
</script>

<template>
  <div
    data-export-map-panel
    class="overflow-hidden rounded-2xl border border-border bg-slate-950/95"
  >
    <div ref="mapEl" class="h-[460px] w-full" />
  </div>
</template>
