<script setup>
import { computed } from 'vue'

const props = defineProps({
  data: {
    type: Array,
    default: () => [],
  },
})

const colors = ['#0E4553', '#3B82F6', '#DCE557', '#F97316']

const max = computed(() => {
  const totals = props.data.map((item) =>
    item.segments.reduce((sum, segment) => sum + segment.value, 0)
  )
  return Math.max(1, ...totals)
})
</script>

<template>
  <div class="space-y-3 rounded-2xl border border-border bg-surface p-4">
    <div v-for="item in data" :key="item.label" class="space-y-1">
      <div class="flex items-center justify-between text-xs">
        <span>{{ item.label }}</span>
        <span class="text-text">{{
          item.segments.reduce((sum, segment) => sum + segment.value, 0)
        }}</span>
      </div>
      <div class="flex h-2 overflow-hidden rounded-full bg-slate-200/60">
        <div
          v-for="(segment, index) in item.segments"
          :key="`${item.label}-${segment.label}`"
          class="h-full"
          :style="{
            width: `${Math.max(2, (segment.value / max) * 100)}%`,
            backgroundColor: colors[index % colors.length],
          }"
          :title="`${segment.label}: ${segment.value}`"
        />
      </div>
    </div>
  </div>
</template>
