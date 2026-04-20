<script setup>
import { reactive } from 'vue'
import { Dialog, DialogContent, DialogHeader, DialogTitle } from '@/components/ui/dialog'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { 
  Select, SelectContent, SelectItem, SelectTrigger, SelectValue 
} from '@/components/ui/select'

const props = defineProps(['open'])
const emit = defineEmits(['close'])

const form = reactive({
  name: '',
  role: '',
  company: '',
  idNumber: '',
  sacap: '',
  email: '',
  titleDeed: ''
})
</script>

<template>
  <Dialog :open="open" @update:open="emit('close')">
    <DialogContent class="max-w-md bg-white">
      <DialogHeader>
        <DialogTitle class="text-sm font-bold uppercase tracking-widest text-slate-500">New Stakeholder</DialogTitle>
      </DialogHeader>

      <div class="grid gap-4 py-4">
        <div class="space-y-2">
          <label class="text-[11px] font-bold uppercase text-slate-400">Basic Information</label>
          <Input v-model="form.name" placeholder="Full Name (As per ID)" />
          <Select v-model="form.role">
            <SelectTrigger><SelectValue placeholder="Select Role" /></SelectTrigger>
            <SelectContent>
              <SelectItem value="Owner">Property Owner</SelectItem>
              <SelectItem value="Engineer">Structural Engineer</SelectItem>
              <SelectItem value="Surveyor">Land Surveyor</SelectItem>
            </SelectContent>
          </Select>
        </div>

        <div class="space-y-2">
          <label class="text-[11px] font-bold uppercase text-slate-400">Legal & Registration</label>
          <Input v-model="form.idNumber" placeholder="ID or Passport Number" />
          <Input v-model="form.sacap" placeholder="Professional Reg No (SACAP/ECSA)" />
          <Input v-model="form.titleDeed" placeholder="Title Deed Reference (Optional)" />
        </div>

        <div class="space-y-2">
          <label class="text-[11px] font-bold uppercase text-slate-400">Contact</label>
          <Input v-model="form.email" type="email" placeholder="Email Address" />
        </div>
      </div>

      <div class="flex justify-end gap-3 pt-4">
        <Button variant="ghost" @click="emit('close')" class="uppercase text-xs font-bold">Cancel</Button>
        <Button class="bg-blue-600 text-white uppercase text-xs font-bold px-8">Save Profile</Button>
      </div>
    </DialogContent>
  </Dialog>
</template>