<script setup>
import { ref, watch, onMounted, onBeforeUnmount } from 'vue'
import * as OBC from '@thatopen/components'
import * as OBCF from '@thatopen/components-front'
import * as THREE from 'three'
import { Upload, Layers, Ruler, RotateCcw, Loader2 } from 'lucide-vue-next'
import { saveIfc } from '@/lib/ifcCache'
import { uploadIfc, downloadIfc } from '@/services/ifcStorageService'

const props = defineProps({
  projectId: { type: String, default: null },
  tenantId: { type: String, default: null },
  ifcPath: { type: String, default: null },
})
const emit = defineEmits(['ifc-loaded', 'ifc-stats', 'ifc-dimensions', 'ifc-path-saved', 'ifc-areas'])

const container = ref(null)
const fileInput = ref(null)
const isLoading = ref(false)
const hasModel = ref(false)
const isDragging = ref(false)
const loadError = ref(null)
const activeOverlay = ref('standard')

const viewModes = [
  { id: 'standard', label: 'Standard' },
  { id: 'zoning', label: 'Zoning' },
  { id: 'sans', label: 'SANS 10400' },
  { id: 'energy', label: 'Energy Flow' },
]

// Non-reactive Three.js/OBC state — must NOT be made reactive
let _components = null
let _world = null
let _ifcLoader = null
let _loadedModel = null

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
  } catch (_) {
    /* not available in this build */
  }

  // Infinite grid
  try {
    const grids = _components.get(OBC.Grids)
    grids.create(_world)
  } catch (_) {
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

  // Auto-restore IFC: prefer Supabase Storage (cross-device) when ifcPath is
  // known; fall back to local IndexedDB cache (same-device, no path yet).
  if (props.projectId) {
    try {
      let buffer = null

      if (props.ifcPath) {
        // Primary: download from Supabase Storage (uses IndexedDB as cache)
        buffer = await downloadIfc(props.projectId, props.ifcPath)
      }

      if (buffer) {
        const fakeFile = new File([buffer], `cached_${props.projectId}.ifc`)
        fakeFile._fromCache = true
        await loadIFC(fakeFile)
      }
    } catch (e) {
      console.warn('IFC auto-restore failed:', e)
    }
  }
})

onBeforeUnmount(() => {
  _components?.dispose()
})

async function loadIFC(file) {
  if (!_components || !_world) return

  isLoading.value = true
  loadError.value = null

  try {
    const buffer = await file.arrayBuffer()
    // coordinate=true: normalises multi-model scene to a shared base origin.
    const model = await _ifcLoader.load(new Uint8Array(buffer), true, file.name)

    // Persist to Supabase Storage (primary) then IndexedDB cache (secondary).
    // Skip on cache-restore restores (_fromCache flag) to avoid re-uploading unchanged data.
    if (props.projectId && props.tenantId && !file._fromCache) {
      uploadIfc(props.tenantId, props.projectId, buffer)
        .then((path) => emit('ifc-path-saved', path))
        .catch((e) => console.warn('IFC upload failed:', e))
    } else if (props.projectId && !file._fromCache) {
      // No tenantId yet — fall back to local cache only
      saveIfc(props.projectId, buffer).catch(() => {})
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

    // model.box is populated from worker metadata during setup — much more
    // reliable than setFromObject which returns empty when no tiles exist yet.
    const bbox = model.box
    if (!bbox.isEmpty()) {
      await _world.camera.controls.fitToBox(bbox, true)
    }

    hasModel.value = true
    emit('ifc-loaded', { fileName: file.name, fileSize: file.size })

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
          const attrs = await model.getItem(node.localId).getAttributes()
          const name =
            attrs?.get('LongName')?.value ||
            attrs?.get('Name')?.value ||
            `Space ${node.localId}`
          // Area may come from a loaded quantity attribute; fall back to 0
          const area = Number(attrs?.get('NetFloorArea')?.value ?? attrs?.get('GrossFloorArea')?.value ?? 0)
          areaSchedule.push({ spaceName: name, level: currentStorey || 'Ground Floor', areaM2: area })
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
  } catch (err) {
    console.error('IFC load failed:', err)
    loadError.value = 'Could not parse this IFC file. Ensure it is a valid IFC2x3/IFC4 model.'
  } finally {
    isLoading.value = false
  }
}

function handleDrop(e) {
  isDragging.value = false
  const file = e.dataTransfer?.files?.[0]
  if (file?.name?.toLowerCase().endsWith('.ifc')) {
    loadIFC(file)
  } else if (file) {
    loadError.value = 'Only .ifc files are supported.'
  }
}

function handleFileSelect(e) {
  const file = e.target?.files?.[0]
  if (file) loadIFC(file)
  // Reset so the same file can be re-selected
  e.target.value = ''
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
        <input
          ref="fileInput"
          type="file"
          accept=".ifc"
          class="hidden"
          @change="handleFileSelect"
        />

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
      <div v-if="hasModel" class="absolute top-4 left-4 z-20">
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
      </div>
    </Transition>

    <!-- ── Tool buttons ── -->
    <Transition name="slide-left">
      <div v-if="hasModel" class="absolute bottom-4 left-4 z-20 flex gap-2">
        <button
          class="p-2.5 bg-black/50 backdrop-blur-md rounded-lg text-slate-400 border border-white/10 hover:text-white hover:bg-white/10 hover:border-white/20 transition-all"
          title="Measure"
        >
          <Ruler class="h-4 w-4" />
        </button>
        <button
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
