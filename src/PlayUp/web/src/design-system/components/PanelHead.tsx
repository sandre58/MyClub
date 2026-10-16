import type { ReactNode } from 'react';
import { Chip } from './Chip';

export type PanelHeadDensity = 'default' | 'inset' | 'tile';
export type PanelHeadMetaVariant = 'count' | 'eyebrow' | 'badge';
export type PanelHeadTitleStyle = 'default' | 'section';

/**
 * Panel title row — optional icon, meta (count or eyebrow), trailing actions.
 * `default` density preserves legacy panel styling for non-Structure consumers.
 */
export function PanelHead({
  title,
  aside,
  metaVariant = 'eyebrow',
  icon,
  leading,
  /** Hub background refetch: spinner in the icon slot (same footprint as the icon). */
  iconRefreshing = false,
  actions,
  id,
  density = 'default',
  titleStyle = 'default',
  className,
}: {
  title: string;
  aside?: ReactNode;
  /** How to render `aside` — badge for composition counts, eyebrow for legacy meta. */
  metaVariant?: PanelHeadMetaVariant;
  /** Uppercase secondary label (composition band sections). */
  titleStyle?: PanelHeadTitleStyle;
  icon?: ReactNode;
  /** Alias for `icon` (leading slot). */
  leading?: ReactNode;
  iconRefreshing?: boolean;
  actions?: ReactNode;
  /** For aria-labelledby on parent section. */
  id?: string;
  density?: PanelHeadDensity;
  className?: string;
}) {
  const lead = leading ?? icon;
  const densityClass =
    density === 'default'
      ? null
      : `ds-panel-head--${density}`;

  const metaClass =
    metaVariant === 'count' ? 'ds-panel-head__meta--count' : 'ds-eyebrow';

  const titleTextClass = [
    'ds-panel-head__title-text',
    titleStyle === 'section' ? 'ds-panel-head__title-text--section' : null,
  ]
    .filter(Boolean)
    .join(' ');

  return (
    <div
      className={['ds-panel-head', densityClass, className]
        .filter(Boolean)
        .join(' ')}
    >
      <h3 id={id} className="ds-panel-head__title">
        {lead ? (
          iconRefreshing ? (
            <span
              className="ds-panel-head__icon ds-panel-head__icon--refreshing"
              aria-hidden="true"
            >
              <span className="ds-spinner ds-panel-head__icon-spinner" />
            </span>
          ) : (
            <span className="ds-panel-head__icon" aria-hidden="true">
              {lead}
            </span>
          )
        ) : null}
        <span className={titleTextClass}>{title}</span>
      </h3>
      {aside != null || actions ? (
        <div className="ds-panel-head__trail">
          {aside != null ? (
            metaVariant === 'badge' ? (
              <Chip tone="accent" className="ds-panel-head__meta-badge">
                {aside}
              </Chip>
            ) : (
              <span className={metaClass}>{aside}</span>
            )
          ) : null}
          {actions ? (
            <div className="ds-panel-head__actions ds-icon-toolbar">{actions}</div>
          ) : null}
        </div>
      ) : null}
    </div>
  );
}
