import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// Dev only: browser calls /competitions|stages|matches on the Vite origin;
// Vite forwards to the ASP.NET Host. No CORS needed in development.
const apiProxyTarget = process.env.VITE_API_PROXY_TARGET ?? 'http://localhost:5287'

export default defineConfig({
  plugins: [react()],
  server: {
    proxy: {
      // 11.3.1 only needed /competitions; multi-view reads need stages + matches too.
      '/competitions': {
        target: apiProxyTarget,
        changeOrigin: true,
      },
      '/stages': {
        target: apiProxyTarget,
        changeOrigin: true,
      },
      '/matches': {
        target: apiProxyTarget,
        changeOrigin: true,
      },
    },
  },
})
