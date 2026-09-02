import '@testing-library/jest-dom/vitest'
import { cleanup, configure } from '@testing-library/react'
import { afterEach } from 'vitest'
import '../i18n'
import { I18nTestProvider } from './renderWithI18n'

if (typeof window.matchMedia !== 'function') {
  window.matchMedia = (query: string) => ({
    matches: false,
    media: query,
    onchange: null,
    addListener: () => {},
    removeListener: () => {},
    addEventListener: () => {},
    removeEventListener: () => {},
    dispatchEvent: () => false,
  })
}

configure({
  wrapper: ({ children }) => (
    <I18nTestProvider>{children}</I18nTestProvider>
  ),
})

afterEach(() => {
  cleanup()
})
