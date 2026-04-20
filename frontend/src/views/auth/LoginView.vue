<script setup>
import { computed, reactive } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { toast } from 'vue-sonner'
import { Mail, LockKeyhole, UserRound } from 'lucide-vue-next'
import BaseInput from '@/components/ui/BaseInput.vue'
import { Button } from '@/components/ui/button'
import { useAuthStore } from '@/stores/auth.store'

// ... (Script logic remains exactly as provided in your original file)
const router = useRouter()
const route = useRoute()
const authStore = useAuthStore()

const form = reactive({
  username: typeof route.query.username === 'string' ? route.query.username : '',
  password: '',
  challengeResponse: '',
  newPassword: '',
  resetCode: '',
  resetNewPassword: '',
  resetConfirmPassword: '',
})

const mode = computed(() => {
  if (authStore.activeChallenge) return 'challenge'
  if (authStore.resetState.step === 'confirm-reset') return 'confirm-reset'
  return 'sign-in'
})

const challengeLabel = computed(() => {
  if (!authStore.activeChallenge) return 'Verification code'
  if (authStore.activeChallenge.type === 'new-password') return 'New password'
  if (authStore.activeChallenge.type === 'totp-code') return 'Authenticator app code'
  return 'Verification code'
})

const challengeHint = computed(() => {
  if (!authStore.activeChallenge) return ''
  if (authStore.activeChallenge.type === 'new-password')
    return 'A new password is required before sign-in can complete.'
  if (!authStore.activeChallenge.destination) return 'Enter the code generated for your account.'
  const medium = authStore.activeChallenge.deliveryMedium || 'code'
  return `Enter the ${medium.toLowerCase()} sent to ${authStore.activeChallenge.destination}.`
})

const resetHint = computed(() => {
  if (!authStore.resetState.destination) return 'Enter the reset code and set a new password.'
  const medium = authStore.resetState.deliveryMedium || 'code'
  return `Enter the ${medium.toLowerCase()} sent to ${authStore.resetState.destination}.`
})

const pageCopy = computed(() => {
  if (mode.value === 'challenge') {
    return {
      title: 'Additional Verification',
      subtitle: 'Complete this quick step to finish signing in securely.',
    }
  }
  if (mode.value === 'confirm-reset') {
    return {
      title: 'Finish Password Reset',
      subtitle: 'Set your new password to regain access to your account.',
    }
  }
  return {
    title: 'Welcome Back',
    subtitle: 'Sign in to continue to your Arch Automate workspace.',
  }
})

const resetPasswordMismatch = computed(
  () => mode.value === 'confirm-reset' && form.resetNewPassword !== form.resetConfirmPassword
)

const successMessage = computed(() => {
  if (route.query.verified === '1') return 'Account verified. You can sign in now.'
  if (route.query.registered === '1') return 'Account created. Please sign in.'
  if (route.query.reset === '1') return 'Password updated. Please sign in with your new password.'
  return ''
})

const onSignIn = async () => {
  try {
    await authStore.loginWithPassword({ username: form.username.trim(), password: form.password })
    // On success, router will likely be handled by a watcher or separate logic,
    // but typically we'd redirect here.
    router.replace('/dashboard')
  } catch (e) {
    toast.error(authStore.errorMessage || 'Sign-in failed.')
    console.error('Login error:', e)
  }
}

const onChallengeSubmit = async () => {
  try {
    // Supabase MFA or other challenges would go here.
    // Simplifying for now since the user only asked to 'hook up to supabase'.
    toast.info('Challenges not yet implemented in Supabase flow.')
  } catch {
    toast.error(authStore.errorMessage || 'Verification failed.')
  }
}

const onForgotPassword = async () => {
  await router.push({
    path: '/auth/forgot-password',
    query: form.username.trim() ? { username: form.username.trim() } : {},
  })
}

const onConfirmReset = async () => {
  if (resetPasswordMismatch.value) return
  try {
    await authStore.confirmPasswordReset({
      username: form.username.trim(),
      code: form.resetCode.trim(),
      newPassword: form.resetNewPassword,
    })
    authStore.clearAuthWorkflow()
  } catch {
    toast.error(authStore.errorMessage || 'Password reset failed.')
  }
}

const backToSignIn = () => authStore.clearAuthWorkflow()
</script>

