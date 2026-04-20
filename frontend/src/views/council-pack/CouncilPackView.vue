<script setup>
import { FileDown, Download, Plus, Search, Filter } from 'lucide-vue-next'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { useUiStore } from '@/stores/ui.store'

const uiStore = useUiStore()

// Called when a council pack is successfully generated.
// TODO: move this call into the real generation action when the feature is built.
function handleGeneratePack() {
  uiStore.markCouncilPackGenerated()
}
</script>

<template>
  <div class="space-y-6">
    <!-- Page Header -->
    <header class="flex flex-col md:flex-row md:items-center justify-between gap-4">
      <div>
        <h1 class="text-2xl font-bold tracking-tight text-slate-900 uppercase">Council Pack</h1>
        <p class="text-sm text-slate-500">
          Generate and manage submission sets for local authorities.
        </p>
      </div>
      <div class="flex flex-wrap items-center gap-2">
        <Button variant="outline" class="gap-2 uppercase text-xs font-bold h-9 border-slate-200">
          <Download class="h-3.5 w-3.5" /> Bulk Export
        </Button>
        <Button class="bg-blue-600 hover:bg-blue-700 gap-2 uppercase text-xs font-bold h-9" @click="handleGeneratePack">
          <Plus class="h-3.5 w-3.5" /> Create New Pack
        </Button>
      </div>
    </header>

    <!-- Empty State / Placeholder Content -->
    <div
      class="bg-white rounded-xl border border-slate-200 shadow-sm min-h-[400px] flex flex-col items-center justify-center p-8 text-center"
    >
      <div class="h-16 w-16 bg-slate-50 rounded-full flex items-center justify-center mb-4">
        <FileDown class="h-8 w-8 text-slate-300" />
      </div>
      <h3 class="text-sm font-bold text-slate-900 uppercase tracking-widest">
        No Submission Packs
      </h3>
      <p class="text-[13px] text-slate-500 max-w-xs mt-2">
        Generate your first municipal submission pack by selecting a project from the Command
        Center.
      </p>
      <Button
        variant="outline"
        class="mt-6 uppercase text-[10px] font-bold tracking-widest border-slate-200 h-8"
      >
        Learn More about SANS Compliance
      </Button>
    </div>
  </div>
</template>
