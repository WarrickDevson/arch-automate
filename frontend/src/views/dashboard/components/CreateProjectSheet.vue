<script setup>
import { reactive, ref, computed, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { toast } from 'vue-sonner'
import { Loader2, ChevronsUpDown, Check } from 'lucide-vue-next'
import {
  Sheet,
  SheetContent,
  SheetHeader,
  SheetTitle,
  SheetDescription,
  SheetFooter,
} from '@/components/ui/sheet'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Textarea } from '@/components/ui/textarea'
import {
  Select,
  SelectTrigger,
  SelectValue,
  SelectContent,
  SelectItem,
} from '@/components/ui/select'
import { Popover, PopoverTrigger, PopoverContent } from '@/components/ui/popover'
import {
  Command,
  CommandInput,
  CommandList,
  CommandEmpty,
  CommandGroup,
  CommandItem,
  CommandSeparator,
} from '@/components/ui/command'
import { useProjectsStore } from '@/stores/projects.store'
import { useMunicipalitiesStore } from '@/stores/municipalities.store'

// These match the 7 hardcoded schemes in ZoningEngine.cs
const ZONING_SCHEMES = [
  'Residential 1',
  'Residential 2',
  'General Residential 1',
  'General Residential 2',
  'Special Residential',
  'Business 1',
  'Industrial 1',
]

const props = defineProps({
  open: { type: Boolean, required: true },
})
const emit = defineEmits(['update:open'])

const projectsStore = useProjectsStore()
const municipalitiesStore = useMunicipalitiesStore()
const router = useRouter()
const submitting = ref(false)
const muniOpen = ref(false)
const muniSearch = ref('')

onMounted(() => municipalitiesStore.fetchMunicipalities())

const groupedMunicipalities = computed(() => {
  const q = muniSearch.value.toLowerCase()
  const list = q
    ? municipalitiesStore.municipalities.filter(
        (m) => m.name.toLowerCase().includes(q) || m.shortName.toLowerCase().includes(q),
      )
    : municipalitiesStore.municipalities

  // Group by provinceName, preserving insertion order (municipalities are sorted by name)
  const map = new Map()
  for (const m of list) {
    const key = m.provinceName || 'Other'
    if (!map.has(key)) map.set(key, [])
    map.get(key).push(m)
  }
  return Array.from(map, ([province, items]) => ({ province, items }))
})

function selectMunicipality(name) {
  form.municipality = name
  muniOpen.value = false
  muniSearch.value = ''
  if (errors.municipality) delete errors.municipality
}

const EMPTY_FORM = {
  name: '',
  description: '',
  municipality: '',
  address: '',
  erf: '',
  siteAreaM2: '',
  zoningScheme: '',
}

const form = reactive({ ...EMPTY_FORM })
const errors = reactive({})

function validate() {
  Object.keys(errors).forEach((k) => delete errors[k])
  if (!form.name.trim()) errors.name = 'Project name is required'
  if (!form.municipality.trim()) errors.municipality = 'Municipality is required'
  if (!form.erf.trim()) errors.erf = 'ERF number is required'
  if (!form.siteAreaM2 || Number(form.siteAreaM2) <= 0)
    errors.siteAreaM2 = 'Enter a valid site area'
  if (!form.zoningScheme) errors.zoningScheme = 'Select a zoning scheme'
  return Object.keys(errors).length === 0
}

async function submit() {
  if (!validate()) return

  submitting.value = true
  try {
    const project = await projectsStore.addProject({
      name: form.name.trim(),
      description: form.description.trim(),
      municipality: form.municipality.trim(),
      address: form.address.trim(),
      erf: form.erf.trim(),
      siteAreaM2: Number(form.siteAreaM2),
      zoningScheme: form.zoningScheme,
    })
    toast.success('Project created', { description: form.name })
    Object.assign(form, EMPTY_FORM)
    emit('update:open', false)
    router.push({ name: 'workbench', params: { projectId: project.id } })
  } catch (err) {
    toast.error('Failed to create project', { description: err.message })
  } finally {
    submitting.value = false
  }
}

function cancel() {
  Object.assign(form, EMPTY_FORM)
  Object.keys(errors).forEach((k) => delete errors[k])
  emit('update:open', false)
}
</script>

