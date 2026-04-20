<script setup>
import { cn } from '@/lib/utils'

const props = defineProps({
  class: {
    type: [Boolean, null, String, Object, Array],
    required: false,
    skipCheck: true,
  },
})
</script>

<template>
  <p :class="cn('text-sm -foreground', props.class)">
    <slot />
  </p>
</template>
