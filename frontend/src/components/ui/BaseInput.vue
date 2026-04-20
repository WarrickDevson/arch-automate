<script setup>
import { computed, ref } from 'vue'
import { Eye, EyeOff, X } from 'lucide-vue-next'
import { Button } from '@/components/ui/button'
import { cn } from '@/lib/utils'

const props = defineProps({
  modelValue: {
    type: String,
    default: '',
  },
  id: {
    type: String,
    required: true,
  },
  label: {
    type: String,
    required: true,
  },
  type: {
    type: String,
    default: 'text',
  },
  autocomplete: {
    type: String,
    default: 'off',
  },
  error: {
    type: String,
    default: '',
  },
  hint: {
    type: String,
    default: '',
  },
  icon: {
    type: [String, Object],
    default: null,
  },
  clearable: {
    type: Boolean,
    default: false,
  },
  numericOnly: {
    type: Boolean,
    default: false,
  },
})

const emit = defineEmits(['update:modelValue', 'blur'])

const showPassword = ref(false)
const isPasswordField = computed(() => props.type === 'password')
const isClearableField = computed(() => props.clearable && !isPasswordField.value)
const canClear = computed(() => isClearableField.value && `${props.modelValue || ''}`.length > 0)
const resolvedType = computed(() => {
  if (!isPasswordField.value) return props.type
  return showPassword.value ? 'text' : 'password'
})

const togglePassword = () => {
  showPassword.value = !showPassword.value
}
const clearInput = () => {
  emit('update:modelValue', '')
}

const onInput = (event) => {
  const input = event?.target
  if (!input) return

  const nextValue = props.numericOnly ? input.value.replace(/\D+/g, '') : input.value
  if (props.numericOnly && input.value !== nextValue) {
    input.value = nextValue
  }
  emit('update:modelValue', nextValue)
}
</script>

<template>
  <div>
    <div class="relative">
      <component
        :is="icon"
        v-if="icon"
        class="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 -foreground"
      />

      <input
        :id="id"
        :value="modelValue"
        :type="resolvedType"
        :inputmode="numericOnly ? 'numeric' : undefined"
        :pattern="numericOnly ? '[0-9]*' : undefined"
        :autocomplete="autocomplete"
        placeholder=" "
        :class="
          cn(
            'peer h-12 w-full rounded-xl border border-input bg-card px-4 pt-4 text-sm text-foreground outline-none transition focus:border-primary focus:ring-2 focus:ring-primary/20',
            icon ? 'pl-10' : '',
            isPasswordField || canClear ? 'pr-11' : '',
            error ? 'border-destructive focus:border-destructive focus:ring-destructive/20' : ''
          )
        "
        :aria-invalid="error ? 'true' : 'false'"
        @input="onInput"
        @blur="emit('blur')"
      />

      <Button
        v-if="canClear"
        type="button"
        variant="ghost"
        size="icon-sm"
        class="absolute right-2 top-1/2 -translate-y-1/2 -foreground"
        aria-label="Clear input"
        @click="clearInput"
      >
        <X class="h-4 w-4" />
      </Button>

      <Button
        v-if="isPasswordField"
        type="button"
        variant="ghost"
        size="icon-sm"
        class="absolute right-2 top-1/2 -translate-y-1/2 -foreground"
        :aria-label="showPassword ? 'Hide password' : 'Show password'"
        @click="togglePassword"
      >
        <Eye v-if="!showPassword" class="h-4 w-4" />
        <EyeOff v-else class="h-4 w-4" />
      </Button>

      <label
        :for="id"
        class="pointer-events-none absolute top-2 origin-left bg-card px-1 text-xs -foreground transition-all peer-placeholder-shown:top-3.5 peer-placeholder-shown:text-sm peer-focus:top-1 peer-focus:text-xs peer-focus:text-primary"
        :class="icon ? 'left-9' : 'left-3'"
      >
        {{ label }}
      </label>
    </div>
    <p v-if="hint && !error" class="mt-1 text-xs -foreground">{{ hint }}</p>
    <p v-if="error" class="mt-1 text-xs text-destructive">{{ error }}</p>
  </div>
</template>
