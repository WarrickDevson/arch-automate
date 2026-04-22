<script setup>
import { ref, computed, watch, onMounted, onBeforeUnmount } from 'vue'
import * as OBC from '@thatopen/components'
import * as OBCF from '@thatopen/components-front'
import * as THREE from 'three'
import { Upload, Layers, Ruler, RotateCcw, Loader2, MousePointerClick, Trash2, AlertTriangle } from 'lucide-vue-next'
import { saveIfc } from '@/lib/ifcCache'
import { uploadIfc, downloadIfc } from '@/services/ifcStorageService'

const props = defineProps({
  projectId: { type: String, default: null },
  tenantId: { type: String, default: null },
  ifcPath: { type: String, default: null },
})
const emit = defineEmits([
  'ifc-loaded',
  'ifc-load-progress',
  'ifc-stats',
  'ifc-dimensions',
  'ifc-path-saved',
  'ifc-areas',
  'ifc-thermal',
  'ifc-schedule',
  'ifc-tally',
  'ifc-materials',
  'ifc-load-error',
  'element-selected',
  'measurement-updated',
])

const container = ref(null)
const fileInput = ref(null)
const isLoading = ref(false)
const hasModel = ref(false)
const isDragging = ref(false)
const loadError = ref(null)
const activeOverlay = ref('standard')
const showViewModes = ref(true)
const selectedElement = ref(null)
const measurementMode = ref(false)
const measurementDistanceM = ref(null)
const viewerHint = ref('Click any model element to inspect IFC attributes.')

const viewModes = [
  { id: 'standard', label: 'Standard' },
  { id: 'zoning', label: 'Zoning' },
  { id: 'sans', label: 'SANS 10400' },
  { id: 'energy', label: 'Energy Flow' },
]

const LEGEND_ITEMS = {
  standard: [],
  zoning: [
    { label: 'Walls', hex: '#dce2e8' },
    { label: 'Floors/Slabs', hex: '#f9c74f' },
    { label: 'Roofs', hex: '#577590' },
    { label: 'Columns', hex: '#f3722c' },
    { label: 'Beams', hex: '#f8961e' },
    { label: 'Windows', hex: '#90e0ef' },
    { label: 'Doors', hex: '#c77dff' },
    { label: 'Stairs', hex: '#ffb703' },
    { label: 'Spaces', hex: '#52b788' },
  ],
  sans: [
    { label: 'Part B – Structure', hex: '#ef233c' },
    { label: 'Part D – Doors', hex: '#f4a261' },
    { label: 'Part F – Floor loads', hex: '#f9c74f' },
    { label: 'Part K – Walls', hex: '#90be6d' },
    { label: 'Part L – Roofs', hex: '#43aa8b' },
    { label: 'Part M – Stairways', hex: '#9b5de5' },
    { label: 'Part N – Glazing', hex: '#4cc9f0' },
  ],
  energy: [
    { label: 'Glazing (high gain/loss)', hex: '#e63946' },
    { label: 'Roof heat loss', hex: '#f4a261' },
    { label: 'Wall heat loss', hex: '#f9c74f' },
    { label: 'Thermal mass', hex: '#43aa8b' },
    { label: 'Thermal zones', hex: '#52b788' },
    { label: 'Structure (low impact)', hex: '#4cc9f0' },
  ],
}

const activeLegend = computed(() => LEGEND_ITEMS[activeOverlay.value] ?? [])

// Signals that onMounted setup is fully complete (used to trigger auto-restore watcher)
const _viewerReady = ref(false)

// Non-reactive Three.js/OBC state — must NOT be made reactive
let _components = null
let _world = null
let _ifcLoader = null
let _loadedModel = null
const _raycaster = new THREE.Raycaster()
const _ndc = new THREE.Vector2()
const _measurementPoints = []
let _measurementLine = null
let _measurementLabels = []

const selectedElementProps = computed(() => selectedElement.value?.properties ?? [])
const measurementLabel = computed(() =>
  measurementDistanceM.value === null ? 'No measurement yet' : `${measurementDistanceM.value.toFixed(2)} m`,
)

// ── View mode colour palettes ─────────────────────────────────────────────
// Zoning: colour each architectural element type a distinct hue
const ZONING_PALETTE = [
  { cats: [/IFCWALL/i, /IFCCURTAINWALL/i], hex: '#dce2e8' }, // walls — light grey
  { cats: [/IFCSLAB/i, /IFCPLATE/i], hex: '#f9c74f' }, // floors — warm yellow
  { cats: [/IFCROOF/i], hex: '#577590' }, // roofs — slate blue
  { cats: [/IFCCOLUMN/i], hex: '#f3722c' }, // columns — orange
  { cats: [/IFCBEAM/i, /IFCMEMBER/i], hex: '#f8961e' }, // beams — amber
  { cats: [/IFCWINDOW/i], hex: '#90e0ef' }, // windows — sky blue
  { cats: [/IFCDOOR/i], hex: '#c77dff' }, // doors — violet
  { cats: [/IFCSTAIR/i], hex: '#ffb703' }, // stairs — gold
  { cats: [/IFCSPACE/i], hex: '#52b788' }, // spaces — green
]

// SANS 10400: colour by the relevant South African building regulation part
const SANS_PALETTE = [
  { cats: [/IFCCOLUMN/i, /IFCBEAM/i, /IFCMEMBER/i, /IFCFOOTING/i], hex: '#ef233c' }, // Part B — Structure
  { cats: [/IFCDOOR/i], hex: '#f4a261' }, // Part D — Doors
  { cats: [/IFCSLAB/i, /IFCPLATE/i], hex: '#f9c74f' }, // Part F — Floor loads
  { cats: [/IFCWALL/i], hex: '#90be6d' }, // Part K — Walls
  { cats: [/IFCROOF/i], hex: '#43aa8b' }, // Part L — Roofs
  { cats: [/IFCSTAIR/i], hex: '#9b5de5' }, // Part M — Stairways
  { cats: [/IFCWINDOW/i, /IFCCURTAINWALL/i], hex: '#4cc9f0' }, // Part N — Glazing
]

