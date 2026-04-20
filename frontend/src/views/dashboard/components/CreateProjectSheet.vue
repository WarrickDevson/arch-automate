<script setup>
import { reactive, ref } from 'vue'
import { useRouter } from 'vue-router'
import { toast } from 'vue-sonner'
import { Loader2 } from 'lucide-vue-next'
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
import { useProjectsStore } from '@/stores/projects.store'

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
const router = useRouter()
const submitting = ref(false)

const EMPTY_FORM = {
  name: '',
  description: '',
  municipality: '',
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
          <Label for="proj-muni" class="text-xs font-bold uppercase tracking-wide">
            Municipality <span class="text-rose-500">*</span>
          </Label>
          <Input
            id="proj-muni"
            v-model="form.municipality"
            placeholder="e.g. City of Johannesburg"
            :class="errors.municipality ? 'border-rose-400 focus-visible:ring-rose-400' : ''"
          />
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
