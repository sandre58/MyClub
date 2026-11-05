import { readFileSync } from 'node:fs';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';
import { describe, expect, it } from 'vitest';

function channel(hex: string, shift: number): number {
  return (Number.parseInt(hex.slice(1), 16) >> shift) & 255;
}

function linear(channelValue: number): number {
  const s = channelValue / 255;
  return s <= 0.04045 ? s / 12.92 : ((s + 0.055) / 1.055) ** 2.4;
}

function luminance(hex: string): number {
  return (
    0.2126 * linear(channel(hex, 16)) +
    0.7152 * linear(channel(hex, 8)) +
    0.0722 * linear(channel(hex, 0))
  );
}

function contrast(foreground: string, background: string): number {
  const lighter = Math.max(luminance(foreground), luminance(background));
  const darker = Math.min(luminance(foreground), luminance(background));
  return (lighter + 0.05) / (darker + 0.05);
}

function token(css: string, name: string): string {
  const match = css.match(new RegExp(`${name}:\\s*(#[0-9a-fA-F]{6})`));
  if (!match?.[1]) {
    throw new Error(`Missing ${name}`);
  }
  return match[1].toLowerCase();
}

describe('Already placed draw affordances', () => {
  it('keeps secondary ink at or above 4.5:1 on surface and canvas', () => {
    const slate = readFileSync(
      join(dirname(fileURLToPath(import.meta.url)), '../palettes/slate.css'),
      'utf8',
    );
    const ink = token(slate, '--primitive-text-secondary');
    const surface = token(slate, '--primitive-surface');
    const canvas = token(slate, '--primitive-canvas');

    expect(contrast(ink, surface)).toBeGreaterThanOrEqual(4.5);
    expect(contrast(ink, canvas)).toBeGreaterThanOrEqual(4.5);
  });
});
