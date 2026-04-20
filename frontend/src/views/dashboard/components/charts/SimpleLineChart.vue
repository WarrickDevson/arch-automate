<script setup>
import { computed } from 'vue'

const props = defineProps({
  points: {
    type: Array,
    default: () => [],
  },
  color: {
    type: String,
    default: '#0E4553',
  },
  height: {
    type: Number,
    default: 180,
  },
})

const width = 680

const maxY = computed(() => Math.max(1, ...props.points.map((point) => point.y || 0)))

const polyline = computed(() => {
  if (!props.points.length) return ''
  const xStep = width / Math.max(1, props.points.length - 1)
  return props.points
    .map((point, index) => {
      const x = index * xStep
      const y = props.height - (point.y / maxY.value) * (props.height - 24) - 10
      return `${x},${y}`
    })
    .join(' ')
})
</script>

<template>
  <div class="rounded-2xl border border-border bg-surface p-3">
    <svg :viewBox="`0 0 ${width} ${height}`" class="h-44 w-full">
      <line
        x1="0"
        :y1="height - 10"
        :x2="width"
        :y2="height - 10"
        stroke="#94a3b8"
        stroke-opacity="0.4"
      />
      <polyline
        :points="polyline"
        fill="none"
        :stroke="color"
        stroke-width="3"
        stroke-linejoin="round"
      />
      <circle
        v-for="(point, index) in points"
        :key="`${point.x}-${index}`"
        :cx="(index * width) / Math.max(1, points.length - 1)"
        :cy="height - (point.y / maxY) * (height - 24) - 10"
        r="4"
        :fill="color"
      />
    </svg>

    <div class="mt-3 grid gap-2 text-xs sm:grid-cols-2 lg:grid-cols-3">
      <div v-for="(point, index) in points" :key="`${point.x}-label-${index}`" class="truncate">
        {{ point.x }}: {{ point.y }}
      </div>
    </div>
  </div>
</template>
