<script setup>
import { reactive } from 'vue'
import { useRoute } from 'vue-router'
import { Mail, RefreshCcw } from 'lucide-vue-next'
import BaseInput from '@/components/ui/BaseInput.vue'
import { Button } from '@/components/ui/button'
import { useAuthStore } from '@/stores/auth.store'

const route = useRoute()
const authStore = useAuthStore()

const form = reactive({
  username: typeof route.query.username === 'string' ? route.query.username : '',
})

const isSent = () => authStore.resetState.step === 'sent'

const onRequestReset = async () => {
  try {
    await authStore.requestPasswordReset({ username: form.username.trim() })
  } catch {
    // The store sets a user-facing error message.
  }
}

const onResend = async () => {
  try {
    await authStore.requestPasswordReset({ username: form.username.trim() })
  } catch {
    // The store sets a user-facing error message.
  }
}
</script>

<template>
  <div class="animate-in fade-in slide-in-from-bottom-4 duration-500">
    <div class="border-l-4 border-primary pl-6 mb-10">
      <p class="text-[10px] font-bold uppercase tracking-[0.3em] text-primary/60">Arch Automate</p>
      <h1
        class="mt-2 text-3xl font-extrabold tracking-tighter text-foreground uppercase"
      >
        {{ isSent() ? 'Check Your Email' : 'Reset Password' }}
      </h1>
      <p class="mt-2 max-w-md text-[13px] font-medium text-slate-500">
        <template v-if="isSent()">
          We've sent a password reset link to
          <span class="font-bold text-foreground">{{ authStore.resetState.destination }}</span
          >. Click the link to set a new password.
        </template>
        <template v-else> Enter your email and we'll send you a secure reset link. </template>
      </p>
    </div>

    <!-- Request form -->
    <form v-if="!isSent()" class="space-y-5" @submit.prevent="onRequestReset">
      <BaseInput
        id="forgotUsername"
        v-model="form.username"
        label="Email"
        autocomplete="email"
        :icon="Mail"
      />

      <p
        v-if="authStore.errorMessage"
        class="text-xs font-bold uppercase tracking-wider text-rose-600"
      >
        {{ authStore.errorMessage }}
      </p>

      <Button
        type="submit"
        class="h-12 w-full bg-primary text-primary-foreground hover:bg-primary/90 uppercase text-xs font-bold tracking-widest transition-all active:scale-95 shadow-lg"
        :disabled="authStore.loading"
      >
        {{ authStore.loading ? 'Sending...' : 'Send Reset Link' }}
      </Button>
    </form>

    <!-- Sent state -->
    <div v-else class="space-y-5">
      <div class="rounded-lg border border-slate-200 bg-slate-100/60 p-5">
        <p class="text-[11px] font-mono font-medium text-slate-500 leading-relaxed">
          <span class="font-bold text-slate-700">Didn't get it?</span>
          Check your spam folder. The link expires in 1 hour.
        </p>
      </div>

      <p
        v-if="authStore.errorMessage"
        class="text-xs font-bold uppercase tracking-wider text-rose-600"
      >
        {{ authStore.errorMessage }}
      </p>

      <Button
        type="button"
        variant="outline"
        class="h-12 w-full gap-2 uppercase text-xs font-bold tracking-widest transition-all active:scale-95"
        :disabled="authStore.loading"
        @click="onResend"
      >
        <RefreshCcw class="h-4 w-4" :class="authStore.loading ? 'animate-spin' : ''" />
        {{ authStore.loading ? 'Resending...' : 'Resend reset link' }}
      </Button>
    </div>

    <p class="mt-8 text-center text-sm text-slate-500">
      <RouterLink to="/auth/login" class="font-bold text-primary/70 hover:text-primary">
        Back to sign in
      </RouterLink>
    </p>
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
