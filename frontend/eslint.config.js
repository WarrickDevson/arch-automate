import js from '@eslint/js'
import pluginVue from 'eslint-plugin-vue'
import eslintConfigPrettier from 'eslint-config-prettier'

export default [
  {
    ignores: ['dist/**', 'node_modules/**'],
  },
  js.configs.recommended,
  ...pluginVue.configs['flat/recommended'],
  {
    files: ['**/*.{js,vue}'],
    languageOptions: {
      ecmaVersion: 'latest',
      sourceType: 'module',
      globals: {
        window: 'readonly',
        document: 'readonly',
        localStorage: 'readonly',
        URL: 'readonly',
        Blob: 'readonly',
        setTimeout: 'readonly',
        clearTimeout: 'readonly',
        structuredClone: 'readonly',
        import: 'readonly',
        process: 'readonly',
        console: 'readonly',
      },
    },
    rules: {
      'no-console': ['warn', { allow: ['warn', 'error'] }],
      'vue/multi-word-component-names': 'off',
    },
  },
  {
    // Record form sub-components pass `form` as a shared reactive object for
    // direct mutation — intentional parent-controlled form state pattern.
    files: ['**/views/records/components/**/*.vue', '**/components/records/**/*.vue'],
    rules: {
      'vue/no-mutating-props': 'off',
    },
  },
  eslintConfigPrettier,
]
