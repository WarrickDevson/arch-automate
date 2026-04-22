<script setup>
import { ref, computed, watch, nextTick } from 'vue'
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs'
import { Label } from '@/components/ui/label'
import { Input } from '@/components/ui/input'
import { Button } from '@/components/ui/button'
import {
  CheckCircle2,
  XCircle,
  AlertTriangle,
  Info,
  Play,
  ArrowRight,
  Layers,
  BarChart3,
  Loader2,
  Zap,
} from 'lucide-vue-next'

const props = defineProps({
  project: { type: Object, default: () => ({ siteAreaM2: 0, zoningScheme: '-' }) },
  complianceResult: { type: Object, default: null },
  energyResult: { type: Object, default: null },
  ifcStats: { type: Object, default: null },
  ifcDimensions: { type: Object, default: null },
  isAnalyzing: { type: Boolean, default: false },
  province: { type: String, default: '' },
  ifcGlazingUValue: { type: Number, default: null },
})

const emit = defineEmits(['run-analysis'])

const activeTab = ref('parameters')
const energyTabRef = ref(null)

// ── General compliance parameters ───────────────────────────────────────────
const params = ref({
  proposedGfaM2: 0,
  footprintM2: 0,
  numberOfStoreys: 1,
  buildingHeightM: 0,
  frontSetbackM: 0,
  rearSetbackM: 0,
  sideSetbackM: 0,
  parkingBaysProvided: 0,
  glaForParkingM2: 0,
})

// ── SANS 10400-XA energy parameters ─────────────────────────────────────────
const energyParams = ref({
  climateZone: 0,          // 0 = auto-resolve from province
  occupancyType: 0,        // 0 = Residential (enum)
  grossWallAreaM2: 0,
  grossWindowAreaM2: 0,
  windowCount: 0,
  roofAreaM2: 0,
  roofRValue: 0,
  wallRValue: 0,
  glazingUValue: 0,
  lightingPowerDensityWPerM2: 0,
  province: '',
  solarHeatGainCoefficient: 0,
})

const CLIMATE_ZONES = [
  { value: 0, label: 'Auto (from province)' },
  { value: 1, label: 'Zone 1 – Cape Coastal' },
  { value: 2, label: 'Zone 2 – Highveld' },
  { value: 3, label: 'Zone 3 – Northern Interior' },
  { value: 4, label: 'Zone 4 – Eastern Coastal Belt' },
  { value: 5, label: 'Zone 5 – Semi-Arid Interior' },
  { value: 6, label: 'Zone 6 – Arid / Karoo' },
]

const OCCUPANCY_TYPES = [
  { value: 0, label: 'Residential' },
  { value: 1, label: 'Office' },
  { value: 2, label: 'Retail' },
  { value: 3, label: 'Industrial' },
  { value: 4, label: 'Educational' },
  { value: 5, label: 'Healthcare' },
]

const PROVINCES = [
  'Western Cape', 'Eastern Cape', 'KwaZulu-Natal', 'Gauteng',
  'Mpumalanga', 'Limpopo', 'North West', 'Northern Cape', 'Free State',
]

// Track which fields the user has manually edited
const userTouched = ref(new Set())
const autoFilled = ref(false)
const windowsAutoFilled = ref(false)
const glazingAutoFilled = ref(false)
const provinceAutoSet = ref(false)

// Seed params from saved project data when the project loads (or changes)
watch(
  () => props.project,
  (proj) => {
    if (!proj) return
    // Only fill fields that haven't been touched by the user this session
    const fields = [
      ['proposedGfaM2', 'proposedGfaM2'],
      ['footprintM2', 'footprintM2'],
      ['numberOfStoreys', 'numberOfStoreys'],
      ['buildingHeightM', 'buildingHeightM'],
      ['frontSetbackM', 'frontSetbackM'],
      ['rearSetbackM', 'rearSetbackM'],
      ['sideSetbackM', 'sideSetbackM'],
      ['parkingBaysProvided', 'parkingBays'],
      ['glaForParkingM2', 'glaForParkingM2'],
    ]
    for (const [paramKey, projKey] of fields) {
      if (!userTouched.value.has(paramKey) && proj[projKey] != null && proj[projKey] !== 0) {
        params.value[paramKey] = proj[projKey]
      }
    }
  },
  { immediate: true, deep: false },
)

