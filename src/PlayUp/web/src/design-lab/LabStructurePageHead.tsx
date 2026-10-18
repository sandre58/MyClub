import type { ReactNode } from 'react';

import { PageHead } from '../design-system/components/PageHead';
import { Status, type StatusTone } from '../design-system/components/Status';
import {
  InfoIcon,
  OverviewAttentionIcon,
  StructureIssueIcon,
} from '../design-system/icons/contentIcons';

const READINESS_COPY_FR = {
  anomaly: 'Anomalies à traiter',
  warning: 'Structure incomplète',
  readyMatches: 'Prête pour les matchs',
  ready: 'Structure prête',
} as const;

const READINESS_COPY_EN = {
  anomaly: 'Issues to address',
  warning: 'Structure incomplete',
  readyMatches: 'Ready for matches',
  ready: 'Structure ready',
} as const;

function ReadinessPill({
  tone,
  icon,
  children,
}: {
  tone: StatusTone;
  icon: ReactNode;
  children: ReactNode;
}) {
  return (
    <Status
      density="context"
      variant="outline"
      shape="pill"
      tone={tone}
      role="status"
    >
      <span className="ds-status-inline">
        <span className="ds-status-inline__icon" aria-hidden="true">
          {icon}
        </span>
        <span>{children}</span>
      </span>
    </Status>
  );
}

function PageHeadSpecimen({
  title,
  label,
  tone,
  icon,
  localeLabel,
}: {
  title: string;
  label: string;
  tone: StatusTone;
  icon: ReactNode;
  localeLabel?: string;
}) {
  return (
    <div className="dlab-structure-pagehead__specimen">
      {localeLabel ? (
        <p className="dlab-structure-pagehead__shape-caption">{localeLabel}</p>
      ) : null}
      <PageHead
        title={title}
        note={
          <ReadinessPill tone={tone} icon={icon}>
            {label}
          </ReadinessPill>
        }
      />
    </div>
  );
}

/**
 * Design Lab — Structure hub PageHead readiness pills (product mirror).
 */
export function LabStructurePageHead() {
  return (
    <div className="dlab-structure-pagehead">
      <header className="dlab-structure-pagehead__intro">
        <p className="ds-eyebrow">Compétition · Structure N1</p>
        <h1 className="dlab-structure-pagehead__title">PageHead — statut global</h1>
        <p className="ds-label">
          <code>Status</code> · <code>context</code> · <code>outline</code> ·{' '}
          <code>pill</code> · libellés nominaux courts (pas de pause, pas de
          phrase). Vérifier nowrap sur les libellés les plus longs (EN).
        </p>
      </header>

      <section
        className="dlab-structure-pagehead__section"
        aria-labelledby="dlab-structure-ph-fr"
      >
        <h2 id="dlab-structure-ph-fr" className="ds-heading">Français</h2>
        <div className="dlab-structure-pagehead__stack">
          <PageHeadSpecimen
            localeLabel="error"
            title="Structure"
            label={READINESS_COPY_FR.anomaly}
            tone="error"
            icon={<StructureIssueIcon size="sm" />}
          />
          <PageHeadSpecimen
            localeLabel="attention"
            title="Structure"
            label={READINESS_COPY_FR.warning}
            tone="attention"
            icon={<OverviewAttentionIcon size="sm" />}
          />
          <PageHeadSpecimen
            localeLabel="info · matérialisation"
            title="Structure"
            label={READINESS_COPY_FR.readyMatches}
            tone="info"
            icon={<InfoIcon size="sm" />}
          />
          <PageHeadSpecimen
            localeLabel="info · prochaine étape"
            title="Structure"
            label={READINESS_COPY_FR.ready}
            tone="info"
            icon={<InfoIcon size="sm" />}
          />
        </div>
      </section>

      <section
        className="dlab-structure-pagehead__section"
        aria-labelledby="dlab-structure-ph-en"
      >
        <h2 id="dlab-structure-ph-en" className="ds-heading">English</h2>
        <div className="dlab-structure-pagehead__stack">
          <PageHeadSpecimen
            localeLabel="error"
            title="Structure"
            label={READINESS_COPY_EN.anomaly}
            tone="error"
            icon={<StructureIssueIcon size="sm" />}
          />
          <PageHeadSpecimen
            localeLabel="attention"
            title="Structure"
            label={READINESS_COPY_EN.warning}
            tone="attention"
            icon={<OverviewAttentionIcon size="sm" />}
          />
          <PageHeadSpecimen
            localeLabel="info · materialization"
            title="Structure"
            label={READINESS_COPY_EN.readyMatches}
            tone="info"
            icon={<InfoIcon size="sm" />}
          />
          <PageHeadSpecimen
            localeLabel="info · next slice"
            title="Structure"
            label={READINESS_COPY_EN.ready}
            tone="info"
            icon={<InfoIcon size="sm" />}
          />
        </div>
      </section>
    </div>
  );
}
