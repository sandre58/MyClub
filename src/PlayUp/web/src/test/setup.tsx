import '@testing-library/jest-dom/vitest'
import { cleanup, configure } from '@testing-library/react'
import { afterEach } from 'vitest'
import '../i18n'
import { I18nTestProvider } from './renderWithI18n'

configure({
  wrapper: ({ children }) => (
    <I18nTestProvider>{children}</I18nTestProvider>
  ),
})

afterEach(() => {
  cleanup()
})
