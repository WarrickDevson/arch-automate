<script setup>
import { ref, computed, watch } from 'vue'
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
  Layers,
  BarChart3,
  Loader2,
} from 'lucide-vue-next'

const props = defineProps({
  project: { type: Object, default: () => ({ siteAreaM2: 0, zoningScheme: '-' }) },
  complianceResult: { type: Object, default: null },
  ifcStats: { type: Object, default: null },
  ifcDimensions: { type: Object, default: null },
  isAnalyzing: { type: Boolean, default: false },
})

const emit = defineEmits(['run-analysis'])

const activeTab = ref('parameters')

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

// Track which fields the user has manually edited so we never overwrite them
const userTouched = ref(new Set())
const autoFilled = ref(false)

watch(
  () => props.ifcDimensions,
  (dims) => {
    if (!dims) return
    // Only pre-fill fields the user hasn't touched yet
    if (!userTouched.value.has('buildingHeightM') && dims.heightM > 0)
      params.value.buildingHeightM = dims.heightM
    if (!userTouched.value.has('footprintM2') && dims.footprintM2 > 0)
      params.value.footprintM2 = dims.footprintM2
    if (!userTouched.value.has('numberOfStoreys') && dims.numberOfStoreys > 0)
      params.value.numberOfStoreys = dims.numberOfStoreys
    autoFilled.value = true
  }
)

function markTouched(field) {
  userTouched.value.add(field)
}

function triggerAnalysis() {
  emit('run-analysis', { ...params.value })
  activeTab.value = 'analysis'
}

defineExpose({ triggerAnalysis })

const severityClass = {
  Advisory: 'bg-blue-50 text-blue-700 border-blue-200',
  Warning: 'bg-amber-50 text-amber-700 border-amber-200',
  NonCompliant: 'bg-rose-50 text-rose-700 border-rose-200',
  0: 'bg-blue-50 text-blue-700 border-blue-200',
  1: 'bg-amber-50 text-amber-700 border-amber-200',
  2: 'bg-rose-50 text-rose-700 border-rose-200',
}

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

const bimRows = computed(() => {
  if (!props.ifcStats) return []
  return Object.entries(props.ifcStats)
    .filter(([k]) => k !== 'total')
    .map(([k, v]) => ({ label: k.charAt(0).toUpperCase() + k.slice(1), count: v }))
})
</script>

