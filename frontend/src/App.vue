<script setup>
import { TooltipProvider } from '@/components/ui/tooltip'
import { Toaster } from '@/components/ui/sonner'
import PromptHost from '@/components/ui/prompt/PromptHost.vue'
4  
</script>

<template>
  <TooltipProvider>
    <RouterView />
    <Toaster position="top-center" :duration="3000" rich-colors :close-button="false" />
    <PromptHost />
  </TooltipProvider>
</template>
