<script setup>
import { computed } from 'vue'

const props = defineProps({
  data: {
    type: Array,
    default: () => [],
  },
  color: {
    type: String,
    default: '#0E4553',
  },
})

const maxValue = computed(() => Math.max(1, ...props.data.map((item) => item.value || 0)))
</script>

<template>
  <div class="space-y-3 rounded-2xl border border-border bg-surface p-4">
    <div v-for="item in data" :key="item.label" class="space-y-1">
      <div class="flex items-center justify-between text-xs">
        <span class="truncate pr-3">{{ item.label }}</span>
        <span class="font-semibold text-text"
          >{{ item.value }}{{ item.unit ? ` ${item.unit}` : '' }}</span
        >
      </div>
      <div class="h-2 rounded-full bg-slate-200/60">
        <div
          class="h-full rounded-full"
          :style="{
            width: `${Math.max(6, (item.value / maxValue) * 100)}%`,
            backgroundColor: color,
          }"
        />
      </div>
    </div>
  </div>
</template>