// Auto-wire province from project when user hasn't touched the province field
watch(
  () => props.province,
  (p) => {
    if (p && !userTouched.value.has('province')) {
      energyParams.value.province = p
      provinceAutoSet.value = true
    }
  },
  { immediate: true },
)

// Auto-populate glazing U-value from IFC extraction
watch(
  () => props.ifcGlazingUValue,
  (u) => {
    if (u != null && !userTouched.value.has('glazingUValue')) {
      energyParams.value.glazingUValue = u
      glazingAutoFilled.value = true
    }
  },
)

watch(
  () => props.ifcDimensions,
  (dims) => {
    if (!dims) return
    if (!userTouched.value.has('buildingHeightM') && dims.heightM > 0)
      params.value.buildingHeightM = dims.heightM
    if (!userTouched.value.has('footprintM2') && dims.footprintM2 > 0)
      params.value.footprintM2 = dims.footprintM2
    if (!userTouched.value.has('numberOfStoreys') && dims.numberOfStoreys > 0)
      params.value.numberOfStoreys = dims.numberOfStoreys
    autoFilled.value = true
  },
)

// Auto-populate window count from IFC stats
watch(
  () => props.ifcStats,
  (stats) => {
    if (!stats) return
    const winCount = stats.windows ?? stats.Windows ?? 0
    if (winCount > 0 && !userTouched.value.has('windowCount')) {
      energyParams.value.windowCount = winCount
      windowsAutoFilled.value = true
    }
  },
)

function markTouched(field) {
  userTouched.value.add(field)
}

function goToEnergyTab() {
  activeTab.value = 'energy'
  nextTick(() => {
    const energyTabEl = energyTabRef.value?.$el ?? energyTabRef.value
    energyTabEl?.scrollTo?.({ top: 0, behavior: 'smooth' })
  })
}

function triggerAnalysis() {
  emit('run-analysis', {
    ...params.value,
    energy: { ...energyParams.value },
  })
  activeTab.value = 'analysis'
}

defineExpose({ triggerAnalysis })

// ── Severity styling ─────────────────────────────────────────────────────────
const severityClass = {
  Advisory: 'bg-blue-50 text-blue-700 border-blue-200',
  Warning: 'bg-amber-50 text-amber-700 border-amber-200',
  NonCompliant: 'bg-rose-50 text-rose-700 border-rose-200',
  0: 'bg-blue-50 text-blue-700 border-blue-200',
  1: 'bg-amber-50 text-amber-700 border-amber-200',
  2: 'bg-rose-50 text-rose-700 border-rose-200',
}

// ── Compliance score ─────────────────────────────────────────────────────────
const passedChecks = computed(() => (props.complianceResult?.checks ?? []).filter((c) => c.passed).length)
const totalChecks = computed(() => (props.complianceResult?.checks ?? []).length)
const score = computed(() =>
  totalChecks.value > 0 ? Math.round((passedChecks.value / totalChecks.value) * 100) : null,
)
const scoreClass = computed(() => {
  if (score.value === null) return 'text-slate-400'
  if (score.value >= 80) return 'text-emerald-600'
  if (score.value >= 60) return 'text-amber-500'
  return 'text-rose-600'
})

// ── Energy rating colour ─────────────────────────────────────────────────────
const energyRatingClass = computed(() => {
  const r = props.energyResult?.energyRating ?? '–'
  return {
    A: 'bg-emerald-500 text-white',
    B: 'bg-green-400 text-white',
    C: 'bg-lime-400 text-slate-800',
    D: 'bg-amber-400 text-slate-800',
    E: 'bg-orange-500 text-white',
    F: 'bg-rose-600 text-white',
  }[r] ?? 'bg-slate-200 text-slate-600'
})

// ── BIM data table ───────────────────────────────────────────────────────────
const bimRows = computed(() => {
  if (!props.ifcStats) return []
  return Object.entries(props.ifcStats)
    .filter(([k]) => k !== 'total')
    .map(([k, v]) => ({ label: k.charAt(0).toUpperCase() + k.slice(1), count: v }))
})
</script>

