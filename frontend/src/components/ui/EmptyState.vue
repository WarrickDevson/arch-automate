<script setup>
import { Card, CardContent } from '@/components/ui/card'

defineProps({
  title: {
    type: String,
    required: true,
  },
  description: {
    type: String,
    required: true,
  },
})
</script>

<template>
  <Card class="rounded-3xl border-dashed bg-card/70 p-10 text-center shadow-panel">
    <CardContent class="p-0">
      <div
        class="mx-auto mb-5 flex h-14 w-14 items-center justify-center rounded-2xl bg-accent/10 text-accent"
      >
        <slot name="icon" />
      </div>
      <h3 class="text-xl font-semibold text-foreground">{{ title }}</h3>
      <p class="mx-auto mt-3 max-w-md text-sm -foreground">{{ description }}</p>
    </CardContent>
  </Card>
</template>