<template>
  <Tabs v-model="activeTab" class="flex flex-col h-full">
    <TabsList class="grid w-full grid-cols-3 bg-slate-100 p-1 rounded-none shrink-0">
      <TabsTrigger value="parameters" class="text-[10px] uppercase font-bold">Parameters</TabsTrigger>
      <TabsTrigger value="analysis" class="text-[10px] uppercase font-bold">Analysis</TabsTrigger>
      <TabsTrigger value="bim" class="text-[10px] uppercase font-bold">BIM Data</TabsTrigger>
    </TabsList>

    <!-- ── Parameters Tab ── -->
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

      <!-- Auto-fill notice -->
      <div
        v-if="autoFilled"
        class="flex items-start gap-2 rounded-lg bg-blue-50 border border-blue-100 px-3 py-2.5 text-[11px] text-blue-700"
      >
        <Info class="h-3.5 w-3.5 mt-0.5 flex-shrink-0" />
        <span>Height, storeys &amp; footprint were read from the IFC model. Setbacks must be entered manually.</span>
      </div>
      <div
        v-else-if="!props.ifcDimensions"
        class="flex items-start gap-2 rounded-lg bg-slate-50 border border-slate-100 px-3 py-2.5 text-[11px] text-slate-500"
      >
        <Info class="h-3.5 w-3.5 mt-0.5 flex-shrink-0" />
        <span>Load an IFC model to auto-fill height, storeys &amp; footprint. Setbacks always require manual input.</span>
      </div>

      <div class="grid grid-cols-2 gap-3">
        <div class="space-y-1">
          <Label class="text-[10px] uppercase font-bold text-slate-500">GFA (m²)</Label>
          <Input v-model.number="params.proposedGfaM2" type="number" min="0" class="h-9 text-sm" placeholder="0" @input="markTouched('proposedGfaM2')" />
        </div>
        <div class="space-y-1">
          <div class="flex items-center justify-between">
            <Label class="text-[10px] uppercase font-bold text-slate-500">Footprint (m²)</Label>
            <span v-if="autoFilled && !userTouched.has('footprintM2')" class="text-[9px] font-bold uppercase tracking-wide text-blue-500">From IFC</span>
          </div>
          <Input v-model.number="params.footprintM2" type="number" min="0" class="h-9 text-sm" placeholder="0" @input="markTouched('footprintM2')" />
        </div>
        <div class="space-y-1">
          <div class="flex items-center justify-between">
            <Label class="text-[10px] uppercase font-bold text-slate-500">Storeys</Label>
            <span v-if="autoFilled && !userTouched.has('numberOfStoreys')" class="text-[9px] font-bold uppercase tracking-wide text-blue-500">From IFC</span>
          </div>
          <Input v-model.number="params.numberOfStoreys" type="number" min="1" class="h-9 text-sm" placeholder="1" @input="markTouched('numberOfStoreys')" />
        </div>
        <div class="space-y-1">
          <div class="flex items-center justify-between">
            <Label class="text-[10px] uppercase font-bold text-slate-500">Height (m)</Label>
            <span v-if="autoFilled && !userTouched.has('buildingHeightM')" class="text-[9px] font-bold uppercase tracking-wide text-blue-500">From IFC</span>
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
        class="w-full bg-blue-600 hover:bg-blue-700 gap-2 uppercase text-xs font-bold h-10"
        :disabled="isAnalyzing"
        @click="triggerAnalysis"
      >
        <Loader2 v-if="isAnalyzing" class="h-3.5 w-3.5 animate-spin" />
        <Play v-else class="h-3.5 w-3.5 fill-current" />
        <span>{{ isAnalyzing ? 'Analyzing…' : 'Run Analysis' }}</span>
      </Button>
    </TabsContent>

    <!-- ── Analysis Tab ── -->
    <TabsContent value="analysis" class="flex-1 overflow-y-auto p-4 space-y-4 mt-0">
      <div v-if="!complianceResult" class="flex flex-col items-center justify-center h-48 text-center gap-3">
        <template v-if="isAnalyzing">
          <Loader2 class="h-8 w-8 text-blue-400 animate-spin" />
          <p class="text-sm font-medium text-slate-500">Running analysis…</p>
        </template>
        <template v-else>
          <BarChart3 class="h-8 w-8 text-slate-200" />
          <p class="text-sm font-medium text-slate-400">No analysis yet</p>
          <p class="text-xs text-slate-400">Fill in the Parameters tab and run analysis</p>
        </template>
      </div>

      <template v-else>
        <div class="bg-slate-50 border border-slate-200 rounded-xl p-4 text-center">
          <p class="text-[10px] font-bold text-slate-400 uppercase tracking-widest mb-1">Compliance Score</p>
          <p class="text-4xl font-mono font-bold" :class="scoreClass">{{ score }}%</p>
          <p class="text-xs text-slate-500 mt-1">{{ passedChecks }} / {{ totalChecks }} checks passed</p>
        </div>

        <div class="grid grid-cols-2 gap-3">
          <div class="bg-white border border-slate-200 rounded-xl p-3">
            <p class="text-[10px] font-bold text-slate-400 uppercase">Site Coverage</p>
            <p class="text-2xl font-mono font-bold text-slate-800 mt-1">
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
            <p class="text-2xl font-mono font-bold text-slate-800 mt-1">
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

        <div class="space-y-2">
          <p class="text-[10px] font-bold text-slate-400 uppercase tracking-widest">Checks</p>
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

        <div v-if="complianceResult.violations?.length" class="space-y-2">
          <p class="text-[10px] font-bold text-slate-400 uppercase tracking-widest">Violations</p>
          <div
            v-for="v in complianceResult.violations"
            :key="v.rule + v.message"
            class="p-3 rounded-lg border text-xs"
            :class="severityClass[v.severity] ?? 'bg-slate-50 text-slate-700 border-slate-200'"
          >
            <p class="font-bold">{{ v.clauseReference }}</p>
            <p class="mt-0.5 leading-snug">{{ v.message }}</p>
          </div>
        </div>
      </template>
    </TabsContent>

    <!-- ── BIM Data Tab ── -->
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
