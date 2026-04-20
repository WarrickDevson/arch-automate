import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'
import { fileURLToPath, URL } from 'node:url'
import { viteStaticCopy } from 'vite-plugin-static-copy'

export default defineConfig({
  plugins: [
    vue(),
    viteStaticCopy({
      targets: [
        {
          src: 'node_modules/web-ifc/*.wasm',
          dest: '.',
        },
      ],
    }),
  ],
  optimizeDeps: {
    exclude: ['web-ifc'],
  },
  server: {
    port: 5289,
    open: true,
  },
  resolve: {
    alias: {
      '@': fileURLToPath(new URL('./src', import.meta.url)),
    },
  },
  build: {
    chunkSizeWarningLimit: 650,
    rollupOptions: {
      output: {
        manualChunks(id) {
          if (id.includes('node_modules/three') || id.includes('node_modules/@thatopen')) {
            return 'vendor-ifc'
          }
          if (id.includes('node_modules/web-ifc')) {
            return 'vendor-web-ifc'
          }
          if (id.includes('node_modules/aws-amplify') || id.includes('node_modules/@aws-amplify')) {
            return 'vendor-amplify'
          }
          if (
            id.includes('node_modules/primevue') ||
            id.includes('node_modules/primeicons') ||
            id.includes('node_modules/@primeuix')
          ) {
            return 'vendor-prime'
          }
          if (id.includes('node_modules/jspdf') || id.includes('node_modules/html2canvas')) {
            return 'vendor-pdf'
          }
          if (id.includes('node_modules/dexie')) {
            return 'vendor-dexie'
          }
          if (
            id.includes('node_modules/reka-ui') ||
            id.includes('node_modules/radix-vue') ||
            id.includes('node_modules/@radix-icons') ||
            id.includes('node_modules/lucide-vue-next')
          ) {
            return 'vendor-ui'
          }
          if (
            id.includes('node_modules/vue') ||
            id.includes('node_modules/pinia') ||
            id.includes('node_modules/@vueuse')
          ) {
            return 'vendor-vue'
          }
        },
      },
    },
  },
})
