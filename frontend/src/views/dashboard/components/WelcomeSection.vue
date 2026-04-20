<script setup>
import { computed, ref, watch } from 'vue'
import { useRouter } from 'vue-router'
import {
  CheckCircle2,
  Circle,
  Building2,
  FolderPlus,
  ScanLine,
  FileStack,
  X,
  Sparkles,
  ArrowRight,
} from 'lucide-vue-next'
import { Button } from '@/components/ui/button'
import { Progress } from '@/components/ui/progress'
import { useAuthStore } from '@/stores/auth.store'
import { useUiStore } from '@/stores/ui.store'
import { useProjectsStore } from '@/stores/projects.store'

const emit = defineEmits(['create-project'])

const router = useRouter()
const authStore = useAuthStore()
const uiStore = useUiStore()
const projectsStore = useProjectsStore()

// ── Greeting ──────────────────────────────────────────────────────────────
const firstName = computed(() => {
  const name = authStore.displayName || authStore.user?.email || ''
  // e.g. "John Smith" → "John", "john@email.com" → "john"
  return name.split(/[\s@]/)[0]
})

// ── Steps ─────────────────────────────────────────────────────────────────
const steps = computed(() => [
  {
    key: 'workspace',
    icon: Building2,
    iconColor: 'text-primary',
    iconBg: 'bg-primary/10',
    title: 'Set up your workspace',
    description: 'Your practice is registered and your workspace is ready to use.',
    done: true,
    cta: null,
  },
  {
    key: 'project',
    icon: FolderPlus,
    iconColor: 'text-blue-600',
    iconBg: 'bg-blue-50',
    title: 'Create your first project',
    description: 'Add a development site to begin tracking compliance and submissions.',
    done: projectsStore.projects.length > 0,
    cta: {
      label: 'New Project',
      action: () => emit('create-project'),
    },
  },
  {
    key: 'workbench',
    icon: ScanLine,
    iconColor: 'text-violet-600',
    iconBg: 'bg-violet-50',
    title: 'Analyse in the Workbench',
    description: 'Upload an IFC model and run automated compliance checks on your site.',
    done: uiStore.hasOpenedWorkbench,
    cta: {
      label: 'Open Workbench',
      action: () => router.push({ name: 'workbench' }),
    },
  },
  {
    key: 'council',
    icon: FileStack,
    iconColor: 'text-amber-600',
    iconBg: 'bg-amber-50',
    title: 'Generate a Council Pack',
    description: 'Export a compliant submission set for your local authority.',
    done: uiStore.hasGeneratedCouncilPack,
    cta: {
      label: 'Council Pack',
      action: () => router.push({ name: 'council-pack' }),
    },
  },
])

const completedCount = computed(() => steps.value.filter((s) => s.done).length)
const progress = computed(() => Math.round((completedCount.value / steps.value.length) * 100))
const allDone = computed(() => completedCount.value === steps.value.length)

// ── Celebration state ──────────────────────────────────────────────────────
// Becomes true the moment all steps flip to done (watch); reverts on dismiss.
const celebrating = ref(false)
watch(allDone, (val) => {
  if (val) celebrating.value = true
})
</script>

