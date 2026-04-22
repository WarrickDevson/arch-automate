<script setup>
import { computed, reactive, ref } from 'vue'
import { useRouter } from 'vue-router'
import { LockKeyhole } from 'lucide-vue-next'
import BaseInput from '@/components/ui/BaseInput.vue'
import { Button } from '@/components/ui/button'
import { useAuthStore } from '@/stores/auth.store'

const router = useRouter()
const authStore = useAuthStore()

const successMessage = ref('')

const form = reactive({
  currentPassword: '',
  newPassword: '',
  confirmNewPassword: '',
})

const passwordMismatch = computed(
  () => form.newPassword.length > 0 && form.newPassword !== form.confirmNewPassword
)

const onSubmit = async () => {
  if (passwordMismatch.value) {
    return
  }

  successMessage.value = ''

  try {
    await authStore.changePassword({
      oldPassword: form.currentPassword,
      newPassword: form.newPassword,
    })

    successMessage.value = 'Password updated successfully.'
    form.currentPassword = ''
    form.newPassword = ''
    form.confirmNewPassword = ''
  } catch {
    // The store sets a user-facing error message.
  }
}

const goBack = async () => {
  await router.push('/settings')
}
</script>

<template>
  <section class="mx-auto max-w-2xl p-4 md:p-8">
    <div class="rounded-xl border border-border bg-card p-6 shadow-panel md:p-8">
      <div class="flex items-start justify-between gap-4">
        <div>
          <p class="text-[10px] font-bold uppercase tracking-[0.3em] text-primary/60">
            Account Security
          </p>
          <h1
            class="mt-2 text-3xl font-extrabold tracking-tighter text-foreground uppercase"
          >
            Change Password
          </h1>
          <p class="mt-2 text-sm text-muted-foreground">
            Keep your account secure by updating your password regularly.
          </p>
        </div>
        <div
          class="hidden h-11 w-11 items-center justify-center rounded-lg border border-border bg-muted text-primary sm:flex"
        >
          <LockKeyhole class="h-5 w-5" />
        </div>
      </div>

      <form class="mt-8 space-y-5" @submit.prevent="onSubmit">
        <BaseInput
          id="currentPassword"
          v-model="form.currentPassword"
          type="password"
          label="Current password"
          autocomplete="current-password"
          :icon="LockKeyhole"
        />

        <BaseInput
          id="newPassword"
          v-model="form.newPassword"
          type="password"
          label="New password"
          autocomplete="new-password"
          hint="Use a strong password with mixed characters and avoid reusing old passwords."
          :icon="LockKeyhole"
        />

        <BaseInput
          id="confirmNewPassword"
          v-model="form.confirmNewPassword"
          type="password"
          label="Confirm new password"
          autocomplete="new-password"
          :error="passwordMismatch ? 'Passwords do not match.' : ''"
          :icon="LockKeyhole"
        />

        <p
          v-if="successMessage"
          class="rounded-lg border border-emerald-200 bg-emerald-50 p-4 text-xs font-bold uppercase tracking-wider text-emerald-700"
        >
          {{ successMessage }}
        </p>
        <p
          v-if="authStore.errorMessage"
          class="text-xs font-bold uppercase tracking-wider text-rose-600"
        >
          {{ authStore.errorMessage }}
        </p>

        <div class="flex flex-col gap-3 sm:flex-row">
          <Button
            type="submit"
            class="h-12 flex-1 gap-2 bg-primary text-primary-foreground hover:bg-primary/90 uppercase text-xs font-bold tracking-widest transition-all active:scale-95 shadow-lg"
            :disabled="authStore.loading || passwordMismatch"
          >
            <LockKeyhole class="h-4 w-4" />
            {{ authStore.loading ? 'Saving...' : 'Update Password' }}
          </Button>

          <Button type="button" variant="outline" class="h-12 flex-1" @click="goBack">
            Back to settings
          </Button>
        </div>
      </form>
    </div>
  </section>
</template>

<style scoped>
:deep(label) {
  @apply text-[10px] font-bold uppercase tracking-widest text-slate-400 mb-1.5;
}
:deep(input) {
  @apply rounded-lg border-slate-200 focus:ring-primary/10 focus:border-primary transition-all;
}
</style>
