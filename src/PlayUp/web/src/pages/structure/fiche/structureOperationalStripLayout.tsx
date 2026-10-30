import type { ReactElement, ReactNode } from 'react';
import { Fragment, cloneElement, createElement, isValidElement } from 'react';

import { GradientSeparator } from '../../../design-system/components/GradientSeparator';

export function withStripChildKey(section: ReactNode, key: string): ReactNode {
  if (!section) {
    return null;
  }
  if (isValidElement(section) && section.key == null) {
    return cloneElement(section as ReactElement, { key });
  }
  if (isValidElement(section)) {
    return section;
  }
  return createElement(Fragment, { key }, section);
}

/** Composition column — Places then Draw, horizontal separator only (no vertical sibling grid). */
export function joinCompositionPlaceAssignmentSections(
  places: ReactNode,
  draw: ReactNode,
  options: { showPlaces: boolean; showDraw: boolean },
): ReactNode[] {
  const joined: ReactNode[] = [];
  if (options.showPlaces && places) {
    joined.push(withStripChildKey(places, 'places'));
  }
  if (options.showPlaces && places && options.showDraw && draw) {
    joined.push(
      <GradientSeparator
        key="composition-tirage-separator"
        orientation="horizontal"
        className="structure-operational-strip__composition-tirage-separator"
      />,
    );
  }
  if (options.showDraw && draw) {
    joined.push(withStripChildKey(draw, 'draw'));
  }
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
