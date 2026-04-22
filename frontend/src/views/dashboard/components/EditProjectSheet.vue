<script setup>
import { reactive, ref, watch, computed, onMounted } from 'vue'
import { toast } from 'vue-sonner'
import { Loader2, Cpu, ChevronsUpDown, Check } from 'lucide-vue-next'
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
import { Badge } from '@/components/ui/badge'
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
  project: { type: Object, default: null },
})
const emit = defineEmits(['update:open'])

const projectsStore = useProjectsStore()
const municipalitiesStore = useMunicipalitiesStore()
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
  // Building dimensions (IFC-sourced, overridable)
  proposedGfaM2: '',
  footprintM2: '',
  numberOfStoreys: '',
  buildingHeightM: '',
  // Setbacks (always manual)
  frontSetbackM: '',
  rearSetbackM: '',
  sideSetbackM: '',
  // Parking
  parkingBays: '',
  glaForParkingM2: '',
}

const form = reactive({ ...EMPTY_FORM })
const errors = reactive({})

// Populate form whenever the project prop changes (sheet opens with a project)
watch(
  () => props.project,
  (p) => {
    if (!p) return
    form.name = p.name ?? ''
    form.description = p.description ?? ''
    form.municipality = p.municipality ?? ''
    form.address = p.address ?? ''
    form.erf = p.erf ?? ''
    form.siteAreaM2 = p.siteAreaM2 != null ? String(p.siteAreaM2) : ''
    form.zoningScheme = p.zoningScheme ?? ''
    form.proposedGfaM2 = p.proposedGfaM2 != null ? String(p.proposedGfaM2) : ''
    form.footprintM2 = p.footprintM2 != null ? String(p.footprintM2) : ''
    form.numberOfStoreys = p.numberOfStoreys != null ? String(p.numberOfStoreys) : ''
    form.buildingHeightM = p.buildingHeightM != null ? String(p.buildingHeightM) : ''
    form.frontSetbackM = p.frontSetbackM != null ? String(p.frontSetbackM) : ''
    form.rearSetbackM = p.rearSetbackM != null ? String(p.rearSetbackM) : ''
    form.sideSetbackM = p.sideSetbackM != null ? String(p.sideSetbackM) : ''
    form.parkingBays = p.parkingBays != null ? String(p.parkingBays) : ''
    form.glaForParkingM2 = p.glaForParkingM2 != null ? String(p.glaForParkingM2) : ''
    Object.keys(errors).forEach((k) => delete errors[k])
  },
  { immediate: true },
)

function num(val) {
  const n = parseFloat(val)
  return isNaN(n) ? null : n
}

function int(val) {
  const n = parseInt(val, 10)
  return isNaN(n) ? null : n
}

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
    await projectsStore.updateProject(props.project.id, {
      name: form.name.trim(),
      description: form.description.trim(),
      municipality: form.municipality.trim(),
      address: form.address.trim(),
      erf: form.erf.trim(),
      siteAreaM2: Number(form.siteAreaM2),
      zoningScheme: form.zoningScheme,
      proposedGfaM2: num(form.proposedGfaM2),
      footprintM2: num(form.footprintM2),
      numberOfStoreys: int(form.numberOfStoreys),
      buildingHeightM: num(form.buildingHeightM),
      frontSetbackM: num(form.frontSetbackM),
      rearSetbackM: num(form.rearSetbackM),
      sideSetbackM: num(form.sideSetbackM),
      parkingBays: int(form.parkingBays),
      glaForParkingM2: num(form.glaForParkingM2),
    })
    toast.success('Project updated', { description: form.name })
    emit('update:open', false)
  } catch (err) {
    toast.error('Failed to update project', { description: err.message })
  } finally {
    submitting.value = false
  }
}

function cancel() {
  emit('update:open', false)
}
</script>

