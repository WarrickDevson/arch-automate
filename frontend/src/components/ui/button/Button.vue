<script setup>
import { computed } from 'vue'
import { Primitive } from 'reka-ui'
import { Loader2 } from 'lucide-vue-next'
import { cn } from '@/lib/utils'
import { buttonVariants } from '.'

const props = defineProps({
  variant: { type: null, required: false },
  size: { type: null, required: false },
  class: {
    type: [Boolean, null, String, Object, Array],
    required: false,
    skipCheck: true,
  },
  asChild: { type: Boolean, required: false },
  as: { type: null, required: false, default: 'button' },
  disabled: { type: Boolean, required: false, default: false },
  loading: { type: Boolean, required: false, default: false },
})

const isDisabled = computed(() => props.disabled || props.loading)
</script>

<template>
  <Primitive
    :as="as"
    :as-child="asChild"
    :class="cn(buttonVariants({ variant, size }), props.class, { 'cursor-wait': loading })"
    :disabled="isDisabled"
    :aria-busy="loading ? 'true' : undefined"
  >
    <Loader2 v-if="loading" class="h-4 w-4 animate-spin" />
    <slot />
  </Primitive>
</template>
