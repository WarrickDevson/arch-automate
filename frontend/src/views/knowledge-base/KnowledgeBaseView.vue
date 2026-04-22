<script setup>
import { Search, BookOpen } from 'lucide-vue-next'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
</script>

<template>
  <div class="view-page">
    <div class="mb-6 flex justify-end">
      <div class="relative">
        <Search class="absolute left-3 top-1/2 -translate-y-1/2 h-3.5 w-3.5 text-slate-400" />
        <Input
          placeholder="Search SANS codes..."
          class="h-9 w-64 border-slate-200 pl-9 text-xs dark:bg-slate-900 dark:border-slate-800"
        />
      </div>
    </div>

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
