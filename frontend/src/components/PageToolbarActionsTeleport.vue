<script setup>
import { onMounted, onUnmounted, ref } from 'vue'

const targetSelector = '#app-page-toolbar-actions'
const hasTarget = ref(false)

onMounted(() => {
  hasTarget.value = Boolean(document.querySelector(targetSelector))
  window.dispatchEvent(new Event('page-toolbar-actions:register'))
})

onUnmounted(() => {
  window.dispatchEvent(new Event('page-toolbar-actions:unregister'))
})
</script>

<template>
  <div class="contents">
    <Teleport :to="targetSelector" :disabled="!hasTarget">
      <div class="hidden items-center gap-2 md:flex">
        <slot />
      </div>
    </Teleport>

    <div class="flex items-center gap-2 md:hidden">
      <slot />
    </div>
  </div>
</template>