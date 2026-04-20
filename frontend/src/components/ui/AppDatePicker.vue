<script setup>
import { computed } from 'vue'

const props = defineProps({
  modelValue: {
    type: String,
    default: '',
  },
  id: {
    type: String,
    default: undefined,
  },
  placeholder: {
    type: String,
    default: 'Select date',
  },
  disabled: {
    type: Boolean,
    default: false,
  },
  inputClass: {
    type: String,
    default:
      'h-10 w-full rounded-lg border border-border bg-background px-3 py-2 text-sm ring-offset-background file:border-0 file:bg-transparent file:text-sm file:font-medium placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-50',
  },
  minDate: {
    type: String,
    default: '',
  },
  maxDate: {
    type: String,
    default: '',
  },
})

const emit = defineEmits(['update:modelValue'])

const onInput = (event) => {
  emit('update:modelValue', event.target.value)
}

const minDateValue = computed(() => props.minDate)
const maxDateValue = computed(() => props.maxDate)
</script>

<template>
  <input
    type="date"
    :id="id"
    :value="modelValue"
    :disabled="disabled"
    :min="minDateValue"
    :max="maxDateValue"
    :class="inputClass"
    @input="onInput"
  />
</template>

<style scoped>
/* Remove default calendar icon in some browsers if needed, or style it */
input[type='date']::-webkit-calendar-picker-indicator {
  filter: invert(var(--is-dark, 0));
  cursor: pointer;
}
</style>
