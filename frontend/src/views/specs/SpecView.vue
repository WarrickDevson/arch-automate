<template>
  <div class="view-page h-[calc(100vh-100px)] flex flex-col print:h-auto print:block">

    <!-- No project: full-height picker card -->
    <div v-if="!resolvedProjectId" class="flex-1 flex flex-col gap-4 overflow-hidden">
      <div class="bg-white rounded-xl border border-slate-200 shadow-sm p-6 flex flex-col gap-4 h-full">
        <!-- Header -->
        <div class="flex items-center justify-between shrink-0">
          <div class="flex items-center gap-3">
            <FileText class="h-5 w-5 text-indigo-600" />
            <div>
              <h1 class="text-base font-extrabold tracking-tight text-slate-900 uppercase">Auto-Specification Compiler</h1>
              <p class="text-xs text-slate-500">Select a project to view or compile its SANS-referenced specification.</p>
            </div>
          </div>
          <Button
            class="bg-blue-600 hover:bg-blue-700 gap-2 uppercase text-xs font-bold"
            @click="uiStore.openCreateProjectSheet()"
          >
            <ArrowRight class="h-3.5 w-3.5" /> New Project
          </Button>
        </div>

        <!-- Search -->
        <div class="relative shrink-0">
          <Search class="absolute left-3 top-1/2 -translate-y-1/2 h-3.5 w-3.5 text-slate-400" />
          <Input
            v-model="pickerSearch"
            class="pl-9 h-9 text-sm"
            placeholder="Search by name or municipality…"
          />
        </div>

        <!-- Project list -->
        <div class="flex-1 overflow-y-auto">
          <div v-if="projectsStore.loading" class="space-y-2">
            <div v-for="n in 5" :key="n" class="h-14 bg-slate-100 rounded-lg animate-pulse" />
          </div>

          <div
            v-else-if="pickerProjects.length === 0"
            class="flex flex-col items-center justify-center h-full py-16 text-center"
          >
            <FolderOpen class="h-10 w-10 text-slate-200 mb-3" />
            <p class="text-sm font-medium text-slate-400">
              {{ pickerSearch ? 'No projects match your search.' : 'No projects yet — create your first one.' }}
            </p>
            <Button
              class="mt-4 bg-blue-600 hover:bg-blue-700 uppercase text-xs font-bold gap-2"
              @click="uiStore.openCreateProjectSheet()"
            >
              New Project
            </Button>
          </div>

          <ul v-else class="space-y-1.5">
            <li
              v-for="p in pickerProjects"
              :key="p.id"
              class="flex items-center gap-4 px-4 py-3 rounded-lg border border-slate-100 bg-slate-50/60 hover:bg-white hover:border-indigo-200 hover:shadow-sm cursor-pointer transition-all group"
              @click="router.push({ name: 'specs', params: { projectId: p.id } })"
            >
              <div class="flex-shrink-0 h-9 w-9 rounded-lg bg-indigo-50 flex items-center justify-center">
                <Building2 class="h-4 w-4 text-indigo-600" />
              </div>
              <div class="flex-1 min-w-0">
                <p class="text-sm font-bold text-slate-800 truncate">{{ p.name }}</p>
                <p class="text-[11px] text-slate-400">
                  {{ p.municipality }} &middot; {{ p.zoningScheme }}
                </p>
              </div>
              <ArrowRight class="h-4 w-4 text-slate-300 group-hover:text-indigo-400 transition-colors shrink-0" />
            </li>
          </ul>
        </div>
      </div>
    </div>

    <!-- Has project -->
    <main v-else class="relative flex-1 min-h-0 overflow-auto print:overflow-visible">

      <!-- Loading overlay (kept out of document flow to prevent layout shift) -->
      <div class="pointer-events-none absolute inset-x-0 top-0 z-10 flex justify-center print:hidden">
        <div
          class="mt-1 flex items-center gap-2 rounded-xl border border-slate-200 bg-white px-4 py-2.5 shadow-sm transition-all duration-200"
          :class="loading ? 'translate-y-0 opacity-100' : '-translate-y-1 opacity-0'"
          role="status"
          aria-live="polite"
        >
          <Loader2 class="h-3.5 w-3.5 animate-spin text-slate-400" />
          <span class="text-xs text-slate-500">Loading project...</span>
        </div>
      </div>

      <!-- Error -->
      <div v-if="loadError" class="bg-red-50 border border-red-200 rounded-xl p-6 text-center mt-4">
        <AlertTriangle class="w-8 h-8 text-red-400 mx-auto mb-3" />
        <p class="text-red-700 font-medium">{{ loadError }}</p>
        <Button class="mt-4" variant="outline" size="sm" @click="loadSpec">Retry</Button>
      </div>

      <div v-else class="max-w-5xl mx-auto px-4 py-6 pb-10">

        <!-- Empty state -->
        <div v-if="!compiledSpec" class="bg-white border border-slate-200 rounded-xl p-10 text-center">
          <FileText class="w-14 h-14 text-slate-300 mx-auto mb-4" />
          <h2 class="text-slate-700 font-semibold text-lg mb-2">No Specification Generated Yet</h2>
          <p class="text-slate-500 text-sm max-w-sm mx-auto mb-6">
            Load an IFC model in the Workbench to automatically extract materials and compile a
            SANS-referenced specification.
          </p>
          <Button
            size="sm"
            @click="router.push({ name: 'workbench', params: { projectId: resolvedProjectId } })"
          >
            <ArrowRight class="w-4 h-4 mr-2" />
            Go to Workbench
          </Button>
        </div>

        <!-- Spec document -->
        <template v-else>
          <!-- Print header -->
          <div class="hidden print:block mb-8 border-b pb-6">
            <h1 class="text-2xl font-bold text-slate-900">Construction Specification</h1>
            <p class="text-slate-600 mt-1">Project: {{ currentProjectName }}</p>
            <p class="text-slate-500 text-sm">
              Compiled: {{ formatDate(compiledSpec?.compiledAt) }} ·
              {{ compiledSpec?.clauseCount }} clause{{ compiledSpec?.clauseCount !== 1 ? 's' : '' }}
            </p>
          </div>

          <!-- Summary + action bar -->
          <div class="flex flex-wrap items-center gap-3 mb-6 print:hidden">
            <div class="bg-white border border-slate-200 rounded-xl px-4 py-2.5 flex items-center gap-1.5">
              <span class="text-lg font-bold text-indigo-600">{{ compiledSpec.clauseCount }}</span>
              <span class="text-xs text-slate-500">Clauses</span>
            </div>
            <div class="bg-white border border-slate-200 rounded-xl px-4 py-2.5 flex items-center gap-1.5">
              <span class="text-lg font-bold text-emerald-600">{{ sections.length }}</span>
              <span class="text-xs text-slate-500">Sections</span>
            </div>
            <div class="bg-white border border-slate-200 rounded-xl px-4 py-2.5 flex items-center gap-1.5">
              <span class="text-lg font-bold text-amber-600">{{ detectedCount }}</span>
              <span class="text-xs text-slate-500">Materials</span>
            </div>
            <span class="text-xs text-slate-400 italic hidden sm:inline">
              · Compiled {{ formatDate(compiledSpec?.compiledAt) }}
            </span>
            <div class="ml-auto flex items-center gap-2">
              <Button
                variant="outline"
                size="sm"
                class="h-8 gap-1.5 text-[10px] font-bold uppercase px-3"
                :disabled="isRecompiling"
                @click="recompile"
              >
                <RefreshCw class="w-3.5 h-3.5" :class="{ 'animate-spin': isRecompiling }" />
                Re-compile
              </Button>
              <Button
                variant="outline"
                size="sm"
                class="h-8 gap-1.5 text-[10px] font-bold uppercase px-3"
                @click="printSpec"
              >
                <Printer class="w-3.5 h-3.5" />
                Print / PDF
              </Button>
            </div>
          </div>

          <div class="space-y-8">
            <section
              v-for="sec in sections"
              :key="sec.name"
              class="rounded-xl border border-slate-200 bg-white shadow-sm overflow-hidden"
            >
              <div class="px-5 py-4 border-b border-slate-100 bg-slate-50/70 flex items-center justify-between gap-3">
                <div class="flex items-center gap-2 min-w-0">
                  <div class="w-1 h-6 rounded bg-indigo-500"></div>
                  <h2 class="text-sm font-bold text-slate-800 truncate">{{ sec.name }}</h2>
                </div>
                <span class="text-[11px] text-slate-400 shrink-0">{{ sec.clauses.length }} clause{{ sec.clauses.length !== 1 ? 's' : '' }}</span>
              </div>

              <div class="divide-y divide-slate-100">
                <div
                  v-for="(clause, idx) in sec.clauses"
                  :key="`${sec.name}-${idx}`"
                  class="px-5 py-4"
                >
                  <div class="flex items-center justify-between gap-3">
                    <div class="flex items-center gap-2 min-w-0">
                      <span class="text-xs font-mono text-slate-400 w-6 shrink-0">{{ idx + 1 }}</span>
                      <span class="text-sm font-semibold text-slate-800 truncate">{{ clause.heading }}</span>
                    </div>
                    <Badge variant="outline" class="text-[10px] px-2 py-0.5 font-mono text-indigo-700 border-indigo-200 bg-indigo-50 shrink-0">
                      {{ clause.reference }}
                    </Badge>
                  </div>
                  <p class="mt-2 text-sm leading-relaxed text-slate-700">{{ clause.text }}</p>
                </div>
              </div>
            </section>

            <section class="rounded-xl border border-slate-200 bg-white shadow-sm overflow-hidden print:hidden">
              <div class="px-5 py-4 border-b border-slate-100 bg-slate-50/70 flex items-center justify-between gap-3">
                <div class="flex items-center gap-2 min-w-0">
                  <div class="w-1 h-6 rounded bg-emerald-500"></div>
                  <h2 class="text-sm font-bold text-slate-800 truncate">Detected Materials</h2>
                </div>
                <span class="text-[11px] text-slate-400 shrink-0">{{ detectedCount }} matched</span>
              </div>

              <div v-if="detectedMaterials.length" class="overflow-x-auto">
                <table class="w-full text-sm">
                  <thead>
                    <tr class="bg-slate-50 border-b border-slate-200">
                      <th class="text-left px-4 py-3 font-medium text-slate-600">Raw IFC String</th>
                      <th class="text-left px-4 py-3 font-medium text-slate-600">Matched Keyword</th>
                      <th class="text-left px-4 py-3 font-medium text-slate-600">Element Type</th>
                    </tr>
                  </thead>
                  <tbody>
                    <tr
                      v-for="(mat, i) in detectedMaterials"
                      :key="i"
                      class="border-b border-slate-100 hover:bg-slate-50"
                    >
                      <td class="px-4 py-2.5 text-slate-700 font-mono text-xs">{{ mat.rawValue }}</td>
                      <td class="px-4 py-2.5">
                        <Badge variant="secondary" class="text-xs">{{ mat.keyword }}</Badge>
                      </td>
                      <td class="px-4 py-2.5 text-slate-500 text-xs">{{ mat.elementCategory }}</td>
                    </tr>
                  </tbody>
                </table>
              </div>

              <div v-else class="px-5 py-8 text-center text-sm text-slate-400">
                No detected materials were found.
              </div>

              <div v-if="unmatchedKeywords.length" class="px-5 py-4 border-t border-slate-100">
                <h3 class="text-sm font-medium text-slate-600 mb-2">Unmatched Keywords</h3>
                <div class="flex flex-wrap gap-2">
                  <Badge
                    v-for="kw in unmatchedKeywords"
                    :key="kw"
                    variant="outline"
                    class="text-xs text-slate-500"
                  >
                    {{ kw }}
                  </Badge>
                </div>
              </div>
            </section>
          </div>
        </template>
      </div>
    </main>
  </div>
