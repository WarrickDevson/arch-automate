<script setup>
import { Mail, Phone, Briefcase, ExternalLink, FileText } from 'lucide-vue-next'
import { Badge } from '@/components/ui/badge'

const props = defineProps(['stakeholder'])
</script>

<template>
  <div class="bg-white rounded-xl border border-slate-200 shadow-md overflow-hidden animate-in fade-in slide-in-from-right-4 duration-300">
    <div class="p-6 bg-slate-900 text-white">
      <div class="flex justify-between items-start mb-4">
        <div class="h-16 w-16 rounded-xl bg-white/10 flex items-center justify-center text-xl font-bold uppercase border border-white/20">
          {{ stakeholder.name[0] }}
        </div>
        <Badge class="bg-blue-600 text-[10px] border-none uppercase font-bold tracking-wider">Profile Verified</Badge>
      </div>
      <h2 class="text-lg font-bold">{{ stakeholder.name }}</h2>
      <p class="text-xs text-slate-400 font-mono tracking-tight">{{ stakeholder.company }}</p>
    </div>

    <div class="p-6 space-y-6">
      <!-- Contact Info -->
      <div class="space-y-3">
        <div class="flex items-center gap-3 text-[13px] text-slate-600">
          <Mail class="h-4 w-4 text-slate-400" /> {{ stakeholder.email }}
        </div>
        <div class="flex items-center gap-3 text-[13px] text-slate-600">
          <Phone class="h-4 w-4 text-slate-400" /> {{ stakeholder.phone }}
        </div>
      </div>

      <!-- Linked Projects -->
      <div>
        <label class="text-[10px] font-bold uppercase tracking-widest text-slate-400 block mb-3">Linked Projects</label>
        <div class="space-y-2">
          <div v-for="proj in stakeholder.projects" :key="proj" 
            class="flex items-center justify-between p-3 rounded-lg border border-slate-100 bg-slate-50 hover:border-blue-200 hover:bg-white transition-all group cursor-pointer">
            <div class="flex items-center gap-2">
              <Briefcase class="h-3.5 w-3.5 text-slate-400" />
              <span class="text-[13px] font-bold text-slate-700">{{ proj }}</span>
            </div>
            <ExternalLink class="h-3 w-3 text-slate-300 group-hover:text-blue-500" />
          </div>
        </div>
      </div>

      <!-- Documents Section -->
      <div class="pt-4 border-t border-slate-100">
        <button class="w-full flex items-center justify-center gap-2 py-3 rounded-lg bg-slate-50 text-slate-500 text-[11px] font-bold uppercase hover:bg-slate-100 transition-colors">
          <FileText class="h-3.5 w-3.5" /> View Identity Documents
        </button>
      </div>
    </div>
  </div>
</template>