<template>
  <Sheet :open="open" @update:open="emit('update:open', $event)">
    <SheetContent side="right" class="w-full sm:max-w-lg overflow-y-auto">
      <SheetHeader class="mb-6">
        <SheetTitle class="text-base font-bold uppercase tracking-wide">New Project</SheetTitle>
        <SheetDescription class="text-xs text-slate-500">
          Create a new site project. Compliance analysis can be run from the Workbench once created.
        </SheetDescription>
      </SheetHeader>

      <form class="space-y-5" @submit.prevent="submit">
        <!-- Project Name -->
        <div class="space-y-1.5">
          <Label for="proj-name" class="text-xs font-bold uppercase tracking-wide">
            Project Name <span class="text-rose-500">*</span>
          </Label>
          <Input
            id="proj-name"
            v-model="form.name"
            placeholder="e.g. Sandton Mixed-Use Development"
            :class="errors.name ? 'border-rose-400 focus-visible:ring-rose-400' : ''"
          />
          <p v-if="errors.name" class="text-[11px] text-rose-500">{{ errors.name }}</p>
        </div>

        <!-- Description -->
        <div class="space-y-1.5">
          <Label for="proj-desc" class="text-xs font-bold uppercase tracking-wide">
            Description
          </Label>
          <Textarea
            id="proj-desc"
            v-model="form.description"
            placeholder="Brief project description (optional)"
            rows="3"
          />
        </div>

        <!-- Municipality -->
        <div class="space-y-1.5">
          <Label class="text-xs font-bold uppercase tracking-wide">
            Municipality <span class="text-rose-500">*</span>
          </Label>
          <Popover v-model:open="muniOpen">
            <PopoverTrigger as-child>
              <Button
                variant="outline"
                role="combobox"
                :aria-expanded="muniOpen"
                class="w-full justify-between font-normal"
                :class="errors.municipality ? 'border-rose-400 focus:ring-rose-400' : ''"
              >
                <span :class="!form.municipality ? 'text-slate-400' : ''">
                  {{ form.municipality || 'Search municipality…' }}
                </span>
                <ChevronsUpDown class="ml-2 h-4 w-4 shrink-0 opacity-50" />
              </Button>
            </PopoverTrigger>
            <PopoverContent class="w-[--radix-popover-trigger-width] p-0" align="start">
              <Command should-filter="false">
                <CommandInput
                  v-model="muniSearch"
                  placeholder="Search municipalities…"
                />
                <CommandList>
                  <CommandEmpty>
                    {{ municipalitiesStore.loading ? 'Loading…' : 'No municipality found.' }}
                  </CommandEmpty>
                  <template v-for="(group, gi) in groupedMunicipalities" :key="group.province">
                    <CommandSeparator v-if="gi > 0" />
                    <CommandGroup :heading="group.province">
                      <CommandItem
                        v-for="m in group.items"
                        :key="m.id"
                        :value="m.name"
                        @select="selectMunicipality(m.name)"
                      >
                        <Check
                          class="mr-2 h-4 w-4"
                          :class="form.municipality === m.name ? 'opacity-100' : 'opacity-0'"
                        />
                        {{ m.name }}
                      </CommandItem>
                    </CommandGroup>
                  </template>
                </CommandList>
              </Command>
            </PopoverContent>
          </Popover>
          <p v-if="errors.municipality" class="text-[11px] text-rose-500">
            {{ errors.municipality }}
          </p>
        </div>

        <!-- ERF Number -->
        <div class="space-y-1.5">
          <Label for="proj-erf" class="text-xs font-bold uppercase tracking-wide">
            ERF Number <span class="text-rose-500">*</span>
          </Label>
          <Input
            id="proj-erf"
            v-model="form.erf"
            placeholder="e.g. Erf 1234"
            :class="errors.erf ? 'border-rose-400 focus-visible:ring-rose-400' : ''"
          />
          <p v-if="errors.erf" class="text-[11px] text-rose-500">{{ errors.erf }}</p>
        </div>

        <!-- Site Area -->
        <div class="space-y-1.5">
          <Label for="proj-area" class="text-xs font-bold uppercase tracking-wide">
            Site Area (m²) <span class="text-rose-500">*</span>
          </Label>
          <Input
            id="proj-area"
            v-model="form.siteAreaM2"
            type="number"
            min="1"
            step="0.01"
            placeholder="e.g. 1250.00"
            :class="errors.siteAreaM2 ? 'border-rose-400 focus-visible:ring-rose-400' : ''"
          />
          <p v-if="errors.siteAreaM2" class="text-[11px] text-rose-500">{{ errors.siteAreaM2 }}</p>
        </div>

        <!-- Address -->
        <div class="space-y-1.5">
          <Label for="proj-addr" class="text-xs font-bold uppercase tracking-wide">Address</Label>
          <Input
            id="proj-addr"
            v-model="form.address"
            placeholder="e.g. 1 Main Street, Sandton"
          />
        </div>

        <!-- Zoning Scheme -->
        <div class="space-y-1.5">
          <Label class="text-xs font-bold uppercase tracking-wide">
            Zoning Scheme <span class="text-rose-500">*</span>
          </Label>
          <Select v-model="form.zoningScheme">
            <SelectTrigger
              :class="errors.zoningScheme ? 'border-rose-400 focus:ring-rose-400' : ''"
            >
              <SelectValue placeholder="Select zoning scheme" />
            </SelectTrigger>
            <SelectContent>
              <SelectItem v-for="scheme in ZONING_SCHEMES" :key="scheme" :value="scheme">
                {{ scheme }}
              </SelectItem>
            </SelectContent>
          </Select>
          <p v-if="errors.zoningScheme" class="text-[11px] text-rose-500">
            {{ errors.zoningScheme }}
          </p>
        </div>

        <SheetFooter class="pt-4 gap-2">
          <Button type="button" variant="outline" class="uppercase text-xs font-bold" @click="cancel">
            Cancel
          </Button>
          <Button
            type="submit"
            class="bg-blue-600 hover:bg-blue-700 uppercase text-xs font-bold gap-2"
            :disabled="submitting"
          >
            <Loader2 v-if="submitting" class="h-3.5 w-3.5 animate-spin" />
            {{ submitting ? 'Creating…' : 'Create Project' }}
          </Button>
        </SheetFooter>
      </form>
    </SheetContent>
  </Sheet>
</template>