<template>
  <Sheet :open="open" @update:open="emit('update:open', $event)">
    <SheetContent side="right" class="w-full sm:max-w-lg overflow-y-auto">
      <SheetHeader class="mb-6">
        <SheetTitle class="text-base font-bold uppercase tracking-wide">Edit Project</SheetTitle>
        <SheetDescription class="text-xs text-slate-500">
          Update project details and parameters. Building dimensions are pre-filled from IFC but can be overridden.
        </SheetDescription>
      </SheetHeader>

      <form class="space-y-5" @submit.prevent="submit">

        <!-- ── Project Info ─────────────────────────────────── -->
        <p class="text-[10px] font-bold uppercase tracking-widest text-slate-400 border-b border-slate-100 pb-1">
          Project Info
        </p>

        <div class="space-y-1.5">
          <Label for="ep-name" class="text-xs font-bold uppercase tracking-wide">
            Project Name <span class="text-rose-500">*</span>
          </Label>
          <Input
            id="ep-name"
            v-model="form.name"
            placeholder="e.g. Sandton Mixed-Use Development"
            :class="errors.name ? 'border-rose-400 focus-visible:ring-rose-400' : ''"
          />
          <p v-if="errors.name" class="text-[11px] text-rose-500">{{ errors.name }}</p>
        </div>

        <div class="space-y-1.5">
          <Label for="ep-desc" class="text-xs font-bold uppercase tracking-wide">Description</Label>
          <Textarea
            id="ep-desc"
            v-model="form.description"
            placeholder="Brief project description (optional)"
            rows="2"
          />
        </div>

        <div class="grid grid-cols-2 gap-3">
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
            <p v-if="errors.municipality" class="text-[11px] text-rose-500">{{ errors.municipality }}</p>
          </div>
          <div class="space-y-1.5">
            <Label for="ep-erf" class="text-xs font-bold uppercase tracking-wide">
              ERF Number <span class="text-rose-500">*</span>
            </Label>
            <Input
              id="ep-erf"
              v-model="form.erf"
              placeholder="e.g. Erf 1234"
              :class="errors.erf ? 'border-rose-400 focus-visible:ring-rose-400' : ''"
            />
            <p v-if="errors.erf" class="text-[11px] text-rose-500">{{ errors.erf }}</p>
          </div>
        </div>

        <div class="space-y-1.5">
          <Label for="ep-addr" class="text-xs font-bold uppercase tracking-wide">Address</Label>
          <Input
            id="ep-addr"
            v-model="form.address"
            placeholder="e.g. 1 Main Street, Sandton"
          />
        </div>

        <div class="grid grid-cols-2 gap-3">
          <div class="space-y-1.5">
            <Label for="ep-area" class="text-xs font-bold uppercase tracking-wide">
              Site Area (m²) <span class="text-rose-500">*</span>
            </Label>
            <Input
              id="ep-area"
              v-model="form.siteAreaM2"
              type="number"
              min="1"
              step="0.01"
              placeholder="e.g. 1250.00"
              :class="errors.siteAreaM2 ? 'border-rose-400 focus-visible:ring-rose-400' : ''"
            />
            <p v-if="errors.siteAreaM2" class="text-[11px] text-rose-500">{{ errors.siteAreaM2 }}</p>
          </div>
          <div class="space-y-1.5">
            <Label class="text-xs font-bold uppercase tracking-wide">
              Zoning Scheme <span class="text-rose-500">*</span>
            </Label>
            <Select v-model="form.zoningScheme">
              <SelectTrigger :class="errors.zoningScheme ? 'border-rose-400 focus:ring-rose-400' : ''">
                <SelectValue placeholder="Select scheme" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem v-for="scheme in ZONING_SCHEMES" :key="scheme" :value="scheme">
                  {{ scheme }}
                </SelectItem>
              </SelectContent>
            </Select>
            <p v-if="errors.zoningScheme" class="text-[11px] text-rose-500">{{ errors.zoningScheme }}</p>
          </div>
        </div>

        <!-- ── Building Dimensions ──────────────────────────── -->
        <p class="text-[10px] font-bold uppercase tracking-widest text-slate-400 border-b border-slate-100 pb-1 flex items-center gap-2 pt-1">
          Building Dimensions
          <Badge variant="outline" class="text-[9px] gap-1 py-0 font-normal border-blue-300 text-blue-600">
            <Cpu class="h-2.5 w-2.5" />From IFC
          </Badge>
        </p>

        <div class="grid grid-cols-2 gap-3">
          <div class="space-y-1.5">
            <Label for="ep-gfa" class="text-xs font-bold uppercase tracking-wide">Proposed GFA (m²)</Label>
            <Input id="ep-gfa" v-model="form.proposedGfaM2" type="number" min="0" step="0.01" placeholder="Auto from IFC" />
          </div>
          <div class="space-y-1.5">
            <Label for="ep-fp" class="text-xs font-bold uppercase tracking-wide">Footprint (m²)</Label>
            <Input id="ep-fp" v-model="form.footprintM2" type="number" min="0" step="0.01" placeholder="Auto from IFC" />
          </div>
          <div class="space-y-1.5">
            <Label for="ep-ht" class="text-xs font-bold uppercase tracking-wide">Building Height (m)</Label>
            <Input id="ep-ht" v-model="form.buildingHeightM" type="number" min="0" step="0.01" placeholder="Auto from IFC" />
          </div>
          <div class="space-y-1.5">
            <Label for="ep-storeys" class="text-xs font-bold uppercase tracking-wide">No. of Storeys</Label>
            <Input id="ep-storeys" v-model="form.numberOfStoreys" type="number" min="1" step="1" placeholder="Auto from IFC" />
          </div>
        </div>

        <!-- ── Setbacks ─────────────────────────────────────── -->
        <p class="text-[10px] font-bold uppercase tracking-widest text-slate-400 border-b border-slate-100 pb-1 pt-1">
          Site Setbacks (m)
        </p>

        <div class="grid grid-cols-3 gap-3">
          <div class="space-y-1.5">
            <Label for="ep-fsb" class="text-xs font-bold uppercase tracking-wide">Front</Label>
            <Input id="ep-fsb" v-model="form.frontSetbackM" type="number" min="0" step="0.01" placeholder="0.00" />
          </div>
          <div class="space-y-1.5">
            <Label for="ep-rsb" class="text-xs font-bold uppercase tracking-wide">Rear</Label>
            <Input id="ep-rsb" v-model="form.rearSetbackM" type="number" min="0" step="0.01" placeholder="0.00" />
          </div>
          <div class="space-y-1.5">
            <Label for="ep-ssb" class="text-xs font-bold uppercase tracking-wide">Side</Label>
            <Input id="ep-ssb" v-model="form.sideSetbackM" type="number" min="0" step="0.01" placeholder="0.00" />
          </div>
        </div>

        <!-- ── Parking ──────────────────────────────────────── -->
        <p class="text-[10px] font-bold uppercase tracking-widest text-slate-400 border-b border-slate-100 pb-1 pt-1">
          Parking
        </p>

        <div class="grid grid-cols-2 gap-3">
          <div class="space-y-1.5">
            <Label for="ep-bays" class="text-xs font-bold uppercase tracking-wide">Parking Bays</Label>
            <Input id="ep-bays" v-model="form.parkingBays" type="number" min="0" step="1" placeholder="0" />
          </div>
          <div class="space-y-1.5">
            <Label for="ep-gla" class="text-xs font-bold uppercase tracking-wide">GLA for Parking (m²)</Label>
            <Input id="ep-gla" v-model="form.glaForParkingM2" type="number" min="0" step="0.01" placeholder="0.00" />
          </div>
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
            {{ submitting ? 'Saving…' : 'Save Changes' }}
          </Button>
        </SheetFooter>
      </form>
    </SheetContent>
  </Sheet>
</template>
