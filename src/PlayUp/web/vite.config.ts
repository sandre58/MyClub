import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// Dev only: browser calls /competitions/... on the Vite origin;
// Vite forwards to the ASP.NET Host. No CORS needed in development.
const apiProxyTarget = process.env.VITE_API_PROXY_TARGET ?? 'http://localhost:5287'

export default defineConfig({
  plugins: [react()],
  server: {
    proxy: {
      '/competitions': {
        target: apiProxyTarget,
        changeOrigin: true,
      },
    },
  },
})
