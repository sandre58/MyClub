import type { ReactNode } from 'react';

import { PageHead } from '../design-system/components/PageHead';
import { Status, type StatusTone } from '../design-system/components/Status';
import {
  CheckIcon,
  InfoIcon,
  OverviewAttentionIcon,
  StructureIssueIcon,
} from '../design-system/icons/contentIcons';

const READINESS_COPY_FR = {
  anomaly: 'Anomalies à traiter',
  warning: 'Structure incomplète',
  prepare: 'Phase à préparer · Poules',
  materialize: 'Matchs à créer · Championnat',
  runDraw: 'Tirage à lancer · Quarts de finale',
  calm: 'Structure prête',
} as const;

const READINESS_COPY_EN = {
  anomaly: 'Issues to address',
  warning: 'Structure incomplete',
  prepare: 'Phase to prepare · Groups',
  materialize: 'Matches to create · League',
  runDraw: 'Draw to run · Quarter-finals',
  calm: 'Structure ready',
  /** Longest nominal info labels — nowrap / overflow QA */
  scheduleLong:
    'Schedule to build · Championship preliminary round group stage',
  swissLong:
    'Swiss round to generate · International open qualification stage',
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
          <code>pill</code> · libellés nominaux (phase nommée sur info). Vérifier
          nowrap sur les libellés les plus longs (EN).
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
            localeLabel="info · PrepareStage"
            title="Structure"
            label={READINESS_COPY_FR.prepare}
            tone="info"
            icon={<InfoIcon size="sm" />}
          />
          <PageHeadSpecimen
            localeLabel="info · MaterializeMatches"
            title="Structure"
            label={READINESS_COPY_FR.materialize}
            tone="info"
            icon={<InfoIcon size="sm" />}
          />
          <PageHeadSpecimen
            localeLabel="info · RunDraw"
            title="Structure"
            label={READINESS_COPY_FR.runDraw}
            tone="info"
            icon={<InfoIcon size="sm" />}
          />
          <PageHeadSpecimen
            localeLabel="success · calme"
            title="Structure"
            label={READINESS_COPY_FR.calm}
            tone="success"
            icon={<CheckIcon size="sm" />}
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
            localeLabel="info · PrepareStage"
            title="Structure"
            label={READINESS_COPY_EN.prepare}
            tone="info"
            icon={<InfoIcon size="sm" />}
          />
          <PageHeadSpecimen
            localeLabel="info · MaterializeMatches"
            title="Structure"
            label={READINESS_COPY_EN.materialize}
            tone="info"
            icon={<InfoIcon size="sm" />}
          />
          <PageHeadSpecimen
            localeLabel="info · RunDraw"
            title="Structure"
            label={READINESS_COPY_EN.runDraw}
            tone="info"
            icon={<InfoIcon size="sm" />}
          />
          <PageHeadSpecimen
            localeLabel="success · calm"
            title="Structure"
            label={READINESS_COPY_EN.calm}
            tone="success"
            icon={<CheckIcon size="sm" />}
          />
        </div>
      </section>

      <section
        className="dlab-structure-pagehead__section"
        aria-labelledby="dlab-structure-ph-responsive"
      >
        <h2 id="dlab-structure-ph-responsive" className="ds-heading">
          Responsive · long labels (EN)
        </h2>
        <p className="ds-label">
          Narrow viewport: pill should wrap or truncate per DS <code>Status</code>{' '}
          — no Structure product CSS on the note.
        </p>
        <div className="dlab-structure-pagehead__shape-grid">
          <PageHeadSpecimen
            localeLabel="info · BuildSchedule (long)"
            title="Structure"
            label={READINESS_COPY_EN.scheduleLong}
            tone="info"
            icon={<InfoIcon size="sm" />}
          />
          <PageHeadSpecimen
            localeLabel="info · RunSwissRounds (long)"
            title="Structure"
            label={READINESS_COPY_EN.swissLong}
            tone="info"
            icon={<InfoIcon size="sm" />}
          />
        </div>
      </section>
    </div>
  );
}