// Energy flow: heat-map — red (high transfer) → blue (low impact)
const ENERGY_PALETTE = [
  { cats: [/IFCWINDOW/i, /IFCCURTAINWALL/i], hex: '#e63946' }, // glazing — solar gain / heat loss
  { cats: [/IFCROOF/i], hex: '#f4a261' }, // roof heat loss
  { cats: [/IFCWALL/i], hex: '#f9c74f' }, // fabric heat loss
  { cats: [/IFCSLAB/i, /IFCPLATE/i], hex: '#43aa8b' }, // thermal mass
  { cats: [/IFCSPACE/i], hex: '#52b788' }, // thermal zones
  { cats: [/IFCCOLUMN/i, /IFCBEAM/i, /IFCMEMBER/i], hex: '#4cc9f0' }, // structural — low energy impact
]

// Fetch items by category regexes then apply a flat colour per group
async function applyPalette(model, palette) {
  const allCats = palette.flatMap((p) => p.cats)
  const result = await model.getItemsOfCategories(allCats)
  for (const entry of palette) {
    const ids = entry.cats.flatMap((re) =>
      Object.keys(result)
        .filter((k) => re.test(k))
        .flatMap((k) => result[k])
    )
    if (ids.length) await model.setColor(ids, new THREE.Color(entry.hex))
  }
}

async function applyViewMode(mode) {
  if (!_loadedModel) return
  await _loadedModel.resetColor(undefined)
  if (mode === 'zoning') await applyPalette(_loadedModel, ZONING_PALETTE)
  else if (mode === 'sans') await applyPalette(_loadedModel, SANS_PALETTE)
  else if (mode === 'energy') await applyPalette(_loadedModel, ENERGY_PALETTE)
}

watch(activeOverlay, (mode) => applyViewMode(mode))

// Auto-restore: fires when (a) viewer finishes setup and ifcPath is already known,
// or (b) ifcPath arrives after setup completes.
// Using a computed source [ready, path] means it triggers on either change.
watch(
  [_viewerReady, () => props.ifcPath],
  async ([ready, path]) => {
    if (!ready || _loadedModel) return
    try {
      let buffer = null
      if (path) {
        // Prefer Supabase Storage (also checks local IndexedDB cache first inside downloadIfc)
        buffer = await downloadIfc(props.projectId, path)
      } else {
        // No remote path yet — try local IndexedDB cache (file was loaded but not yet uploaded)
        const { loadIfc } = await import('@/lib/ifcCache')
        buffer = await loadIfc(props.projectId).catch(() => null)
      }
      if (buffer && !_loadedModel) {
        await loadIFC({
          name: `cached_${props.projectId}.ifc`,
          size: buffer.byteLength,
          _fromCache: true,
          async arrayBuffer() { return buffer },
        })
      }
    } catch (e) {
      console.warn('IFC auto-restore failed:', e)
    }
  },
)

function setLoadError(message, detail = '') {
  loadError.value = message
  emit('ifc-load-error', { message, detail })
}

function clearLoadError() {
  loadError.value = null
}

function updateLoadProgress(active, label = '', percent = null, source = 'manual') {
  emit('ifc-load-progress', { active, label, percent, source })
}

function extractIfcId(object3d) {
  const keyCandidates = ['expressID', 'expressId', 'localId', 'itemID', 'elementId', 'elementID']
  let node = object3d
  while (node) {
    const data = node.userData ?? {}
    for (const key of keyCandidates) {
      const raw = data[key]
      const id = Number(raw)
      if (Number.isFinite(id) && id > 0) return id
    }
    node = node.parent
  }
  return null
}

function getMeshHit(event) {
  if (!_world?.camera?.three || !container.value) return null
  const rect = container.value.getBoundingClientRect()
  if (!rect.width || !rect.height) return null

  _ndc.x = ((event.clientX - rect.left) / rect.width) * 2 - 1
  _ndc.y = -((event.clientY - rect.top) / rect.height) * 2 + 1
  _raycaster.setFromCamera(_ndc, _world.camera.three)

  const hits = _raycaster.intersectObjects(_world.scene.three.children, true)
  return hits.find((hit) => !(hit.object?.userData?.__measurementHelper)) ?? null
}

function resetMeasurement(clearPublished = true) {
  _measurementPoints.length = 0
  measurementDistanceM.value = null

  if (_measurementLine && _world?.scene?.three) {
    _world.scene.three.remove(_measurementLine)
    _measurementLine.geometry?.dispose?.()
    _measurementLine.material?.dispose?.()
    _measurementLine = null
  }

  for (const marker of _measurementLabels) {
    _world?.scene?.three?.remove(marker)
    marker.geometry?.dispose?.()
    marker.material?.dispose?.()
  }
  _measurementLabels = []

  if (clearPublished) {
    emit('measurement-updated', null)
  }
}

function addMeasurementMarker(point) {
  const marker = new THREE.Mesh(
    new THREE.SphereGeometry(0.12, 12, 12),
    new THREE.MeshBasicMaterial({ color: '#22d3ee' }),
  )
  marker.position.copy(point)
  marker.userData.__measurementHelper = true
  _measurementLabels.push(marker)
  _world.scene.three.add(marker)
}

function drawMeasurementLine(a, b) {
  if (_measurementLine) {
    _world.scene.three.remove(_measurementLine)
    _measurementLine.geometry?.dispose?.()
    _measurementLine.material?.dispose?.()
    _measurementLine = null
  }

  const geometry = new THREE.BufferGeometry().setFromPoints([a, b])
  const material = new THREE.LineBasicMaterial({ color: '#22d3ee' })
  _measurementLine = new THREE.Line(geometry, material)
  _measurementLine.userData.__measurementHelper = true
  _world.scene.three.add(_measurementLine)
}

