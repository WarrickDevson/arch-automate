<script setup>
import { computed } from 'vue'
import { AlertTriangle } from 'lucide-vue-next'
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogDescription, DialogFooter } from '@/components/ui/dialog'
import { Button } from '@/components/ui/button'
import { promptState, resolvePrompt } from '@/services/promptService'

const isConfirm = computed(() => promptState.mode === 'confirm')

const onOpenChange = (v) => {
  if (!v) {
    resolvePrompt(false)
  }
}
</script>

<template>
  <Dialog :open="promptState.open" @update:open="onOpenChange">
    <DialogContent class="max-w-md rounded-2xl">
      <DialogHeader class="space-y-2">
        <DialogTitle class="text-lg font-bold">{{ promptState.title }}</DialogTitle>
        <DialogDescription class="text-sm">
          <div class="flex items-start gap-3">
            <AlertTriangle
              v-if="isConfirm"
              class="mt-0.5 h-5 w-5 shrink-0 text-muted-foreground"
              aria-hidden="true"
            />
            <span>{{ promptState.message }}</span>
          </div>
        </DialogDescription>
      </DialogHeader>

      <DialogFooter class="mt-3 flex w-full gap-3 sm:justify-end">
        <Button
          v-if="isConfirm"
          variant="outline"
          class="h-11 rounded-xl"
          @click="resolvePrompt(false)"
        >
          {{ promptState.cancelText }}
        </Button>
        <Button
          class="h-11 rounded-xl"
          :variant="promptState.isDestructive ? 'destructive' : 'default'"
          @click="resolvePrompt(true)"
        >
          {{ promptState.confirmText }}
        </Button>
      </DialogFooter>
    </DialogContent>
  </Dialog>
</template>

