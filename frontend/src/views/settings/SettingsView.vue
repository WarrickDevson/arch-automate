<script setup>
import { Settings, User, Building, Shield, Bell, CreditCard, ChevronRight } from 'lucide-vue-next'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
</script>

<template>
  <div class="space-y-6">
    <!-- Page Header -->
    <header class="flex flex-col md:flex-row md:items-center justify-between gap-4">
      <div>
        <h1 class="text-2xl font-bold tracking-tight text-slate-900 uppercase">Studio Settings</h1>
        <p class="text-sm text-slate-500">Manage your practice preferences and account.</p>
      </div>
      <div class="flex flex-wrap items-center gap-2">
        <Button class="bg-blue-600 hover:bg-blue-700 gap-2 uppercase text-xs font-bold h-9">
          Save Changes
        </Button>
      </div>
    </header>

    <!-- Settings Body -->
    <div class="grid grid-cols-1 lg:grid-cols-[240px_1fr] gap-8">
      <!-- Navigation Sidebar -->
      <aside class="space-y-1">
        <button
          v-for="item in [
            { label: 'Profile Settings', icon: User, active: true },
            { label: 'Studio Identity', icon: Building, active: false },
            { label: 'Subscription', icon: CreditCard, active: false },
            { label: 'Security', icon: Shield, active: false },
            { label: 'Notifications', icon: Bell, active: false },
          ]"
          :key="item.label"
          class="w-full flex items-center gap-3 px-3 py-2.5 rounded-lg text-xs font-bold uppercase tracking-widest transition-all"
          :class="
            item.active ? 'bg-slate-900 text-white shadow-lg' : 'text-slate-500 hover:bg-slate-100'
          "
        >
          <component :is="item.icon" class="h-4 w-4 shrink-0" />
          {{ item.label }}
        </button>
      </aside>

      <!-- Main Settings Area -->
      <main class="bg-white rounded-xl border border-slate-200 shadow-sm p-8 min-h-[500px]">
        <h3
          class="text-sm font-bold text-slate-900 uppercase tracking-widest border-b border-slate-100 pb-4 mb-6"
        >
          Profile Settings
        </h3>
        <div class="space-y-8 max-w-2xl">
          <div class="flex items-center gap-6">
            <div
              class="h-20 w-20 rounded-2xl bg-slate-100 flex items-center justify-center border-2 border-dashed border-slate-200 text-slate-400"
            >
              <User class="h-8 w-8" />
            </div>
            <div>
              <Button
                variant="outline"
                class="h-8 text-[11px] font-bold uppercase tracking-widest p-4 border-slate-200"
                >Change Photo</Button
              >
              <p class="text-[11px] text-slate-400 mt-2">Recommended: 400x400px JPG or PNG</p>
            </div>
          </div>

          <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div class="space-y-2">
              <label class="text-[10px] font-bold text-slate-400 uppercase tracking-widest"
                >Full Name</label
              >
              <input
                type="text"
                value="Warrick"
                class="w-full h-10 px-4 rounded-lg border border-slate-200 text-sm focus:border-blue-600 outline-none transition"
              />
            </div>
            <div class="space-y-2">
              <label class="text-[10px] font-bold text-slate-400 uppercase tracking-widest"
                >Email Address</label
              >
              <input
                type="email"
                value="user@example.com"
                class="w-full h-10 px-4 rounded-lg border border-slate-200 text-sm focus:border-blue-600 outline-none transition"
              />
            </div>
          </div>
        </div>
      </main>
    </div>
  </div>
</template>
