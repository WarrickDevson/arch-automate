<script setup>
import { Collapsible, CollapsibleContent, CollapsibleTrigger } from '@/components/ui/collapsible'

defineProps({
  title: {
    type: String,
    required: true,
  },
  icon: {
    type: [Object, Function],
    default: null,
  },
  modelValue: {
    type: Boolean,
    default: false,
  },
})

const emit = defineEmits(['update:modelValue'])
</script>

<template>
  <Collapsible
    :open="modelValue"
    class="rounded-2xl border border-border bg-surface/80"
    @update:open="emit('update:modelValue', $event)"
  >
    <CollapsibleTrigger class="flex w-full items-center justify-between px-4 py-3 text-left">
      <span class="inline-flex items-center gap-2 text-sm font-semibold text-text">
        <component :is="icon" v-if="icon" class="h-4 w-4 text-accent" />
        {{ title }}
      </span>
      <ChevronDown class="h-4 w-4 transition" :class="modelValue ? 'rotate-180' : ''" />
    </CollapsibleTrigger>

    <CollapsibleContent class="border-t border-border px-4 py-3">
      <slot />
    </CollapsibleContent>
  </Collapsible>
</template>
