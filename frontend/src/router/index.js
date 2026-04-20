import { createRouter, createWebHistory } from 'vue-router'
import { useAuthStore } from '@/stores/auth.store'

const router = createRouter({
  history: createWebHistory(),
  routes: [
    {
      path: '/',
      redirect: '/dashboard',
    },
    // AUTH ROUTES
    {
      path: '/auth',
      component: () => import('@/layouts/AuthLayout.vue'),
      children: [
        {
          path: 'login',
          name: 'login',
          component: () => import('@/views/auth/LoginView.vue'),
          meta: {
            breadcrumb: 'Login',
            public: true,
            guestOnly: true,
          },
        },
        {
          path: 'register',
          name: 'register',
          component: () => import('@/views/auth/SignupView.vue'),
          meta: {
            breadcrumb: 'Register',
            public: true,
            guestOnly: true,
          },
        },
        {
          path: 'forgot-password',
          name: 'forgot-password',
          component: () => import('@/views/auth/ForgotPasswordView.vue'),
          meta: {
            breadcrumb: 'Forgot Password',
            public: true,
            guestOnly: true,
          },
        },
        {
          path: 'verify-account',
          name: 'verify-account',
          component: () => import('@/views/auth/VerifyAccountView.vue'),
          meta: {
            breadcrumb: 'Verify Account',
            public: true,
          },
        },
      ],
    },
    // Alias /login and /register to the new auth paths for convenience
    { path: '/login', redirect: '/auth/login' },
    { path: '/register', redirect: '/auth/register' },
    // Supabase auth callback — handles email confirmation and password reset links.
    // Must be outside AuthLayout so the spinner is shown fullscreen.
    {
      path: '/auth/callback',
      name: 'auth-callback',
      component: () => import('@/views/auth/AuthCallbackView.vue'),
      meta: { public: true },
    },
    // ONBOARDING — shown after signup when no profile/tenant exists yet
    {
      path: '/onboarding',
      component: () => import('@/layouts/OnboardingLayout.vue'),
      children: [
        {
          path: '',
          name: 'onboarding',
          component: () => import('@/views/onboarding/OnboardingView.vue'),
          meta: { onboarding: true },
        },
      ],
    },
    // APP ROUTES (Dashboard Only)
    {
      path: '/',
      component: () => import('@/layouts/AppLayout.vue'),
      children: [
        {
          path: 'dashboard',
          name: 'dashboard',
          component: () => import('@/views/dashboard/DashboardView.vue'),
          meta: {
            breadcrumb: 'Dashboard',
          },
        },
        {
          path: 'workbench/:projectId?',
          name: 'workbench',
          component: () => import('@/views/workbench/WorkbenchView.vue'),
          props: true,
          meta: {
            breadcrumb: 'Workbench',
          },
        },
        {
          path: 'stakeholders',
          name: 'stakeholders',
          component: () => import('@/views/stakeholder-registry/StakeholderRegistry.vue'),
          meta: {
            breadcrumb: 'Stakeholders',
          },
        },
        {
          path: 'knowledge',
          name: 'knowledge',
          component: () => import('@/views/knowledge-base/KnowledgeBaseView.vue'),
          meta: {
            breadcrumb: 'Knowledge Base',
          },
        },
        {
          path: 'council-pack',
          name: 'council-pack',
          component: () => import('@/views/council-pack/CouncilPackView.vue'),
          meta: {
            breadcrumb: 'Council Pack',
          },
        },
        {
          path: 'rejections',
          name: 'rejections',
          component: () => import('@/views/rejection-tracker/RejectionTrackerView.vue'),
          meta: {
            breadcrumb: 'Rejection Tracker',
          },
        },
        {
          path: 'settings',
          name: 'settings',
          component: () => import('@/views/settings/SettingsView.vue'),
          meta: {
            breadcrumb: 'Settings',
          },
        },
        {
          path: 'settings/change-password',
          name: 'change-password',
          component: () => import('@/views/auth/ChangePasswordView.vue'),
          meta: {
            breadcrumb: 'Change Password',
          },
        },
      ],
    },
    // CATCH ALL
    {
      path: '/:pathMatch(.*)*',
      redirect: '/dashboard',
    },
  ],
})

router.beforeEach(async (to) => {
  const authStore = useAuthStore()

  if (!authStore.initialized) {
    await authStore.initializeSession()
  }

  // Handle Public Routes (Login)
  if (to.meta.public) {
    if (to.meta.guestOnly && authStore.isAuthenticated) {
      return { path: '/dashboard' }
    }
    return true
  }

  // Handle Protected Routes
  if (!authStore.isAuthenticated) {
    return {
      path: '/auth/login',
      query: { redirect: to.fullPath },
    }
  }

  // Run bootstrapProfile if:
  //  (a) it hasn't run yet this session, OR
  //  (b) it ran but still thinks onboarding is needed — re-check in case
  //      the user completed onboarding in another tab/window
  if (!authStore.profileLoaded || authStore.needsOnboarding) {
    authStore.profileLoaded = false  // force a re-fetch when needsOnboarding is true
    try {
      await authStore.bootstrapProfile()
    } catch {
      return { path: '/auth/login', query: { redirect: to.fullPath } }
    }
  }

  // Redirect to onboarding if no tenant/profile exists yet
  if (authStore.needsOnboarding && !to.meta.onboarding) {
    return { name: 'onboarding' }
  }

  // Prevent already-onboarded users from re-visiting onboarding
  if (!authStore.needsOnboarding && to.meta.onboarding) {
    return { name: 'dashboard' }
  }

  return true
})

export default router
