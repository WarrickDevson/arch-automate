<script setup>
import { Library, Search, Filter, BookOpen } from 'lucide-vue-next'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
</script>

<template>
  <div class="space-y-6">
    <!-- Page Header -->
    <header class="flex flex-col md:flex-row md:items-center justify-between gap-4">
      <div>
        <h1 class="text-2xl font-bold tracking-tight text-slate-900 uppercase">Knowledge Base</h1>
        <p class="text-sm text-slate-500">SANS 10400 standards and architectural guidelines.</p>
      </div>
      <div class="flex flex-wrap items-center gap-2 text-slate-400">
        <div class="relative">
          <Search class="absolute left-3 top-1/2 -translate-y-1/2 h-3.5 w-3.5 text-slate-400" />
          <Input
            placeholder="Search SANS codes..."
            class="pl-9 h-9 w-64 text-xs border-slate-200"
          />
        </div>
      </div>
    </header>

    <!-- Main Grid -->
    <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
      <div
        v-for="i in 3"
        :key="i"
        class="bg-white rounded-xl border border-slate-200 shadow-sm p-6 hover:border-blue-200 transition-colors group cursor-pointer"
      >
        <div
          class="h-10 w-10 bg-blue-50 text-blue-600 rounded-lg flex items-center justify-center mb-4 group-hover:bg-blue-600 group-hover:text-white transition-colors"
        >
          <BookOpen class="h-5 w-5" />
        </div>
        <h3 class="text-sm font-bold text-slate-900 uppercase tracking-widest mb-2">
          Part {{ i }}: General Requirements
        </h3>
        <p class="text-[12px] text-slate-500 leading-relaxed mb-4">
          Documentation and administrative requirements for all building applications submitted to
          local authorities.
        </p>
        <div class="flex items-center justify-between pt-4 border-t border-slate-50">
          <span class="text-[10px] font-bold text-slate-400 uppercase tracking-tighter"
            >SANS 10400-A</span
          >
          <Button
            variant="ghost"
            class="h-7 text-[10px] font-bold uppercase text-blue-600 hover:text-blue-700 p-0"
            >View Details</Button
          >
        </div>
      </div>
    </div>
  </div>
</template>
