import { defineConfig } from 'vitest/config';
import react from '@vitejs/plugin-react';
import type { ProxyOptions } from 'vite';

// Dev only: browser calls /competitions|stages|matches on the Vite origin;
// Vite forwards to the ASP.NET Host. No CORS needed in development.
const apiProxyTarget =
  process.env.VITE_API_PROXY_TARGET ?? 'http://localhost:5287';

/**
 * Same path prefix serves SPA deep-links (Accept: text/html) and API fetch (JSON).
 * Without this bypass, opening /matches/{id} in the address bar returns Host JSON.
 */
function apiProxy(options?: { spaBypass?: boolean }): ProxyOptions {
  const spaBypass = options?.spaBypass !== false;
  return {
    target: apiProxyTarget,
    changeOrigin: true,
    bypass(req) {
      if (!spaBypass) {
        return undefined;
      }

      const accept = req.headers.accept ?? '';
      if (accept.includes('text/html')) {
        return '/index.html';
      }
    },
  };
}

export default defineConfig({
  plugins: [react()],
  resolve: {
    // Prevent duplicate React when both `react-dom/client` and `react-dom` (createPortal) are imported.
    dedupe: ['react', 'react-dom'],
  },
  optimizeDeps: {
    include: ['react', 'react-dom', 'react-dom/client'],
  },
  server: {
    proxy: {
      // 11.3.1 only needed /competitions; multi-view reads need stages + matches too.
      '/competitions': apiProxy(),
      '/stages': apiProxy(),
      '/matches': apiProxy(),
      // Binary content: never SPA-bypass (Accept may include text/html in some browsers).
      '/media': apiProxy({ spaBypass: false }),
    },
  },
  test: {
    environment: 'jsdom',
    setupFiles: ['./src/test/setup.tsx'],
    css: false,
    exclude: ['**/node_modules/**', '**/dist/**', '**/e2e/**'],
  },
});