<template>
  <Tabs v-model="activeTab" class="flex flex-col h-full">
    <TabsList class="grid w-full grid-cols-4 bg-slate-100 p-1 rounded-none shrink-0">
      <TabsTrigger value="parameters" class="text-[9px] uppercase font-bold">Params</TabsTrigger>
      <TabsTrigger value="energy" class="text-[9px] uppercase font-bold flex items-center gap-1">
        Energy
      </TabsTrigger>
      <TabsTrigger value="analysis" class="text-[9px] uppercase font-bold">Analysis</TabsTrigger>
      <TabsTrigger value="bim" class="text-[9px] uppercase font-bold">BIM</TabsTrigger>
    </TabsList>

    <!-- ── Parameters Tab ───────────────────────────────────────────────────── -->
    <TabsContent value="parameters" class="flex-1 overflow-y-auto p-4 space-y-4 mt-0">
      <div class="grid grid-cols-2 gap-3">
        <div class="space-y-1">
          <Label class="text-[10px] uppercase font-bold text-slate-400">Site Area (m²)</Label>
          <div class="h-9 flex items-center px-3 bg-slate-50 border border-slate-200 rounded-md text-sm font-mono text-slate-500">
            {{ project.siteAreaM2?.toLocaleString() || '—' }}
          </div>
        </div>
        <div class="space-y-1">
          <Label class="text-[10px] uppercase font-bold text-slate-400">Zoning Scheme</Label>
          <div class="h-9 flex items-center px-3 bg-slate-50 border border-slate-200 rounded-md text-xs font-medium text-slate-500 truncate">
            {{ project.zoningScheme || '—' }}
          </div>
        </div>
      </div>

      <div class="h-px bg-slate-100" />

      <div
        v-if="autoFilled"
        class="flex items-start gap-2 rounded-lg bg-blue-50 border border-blue-100 px-3 py-2.5 text-[11px] text-blue-700"
      >
        <Info class="h-3.5 w-3.5 mt-0.5 flex-shrink-0" />
        <span>Height, storeys &amp; footprint were read from the IFC model.</span>
      </div>
      <div
        v-else-if="!props.ifcDimensions"
        class="flex items-start gap-2 rounded-lg bg-slate-50 border border-slate-100 px-3 py-2.5 text-[11px] text-slate-500"
      >
        <Info class="h-3.5 w-3.5 mt-0.5 flex-shrink-0" />
        <span>Load an IFC model to auto-fill dimensions.</span>
      </div>

      <div class="grid grid-cols-2 gap-3">
        <div class="space-y-1">
          <Label class="text-[10px] uppercase font-bold text-slate-500">GFA (m²)</Label>
          <Input v-model.number="params.proposedGfaM2" type="number" min="0" class="h-9 text-sm" placeholder="0" @input="markTouched('proposedGfaM2')" />
        </div>
        <div class="space-y-1">
          <div class="flex items-center justify-between">
            <Label class="text-[10px] uppercase font-bold text-slate-500">Footprint (m²)</Label>
            <span v-if="autoFilled && !userTouched.has('footprintM2')" class="text-[9px] font-bold uppercase tracking-wide text-blue-500">IFC</span>
          </div>
          <Input v-model.number="params.footprintM2" type="number" min="0" class="h-9 text-sm" placeholder="0" @input="markTouched('footprintM2')" />
        </div>
        <div class="space-y-1">
          <div class="flex items-center justify-between">
            <Label class="text-[10px] uppercase font-bold text-slate-500">Storeys</Label>
            <span v-if="autoFilled && !userTouched.has('numberOfStoreys')" class="text-[9px] font-bold uppercase tracking-wide text-blue-500">IFC</span>
          </div>
          <Input v-model.number="params.numberOfStoreys" type="number" min="1" class="h-9 text-sm" placeholder="1" @input="markTouched('numberOfStoreys')" />
        </div>
        <div class="space-y-1">
          <div class="flex items-center justify-between">
            <Label class="text-[10px] uppercase font-bold text-slate-500">Height (m)</Label>
            <span v-if="autoFilled && !userTouched.has('buildingHeightM')" class="text-[9px] font-bold uppercase tracking-wide text-blue-500">IFC</span>
          </div>
          <Input v-model.number="params.buildingHeightM" type="number" min="0" step="0.1" class="h-9 text-sm" placeholder="0" @input="markTouched('buildingHeightM')" />
        </div>
        <div class="space-y-1">
          <Label class="text-[10px] uppercase font-bold text-slate-500">Front Setback (m)</Label>
          <Input v-model.number="params.frontSetbackM" type="number" min="0" step="0.1" class="h-9 text-sm" placeholder="0" />
        </div>
        <div class="space-y-1">
          <Label class="text-[10px] uppercase font-bold text-slate-500">Rear Setback (m)</Label>
          <Input v-model.number="params.rearSetbackM" type="number" min="0" step="0.1" class="h-9 text-sm" placeholder="0" />
        </div>
        <div class="space-y-1">
          <Label class="text-[10px] uppercase font-bold text-slate-500">Side Setback (m)</Label>
          <Input v-model.number="params.sideSetbackM" type="number" min="0" step="0.1" class="h-9 text-sm" placeholder="0" />
        </div>
        <div class="space-y-1">
          <Label class="text-[10px] uppercase font-bold text-slate-500">Parking Bays</Label>
          <Input v-model.number="params.parkingBaysProvided" type="number" min="0" class="h-9 text-sm" placeholder="0" />
        </div>
        <div class="col-span-2 space-y-1">
          <Label class="text-[10px] uppercase font-bold text-slate-500">GLA for Parking (m²)</Label>
          <Input v-model.number="params.glaForParkingM2" type="number" min="0" class="h-9 text-sm" placeholder="0" />
        </div>
      </div>

      <Button
        class="w-full gap-2 uppercase text-xs font-bold h-10"
        variant="outline"
        @click="goToEnergyTab"
      >
        <ArrowRight class="h-3.5 w-3.5" />
        <span>Next: Energy</span>
      </Button>
      <p class="text-[10px] text-slate-400 text-center">Step 1 of 2: set zoning and dimensional inputs.</p>
    </TabsContent>

    <!-- ── Energy (SANS 10400-XA) Parameters Tab ─────────────────────────────── -->
    <TabsContent ref="energyTabRef" value="energy" class="flex-1 overflow-y-auto p-4 space-y-4 mt-0">
      <!-- Info banner -->
      <div class="flex items-start gap-2 rounded-lg bg-emerald-50 border border-emerald-100 px-3 py-2.5 text-[11px] text-emerald-800">
        <Zap class="h-3.5 w-3.5 mt-0.5 flex-shrink-0" />
        <span>SANS 10400-XA energy parameters. Leave thermal values at 0 if unknown — the engine will flag them as advisory checks.</span>
      </div>

      <!-- Climate zone + occupancy -->
      <div class="space-y-3">
        <p class="text-[10px] font-bold text-slate-400 uppercase tracking-widest">Climate &amp; Occupancy</p>

        <div class="space-y-1">
          <Label class="text-[10px] uppercase font-bold text-slate-500">Climate Zone</Label>
          <select
            v-model.number="energyParams.climateZone"
            class="w-full h-9 rounded-md border border-slate-200 dark:border-slate-700 bg-white dark:bg-slate-950 px-3 text-sm text-slate-700 dark:text-slate-100 focus:outline-none focus:ring-2 focus:ring-blue-500"
          >
            <option v-for="z in CLIMATE_ZONES" :key="z.value" :value="z.value">{{ z.label }}</option>
          </select>
        </div>

        <div v-if="energyParams.climateZone === 0 && energyParams.province" class="flex items-center gap-2 rounded-md bg-slate-50 border border-slate-100 px-3 py-2">
          <span class="text-[10px] text-slate-500 uppercase font-bold">Province</span>
          <span class="text-[11px] font-semibold text-slate-700 flex-1">{{ energyParams.province }}</span>
          <span class="text-[9px] bg-emerald-100 text-emerald-700 rounded px-1 font-bold">Auto</span>
        </div>
        <div v-else-if="energyParams.climateZone === 0 && !energyParams.province" class="rounded-md bg-amber-50 border border-amber-100 px-3 py-2 text-[10px] text-amber-700">
          Province not resolved — select a municipality on the project to auto-detect the climate zone.
        </div>

        <div class="space-y-1">
          <Label class="text-[10px] uppercase font-bold text-slate-500">Occupancy Type</Label>
          <select
            v-model.number="energyParams.occupancyType"
            class="w-full h-9 rounded-md border border-slate-200 dark:border-slate-700 bg-white dark:bg-slate-950 px-3 text-sm text-slate-700 dark:text-slate-100 focus:outline-none focus:ring-2 focus:ring-blue-500"
          >
            <option v-for="o in OCCUPANCY_TYPES" :key="o.value" :value="o.value">{{ o.label }}</option>
          </select>
        </div>
      </div>

      <div class="h-px bg-slate-100" />

      <!-- Envelope areas -->
      <div class="space-y-3">
        <p class="text-[10px] font-bold text-slate-400 uppercase tracking-widest">Envelope Areas</p>

        <div class="grid grid-cols-2 gap-3">
          <div class="space-y-1">
            <Label class="text-[10px] uppercase font-bold text-slate-500">Wall Area (m²)</Label>
            <Input v-model.number="energyParams.grossWallAreaM2" type="number" min="0" class="h-9 text-sm" placeholder="0 = estimate" />
          </div>
          <div class="space-y-1">
            <div class="flex items-center justify-between">
              <Label class="text-[10px] uppercase font-bold text-slate-500">Window Area (m²)</Label>
            </div>
            <Input v-model.number="energyParams.grossWindowAreaM2" type="number" min="0" class="h-9 text-sm" placeholder="0 = from count" />
          </div>
          <div class="col-span-2 space-y-1">
            <div class="flex items-center justify-between">
              <Label class="text-[10px] uppercase font-bold text-slate-500">Window Count</Label>
              <span v-if="windowsAutoFilled && !userTouched.has('windowCount')" class="text-[9px] font-bold uppercase tracking-wide text-blue-500">From IFC</span>
            </div>
            <Input
              v-model.number="energyParams.windowCount"
              type="number"
              min="0"
              class="h-9 text-sm"
              placeholder="0"
              @input="markTouched('windowCount')"
            />
          </div>
          <div class="col-span-2 space-y-1">
            <Label class="text-[10px] uppercase font-bold text-slate-500">Roof Area (m²)</Label>
            <Input v-model.number="energyParams.roofAreaM2" type="number" min="0" class="h-9 text-sm" placeholder="0 = use footprint" />
          </div>
        </div>
      </div>

      <div class="h-px bg-slate-100" />

      <!-- Thermal properties -->
      <div class="space-y-3">
        <p class="text-[10px] font-bold text-slate-400 uppercase tracking-widest">Thermal Properties</p>
        <p class="text-[10px] text-slate-400">Leave at 0 if unknown. Checks become advisory.</p>

        <div class="grid grid-cols-2 gap-3">
          <div class="space-y-1">
            <Label class="text-[10px] uppercase font-bold text-slate-500">Roof R-value</Label>
            <div class="relative">
              <Input v-model.number="energyParams.roofRValue" type="number" min="0" step="0.1" class="h-9 text-sm pr-14" placeholder="0.0" />
              <span class="absolute right-3 top-1/2 -translate-y-1/2 text-[10px] text-slate-400 font-mono">m²·K/W</span>
            </div>
          </div>
          <div class="space-y-1">
            <Label class="text-[10px] uppercase font-bold text-slate-500">Wall R-value</Label>
            <div class="relative">
              <Input v-model.number="energyParams.wallRValue" type="number" min="0" step="0.1" class="h-9 text-sm pr-14" placeholder="0.0" />
              <span class="absolute right-3 top-1/2 -translate-y-1/2 text-[10px] text-slate-400 font-mono">m²·K/W</span>
            </div>
          </div>
          <div class="space-y-1">
            <div class="flex items-center justify-between">
              <Label class="text-[10px] uppercase font-bold text-slate-500">Glazing U-value</Label>
              <span v-if="glazingAutoFilled && !userTouched.has?.('glazingUValue')" class="text-[9px] bg-sky-100 text-sky-700 rounded px-1 font-bold">From IFC</span>
            </div>
            <div class="relative">
              <Input v-model.number="energyParams.glazingUValue" type="number" min="0" step="0.1" class="h-9 text-sm pr-16" placeholder="0.0" @input="markTouched('glazingUValue')" />
              <span class="absolute right-3 top-1/2 -translate-y-1/2 text-[10px] text-slate-400 font-mono">W/m²K</span>
            </div>
          </div>
          <div class="space-y-1">
            <Label class="text-[10px] uppercase font-bold text-slate-500">SHGC (g-value)</Label>
            <div class="relative">
              <Input v-model.number="energyParams.solarHeatGainCoefficient" type="number" min="0" max="1" step="0.01" class="h-9 text-sm pr-16" placeholder="0.00" @input="markTouched('shgc')" />
              <span class="absolute right-3 top-1/2 -translate-y-1/2 text-[10px] text-slate-400 font-mono">SHGC</span>
            </div>
          </div>
          <div class="space-y-1">
            <Label class="text-[10px] uppercase font-bold text-slate-500">Lighting LPD</Label>
            <div class="relative">
              <Input v-model.number="energyParams.lightingPowerDensityWPerM2" type="number" min="0" step="0.5" class="h-9 text-sm pr-10" placeholder="0" />
              <span class="absolute right-3 top-1/2 -translate-y-1/2 text-[10px] text-slate-400 font-mono">W/m²</span>
            </div>
          </div>
        </div>
      </div>

      <Button
        class="w-full bg-blue-600 hover:bg-blue-700 gap-2 uppercase text-xs font-bold h-10"
        :disabled="isAnalyzing"
        @click="triggerAnalysis"
      >
        <Loader2 v-if="isAnalyzing" class="h-3.5 w-3.5 animate-spin" />
        <Play v-else class="h-3.5 w-3.5 fill-current" />
        <span>{{ isAnalyzing ? 'Analyzing…' : 'Run Full Analysis' }}</span>
      </Button>
      <p class="text-[10px] text-slate-400 text-center">Step 2 of 2: runs zoning + SANS 10400-XA together.</p>
    </TabsContent>

    <!-- ── Analysis Tab ──────────────────────────────────────────────────────── -->
    <TabsContent value="analysis" class="flex-1 overflow-y-auto p-4 space-y-4 mt-0">
      <div v-if="!complianceResult && !energyResult" class="flex flex-col items-center justify-center h-48 text-center gap-3">
        <template v-if="isAnalyzing">
          <Loader2 class="h-8 w-8 text-blue-400 animate-spin" />
          <p class="text-sm font-medium text-slate-500">Running analysis…</p>
        </template>
        <template v-else>
          <BarChart3 class="h-8 w-8 text-slate-200" />
          <p class="text-sm font-medium text-slate-400">No analysis yet</p>
          <p class="text-xs text-slate-400">Fill in Parameters &amp; Energy tabs, then run full analysis</p>
        </template>
      </div>

      <template v-else>
        <!-- ── Score cards ── -->
        <div class="grid grid-cols-2 gap-3">
          <!-- Compliance score -->
          <div class="bg-slate-50 border border-slate-200 rounded-xl p-3 text-center">
            <p class="text-[9px] font-bold text-slate-400 uppercase tracking-widest mb-1">Compliance</p>
            <p class="text-3xl font-mono font-bold" :class="scoreClass">{{ score ?? '–' }}%</p>
            <p class="text-[10px] text-slate-500 mt-0.5">{{ passedChecks }}/{{ totalChecks }} checks</p>
          </div>
          <!-- Energy rating -->
          <div class="bg-slate-50 border border-slate-200 rounded-xl p-3 flex flex-col items-center justify-center gap-1">
            <p class="text-[9px] font-bold text-slate-400 uppercase tracking-widest">XA Rating</p>
            <span
              class="inline-flex items-center justify-center w-12 h-12 rounded-full text-2xl font-black"
              :class="energyRatingClass"
            >
              {{ energyResult?.energyRating ?? '–' }}
            </span>
            <p class="text-[9px] text-slate-400">{{ energyResult?.climateZoneName?.split('(')[0]?.trim() ?? '' }}</p>
          </div>
        </div>

        <!-- ── Metrics row ── -->
        <div v-if="complianceResult" class="grid grid-cols-2 gap-3">
          <div class="bg-white border border-slate-200 rounded-xl p-3">
            <p class="text-[10px] font-bold text-slate-400 uppercase">Site Coverage</p>
            <p class="text-xl font-mono font-bold text-slate-800 mt-1">
              {{ complianceResult.siteCoveragePercent?.toFixed(1) }}%
            </p>
            <div class="w-full h-1.5 bg-slate-100 rounded-full mt-2 overflow-hidden">
              <div
                class="h-full rounded-full transition-all"
                :class="complianceResult.siteCoveragePercent > 70 ? 'bg-rose-500' : 'bg-emerald-500'"
                :style="{ width: Math.min(complianceResult.siteCoveragePercent ?? 0, 100) + '%' }"
              />
            </div>
          </div>
          <div class="bg-white border border-slate-200 rounded-xl p-3">
            <p class="text-[10px] font-bold text-slate-400 uppercase">FAR</p>
            <p class="text-xl font-mono font-bold text-slate-800 mt-1">
              {{ complianceResult.floorAreaRatio?.toFixed(2) }}
            </p>
            <div class="w-full h-1.5 bg-slate-100 rounded-full mt-2 overflow-hidden">
              <div
                class="h-full rounded-full transition-all bg-blue-500"
                :style="{ width: Math.min((complianceResult.floorAreaRatio ?? 0) * 25, 100) + '%' }"
              />
            </div>
          </div>
        </div>

        <!-- ── Energy metrics ── -->
        <div v-if="energyResult" class="grid grid-cols-3 gap-2">
          <div class="bg-white border border-slate-100 rounded-lg p-2 text-center">
            <p class="text-[9px] font-bold text-slate-400 uppercase">WWR</p>
            <p class="text-base font-mono font-bold text-slate-800 mt-0.5">
              {{ energyResult.windowToWallRatioPercent != null ? energyResult.windowToWallRatioPercent + '%' : '—' }}
            </p>
            <p class="text-[9px] text-slate-400">max {{ energyResult.maxWindowToWallRatioPercent }}%</p>
          </div>
          <div class="bg-white border border-slate-100 rounded-lg p-2 text-center">
            <p class="text-[9px] font-bold text-slate-400 uppercase">Daylight</p>
            <p class="text-base font-mono font-bold text-slate-800 mt-0.5">
              {{ energyResult.daylightingRatioPercent != null ? energyResult.daylightingRatioPercent + '%' : '—' }}
            </p>
            <p class="text-[9px] text-slate-400">min 10%</p>
          </div>
          <div class="bg-white border border-slate-100 rounded-lg p-2 text-center">
            <p class="text-[9px] font-bold text-slate-400 uppercase">S/V</p>
            <p class="text-base font-mono font-bold text-slate-800 mt-0.5">
              {{ energyResult.surfaceToVolumeRatio != null ? energyResult.surfaceToVolumeRatio : '—' }}
            </p>
            <p class="text-[9px] text-slate-400">m⁻¹</p>
          </div>
        </div>

        <!-- ── Zoning checks ── -->
        <div v-if="complianceResult?.checks?.length" class="space-y-2">
          <p class="text-[10px] font-bold text-slate-400 uppercase tracking-widest">Zoning &amp; SANS Checks</p>
          <div
            v-for="check in complianceResult.checks"
            :key="check.rule"
            class="flex items-start gap-3 p-3 rounded-lg border bg-white"
            :class="check.passed ? 'border-emerald-100' : 'border-rose-100'"
          >
            <CheckCircle2 v-if="check.passed" class="h-4 w-4 text-emerald-500 shrink-0 mt-0.5" />
            <XCircle v-else class="h-4 w-4 text-rose-500 shrink-0 mt-0.5" />
            <div class="min-w-0 flex-1">
              <p class="text-xs font-bold text-slate-700 leading-snug">{{ check.description }}</p>
              <p class="text-[10px] text-slate-500 font-mono mt-0.5">
                {{ check.providedValue }} {{ check.unit }} / required {{ check.requiredValue }} {{ check.unit }}
              </p>
            </div>
          </div>
        </div>

        <!-- ── Energy (XA) checks ── -->
        <div v-if="energyResult?.checks?.length" class="space-y-2">
          <p class="text-[10px] font-bold text-emerald-600 uppercase tracking-widest flex items-center gap-1">
            <Zap class="h-3 w-3" /> SANS 10400-XA Energy Checks
          </p>
          <div
            v-for="check in energyResult.checks"
            :key="check.rule"
            class="flex items-start gap-3 p-3 rounded-lg border bg-white"
            :class="check.passed ? 'border-emerald-100' : 'border-rose-100'"
          >
            <CheckCircle2 v-if="check.passed" class="h-4 w-4 text-emerald-500 shrink-0 mt-0.5" />
            <XCircle v-else class="h-4 w-4 text-rose-500 shrink-0 mt-0.5" />
            <div class="min-w-0 flex-1">
              <p class="text-xs font-bold text-slate-700 leading-snug">{{ check.description }}</p>
              <p v-if="check.providedValue > 0 || check.requiredValue > 0" class="text-[10px] text-slate-500 font-mono mt-0.5">
                {{ check.providedValue }} {{ check.unit }} / {{ check.passed ? 'compliant ≤' : 'required' }} {{ check.requiredValue }} {{ check.unit }}
              </p>
            </div>
          </div>
        </div>

        <!-- ── All violations ── -->
        <div
          v-if="(complianceResult?.violations?.length || energyResult?.violations?.length)"
          class="space-y-2"
        >
          <p class="text-[10px] font-bold text-slate-400 uppercase tracking-widest">Violations &amp; Advisories</p>
          <div
            v-for="v in [...(complianceResult?.violations ?? []), ...(energyResult?.violations ?? [])]"
            :key="v.rule + v.message"
            class="p-3 rounded-lg border text-xs"
            :class="severityClass[v.severity] ?? 'bg-slate-50 text-slate-700 border-slate-200'"
          >
            <div class="flex items-center gap-2">
              <AlertTriangle v-if="v.severity === 'Warning' || v.severity === 1" class="h-3.5 w-3.5 shrink-0" />
              <p class="font-bold">{{ v.clauseReference }}</p>
            </div>
            <p class="mt-0.5 leading-snug">{{ v.message }}</p>
          </div>
        </div>
      </template>
    </TabsContent>

    <!-- ── BIM Data Tab ───────────────────────────────────────────────────────── -->
    <TabsContent value="bim" class="flex-1 overflow-y-auto p-4 space-y-4 mt-0">
      <div v-if="!ifcStats" class="flex flex-col items-center justify-center h-48 text-center gap-3">
        <Layers class="h-8 w-8 text-slate-200" />
        <p class="text-sm font-medium text-slate-400">No IFC loaded</p>
        <p class="text-xs text-slate-400">Load an IFC model to see element counts</p>
      </div>
      <template v-else>
        <div class="bg-slate-50 border border-slate-200 rounded-xl p-3 text-center">
          <p class="text-[10px] font-bold text-slate-400 uppercase">Total Elements</p>
          <p class="text-3xl font-mono font-bold text-slate-800">{{ ifcStats.total?.toLocaleString() }}</p>
        </div>
        <div class="space-y-1.5">
          <div
            v-for="row in bimRows"
            :key="row.label"
            class="flex items-center justify-between px-3 py-2 bg-white border border-slate-100 rounded-lg"
          >
            <span class="text-xs font-bold text-slate-600 uppercase">{{ row.label }}</span>
            <span class="text-xs font-mono font-bold text-slate-900">{{ row.count.toLocaleString() }}</span>
          </div>
        </div>
      </template>
    </TabsContent>
  </Tabs>
</template>