async function handleInspectClick(hit) {
  if (!hit) {
    selectedElement.value = null
    emit('element-selected', null)
    return
  }

  const ifcId = extractIfcId(hit.object)
  const fallback = {
    localId: ifcId,
    name: hit.object?.name || 'Unnamed element',
    category: hit.object?.type || 'Unknown',
    properties: [],
  }

  if (!_loadedModel || !ifcId) {
    selectedElement.value = fallback
    emit('element-selected', fallback)
    return
  }

  try {
    const item = await _loadedModel.getItem(ifcId)
    const attrs = (await item?.getAttributes?.()) ?? null
    const getVal = (k) => attrs?.get?.(k)?.value ?? null
    const category = getVal('type') || getVal('Entity') || fallback.category
    const name = getVal('LongName') || getVal('Name') || fallback.name

    const details = {
      localId: ifcId,
      name,
      category,
      properties: [
        { label: 'GlobalId', value: getVal('GlobalId') },
        { label: 'PredefinedType', value: getVal('PredefinedType') },
        { label: 'ObjectType', value: getVal('ObjectType') },
        { label: 'Tag', value: getVal('Tag') },
      ].filter((x) => x.value !== null && x.value !== undefined && String(x.value).length > 0),
    }

    selectedElement.value = details
    emit('element-selected', details)
  } catch {
    selectedElement.value = fallback
    emit('element-selected', fallback)
  }
}

function handleMeasureClick(hit) {
  if (!hit?.point) return

  const point = hit.point.clone()
  _measurementPoints.push(point)
  addMeasurementMarker(point)

  if (_measurementPoints.length === 1) {
    viewerHint.value = 'Measurement: click second point.'
    return
  }

  if (_measurementPoints.length >= 2) {
    const a = _measurementPoints[_measurementPoints.length - 2]
    const b = _measurementPoints[_measurementPoints.length - 1]
    drawMeasurementLine(a, b)

    measurementDistanceM.value = a.distanceTo(b)
    emit('measurement-updated', {
      distanceM: measurementDistanceM.value,
      start: { x: a.x, y: a.y, z: a.z },
      end: { x: b.x, y: b.y, z: b.z },
    })

    _measurementPoints.length = 0
    viewerHint.value = 'Measurement captured. Click a new point to start another.'
  }
}

function handleViewportClick(event) {
  if (!hasModel.value || isLoading.value) return
  const hit = getMeshHit(event)

  if (measurementMode.value) {
    handleMeasureClick(hit)
    return
  }

  handleInspectClick(hit)
}

function toggleMeasurementMode() {
  measurementMode.value = !measurementMode.value
  if (measurementMode.value) {
    viewerHint.value = 'Measurement mode enabled. Click two points in the model.'
    selectedElement.value = null
    emit('element-selected', null)
  } else {
    viewerHint.value = 'Click any model element to inspect IFC attributes.'
    resetMeasurement(false)
  }
}

function toggleViewModes() {
  showViewModes.value = !showViewModes.value
}

onMounted(async () => {
  _components = new OBC.Components()

  // FragmentsManager MUST be fully initialised (not just retrieved) before
  // IfcLoader.load() is called. getWorker() fetches the worker script from unpkg.
  const fragments = _components.get(OBC.FragmentsManager)
  const workerUrl = await OBC.FragmentsManager.getWorker()
  fragments.init(workerUrl)

  const worlds = _components.get(OBC.Worlds)
  _world = worlds.create()

  _world.scene = new OBC.SimpleScene(_components)
  _world.renderer = new OBCF.PostproductionRenderer(_components, container.value)
  _world.camera = new OBC.SimpleCamera(_components)

  _components.init()
  _world.scene.setup()

  _world.scene.three.background = new THREE.Color(0x101018)

  // Postproduction – outlines + AO give a great BIM look
  try {
    const pp = _world.renderer.postproduction
    pp.enabled = true
    pp.customEffects.outlineEnabled = true
  } catch {
    /* not available in this build */
  }

  // Infinite grid
  try {
    const grids = _components.get(OBC.Grids)
    grids.create(_world)
  } catch {
    /* optional */
  }

  _world.camera.controls.setLookAt(15, 12, 15, 0, 2, 0, false)

  // Drive tile refresh on every render frame so loaded models stay visible
  _world.onAfterUpdate.add(() => {
    const frags = _components.get(OBC.FragmentsManager)
    if (frags.initialized) frags.core.update()
  })

  // Pre-initialise IFC loader so it's ready when the user drops a file
  _ifcLoader = _components.get(OBC.IfcLoader)
  await _ifcLoader.setup()
  // autoSetWasm fetches the semver range ">=0.0.77" from unpkg which resolves
  // to the latest web-ifc WASM — a different version than our local JS wrapper.
  // Disable it and pin to the exact installed version to avoid the import mismatch.
  _ifcLoader.settings.autoSetWasm = false
  _ifcLoader.settings.wasm.path = 'https://unpkg.com/web-ifc@0.0.77/'
  _ifcLoader.settings.wasm.absolute = true

  // Signal the auto-restore watcher that setup is complete.
  // The watcher handles both "ifcPath already known" and "ifcPath arrives later" cases.
  _viewerReady.value = true

  container.value?.addEventListener('click', handleViewportClick)
})

onBeforeUnmount(() => {
  container.value?.removeEventListener('click', handleViewportClick)
  resetMeasurement(false)
  _components?.dispose()
})

