import { describe, expect, it } from 'vitest';
import { deriveShortName } from './deriveShortName';

describe('deriveShortName', () => {
  it('uses initials for 2+ space-separated words (max 3)', () => {
    expect(deriveShortName('Paris Saint-Germain')).toBe('PS');
    expect(deriveShortName('Real Madrid')).toBe('RM');
    expect(deriveShortName('Olympique de Marseille')).toBe('ODM');
    expect(deriveShortName('Alpha FC United Extra')).toBe('AFU');
  });

  it('uppercases a single word and truncates to 5', () => {
    expect(deriveShortName('Lyon')).toBe('LYON');
    expect(deriveShortName('PSG')).toBe('PSG');
    expect(deriveShortName('Olympique')).toBe('OLYMP');
    expect(deriveShortName('abcdefghijklmnopqrstuvwxyz')).toBe('ABCDE');
  });

  it('returns empty for blank input', () => {
    expect(deriveShortName('   ')).toBe('');
  });
});
