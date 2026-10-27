import type { ReactNode } from 'react';

import '../pages/structure/structure.css';
import { PageHead } from '../design-system/components/PageHead';
import {
  RoundsStatIcon,
  StructureIcon,
} from '../design-system/icons/contentIcons';
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
  prepareSolo: 'À préparer',
  prepareNamed: 'À préparer · Poules',
  materializeSolo: 'Matchs à créer',
  materializeNamed: 'Matchs à créer · Championnat',
  runDrawNamed: 'Tirage à lancer · Quarts de finale',
  calm: 'Structure prête',
} as const;

const READINESS_COPY_EN = {
  anomaly: 'Issues to address',
  warning: 'Structure incomplete',
  prepareSolo: 'To prepare',
  prepareNamed: 'To prepare · Groups',
  materializeSolo: 'Matches to create',
  materializeNamed: 'Matches to create · League',
  runDrawNamed: 'Draw to run · Quarter-finals',
  calm: 'Structure ready',
  drawLong: 'Draw to run · International open qualification preliminary round',
  materializeLong:
    'Matches to create · Championship preliminary round group stage',
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
        <h1 className="dlab-structure-pagehead__title">
          PageHead — statut global
        </h1>
        <p className="ds-label">
          <code>Status</code> · <code>context</code> · <code>outline</code> ·{' '}
          <code>pill</code> · libellés nominaux (nom de phase si plusieurs
          phases). Vérifier nowrap sur les libellés les plus longs (EN).
        </p>
      </header>

      <section
        className="dlab-structure-pagehead__section"
        aria-labelledby="dlab-structure-ph-fr"
      >
        <h2 id="dlab-structure-ph-fr" className="ds-heading">
          Français
        </h2>
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
            localeLabel="info · PrepareStage (1 phase)"
            title="Structure"
            label={READINESS_COPY_FR.prepareSolo}
            tone="info"
            icon={<InfoIcon size="sm" />}
          />
          <PageHeadSpecimen
            localeLabel="info · PrepareStage (multi)"
            title="Structure"
            label={READINESS_COPY_FR.prepareNamed}
            tone="info"
            icon={<InfoIcon size="sm" />}
          />
          <PageHeadSpecimen
            localeLabel="info · MaterializeMatches (1 phase)"
            title="Structure"
            label={READINESS_COPY_FR.materializeSolo}
            tone="info"
            icon={<InfoIcon size="sm" />}
          />
          <PageHeadSpecimen
            localeLabel="info · RunDraw (multi)"
            title="Structure"
            label={READINESS_COPY_FR.runDrawNamed}
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
        <h2 id="dlab-structure-ph-en" className="ds-heading">
          English
        </h2>
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
            localeLabel="info · PrepareStage (solo)"
            title="Structure"
            label={READINESS_COPY_EN.prepareSolo}
            tone="info"
            icon={<InfoIcon size="sm" />}
          />
          <PageHeadSpecimen
            localeLabel="info · MaterializeMatches (named)"
            title="Structure"
            label={READINESS_COPY_EN.materializeNamed}
            tone="info"
            icon={<InfoIcon size="sm" />}
          />
          <PageHeadSpecimen
            localeLabel="info · RunDraw (named)"
            title="Structure"
            label={READINESS_COPY_EN.runDrawNamed}
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
          Narrow viewport: pill should wrap or truncate per DS{' '}
          <code>Status</code> — no Structure product CSS on the note.
        </p>
        <div className="dlab-structure-pagehead__shape-grid">
          <PageHeadSpecimen
            localeLabel="info · RunDraw (long EN)"
            title="Structure"
            label={READINESS_COPY_EN.drawLong}
            tone="info"
            icon={<InfoIcon size="sm" />}
          />
          <PageHeadSpecimen
            localeLabel="info · MaterializeMatches (long EN)"
            title="Structure"
            label={READINESS_COPY_EN.materializeLong}
            tone="info"
            icon={<InfoIcon size="sm" />}
          />
        </div>
      </section>

      <section
        className="dlab-structure-pagehead__section"
        aria-labelledby="dlab-structure-ph-hero"
      >
        <h2 id="dlab-structure-ph-hero" className="ds-heading">
          Fiche phase — chrome hero (B2 F2)
        </h2>
        <p className="ds-label">
          Groupe centré : stats + icône structure (tooltip « Modifier la
          structure ») juste à droite des stats.
        </p>
        <div className="dlab-structure-pagehead__shape-grid">
          <div className="structure-phase-hero__chrome">
            <div className="structure-phase-hero__chrome-inner">
              <ul
                className="structure-phase-hero__stats"
                aria-label="Statistiques"
              >
                <li className="structure-phase-hero__stat">
                  <span
                    className="structure-phase-hero__stat-icon"
                    aria-hidden="true"
                  >
                    <RoundsStatIcon size="sm" />
                  </span>
                  <span className="structure-phase-hero__stat-value">3</span>
                  <span className="structure-phase-hero__stat-label">
                    tours
                  </span>
                </li>
              </ul>
              <button
                type="button"
                className="ds-btn ds-btn--ghost ds-icon-button ds-icon-button--compact structure-phase-hero__edit-forme"
                aria-label="Modifier la structure"
              >
                <StructureIcon size="sm" />
              </button>
            </div>
          </div>
        </div>
      </section>
    </div>
  );
}
