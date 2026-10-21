import { render } from '@testing-library/react';
import { afterEach, describe, expect, it } from 'vitest';
import { THEME_STORAGE_KEY } from './config';
import { resetThemeState, setThemePreference } from './setThemePreference';
import { useThemeRoot } from './useThemeRoot';

function ThemeRootProbe() {
  useThemeRoot();
  return <div className="ds-root" data-palette="slate" />;
}

describe('useThemeRoot', () => {
  afterEach(() => {
    window.localStorage.removeItem(THEME_STORAGE_KEY);
    resetThemeState('system');
    document.documentElement.removeAttribute('data-theme');
    document.documentElement.style.colorScheme = '';
  });

  it('re-stamps data-theme after React render', () => {
    setThemePreference('dark');

    const { rerender } = render(<ThemeRootProbe />);
    const root = document.querySelector('.ds-root');

    expect(root?.getAttribute('data-theme')).toBe('dark');

    rerender(<ThemeRootProbe />);
    expect(root?.getAttribute('data-theme')).toBe('dark');
  });
});
