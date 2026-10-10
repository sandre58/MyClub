import type { ReactNode } from 'react';
import { createElement } from 'react';

export function joinOperationalStripSections(sections: ReactNode[]): ReactNode[] {
  const visible = sections.filter(Boolean);
  const joined: ReactNode[] = [];
  visible.forEach((section, index) => {
    if (index > 0) {
      joined.push(
        createElement('div', {
          key: `operational-strip-sep-${index}`,
          className: 'structure-operational-strip__separator',
          'aria-hidden': 'true',
        }),
      );
    }
    joined.push(section);
  });
  return joined;
}

export function operationalStripGridColumns(sectionCount: number): string {
  if (sectionCount <= 1) {
    return 'minmax(0, 1fr)';
  }
  const parts: string[] = [];
  for (let index = 0; index < sectionCount; index += 1) {
    if (index > 0) {
      parts.push('auto');
    }
    parts.push('minmax(0, 1fr)');
  }
  return parts.join(' ');
}
