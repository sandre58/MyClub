import { defineConfig, devices } from '@playwright/test';

/**
 * Host-backed E2E — Vite dev server proxies API to a running Play’Up Host.
 * Postgres must be seeded (e.g. DevRunner championship-ready) before tests.
 */
export default defineConfig({
  testDir: './e2e/business',
  fullyParallel: false,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 1 : 0,
  workers: 1,
  timeout: 60_000,
  expect: { timeout: 15_000 },
  reporter: process.env.CI ? [['github'], ['list']] : 'list',
  use: {
    baseURL: 'http://127.0.0.1:5173',
    locale: 'fr-FR',
    trace: 'on-first-retry',
  },
  projects: [{ name: 'chromium', use: { ...devices['Desktop Chrome'] } }],
  webServer: {
    command: 'npm run dev -- --host 127.0.0.1 --port 5173',
    url: 'http://127.0.0.1:5173',
    reuseExistingServer: !process.env.CI,
    timeout: 120_000,
    env: {
      VITE_API_PROXY_TARGET:
        process.env.VITE_API_PROXY_TARGET ?? 'http://127.0.0.1:5287',
    },
  },
});
