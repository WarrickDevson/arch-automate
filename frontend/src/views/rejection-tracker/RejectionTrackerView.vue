<script setup>
import { History, Search, Filter, AlertOctagon, Clock, Download } from 'lucide-vue-next'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Badge } from '@/components/ui/badge'
</script>

<template>
  <div class="space-y-6">
    <!-- Page Header -->
    <header class="flex flex-col md:flex-row md:items-center justify-between gap-4">
      <div>
        <h1 class="text-2xl font-bold tracking-tight text-slate-900 uppercase">
          Rejection Tracker
        </h1>
        <p class="text-sm text-slate-500">
          Track council building plan rejections and resolutions.
        </p>
      </div>
      <div class="flex flex-wrap items-center gap-2">
        <Button variant="outline" class="gap-2 uppercase text-xs font-bold h-9 border-slate-200">
          <Download class="h-3.5 w-3.5 text-blue-600" /> Export History
        </Button>
      </div>
    </header>

    <!-- Main List View -->
    <div
      class="bg-white rounded-xl border border-slate-200 shadow-sm overflow-hidden min-h-[500px]"
    >
      <div class="p-4 border-b border-slate-100 flex items-center gap-4 bg-slate-50/50">
        <div class="relative flex-1">
          <Search class="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-slate-400" />
          <Input
            placeholder="Search by project, municipality, or ERF number..."
            class="pl-10 h-10 text-sm border-slate-200"
          />
        </div>
        <Button variant="outline" size="icon" class="border-slate-200 bg-white">
          <Filter class="h-4 w-4" />
        </Button>
      </div>

      <!-- Empty Tracker State -->
      <div class="flex flex-col items-center justify-center p-12 text-center opacity-70">
        <div class="h-16 w-16 bg-blue-50/50 rounded-full flex items-center justify-center mb-6">
          <History class="h-8 w-8 text-blue-400" />
        </div>
        <h3 class="text-sm font-bold text-slate-900 uppercase tracking-widest">
          No Rejection History Found
        </h3>
        <p class="text-[13px] text-slate-500 max-w-sm mt-2 leading-relaxed">
          Projects submitted through ArchAutomate that have received council feedback or rejections
          will automatically be tracked here with detailed resolution steps.
        </p>
      </div>
    </div>
  </div>
</template>