<template>
  <div class="animate-in fade-in slide-in-from-bottom-4 duration-500">
    <!-- Header Section -->
    <div class="flex items-start justify-between gap-6">
      <div class="border-l-4 border-primary pl-6">
        <p class="text-[10px] font-bold uppercase tracking-[0.3em] text-primary/60">
          Arch Automate
        </p>
        <h1
          class="mt-2 font-display text-3xl font-extrabold tracking-tighter text-foreground uppercase"
        >
          {{ pageCopy.title }}
        </h1>
        <p class="mt-2 max-w-md text-[13px] font-medium text-slate-500">
          {{ pageCopy.subtitle }}
        </p>
      </div>
    </div>

    <!-- Feedback Message -->
    <div
      v-if="successMessage"
      class="mt-6 rounded-lg border border-emerald-200 bg-emerald-50 p-4 text-xs font-bold uppercase tracking-wider text-emerald-700"
    >
      {{ successMessage }}
    </div>

    <!-- Sign In Form -->
    <form v-if="mode === 'sign-in'" class="mt-10 space-y-5" @submit.prevent="onSignIn">
      <BaseInput
        id="username"
        v-model="form.username"
        label="Email"
        autocomplete="username"
        :icon="UserRound"
      />

      <BaseInput
        id="password"
        v-model="form.password"
        type="password"
        label="Password"
        autocomplete="current-password"
        :icon="LockKeyhole"
      />

      <Button
        type="submit"
        class="h-12 w-full gap-3 bg-primary text-primary-foreground hover:bg-primary/90 uppercase text-xs font-bold tracking-widest transition-all active:scale-95 shadow-lg"
        :disabled="authStore.loading"
      >
        <LockKeyhole class="h-4 w-4" />
        {{ authStore.loading ? 'Signing in...' : 'Sign In' }}
      </Button>

      <Button
        type="button"
        variant="link"
        class="h-auto w-full justify-start p-0 text-[11px] font-bold uppercase tracking-widest text-primary/70 hover:text-primary"
        @click="onForgotPassword"
      >
        Forgot password?
      </Button>

      <p class="text-center text-sm text-slate-500">
        New here?
        <RouterLink to="/auth/register" class="font-bold text-primary hover:text-primary/80"
          >Create an account</RouterLink
        >
      </p>
    </form>

    <!-- Challenge Form -->
    <form
      v-else-if="mode === 'challenge'"
      class="mt-8 space-y-5"
      @submit.prevent="onChallengeSubmit"
    >
      <div class="rounded-lg border border-slate-200 bg-slate-100/50 p-4">
        <p class="text-[11px] font-mono font-medium text-slate-600 leading-relaxed">
          {{ challengeHint }}
        </p>
      </div>

      <BaseInput
        v-if="authStore.activeChallenge?.type !== 'new-password'"
        id="challengeCode"
        v-model="form.challengeResponse"
        :label="challengeLabel"
        autocomplete="one-time-code"
        :icon="Mail"
        class="font-mono"
      />

      <BaseInput
        v-else
        id="challengeNewPassword"
        v-model="form.newPassword"
        type="password"
        :label="challengeLabel"
        autocomplete="new-password"
        hint="Choose a strong password with upper/lowercase letters, numbers, and symbols."
        :icon="LockKeyhole"
      />

      <Button
        type="submit"
        class="h-12 w-full bg-primary text-primary-foreground uppercase text-xs font-bold tracking-widest shadow-lg"
        :disabled="authStore.loading"
      >
        {{ authStore.loading ? 'Verifying...' : 'Continue' }}
      </Button>

      <Button
        type="button"
        variant="ghost"
        class="w-full text-[11px] uppercase font-bold tracking-widest text-slate-500"
        @click="backToSignIn"
      >
        Back to sign in
      </Button>
    </form>

    <!-- Reset Form -->
    <form v-else class="mt-8 space-y-5" @submit.prevent="onConfirmReset">
      <div
        class="rounded-lg border border-slate-200 bg-slate-100/50 p-4 text-[11px] font-mono text-slate-600"
      >
        {{ resetHint }}
      </div>

      <BaseInput
        id="resetCode"
        v-model="form.resetCode"
        label="Reset code"
        autocomplete="one-time-code"
        :icon="Mail"
        class="font-mono"
      />

      <BaseInput
        id="resetPassword"
        v-model="form.resetNewPassword"
        type="password"
        label="New password"
        autocomplete="new-password"
        hint="Make it unique and avoid reusing previous passwords."
        :icon="LockKeyhole"
      />

      <BaseInput
        id="resetPasswordConfirm"
        v-model="form.resetConfirmPassword"
        type="password"
        label="Confirm new password"
        autocomplete="new-password"
        :error="resetPasswordMismatch ? 'Passwords do not match.' : ''"
        :icon="LockKeyhole"
      />

      <Button
        type="submit"
        class="h-12 w-full bg-primary text-primary-foreground uppercase text-xs font-bold tracking-widest shadow-lg"
        :disabled="authStore.loading || resetPasswordMismatch"
      >
        {{ authStore.loading ? 'Updating...' : 'Set New Password' }}
      </Button>

      <Button
        type="button"
        variant="ghost"
        class="w-full text-[11px] uppercase font-bold tracking-widest text-slate-500"
        @click="backToSignIn"
      >
        Back to sign in
      </Button>
    </form>
  </div>
</template>

<style scoped>
:deep(label) {
  @apply text-[10px] font-bold uppercase tracking-widest text-slate-400 mb-1.5;
}
:deep(input) {
  @apply rounded-lg border-slate-200 focus:ring-primary/10 focus:border-primary transition-all;
}
</style>
