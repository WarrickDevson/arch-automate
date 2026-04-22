<script setup>
import { ref, computed } from 'vue'
import {
  Users,
  UserPlus,
  Search,
  Filter,
  MoreHorizontal,
  Download,
  FileBadge,
} from 'lucide-vue-next'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Badge } from '@/components/ui/badge'
import QuickAddStakeholder from './components/QuickAddStakeholder.vue'
import StakeholderDetailView from './components/StakeholderDetailView.vue'

const searchQuery = ref('')
const isAddModalOpen = ref(false)
const selectedStakeholder = ref(null)

const stakeholders = ref([])

const filteredStakeholders = computed(() => {
  return stakeholders.value.filter(
    (s) =>
      s.name.toLowerCase().includes(searchQuery.value.toLowerCase()) ||
      s.company.toLowerCase().includes(searchQuery.value.toLowerCase())
  )
})

const getRoleBadgeColor = (role) => {
  switch (role) {
    case 'Property Owner':
      return 'bg-emerald-50 text-emerald-700 border-emerald-100'
    case 'Structural Engineer':
      return 'bg-blue-50 text-blue-700 border-blue-100'
    case 'Land Surveyor':
      return 'bg-purple-50 text-purple-700 border-purple-100'
    default:
      return 'bg-slate-50 text-slate-700 border-slate-100'
  }
}
</script>

<template>
  <div class="view-page">
    <div class="grid grid-cols-1 xl:grid-cols-[1fr_400px] gap-6 items-start">
      <!-- Main List -->
      <div class="bg-white rounded-xl border border-slate-200 shadow-sm overflow-hidden">
        <div class="p-4 border-b border-slate-100 flex items-center gap-4 bg-slate-50/50">
          <div class="relative flex-1">
            <Search class="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-slate-400" />
            <Input
              v-model="searchQuery"
              placeholder="Search by name, company or SACAP..."
              class="pl-10 h-10 text-sm border-slate-200 focus:ring-blue-600/10"
            />
          </div>
          <Button variant="outline" size="icon" class="border-slate-200">
            <Filter class="h-4 w-4" />
          </Button>
        </div>

        <div class="overflow-x-auto">
          <table class="w-full text-left">
            <thead>
              <tr
                class="text-[10px] font-bold uppercase tracking-widest text-slate-400 border-b border-slate-100 bg-slate-50/30"
              >
                <th class="px-6 py-4">Stakeholder</th>
                <th class="px-6 py-4">Role</th>
                <th class="px-6 py-4">Legal / Registration</th>
                <th class="px-6 py-4">Linked Projects</th>
                <th class="px-6 py-4"></th>
              </tr>
            </thead>
            <tbody class="divide-y divide-slate-50">
              <tr
                v-for="s in filteredStakeholders"
                :key="s.id"
                class="hover:bg-slate-50/80 cursor-pointer transition-colors group"
                @click="selectedStakeholder = s"
              >
                <td class="px-6 py-4">
                  <div class="flex items-center gap-3">
                    <div
                      class="h-9 w-9 rounded-full bg-slate-100 flex items-center justify-center text-slate-600 font-bold text-xs uppercase"
                    >
                      {{
                        s.name
                          .split(' ')
                          .map((n) => n[0])
                          .join('')
                      }}
                    </div>
                    <div>
                      <p class="text-sm font-bold text-slate-800">{{ s.name }}</p>
                      <p class="text-xs text-slate-500">{{ s.company }}</p>
                    </div>
                  </div>
                </td>
                <td class="px-6 py-4">
                  <Badge
                    variant="outline"
                    :class="getRoleBadgeColor(s.role)"
                    class="rounded-full text-[10px] font-bold uppercase px-2"
                  >
                    {{ s.role }}
                  </Badge>
                </td>
                <td class="px-6 py-4">
                  <div class="flex flex-col gap-1">
                    <span
                      v-if="s.sacap"
                      class="text-[11px] font-mono font-bold text-blue-600 flex items-center gap-1"
                    >
                      <FileBadge class="h-3 w-3" /> {{ s.sacap }}
                    </span>
                    <span class="text-[11px] font-mono text-slate-400"
                      >ID: {{ s.idNumber.slice(0, 6) }}...</span
                    >
                  </div>
                </td>
                <td class="px-6 py-4 text-xs font-bold text-slate-700">
                  {{ s.projects.length }} Projects
                </td>
                <td class="px-6 py-4 text-right">
                  <MoreHorizontal class="h-4 w-4 text-slate-400 group-hover:text-slate-600" />
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>

      <!-- Detail View (Contact Card) -->
      <div class="flex flex-col gap-6 sticky top-6">
        <!-- Actions above details -->
        <div class="flex justify-end gap-3">
          <Button
            variant="outline"
            class="hidden h-9 gap-2 rounded-lg border-slate-200 bg-white px-3 text-[11px] font-bold uppercase tracking-wider dark:bg-slate-900 dark:border-slate-800 dark:text-slate-300 md:flex"
            disabled
          >
            <Download class="h-4 w-4 text-slate-500" />
            <span>Export</span>
          </Button>
          <Button
            class="h-9 gap-2 rounded-lg bg-blue-600 px-3 text-[11px] font-bold uppercase tracking-wider text-white shadow-md shadow-blue-900/10 hover:bg-blue-700"
            @click="isAddModalOpen = true"
          >
            <UserPlus class="h-4 w-4" /> Quick Add Stakeholder
          </Button>
        </div>

        <StakeholderDetailView v-if="selectedStakeholder" :stakeholder="selectedStakeholder" />
        <div
          v-else
          class="h-[400px] border-2 border-dashed border-slate-200 rounded-xl flex flex-col items-center justify-center text-center p-8"
        >
          <Users class="h-10 w-10 text-slate-200 mb-4" />
          <p class="text-sm font-bold text-slate-400 uppercase tracking-widest">
            No Stakeholder Selected
          </p>
          <p class="text-xs text-slate-400 mt-2">
            Select a row to view linked projects and title deed details.
          </p>
        </div>
      </div>
    </div>

    <!-- Modals -->
    <QuickAddStakeholder :open="isAddModalOpen" @close="isAddModalOpen = false" />
  </div>
</template>
