import { describe, expect, it } from 'vitest';
import {
  HEX6,
  hexToHsv,
  hexToRgb,
  hsvToHex,
  hsvToRgb,
  hueCss,
  normalizeHex,
  rgbToHex,
  rgbToHsv,
} from './colorMath';

describe('colorMath', () => {
  it('round-trips common hex colours', () => {
    for (const hex of [
      '#000000',
      '#FFFFFF',
      '#FF0000',
      '#00FF00',
      '#0000FF',
      '#295A9E',
    ]) {
      const hsv = hexToHsv(hex);
      expect(hsv).not.toBeNull();
      expect(hsvToHex(hsv!)).toBe(hex);
      expect(rgbToHex(hsvToRgb(hsv!))).toBe(hex);
    }
  });

  it('converts hex ↔ rgb', () => {
    expect(hexToRgb('#295A9E')).toEqual({ r: 41, g: 90, b: 158 });
    expect(rgbToHex({ r: 41, g: 90, b: 158 })).toBe('#295A9E');
  });

  it('keeps hue when parsing near-grey', () => {
    const hsv = hexToHsv('#808080', 210);
    expect(hsv).not.toBeNull();
    expect(hsv!.h).toBe(210);
    expect(hsv!.s).toBeCloseTo(0, 5);
    expect(rgbToHsv({ r: 128, g: 128, b: 128 }, 210).h).toBe(210);
  });

  it('rejects invalid hex', () => {
    expect(hexToHsv('#fff')).toBeNull();
    expect(HEX6.test('#GGGGGG')).toBe(false);
    expect(normalizeHex('#abc')).toBe('#AABBCC');
    expect(normalizeHex('nope')).toBeNull();
  });

  it('exposes pure hue for the S/V panel', () => {
    expect(hueCss(0)).toBe('#FF0000');
    expect(hueCss(120)).toBe('#00FF00');
  });
});
