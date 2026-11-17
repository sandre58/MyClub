import { expect, type Page } from '@playwright/test';

export type CupSlotTextViolation = {
  address?: string;
  issues: string[];
};

export async function collectCupSlotTextViolations(
  page: Page,
): Promise<CupSlotTextViolation[]> {
  return page.locator('.schematic-cup .schematic-slot').evaluateAll((slots) => {
    const textSelectors = [
      '.schematic-slot__address',
      '.schematic-slot__primary',
      '.schematic-slot__secondary',
    ];

    function contentBox(el: Element) {
      const rect = el.getBoundingClientRect();
      const cs = getComputedStyle(el);
      const padTop = parseFloat(cs.paddingTop);
      const padBottom = parseFloat(cs.paddingBottom);
      return { top: rect.top + padTop, bottom: rect.bottom - padBottom };
    }

    const violations: { address?: string; issues: string[] }[] = [];

    for (const slot of slots) {
      const cb = contentBox(slot);
      const issues: string[] = [];

      for (const sel of textSelectors) {
        const el = slot.querySelector(sel);
        if (!el) {
          continue;
        }
        const textRect = el.getBoundingClientRect();
        if (el.scrollHeight > el.clientHeight + 1) {
          issues.push(`${sel}:overflow(${el.scrollHeight}>${el.clientHeight})`);
        }
        if (textRect.top < cb.top - 0.5) {
          issues.push(`${sel}:top(${textRect.top - cb.top})`);
        }
        if (textRect.bottom > cb.bottom + 0.5) {
          issues.push(`${sel}:bottom(${textRect.bottom - cb.bottom})`);
        }
      }

      if (issues.length > 0) {
        violations.push({
          address:
            slot
              .querySelector('.schematic-slot__address')
              ?.textContent?.trim() ?? undefined,
          issues,
        });
      }
    }

    return violations;
  });
}

export async function expectCupSlotsTextVisible(page: Page): Promise<void> {
  const violations = await collectCupSlotTextViolations(page);
  expect(violations, JSON.stringify(violations, null, 2)).toEqual([]);
}
