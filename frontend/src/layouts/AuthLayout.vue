<script setup>
import { onBeforeUnmount, onMounted, ref, watch } from 'vue'
import { useRoute } from 'vue-router'
import { useAuthStore } from '@/stores/auth.store'

const route = useRoute()
const authStore = useAuthStore()

watch(
  () => route.path,
  () => authStore.clearAuthWorkflow(),
)

const highlights = [
  {
    title: 'SANS 10400 Compliant',
    description:
      'Automated verification of fenestration, energy, and plumbing rules in seconds, not hours.',
  },
  {
    title: 'One-Click Council Packs',
    description:
      'Generate submission-ready DXF tables and pre-filled PDF forms without manual drafting.',
  },
  {
    title: 'SACAP-Ready Profiles',
    description:
      'Store professional and client credentials for instant application auto-filling and digital signing.',
  },
  {
    title: 'Revit LT Bridge',
    description:
      'Unlock high-end BIM automation and data extraction for LT users via open-source IFC standards.',
  },
  {
    title: 'Municipal Intelligence',
    description:
      'Built-in library of local council by-laws, submission fees, and specific regional requirements.',
  },
]

const activeSlide = ref(0)
let carouselTimer = null

const nextSlide = () => {
  activeSlide.value = (activeSlide.value + 1) % highlights.length
}

const startCarousel = () => {
  stopCarousel()
  carouselTimer = window.setInterval(() => {
    nextSlide()
  }, 4500)
}

const stopCarousel = () => {
  if (carouselTimer) {
    window.clearInterval(carouselTimer)
    carouselTimer = null
  }
}

onMounted(() => {
  startCarousel()
})

onBeforeUnmount(() => {
  stopCarousel()
})
</script>

<template>
  <div class="relative min-h-screen overflow-hidden bg-slate-950">
    <!-- Architectural Background Elements -->
    <div class="pointer-events-none absolute inset-0 opacity-40">
      <!-- "Blueprint" Gradients -->
      <div class="absolute -left-20 top-10 h-72 w-72 rounded-full bg-blue-600/20 blur-3xl" />
      <div class="absolute right-0 top-1/3 h-80 w-80 rounded-full bg-cyan-500/10 blur-3xl" />
      <div class="absolute -bottom-20 left-1/3 h-96 w-96 rounded-full bg-blue-800/20 blur-3xl" />

      <!-- Technical Grid Overlay (Optional CSS Pattern) -->
      <div
        class="absolute inset-0 bg-[linear-gradient(rgba(255,255,255,0.02)_1px,transparent_1px),linear-gradient(90deg,rgba(255,255,255,0.02)_1px,transparent_1px)] bg-[size:40px_40px]"
      />

      <div
        class="absolute inset-0 bg-[radial-gradient(circle_at_10%_20%,rgba(30,58,138,0.2),transparent_40%),radial-gradient(circle_at_85%_80%,rgba(14,165,233,0.1),transparent_40%)]"
      />
    </div>

    <div class="relative flex min-h-screen w-full items-stretch overflow-hidden bg-transparent">
      <!-- Left Sidebar: The "Studio" Side -->
      <aside
        class="relative hidden w-[44%] overflow-hidden border-r border-white/5 bg-slate-900 px-10 py-12 text-white md:flex md:flex-col md:items-center md:justify-center lg:px-12"
      >
        <!-- Subtle internal depth gradient -->
        <div
          class="absolute inset-0 bg-[radial-gradient(circle_at_25%_20%,rgba(30,58,138,0.3),transparent_40%),radial-gradient(circle_at_85%_80%,rgba(0,0,0,0.4),transparent_50%)]"
        />

        <div class="relative my-auto w-full max-w-md">
          <div class="border-l-2 border-highlight pl-6">
            <p class="text-[10px] font-bold uppercase tracking-[0.3em] text-highlight">
              Arch Automate SA
            </p>
            <h2
              class="mt-4 font-display text-4xl font-extrabold uppercase leading-tight tracking-tighter"
            >
              Engineering <br />
              Success.
            </h2>
            <p class="mt-4 max-w-sm text-sm font-medium text-slate-400">
              Automating municipal compliance and production workflows for South African
              architectural professionals.
            </p>
          </div>

          <!-- Technical Carousel Callout -->
          <div class="mt-12" @mouseenter="stopCarousel" @mouseleave="startCarousel">
            <Transition
              mode="out-in"
              enter-active-class="transition duration-400 ease-out"
              enter-from-class="translate-x-4 opacity-0"
              enter-to-class="translate-x-0 opacity-100"
              leave-active-class="transition duration-300 ease-in"
              leave-from-class="translate-x-0 opacity-100"
              leave-to-class="-translate-x-4 opacity-0"
            >
              <div
                :key="activeSlide"
                class="group relative rounded-xl border border-white/10 bg-white/5 p-6 backdrop-blur-md transition-colors hover:border-highlight/30"
              >
                <div
                  class="absolute -top-3 left-6 bg-highlight px-2 py-0.5 text-[8px] font-bold uppercase tracking-widest text-slate-950"
                >
                  Core Module 0{{ activeSlide + 1 }}
                </div>
                <p class="text-xs font-bold uppercase tracking-widest text-highlight">
                  {{ highlights[activeSlide].title }}
                </p>
                <p class="mt-2 text-sm leading-relaxed text-slate-300">
                  {{ highlights[activeSlide].description }}
                </p>
              </div>
            </Transition>

            <!-- Slide Indicators -->
            <div class="mt-6 flex gap-2">
              <div
                v-for="(_, i) in highlights"
                :key="i"
                class="h-1 rounded-full transition-all duration-300"
                :class="activeSlide === i ? 'w-8 bg-highlight' : 'w-2 bg-white/10'"
              />
            </div>
          </div>
        </div>
      </aside>

      <!-- Right Side: The Login Form -->
      <main
        class="flex w-full items-center bg-slate-50 px-5 py-7 sm:px-8 sm:py-9 md:w-[56%] lg:px-12 lg:py-12"
      >
        <div class="mx-auto w-full max-w-[400px]">
          <RouterView />
        </div>
      </main>
    </div>
  </div>
</template>

<style scoped>
/* Optional technical grid animation */
@keyframes grid-fade {
  from {
    opacity: 0;
  }
  to {
    opacity: 0.4;
  }
}

.bg-surface\/90 {
  background-color: rgb(var(--color-bg));
}
</style>
