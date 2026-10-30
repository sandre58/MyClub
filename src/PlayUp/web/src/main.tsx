import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import { I18nextProvider } from 'react-i18next';
import { BrowserRouter } from 'react-router-dom';
import App from './App.tsx';
import { mountAfterI18nReady } from './appStartup';
import i18n, { i18nReady } from './i18n';
import { FALLBACK_LOCALE } from './i18n/config';
import './index.css';
import { initTheme } from './theme';

initTheme();

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 30_000,
      retry: false,
    },
  },
});

void mountAfterI18nReady({
  i18nReady,
  changeLanguage: i18n.changeLanguage.bind(i18n),
  fallbackLocale: FALLBACK_LOCALE,
  logError: (message, error) => {
    console.error(message, error);
  },
  render: () => {
    createRoot(document.getElementById('root')!).render(
      <StrictMode>
        <QueryClientProvider client={queryClient}>
          <I18nextProvider i18n={i18n}>
            <BrowserRouter>
              <App />
            </BrowserRouter>
          </I18nextProvider>
        </QueryClientProvider>
      </StrictMode>,
    );
  },
});
