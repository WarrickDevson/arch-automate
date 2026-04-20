import { computed, ref } from 'vue'

const THEME_KEY = 'arch-automate-theme'
const theme = ref('light')

const applyTheme = (nextTheme) => {
  theme.value = nextTheme
  document.documentElement.setAttribute('data-theme', nextTheme)
  localStorage.setItem(THEME_KEY, nextTheme)
}

export const useTheme = () => {
  const isDark = computed(() => theme.value === 'dark')

  const initTheme = () => {
    const savedTheme = localStorage.getItem(THEME_KEY)
    const prefersDark = window.matchMedia('(prefers-color-scheme: dark)').matches
    applyTheme(savedTheme || (prefersDark ? 'dark' : 'light'))
  }

  const toggleTheme = () => {
    applyTheme(isDark.value ? 'light' : 'dark')
  }

  return {
    theme,
    isDark,
    initTheme,
    toggleTheme,
  }
}