async function loadIFC(file) {
  if (!_components || !_world) return

  const loadSource = file?._fromCache ? 'restore' : 'manual'

  isLoading.value = true
  updateLoadProgress(true, 'Reading IFC file...', 8, loadSource)
  clearLoadError()
  resetMeasurement()
  selectedElement.value = null
  emit('element-selected', null)

  let hasPendingCloudUpload = false

  try {
    const buffer = await file.arrayBuffer()
    updateLoadProgress(true, 'Parsing IFC geometry...', 24, loadSource)

    // coordinate=true: normalises multi-model scene to a shared base origin.
    const model = await _ifcLoader.load(new Uint8Array(buffer), true, file.name)
    updateLoadProgress(true, 'Building 3D fragments...', 46, loadSource)

    // Persist to Supabase Storage (primary) then IndexedDB cache (secondary).
    // Skip on cache-restore restores (_fromCache flag) to avoid re-uploading unchanged data.
    if (props.projectId && props.tenantId && !file._fromCache) {
      hasPendingCloudUpload = true
      updateLoadProgress(true, 'Uploading IFC to cloud...', 72, loadSource)

      uploadIfc(props.tenantId, props.projectId, buffer)
        .then((path) => {
          emit('ifc-path-saved', path)
          updateLoadProgress(true, 'IFC upload complete', 100, loadSource)
          window.setTimeout(() => {
            updateLoadProgress(false, '', null, loadSource)
          }, 700)
        })
        .catch((e) => {
          console.warn('IFC upload failed:', e)
          updateLoadProgress(false, '', null, loadSource)
        })
    } else if (props.projectId && !file._fromCache) {
      // No tenantId yet — fall back to local cache only
      saveIfc(props.projectId, buffer).catch(() => {})
    }

    if (_loadedModel?.object) {
      _world.scene.three.remove(_loadedModel.object)
    }

    _loadedModel = model

    // Connect the active camera so the ViewManager knows the frustum and
    // can determine which tiles to request from the worker thread.
    model.useCamera(_world.camera.three)

    // model.object is an empty Object3D until tiles arrive — add it first
    // so the scene graph / matrixWorld is correct when tiles are attached.
    _world.scene.three.add(model.object)

    // Force-build all tiles immediately (they are created lazily).
    const fragments = _components.get(OBC.FragmentsManager)
    await fragments.core.update(true)
    updateLoadProgress(true, 'Preparing model view...', 62, loadSource)

    // model.box is populated from worker metadata during setup — much more
    // reliable than setFromObject which returns empty when no tiles exist yet.
    const bbox = model.box
    if (!bbox.isEmpty()) {
      await _world.camera.controls.fitToBox(bbox, true)
    }

    hasModel.value = true
    emit('ifc-loaded', { fileName: file.name, fileSize: file.size })
    updateLoadProgress(true, 'Extracting IFC metadata...', hasPendingCloudUpload ? 78 : 82, loadSource)

    // Emit element counts for the BIM Data tab
    try {
      const allCatPatterns = [
        /IFCWALL/i, /IFCCURTAINWALL/i, /IFCSLAB/i, /IFCPLATE/i,
        /IFCCOLUMN/i, /IFCBEAM/i, /IFCMEMBER/i, /IFCWINDOW/i,
        /IFCDOOR/i, /IFCROOF/i, /IFCSTAIR/i, /IFCSPACE/i,
      ]
      const catResult = await model.getItemsOfCategories(allCatPatterns)
      const countCats = (...regexes) => {
        const keys = Object.keys(catResult)
        const matched = [...new Set(regexes.flatMap((re) => keys.filter((k) => re.test(k))))]
        return matched.flatMap((k) => catResult[k]).length
      }
      const ifcStats = {
        walls: countCats(/IFCWALL/i, /IFCCURTAINWALL/i),
        slabs: countCats(/IFCSLAB/i, /IFCPLATE/i),
        columns: countCats(/IFCCOLUMN/i),
        beams: countCats(/IFCBEAM/i, /IFCMEMBER/i),
        windows: countCats(/IFCWINDOW/i),
        doors: countCats(/IFCDOOR/i),
        roofs: countCats(/IFCROOF/i),
        stairs: countCats(/IFCSTAIR/i),
        spaces: countCats(/IFCSPACE/i),
      }
      ifcStats.total = Object.values(ifcStats).reduce((a, b) => a + b, 0)
      emit('ifc-stats', ifcStats)

      // ── Glazing U-value (ThermalTransmittance) extraction ─────────────────
      // IFC4: IfcWindow may carry ThermalTransmittance as a direct attribute or
      // within Pset_WindowCommon. We read it as a direct attribute (works for
      // most Revit / ArchiCAD exports). Values found are averaged across windows.
      try {
        const windowIds = Object.values(catResult)
          .flatMap((ids, _) => ids)
          .filter((_, idx, arr) => {
            // Re-filter to only window category keys
            const keys = Object.keys(catResult)
            return keys.some((k) => /IFCWINDOW/i.test(k))
          })
        const windowKeys = Object.keys(catResult).filter((k) => /IFCWINDOW/i.test(k))
        const allWindowIds = windowKeys.flatMap((k) => catResult[k])

        const uValues = []
        for (const localId of allWindowIds.slice(0, 50)) { // cap at 50 to avoid blocking
          try {
            const attrs = await model.getItem(localId)?.getAttributes?.()
            const uVal = attrs?.get('ThermalTransmittance')?.value
            if (uVal != null && Number(uVal) > 0) uValues.push(Number(uVal))
          } catch {
            // individual window read failure is non-critical
          }
        }

        if (uValues.length > 0) {
          const avgU = uValues.reduce((a, b) => a + b, 0) / uValues.length
          emit('ifc-thermal', { glazingUValue: parseFloat(avgU.toFixed(2)), sampleCount: uValues.length })
        }
      } catch (err) {
        console.warn('IFC glazing U-value extraction failed:', err)
      }

      // ── Door & Window schedule extraction ───────────────────────────────
      // catResult is in scope here. Walk the spatial tree to resolve each
      // element's BuildingStorey, then read OverallWidth/Height for every door/window.
      try {
        const doorKeys = Object.keys(catResult).filter((k) => /IFCDOOR/i.test(k))
        const winKeys  = Object.keys(catResult).filter((k) => /IFCWINDOW/i.test(k))
        const doorIds  = doorKeys.flatMap((k) => catResult[k])
        const winIds   = winKeys.flatMap((k) => catResult[k])

        if (doorIds.length || winIds.length) {
          // ── Detect project length unit → factor to convert raw value to mm ──
          // IFC stores OverallWidth/Height in whatever unit the project declares.
          // Revit metric: mm (factor=1), ArchiCAD/IFC standard default: m (factor=1000),
          // some tools: cm (factor=10).  We read IfcSIUnit first; fall back to a
          // value-sniff heuristic so we never silently produce garbage.
          let lengthToMmFactor = null
          try {
            const siUnitResult = await model.getItemsOfCategories([/IFCSIUNIT/i])
            const siUnitIds = Object.values(siUnitResult).flat()
            for (const uid of siUnitIds) {
              const ua = await model.getItem(uid)?.getAttributes?.()
              if (!ua) continue
              const unitType = String(ua.get('UnitType')?.value ?? '')
              if (!/LENGTHUNIT/i.test(unitType)) continue
              const prefix = String(ua.get('Prefix')?.value ?? '')
              const name   = String(ua.get('Name')?.value   ?? '')
              if (/MILLI/i.test(prefix))        { lengthToMmFactor = 1;     break }
              if (/CENTI/i.test(prefix))        { lengthToMmFactor = 10;    break }
              if (/METRE|METER/i.test(name))    { lengthToMmFactor = 1000;  break }
            }
          } catch { /* unit detection failed — fall through to heuristic */ }

          // Heuristic fallback: sample the first non-zero dimension value.
          // A door wider than 100 real-world-units can only be in mm.
          if (lengthToMmFactor === null) {
            const sampleId = [...doorIds, ...winIds][0]
            if (sampleId != null) {
              try {
                const sa = await model.getItem(sampleId)?.getAttributes?.()
                const sampleW = Number(sa?.get('OverallWidth')?.value ?? 0)
                const sampleH = Number(sa?.get('OverallHeight')?.value ?? 0)
                const sample = sampleW > 0 ? sampleW : sampleH
                // Values > 100 must be mm (no door is 100 m wide)
                lengthToMmFactor = sample > 100 ? 1 : 1000
              } catch { lengthToMmFactor = 1 }
            } else {
              lengthToMmFactor = 1
            }
          }

          const toMm = (raw) => {
            if (raw == null || Number(raw) <= 0) return 0
            return Math.round(Number(raw) * lengthToMmFactor)
          }

          // Build localId → storeyName map
          const storeyMap = new Map()
          const schedTree = await model.getSpatialStructure()
          const walkStoreys = async (node, currentStorey) => {
            const cat = (node.category ?? '').toUpperCase()
            let storey = currentStorey
            if (cat.includes('BUILDINGSTOREY')) {
              try {
                const sa = await model.getItem(node.localId)?.getAttributes?.()
                storey = sa?.get('Name')?.value || sa?.get('LongName')?.value || currentStorey || 'Level 1'
              } catch { /* keep parent */ }
            }
            if (node.localId != null) storeyMap.set(node.localId, storey)
            for (const child of node.children ?? []) await walkStoreys(child, storey)
          }
          await walkStoreys(schedTree, 'Ground Floor')

          const extractRow = async (localId) => {
            try {
              const attrs = await model.getItem(localId)?.getAttributes?.()
              const g = (k) => attrs?.get(k)?.value ?? null
              const widthMm  = toMm(g('OverallWidth'))
              const heightMm = toMm(g('OverallHeight'))
              const areaM2   = widthMm > 0 && heightMm > 0
                ? parseFloat(((widthMm / 1000) * (heightMm / 1000)).toFixed(3))
                : 0
              return {
                localId,
                mark:    g('Tag')          || g('Name')          || String(localId),
                name:    g('Name')         || g('LongName')       || '',
                type:    g('ObjectType')   || g('PredefinedType') || '',
                widthMm,
                heightMm,
                areaM2,
                level: storeyMap.get(localId) || 'Ground Floor',
              }
            } catch { return null }
          }

          const doors   = (await Promise.all(doorIds.slice(0, 500).map(extractRow))).filter(Boolean)
          const windows = (await Promise.all(winIds.slice(0, 500).map(extractRow))).filter(Boolean)

          if (doors.length || windows.length) emit('ifc-schedule', { doors, windows })
        }
      } catch (err) {
        console.warn('IFC schedule extraction failed:', err)
      }

      // ── Electrical / Fixture Tally extraction ────────────────────────────
      // Walks the IFC model for MEP and sanitary fixture entity types.
      // Emits `ifc-tally` with a flat array of TallyItem objects.
      // The storey map built for the schedule is reused when available.
      try {
        const tallyItems = []
        const TALLY_CATEGORIES = [
          // [regex to match IFC type key, category label]
          [/IFCLIGHTFIXTURE/i,               'Lighting'],
          [/IFCOUTLET/i,                     'Electrical'],
          [/IFCELECTRICAPPLIANCE/i,          'Electrical'],
          [/IFCELECTRICDISTRIBUTIONBOARD/i,  'Electrical'],
          [/IFCELECTRICFLOWSTORAGEDEVICE/i,  'Electrical'],
          [/IFCELECTRICGENERATOR/i,          'Electrical'],
          [/IFCELECTRICMOTOR/i,              'Electrical'],
          [/IFCSANITARYTERMINAL/i,           'Sanitary'],
          [/IFCWASTETERMINAL/i,              'Sanitary'],
          [/IFCPLUMBINGFIXTURETYPE/i,        'Sanitary'],
          [/IFCAIRTERMINAL/i,                'HVAC'],
          [/IFCFAN/i,                        'HVAC'],
          [/IFCPUMP/i,                       'HVAC'],
          [/IFCUNITARYEQUIPMENT/i,           'HVAC'],
          [/IFCDUCTFITTING/i,                'HVAC'],
          [/IFCFLOWMETER/i,                  'HVAC'],
          [/IFCFIRESUPPRESSIONTERMINAL/i,    'Fire'],
          [/IFCALARM/i,                      'Fire'],
          [/IFCSENSOR/i,                     'Fire'],
          [/IFCCOMMUNICATIONSAPPLIANCE/i,    'Other'],
          [/IFCMEDICALDEVICE/i,              'Other'],
          [/IFCFURNITURE(?!TYPE)/i,          'Other'],
        ]

        const tallyPatterns = TALLY_CATEGORIES.map(([re]) => re)
        const tallyCatResult = await model.getItemsOfCategories(tallyPatterns)
        const tallyCatKeys = Object.keys(tallyCatResult)

        if (tallyCatKeys.length > 0) {
          // Build a fresh storey map (the one from schedule extraction is in a
          // separate block-scoped try, so we rebuild here safely).
          const tallyStoreyMap = new Map()
          try {
            const tallyTree = await model.getSpatialStructure()
            const walkTallyStoreys = async (node, parentStorey) => {
              const cat = (node.category ?? '').toUpperCase()
              let storey = parentStorey
              if (cat.includes('BUILDINGSTOREY')) {
                try {
                  const sa = await model.getItem(node.localId)?.getAttributes?.()
                  storey = sa?.get('Name')?.value || sa?.get('LongName')?.value || parentStorey || 'Level 1'
                } catch { /* keep parent */ }
              }
              if (node.localId != null) tallyStoreyMap.set(node.localId, storey)
              for (const child of node.children ?? []) await walkTallyStoreys(child, storey)
            }
            await walkTallyStoreys(tallyTree, 'Ground Floor')
          } catch { /* storey map optional */ }

          const resolveCategory = (typeKey) => {
            for (const [re, cat] of TALLY_CATEGORIES) {
              if (re.test(typeKey)) return cat
            }
            return 'Other'
          }

          for (const typeKey of tallyCatKeys) {
            const category = resolveCategory(typeKey)
            const localIds = tallyCatResult[typeKey] ?? []
            for (const localId of localIds.slice(0, 1000)) {
              try {
                const attrs = await model.getItem(localId)?.getAttributes?.()
                const g = (k) => attrs?.get(k)?.value ?? null
                tallyItems.push({
                  localId,
                  category,
                  ifcType: typeKey.replace(/^IFC/i, ''),  // strip IFC prefix for display
                  mark: g('Tag') || g('Name') || String(localId),
                  name: g('Name') || g('LongName') || '',
                  type: g('ObjectType') || g('PredefinedType') || '',
                  level: tallyStoreyMap.get(localId) || 'Ground Floor',
                })
              } catch { /* skip this element */ }
            }
          }
        }

        // Emit even when no supported fixture entities are present so the
        // backend can persist an explicit zero-count tally state.
        emit('ifc-tally', tallyItems)
      } catch (err) {
        console.warn('IFC tally extraction failed:', err)
      }

      // ── Materials extraction (for Auto-Specification Compiler) ─────────
      try {
        const MAT_CATEGORIES = [
          /IFCWALL/, /IFCWALLSTANDARDCASE/,
          /IFCSLAB/, /IFCROOFING/, /IFCROOF/,
          /IFCCOLUMN/, /IFCBEAM/,
          /IFCDOOR/, /IFCWINDOW/,
          /IFCSTAIR/, /IFCFOOTING/,
          /IFCCOVERING/, /IFCPLATE/,
        ]
        const matSet = new Set()
        const matItems = []

        for (const categoryRe of MAT_CATEGORIES) {
          let matResult
          try { matResult = await model.getItemsOfCategories([categoryRe]) } catch { continue }
          if (!matResult) continue
          for (const [, ids] of Object.entries(matResult)) {
            for (const localId of ids.slice(0, 1000)) {
              try {
                const attrs = await model.getItem(localId).getAttributes()
                const g = (k) => attrs?.get(k)?.value?.toString()?.trim() ?? ''
                const elementCategory = categoryRe.source.replace(/\^?IFC/, '').replace(/\$?\//g, '')
                const candidates = [g('Name'), g('ObjectType'), g('Description'), g('Tag')]
                for (const raw of candidates) {
                  if (raw && raw.length > 1 && !matSet.has(raw.toLowerCase())) {
                    matSet.add(raw.toLowerCase())
                    matItems.push({ rawValue: raw, elementCategory })
                  }
                }
              } catch { /* skip */ }
            }
          }
        }

        if (matItems.length > 0) emit('ifc-materials', matItems)
      } catch (err) {
        console.warn('IFC materials extraction failed:', err)
      }
    } catch (err) {
      console.warn('IFC stats extraction failed:', err)
    }

    // ── Area schedule extraction (IfcSpace names + storeys) ───────────────
    try {
      const areaSchedule = []
      const spatialTree = await model.getSpatialStructure()

      // Walk the spatial tree: Project → Site → Building → Storey → Space
      const walkTree = async (node, storeyName) => {
        const cat = node.category?.toUpperCase() ?? ''
        let currentStorey = storeyName

        if (cat.includes('BUILDINGSTOREY')) {
          const storeyAttrs = await model.getItem(node.localId).getAttributes()
          currentStorey =
            storeyAttrs?.get('Name')?.value ||
            storeyAttrs?.get('LongName')?.value ||
            `Level ${areaSchedule.length + 1}`
        }

        if (cat.includes('SPACE')) {
          const item = model.getItem(node.localId)
          const attrs = await item?.getAttributes?.()
          const name =
            attrs?.get('LongName')?.value ||
            attrs?.get('Name')?.value ||
            `Space ${node.localId}`

          // IFC stores areas in quantity sets, not direct attributes.
          // Strategy 1: direct attribute (works in some IFC2x3 exports)
          // Strategy 2: check common alternate key names
          // Strategy 3: fall back to 0 and let user override
          const rawArea =
            attrs?.get('NetFloorArea')?.value ??
            attrs?.get('GrossFloorArea')?.value ??
            attrs?.get('GrossArea')?.value ??
            attrs?.get('NetArea')?.value ??
            null
          const areaM2 = rawArea !== null ? Number(rawArea) : 0

          areaSchedule.push({ spaceName: name, level: currentStorey || 'Ground Floor', areaM2 })
        }

        for (const child of node.children ?? []) {
          await walkTree(child, currentStorey)
        }
      }

      await walkTree(spatialTree, '')
      if (areaSchedule.length) emit('ifc-areas', areaSchedule)
    } catch (err) {
      console.warn('Area schedule extraction failed:', err)
    }

    // ── Dimensional extraction (pre-fills Analysis sidebar) ───────────────
    try {
      const bbox = model.box
      const heightM = parseFloat((bbox.max.y - bbox.min.y).toFixed(2))
      // Footprint approximation: bounding box X × Z (horizontal plane in Three.js)
      const footprintM2 = parseFloat(
        ((bbox.max.x - bbox.min.x) * (bbox.max.z - bbox.min.z)).toFixed(1)
      )

      // Count building storeys from the IFC hierarchy
      const storeyResult = await model.getItemsOfCategories([/IFCBUILDINGSTOREY/i])
      const storeyIds = Object.values(storeyResult).flat()
      const numberOfStoreys = Math.max(1, storeyIds.length)

      emit('ifc-dimensions', { heightM, footprintM2, numberOfStoreys })
    } catch (err) {
      console.warn('IFC dimension extraction failed:', err)
    }

    // ── Empty model guard ─────────────────────────────────────────────────
    try {
      const allCatPatterns2 = [/IFCWALL/i, /IFCSLAB/i, /IFCSPACE/i]
      const minimal = await model.getItemsOfCategories(allCatPatterns2)
      const totalMinimal = Object.values(minimal).flat().length
      if (totalMinimal === 0) {
        setLoadError(
          'File parsed but no architectural elements were found. Verify this is a building model with walls, slabs, or spaces.'
        )
      }
    } catch {
      // non-critical
    }

    if (!hasPendingCloudUpload) {
      updateLoadProgress(true, 'IFC ready', 100, loadSource)
      window.setTimeout(() => {
        updateLoadProgress(false, '', null, loadSource)
      }, 500)
    }
  } catch (err) {
    console.error('IFC load failed:', err)
    const message =
      err?.message ||
      'Could not parse this IFC file. Ensure it is a valid IFC2x3/IFC4 model.'
    setLoadError('Could not parse this IFC file.', message)
    updateLoadProgress(false, '', null, loadSource)
  } finally {
    isLoading.value = false
  }
}

function handleDrop(e) {
  isDragging.value = false
  const file = e.dataTransfer?.files?.[0]
  if (file?.name?.toLowerCase().endsWith('.ifc')) {
    validateAndLoadIFC(file)
  } else if (file) {
    loadError.value = 'Only .ifc files are supported.'
  }
}

function handleFileSelect(e) {
  const file = e.target?.files?.[0]
  if (file) validateAndLoadIFC(file)
  // Reset so the same file can be re-selected
  e.target.value = ''
}

async function validateAndLoadIFC(file) {
  // Pre-validate: check the STEP magic bytes before handing off to the WASM parser.
  // Valid IFC/STEP files always begin with the ASCII string "ISO-10303-21".
  try {
    const header = await file.slice(0, 32).text()
    if (!header.trimStart().startsWith('ISO-10303-21')) {
      setLoadError(
        'Invalid file: not a valid IFC/STEP file.',
        'Ensure you export an IFC2x3 or IFC4 model from your CAD software.',
      )
      updateLoadProgress(false, '', null, 'manual')
      return
    }
  } catch {
    // If we can't read the header, let the WASM parser handle the error
  }
  loadIFC(file)
}

function openFilePicker() {
  fileInput.value?.click()
}

defineExpose({ loadIFC, openFilePicker })
</script>

<template>
  <div class="w-full h-full relative select-none">
    <!-- Three.js rendering surface -->
    <div ref="container" class="w-full h-full" />

    <input
      ref="fileInput"
      type="file"
      accept=".ifc"
      class="hidden"
      @change="handleFileSelect"
    />

    <!-- ── Error panel (always visible, not only on empty state) ── -->
    <Transition name="fade">
      <div
        v-if="loadError"
        class="absolute top-4 right-4 z-30 max-w-md bg-rose-950/85 text-rose-50 border border-rose-500/40 rounded-xl px-4 py-3 shadow-xl backdrop-blur-md"
      >
        <div class="flex items-start gap-2">
          <AlertTriangle class="h-4 w-4 mt-0.5 text-rose-300 flex-shrink-0" />
          <div class="min-w-0">
            <p class="text-[10px] font-bold uppercase tracking-widest text-rose-200">IFC Load Warning</p>
            <p class="text-[11px] leading-snug mt-0.5">{{ loadError }}</p>
          </div>
        </div>
      </div>
    </Transition>

    <!-- ── Upload dropzone overlay (no model yet) ── -->
    <Transition name="fade">
      <div
        v-if="!hasModel && !isLoading"
        class="absolute inset-0 flex flex-col items-center justify-center cursor-pointer group z-10"
        :class="isDragging ? 'bg-blue-950/85' : 'bg-[#101018]/92'"
        @dragover.prevent="isDragging = true"
        @dragleave.prevent="isDragging = false"
        @drop.prevent="handleDrop"
        @click="openFilePicker"
      >
        <div
          class="flex flex-col items-center gap-5 px-12 py-10 rounded-2xl border-2 border-dashed transition-all duration-300 max-w-sm text-center pointer-events-none"
          :class="
            isDragging
              ? 'border-blue-400 bg-blue-500/10 scale-105'
              : 'border-white/10 bg-white/[0.03] group-hover:border-blue-500/50 group-hover:bg-blue-500/5'
          "
        >
          <div
            class="h-20 w-20 rounded-2xl flex items-center justify-center transition-all duration-300"
            :class="isDragging ? 'bg-blue-500/20' : 'bg-white/5 group-hover:bg-blue-500/10'"
          >
            <Upload
              class="h-9 w-9 transition-colors duration-300"
              :class="isDragging ? 'text-blue-400' : 'text-slate-500 group-hover:text-blue-400'"
            />
          </div>

          <div class="space-y-1.5">
            <p class="text-sm font-bold text-white uppercase tracking-widest">
              {{ isDragging ? 'Drop to Load' : 'Load IFC Model' }}
            </p>
            <p class="text-[11px] text-slate-500 font-mono">Drag & drop or click to browse</p>
            <p class="text-[10px] text-slate-600 font-mono uppercase tracking-widest">
              IFC 2x3 &amp; IFC 4 supported
            </p>
          </div>
        </div>

        <p
          v-if="loadError"
          class="mt-4 text-[11px] text-rose-400 font-mono max-w-sm text-center px-4"
        >
          ⚠ {{ loadError }}
        </p>
      </div>
    </Transition>

    <!-- ── Loading overlay ── -->
    <Transition name="fade">
      <div
        v-if="isLoading"
        class="absolute inset-0 flex items-center justify-center bg-[#101018]/95 z-20"
      >
        <div class="flex flex-col items-center gap-5">
          <div
            class="h-16 w-16 rounded-2xl bg-blue-500/10 border border-blue-500/20 flex items-center justify-center"
          >
            <Loader2 class="h-8 w-8 text-blue-400 animate-spin" />
          </div>
          <div class="text-center space-y-1">
            <p class="text-xs font-bold text-white uppercase tracking-widest">Parsing IFC</p>
            <p class="text-[10px] text-slate-500 font-mono">
              Processing geometry &amp; properties…
            </p>
          </div>
        </div>
      </div>
    </Transition>

    <!-- ── View mode panel (visible after model loads) ── -->
    <Transition name="slide-left">
      <div v-if="hasModel && showViewModes" class="absolute top-4 left-4 z-20 flex flex-col gap-2">
        <div
          class="bg-black/50 backdrop-blur-md border border-white/10 p-1 rounded-xl flex flex-col gap-0.5 shadow-xl"
        >
          <button
            v-for="mode in viewModes"
            :key="mode.id"
            @click="activeOverlay = mode.id"
            :class="
              activeOverlay === mode.id
                ? 'bg-blue-600 text-white shadow-md shadow-blue-900/40'
                : 'text-slate-400 hover:bg-white/10 hover:text-white'
            "
            class="px-3 py-1.5 rounded-lg text-[10px] font-bold uppercase tracking-wider transition-all duration-150"
          >
            {{ mode.label }}
          </button>
        </div>

        <!-- ── Colour legend (hidden in Standard mode) ── -->
        <Transition name="fade">
          <div
            v-if="activeLegend.length"
            class="bg-black/50 backdrop-blur-md border border-white/10 rounded-xl p-2.5 shadow-xl flex flex-col gap-1.5"
          >
            <p class="text-[9px] font-bold uppercase tracking-widest text-slate-400 mb-0.5">Legend</p>
            <div
              v-for="item in activeLegend"
              :key="item.label"
              class="flex items-center gap-2"
            >
              <span
                class="w-3 h-3 rounded-sm flex-shrink-0 border border-white/10"
                :style="{ backgroundColor: item.hex }"
              />
              <span class="text-[10px] text-slate-300 leading-none">{{ item.label }}</span>
            </div>
          </div>
        </Transition>
      </div>
    </Transition>

    <!-- ── Tool buttons ── -->
    <Transition name="slide-left">
      <div v-if="hasModel" class="absolute bottom-4 left-4 z-20 flex gap-2">
        <button
          @click="toggleMeasurementMode"
          :class="
            measurementMode
              ? 'text-cyan-300 border-cyan-300/40 bg-cyan-500/10'
              : 'text-slate-400 border-white/10 bg-black/50 hover:text-white hover:bg-white/10 hover:border-white/20'
          "
          class="p-2.5 backdrop-blur-md rounded-lg border transition-all"
          title="Measure"
        >
          <Ruler class="h-4 w-4" />
        </button>
        <button
          @click="toggleViewModes"
          :class="
            showViewModes
              ? 'text-cyan-300 border-cyan-300/40 bg-cyan-500/10'
              : 'text-slate-400 border-white/10 bg-black/50 hover:text-white hover:bg-white/10 hover:border-white/20'
          "
          class="p-2.5 bg-black/50 backdrop-blur-md rounded-lg text-slate-400 border border-white/10 hover:text-white hover:bg-white/10 hover:border-white/20 transition-all"
          title="Layers"
        >
          <Layers class="h-4 w-4" />
        </button>
        <button
          @click="openFilePicker"
          class="p-2.5 bg-black/50 backdrop-blur-md rounded-lg text-slate-400 border border-white/10 hover:text-white hover:bg-white/10 hover:border-white/20 transition-all"
          title="Load new IFC"
        >
          <RotateCcw class="h-4 w-4" />
        </button>
        <button
          @click="resetMeasurement()"
          class="p-2.5 bg-black/50 backdrop-blur-md rounded-lg text-slate-400 border border-white/10 hover:text-white hover:bg-white/10 hover:border-white/20 transition-all"
          title="Clear measurement"
        >
          <Trash2 class="h-4 w-4" />
        </button>
      </div>
    </Transition>

    <!-- ── Inspector / measurement panel ── -->
    <Transition name="slide-left">
      <div
        v-if="hasModel"
        class="absolute bottom-4 right-4 z-20 w-[min(380px,calc(100%-2rem))] rounded-xl border border-white/10 bg-black/55 backdrop-blur-md text-white shadow-2xl"
      >
        <div class="px-3 py-2 border-b border-white/10 flex items-center justify-between gap-2">
          <div class="flex items-center gap-2 min-w-0">
            <MousePointerClick class="h-3.5 w-3.5 text-cyan-300" />
            <p class="text-[10px] font-bold uppercase tracking-widest truncate">Element Inspector</p>
          </div>
          <span class="text-[10px] font-mono text-cyan-300">{{ measurementLabel }}</span>
        </div>

        <div class="px-3 py-2.5 space-y-2">
          <p class="text-[10px] text-slate-300">{{ viewerHint }}</p>

          <template v-if="selectedElement">
            <div class="rounded-lg border border-white/10 bg-white/5 p-2.5 space-y-1.5">
              <p class="text-xs font-bold leading-tight break-words">{{ selectedElement.name }}</p>
              <p class="text-[10px] font-mono text-slate-300">{{ selectedElement.category }}</p>
              <p v-if="selectedElement.localId" class="text-[10px] font-mono text-slate-400">
                LocalId: {{ selectedElement.localId }}
              </p>

              <div v-if="selectedElementProps.length" class="pt-1 border-t border-white/10 space-y-1">
                <p
                  v-for="row in selectedElementProps"
                  :key="row.label"
                  class="text-[10px] text-slate-300 flex items-start justify-between gap-2"
                >
                  <span class="font-bold text-slate-400">{{ row.label }}</span>
                  <span class="text-right break-all">{{ row.value }}</span>
                </p>
              </div>
            </div>
          </template>
          <p v-else class="text-[10px] text-slate-400">No element selected.</p>
        </div>
      </div>
    </Transition>
  </div>
</template>

<style scoped>
.fade-enter-active,
.fade-leave-active {
  transition: opacity 0.35s ease;
}
.fade-enter-from,
.fade-leave-to {
  opacity: 0;
}

.slide-left-enter-active {
  transition: all 0.3s cubic-bezier(0.25, 0.46, 0.45, 0.94);
}
.slide-left-enter-from {
  opacity: 0;
  transform: translateX(-12px);
}
</style>