</template>

<script setup>
import { ref, computed, watch, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import {
  FileText, RefreshCw, Printer, ArrowRight,
  Loader2, AlertTriangle,
  FolderOpen, Search, Building2,
} from 'lucide-vue-next'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { Input } from '@/components/ui/input'
import { useSpecStore } from '@/stores/spec.store'
import { useProjectsStore } from '@/stores/projects.store'
import { useUiStore } from '@/stores/ui.store'

const props = defineProps({
  projectId: { type: String, default: null },
})

const router = useRouter()
const specStore = useSpecStore()
const projectsStore = useProjectsStore()
const uiStore = useUiStore()

// ── Project picker ────────────────────────────────────────────────────────────

const pickerSearch = ref('')
const pickerProjects = computed(() => {
  const q = pickerSearch.value.trim().toLowerCase()
  if (!q) return projectsStore.projects
  return projectsStore.projects.filter(
    (p) =>
      p.name.toLowerCase().includes(q) ||
      p.municipality.toLowerCase().includes(q),
  )
})

// ── State ─────────────────────────────────────────────────────────────────────

const loading = ref(false)
const loadError = ref(null)
const isRecompiling = ref(false)

// ── Computed ──────────────────────────────────────────────────────────────────

const resolvedProjectId = computed(() => props.projectId || null)

const currentProjectName = computed(() => {
  if (!resolvedProjectId.value) return ''
  return projectsStore.projects.find(p => p.id === resolvedProjectId.value)?.name ?? 'Project'
})

const storeData = computed(() =>
  resolvedProjectId.value ? specStore.getSpec(resolvedProjectId.value) : null
)

const compiledSpec = computed(() => storeData.value ?? null)

const sections = computed(() => compiledSpec.value?.spec?.sections ?? [])

const detectedMaterials = computed(() => compiledSpec.value?.spec?.detectedMaterials ?? [])

const unmatchedKeywords = computed(() => compiledSpec.value?.spec?.unmatchedKeywords ?? [])

const detectedCount = computed(() => detectedMaterials.value.length)

// ── Lifecycle ─────────────────────────────────────────────────────────────────

watch(() => props.projectId, async (id) => {
  if (!projectsStore.projects.length) await projectsStore.fetchProjects()
  if (id) await loadSpec()
}, { immediate: true })

// ── Methods ───────────────────────────────────────────────────────────────────

async function loadSpec() {
  loading.value = true
  loadError.value = null
  try {
    await specStore.fetchSpec(resolvedProjectId.value, true)
  } catch (e) {
    loadError.value = e.message ?? 'Failed to load specification'
  } finally {
    loading.value = false
  }
}

async function recompile() {
  isRecompiling.value = true
  try {
    await specStore.fetchSpec(resolvedProjectId.value, true)
  } finally {
    isRecompiling.value = false
  }
}

function printSpec() {
  window.print()
}

function formatDate(iso) {
  if (!iso) return '—'
  return new Date(iso).toLocaleDateString('en-ZA', {
    day: '2-digit', month: 'short', year: 'numeric',
  })
}
</script>
