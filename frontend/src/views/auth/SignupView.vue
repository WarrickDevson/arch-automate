<script setup>
import { computed, reactive } from 'vue'
import { useRouter } from 'vue-router'
import { UserRound, Mail, LockKeyhole, ArrowRight } from 'lucide-vue-next'
import BaseInput from '@/components/ui/BaseInput.vue'
import { Button } from '@/components/ui/button'
import { Checkbox } from '@/components/ui/checkbox'
import { Label } from '@/components/ui/label'
import { useAuthStore } from '@/stores/auth.store'

const router = useRouter()
const authStore = useAuthStore()

authStore.clearAuthWorkflow()

const form = reactive({
  firstName: '',
  lastName: '',
  email: '',
  password: '',
  confirmPassword: '',
  acceptTerms: false,
})

const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/

const normalizedEmail = computed(() => form.email.trim().toLowerCase())
const hasValidEmail = computed(() => emailRegex.test(normalizedEmail.value))

const hasStrongPassword = computed(() => {
  if (form.password.length < 8) return false
  const hasLower = /[a-z]/.test(form.password)
  const hasUpper = /[A-Z]/.test(form.password)
  return hasLower && hasUpper
})

const passwordMismatch = computed(
  () => form.confirmPassword.length > 0 && form.password !== form.confirmPassword
)

const canSubmit = computed(
  () =>
    !!form.firstName.trim() &&
    !!form.lastName.trim() &&
    hasValidEmail.value &&
    hasStrongPassword.value &&
    !passwordMismatch.value &&
    form.acceptTerms
)

const onRegister = async () => {
  if (!canSubmit.value) return

  try {
    const result = await authStore.registerWithPassword({
      firstName: form.firstName.trim(),
      lastName: form.lastName.trim(),
      username: normalizedEmail.value,
      password: form.password,
    })

    if (result.status === 'confirmed') {
      await router.replace({
        path: '/auth/login',
        query: { registered: '1', username: normalizedEmail.value },
      })
      return
    }

    if (result.status === 'onboarding-required') {
      await router.replace({ name: 'onboarding' })
      return
    }

    await router.replace({
      path: '/auth/verify-account',
      query: { username: normalizedEmail.value },
    })
  } catch {
    // Error handled by store
  }
}
</script>

<template>
  <div class="animate-in fade-in slide-in-from-bottom-4 duration-500">
    <!-- Header Section -->
    <div class="border-l-4 border-primary pl-6 mb-10">
      <p class="text-[10px] font-bold uppercase tracking-[0.3em] text-primary/60">Arch Automate</p>
      <h1
        class="mt-2 font-display text-3xl font-extrabold tracking-tighter text-foreground uppercase"
      >
        Create Your Account
      </h1>
      <p class="mt-2 max-w-md text-[13px] font-medium text-slate-500">
        Sign up with your email to create an account, then finish workspace setup during onboarding.
      </p>
    </div>

    <form class="space-y-5" @submit.prevent="onRegister">
      <!-- Name Grid -->
      <div class="grid grid-cols-1 gap-4 sm:grid-cols-2">
        <BaseInput
          id="signupFirstName"
          v-model="form.firstName"
          label="First name"
          autocomplete="given-name"
          :icon="UserRound"
        />
        <BaseInput
          id="signupLastName"
          v-model="form.lastName"
          label="Last name"
          autocomplete="family-name"
          :icon="UserRound"
        />
      </div>

      <!-- Email -->
      <BaseInput
        id="signupEmail"
        v-model="form.email"
        label="Email"
        autocomplete="email"
        :icon="Mail"
        :error="form.email && !hasValidEmail ? 'Enter a valid email address.' : ''"
      />

      <!-- Password -->
      <BaseInput
        id="signupPassword"
        v-model="form.password"
        type="password"
        label="Password"
        autocomplete="new-password"
        hint="Minimum 8 characters and both uppercase/lowercase letters."
        :icon="LockKeyhole"
        :error="
          form.password && !hasStrongPassword ? 'Password does not meet minimum requirements.' : ''
        "
      />

      <!-- Confirm Password -->
      <BaseInput
        id="signupConfirmPassword"
        v-model="form.confirmPassword"
        type="password"
        label="Confirm password"
        autocomplete="new-password"
        :icon="LockKeyhole"
        :error="passwordMismatch ? 'Passwords do not match.' : ''"
      />

      <!-- Terms and Conditions -->
      <div
        class="flex items-start gap-3 rounded-lg border border-border bg-card px-4 py-4 transition-colors hover:border-primary/20"
      >
        <Checkbox id="acceptTerms" v-model="form.acceptTerms" class="mt-0.5" />
        <Label
          for="acceptTerms"
          class="text-[13px] font-medium text-slate-600 cursor-pointer leading-snug"
        >
          I accept the Terms and Conditions and Privacy Policy
        </Label>
      </div>

      <!-- Error Message -->
      <p
        v-if="authStore.errorMessage"
        class="text-xs font-bold uppercase tracking-wider text-rose-600"
      >
        {{ authStore.errorMessage }}
      </p>

      <!-- Submit Button -->
      <Button
        type="submit"
        class="h-12 w-full gap-3 bg-primary text-primary-foreground hover:bg-primary/90 uppercase text-xs font-bold tracking-widest transition-all active:scale-95 shadow-lg"
        :disabled="authStore.loading || !canSubmit"
      >
        {{ authStore.loading ? 'Creating account...' : 'Create Account' }}
      </Button>
    </form>

    <!-- Footer Link -->
    <div class="relative py-8">
      <div class="absolute inset-0 flex items-center">
        <span class="w-full border-t border-slate-200" />
      </div>
      <div
        class="relative flex justify-center text-[10px] uppercase font-bold tracking-widest text-slate-400"
      >
        <span class="bg-slate-50 px-2">Already Registered?</span>
      </div>
    </div>

    <p class="text-center text-sm">
      <RouterLink
        to="/auth/login"
        class="font-bold text-primary hover:text-primary/80 flex items-center justify-center gap-2"
      >
        Sign in to workspace <ArrowRight class="h-4 w-4" />
      </RouterLink>
    </p>
  </div>
</template>

<style scoped>
:deep(label) {
  font-size: 10px;
  font-weight: 700;
  text-transform: uppercase;
  letter-spacing: 0.1em;
  color: rgb(148 163 184);
  margin-bottom: 0.375rem;
}
:deep(input) {
  border-radius: 0.5rem;
  border-color: rgb(226 232 240);
  transition: all 150ms ease;
}
:deep(.text-xs.text-muted-foreground) {
  font-family: ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, "Liberation Mono", "Courier New", monospace;
  font-size: 11px;
  color: rgb(148 163 184);
}
</style>
