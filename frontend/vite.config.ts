import { defineConfig } from 'vite'
import solid from 'vite-plugin-solid'
import tailwindcss from '@tailwindcss/vite'
import { fileURLToPath, URL } from 'node:url'

export default defineConfig({
  // GitHub Pages serves the app from /<repo>/; the deploy workflow sets VITE_BASE_PATH=/ExamPlatform/.
  base: process.env.VITE_BASE_PATH ?? '/',
  plugins: [solid(), tailwindcss()],
  resolve: {
    alias: {
      '~': fileURLToPath(new URL('./src', import.meta.url)),
    },
  },
  server: {
    port: 5173,
    proxy: {
      // In development the SPA calls /api on its own origin; Vite forwards to the ASP.NET Core API.
      '/api': {
        target: process.env.VITE_API_PROXY_TARGET ?? 'http://localhost:5259',
        changeOrigin: true,
      },
    },
  },
})