<template>
  <section
    v-if="!uiStore.gettingStartedDismissed"
    class="bg-white rounded-xl border border-slate-200 shadow-sm overflow-hidden animate-in fade-in slide-in-from-top-2 duration-500"
  >
    <!-- Accent line -->
    <div class="h-1 w-full bg-gradient-to-r from-primary via-primary/70 to-primary/30" />

    <!-- Celebration state -->
    <div v-if="celebrating" class="px-6 py-8 flex flex-col sm:flex-row items-center gap-6">
      <div class="flex-shrink-0 bg-emerald-50 rounded-full p-4">
        <Sparkles class="h-8 w-8 text-emerald-500" />
      </div>
      <div class="flex-1 text-center sm:text-left">
        <p class="text-[10px] font-bold uppercase tracking-[0.25em] text-emerald-600">All done</p>
        <h2 class="mt-1 text-xl font-extrabold tracking-tight text-slate-900 uppercase">
          You're all set, {{ firstName }}!
        </h2>
        <p class="mt-1 text-sm text-slate-500">
          Your workspace is fully configured. Use the sidebar to navigate between modules.
        </p>
      </div>
      <Button
        variant="outline"
        size="sm"
        class="flex-shrink-0 text-xs uppercase font-bold border-slate-200 gap-1.5"
        @click="uiStore.dismissGettingStarted()"
      >
        <X class="h-3.5 w-3.5" /> Dismiss
      </Button>
    </div>

    <!-- Normal checklist state -->
    <template v-else>
      <!-- Header -->
      <div class="px-6 pt-5 pb-4 flex items-start justify-between gap-4">
        <div>
          <p class="text-[10px] font-bold uppercase tracking-[0.25em] text-primary/70">
            Getting Started
          </p>
          <h2 class="mt-0.5 text-lg font-extrabold tracking-tight text-slate-900 uppercase">
            Welcome, {{ firstName }}!
          </h2>
          <p class="mt-0.5 text-xs text-slate-500">
            Complete these steps to get the most out of the platform.
          </p>
        </div>

        <div class="flex items-center gap-3 flex-shrink-0 pt-0.5">
          <span class="text-[11px] font-bold text-slate-400 whitespace-nowrap">
            {{ completedCount }}&nbsp;of&nbsp;{{ steps.length }}&nbsp;complete
          </span>
          <button
            class="text-[11px] font-bold uppercase tracking-wide text-slate-400 hover:text-slate-600 transition-colors flex items-center gap-1"
            @click="uiStore.dismissGettingStarted()"
          >
            <X class="h-3 w-3" /> Dismiss
          </button>
        </div>
      </div>

      <!-- Progress bar -->
      <div class="px-6 pb-4">
        <Progress :model-value="progress" class="h-1.5" />
      </div>

      <!-- Steps list -->
      <ul class="divide-y divide-slate-100 px-2 pb-2">
        <li
          v-for="step in steps"
          :key="step.key"
          class="flex items-center gap-4 px-4 py-4 rounded-lg transition-colors"
          :class="step.done ? 'opacity-60' : 'hover:bg-slate-50/60'"
        >
          <!-- Icon badge -->
          <div
            class="flex-shrink-0 h-9 w-9 rounded-lg flex items-center justify-center transition-all"
            :class="step.done ? 'bg-emerald-50' : step.iconBg"
          >
            <CheckCircle2 v-if="step.done" class="h-5 w-5 text-emerald-500" />
            <component :is="step.icon" v-else class="h-5 w-5" :class="step.iconColor" />
          </div>

          <!-- Text -->
          <div class="flex-1 min-w-0">
            <p
              class="text-[13px] font-bold leading-snug"
              :class="step.done ? 'text-slate-400 line-through decoration-slate-300' : 'text-slate-800'"
            >
              {{ step.title }}
            </p>
            <p class="mt-0.5 text-[11px] leading-relaxed text-slate-400">
              {{ step.description }}
            </p>
          </div>

          <!-- Right-side CTA or Complete badge -->
          <div class="flex-shrink-0">
            <span
              v-if="step.done"
              class="inline-flex items-center gap-1 rounded-md bg-emerald-50 px-2.5 py-1 text-[10px] font-bold uppercase tracking-wider text-emerald-600"
            >
              <CheckCircle2 class="h-3 w-3" /> Complete
            </span>
            <Button
              v-else-if="step.cta"
              size="sm"
              variant="outline"
              class="text-[11px] font-bold uppercase tracking-wide border-slate-200 gap-1.5 h-8 px-3 whitespace-nowrap"
              @click="step.cta.action()"
            >
              {{ step.cta.label }} <ArrowRight class="h-3 w-3" />
            </Button>
          </div>
        </li>
      </ul>
    </template>
  </section>
</template>
