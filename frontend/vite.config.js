import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'
import { VitePWA } from 'vite-plugin-pwa'
import { fileURLToPath, URL } from 'node:url'

// https://vitejs.dev/config/
export default defineConfig({
  plugins: [
    vue(),
    VitePWA({
      registerType: 'autoUpdate',
      manifest: {
        name: 'Project Manager Web',
        short_name: 'ProjectManagerWeb',
        theme_color: '#333',
        icons: [
          {
            src: '/logo.svg',
            sizes: '192x192',
            type: 'image/svg+xml'
          }
        ]
      }
    })
  ],
  resolve: {
    alias: {
      '@': fileURLToPath(new URL('./src', import.meta.url))
    }
  },
  build: {
    chunkSizeWarningLimit: 1000,
    rollupOptions: {
      output: {
        manualChunks: {
          // Separar Vue e bibliotecas relacionadas
          'vue-vendor': ['vue', 'vue-router'],
          // Separar Vuetify
          'vuetify-vendor': ['vuetify'],
          // Separar Axios
          'http-vendor': ['axios'],
          // Separar outras bibliotecas grandes se houver
          'vendor': ['@vue/runtime-core', '@vue/runtime-dom', '@vue/reactivity']
        }
      }
    }
  }
})
