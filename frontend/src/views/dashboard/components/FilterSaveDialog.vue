<script setup>
import { reactive, watch } from 'vue'
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogDescription,
  DialogFooter,
} from '@/components/ui/dialog'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Checkbox } from '@/components/ui/checkbox'
import { RadioGroup, RadioGroupItem } from '@/components/ui/radio-group'

const props = defineProps({
  open: {
    type: Boolean,
    default: false,
  },
  selectedFilterName: {
    type: String,
    default: '',
  },
  saving: {
    type: Boolean,
    default: false,
  },
})

const emit = defineEmits(['close', 'save'])

const form = reactive({
  name: '',
  isDefault: false,
  mode: 'new',
})

watch(
  () => props.open,
  (value) => {
    if (!value) return
    form.name = props.selectedFilterName || ''
    form.isDefault = false
    form.mode = props.selectedFilterName ? 'update' : 'new'
  }
)

const submit = () => {
  if (props.saving) return
  if (!form.name.trim()) return
  emit('save', {
    name: form.name.trim(),
    isDefault: form.isDefault,
    mode: form.mode,
  })
}
</script>

<template>
  <Dialog
    :open="open"
    @update:open="
      (v) => {
        if (!v) emit('close')
      }
    "
  >
    <DialogContent class="max-w-md">
      <DialogHeader>
        <DialogTitle>Save Filter</DialogTitle>
        <DialogDescription>Store this filter profile for quick reuse.</DialogDescription>
      </DialogHeader>

      <div class="space-y-4">
        <div class="space-y-1.5">
          <Label
            for="filter-name"
            class="text-xs font-semibold uppercase tracking-wide -foreground"
          >
            Filter Name
          </Label>
          <Input id="filter-name" v-model="form.name" placeholder="e.g. Pest Focus - North" />
        </div>

        <RadioGroup v-model="form.mode" class="flex flex-wrap gap-4">
          <div class="flex items-center gap-2">
            <RadioGroupItem id="mode-new" value="new" />
            <Label for="mode-new" class="text-sm font-normal -foreground cursor-pointer"
              >Save as new</Label
            >
          </div>
          <div class="flex items-center gap-2">
            <RadioGroupItem id="mode-update" value="update" />
            <Label for="mode-update" class="text-sm font-normal -foreground cursor-pointer"
              >Update selected</Label
            >
          </div>
        </RadioGroup>

        <div class="flex items-center gap-2">
          <Checkbox
            id="is-default"
            :checked="form.isDefault"
            @update:checked="form.isDefault = $event"
          />
          <Label for="is-default" class="text-sm font-normal -foreground cursor-pointer">
            Set as default filter
          </Label>
        </div>
      </div>

      <DialogFooter>
        <Button variant="outline" :disabled="saving" @click="emit('close')">Cancel</Button>
        <Button
          class="bg-accent text-accent-foreground hover:bg-accent/90"
          :loading="saving"
          :disabled="saving"
          @click="submit"
          >Save Filter</Button
        >
      </DialogFooter>
    </DialogContent>
  </Dialog>
</template>
