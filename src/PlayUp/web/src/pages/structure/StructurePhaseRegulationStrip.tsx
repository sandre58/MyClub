import { Fragment, type ReactNode } from 'react';
import { useTranslation } from 'react-i18next';

import { Chip } from '../../design-system/components/Chip';
import { GradientSeparator } from '../../design-system/components/GradientSeparator';
import { Status } from '../../design-system/components/Status';
import { TextLink } from '../../design-system/components/TextLink';
import { Tooltip } from '../../design-system/components/Tooltip';
import {
  ConfrontationIcon,
  MatchRulesIcon,
  PencilIcon,
  StandingRulesIcon,
} from '../../design-system/icons/contentIcons';
import type { StructureStageHubSummary } from '../../types';
import { statusToneFromBadgeTone } from '../../design-system/components/statusTone';
import {
  buildConfrontationDigestTokens,
  buildMatchDigestTokens,
  buildStandingDigestTokens,
  RegulationDigestFacts,
} from './structureRegulationDigest';

const compactIcon =
  'ds-btn ds-btn--ghost ds-icon-button ds-icon-button--compact';

function RegulationSectionActions({
  editLabel,
  onEdit,
  badge,
}: {
  editLabel: string;
  onEdit?: () => void;
  badge?: ReactNode;
}) {
  if (!badge && !onEdit) {
    return null;
  }

  return (
    <div className="structure-regulation-digest__actions">
      {badge ? (
        <span className="structure-regulation-strip__binding">{badge}</span>
      ) : null}
      {onEdit ? (
        <Tooltip content={editLabel}>
          <button
            type="button"
            className={compactIcon}
            aria-label={editLabel}
            onClick={onEdit}
          >
            <PencilIcon size="sm" />
          </button>
        </Tooltip>
      ) : null}
    </div>
  );
}

function RegulationDigestSection({
  id,
  sectionId,
  title,
  icon,
  editLabel,
  onEdit,
  badge,
  tokens,
  factsAriaLabel,
  issues,
}: {
  id: string;
  sectionId: string;
  title: string;
  icon: ReactNode;
  editLabel: string;
  onEdit?: () => void;
  badge?: ReactNode;
  tokens: ReturnType<typeof buildMatchDigestTokens>;
  factsAriaLabel: string;
  issues?: ReactNode;
}) {
  return (
    <article
      id={sectionId}
      className="structure-regulation-digest__section"
      aria-labelledby={id}
    >
      <div className="structure-regulation-digest__hero-icon" aria-hidden="true">
        {icon}
      </div>
      <div className="structure-regulation-digest__body">
        <div className="structure-regulation-digest__header">
          <h3 id={id} className="structure-regulation-digest__title">
            {title}
          </h3>
          <RegulationSectionActions
            editLabel={editLabel}
            onEdit={onEdit}
            badge={badge}
          />
        </div>
        <RegulationDigestFacts tokens={tokens} ariaLabel={factsAriaLabel} />
        {issues}
      </div>
    </article>
  );
}

function RegulationDigestStrip({ children }: { children: ReactNode[] }) {
  return (
    <div className="structure-regulation-digest">
      {children.map((child, index) => (
        <Fragment key={child.key ?? index}>
          {index > 0 ? (
            <GradientSeparator
              orientation="vertical"
              className="structure-regulation-digest__sep"
            />
          ) : null}
          {child}
        </Fragment>
      ))}
    </div>
  );
}

export function StructurePhaseRegulationStrip({
  stage,
  regulationHref,
  matchBound,
  standingBound,
  showStanding,
  showConfrontation,
  canEditMatch,
  canRebind,
  canEditStanding,
  canEditTie,
  onEditMatch,
  onEditStanding,
  onEditConfrontation,
  standingIssues,
  confrontationIssues,
}: {
  stage: StructureStageHubSummary;
  regulationHref: string;
  matchBound: boolean;
  standingBound: boolean | null;
  showStanding: boolean;
  showConfrontation: boolean;
  canEditMatch: boolean;
  canRebind: boolean;
  canEditStanding: boolean;
  canEditTie: boolean;
  onEditMatch: () => void;
  onEditStanding: () => void;
  onEditConfrontation: () => void;
  standingIssues?: ReactNode;
  confrontationIssues?: ReactNode;
}) {
  const { t } = useTranslation('structure');
  const { t: tReg } = useTranslation('regulation');

  const matchBadge = matchBound ? (
    <Chip tone="neutral">{t('fiche.binding.general')}</Chip>
  ) : (
    <Status density="compact" tone={statusToneFromBadgeTone('warn')}>
      {t('fiche.binding.personalized')}
    </Status>
  );

  const standingBadge =
    standingBound === null
      ? null
      : standingBound ? (
          <Chip tone="neutral">{t('fiche.binding.general')}</Chip>
        ) : (
          <Status density="compact" tone={statusToneFromBadgeTone('warn')}>
            {t('fiche.binding.personalized')}
          </Status>
        );

  const sections: ReactNode[] = [
    <RegulationDigestSection
      key="match"
      id="regulation-match-heading"
      sectionId="tile-match"
      title={t('fiche.tiles.match')}
      icon={<MatchRulesIcon size="lg" />}
      editLabel={t('fiche.edit')}
      onEdit={canEditMatch || canRebind ? onEditMatch : undefined}
      badge={matchBadge}
      tokens={buildMatchDigestTokens(stage, tReg)}
      factsAriaLabel={tReg('matchTimeline.aria')}
    />,
  ];

  if (showStanding) {
    sections.push(
      <RegulationDigestSection
        key="standing"
        id="regulation-standing-heading"
        sectionId="tile-standing"
        title={t('fiche.tiles.standing')}
        icon={<StandingRulesIcon size="lg" />}
        editLabel={t('fiche.editStanding')}
        onEdit={
          canEditStanding || canRebind ? onEditStanding : undefined
        }
        badge={standingBadge ?? undefined}
        tokens={buildStandingDigestTokens(stage, tReg)}
        factsAriaLabel={tReg('points.aria')}
        issues={standingIssues}
      />,
    );
  }

  if (showConfrontation) {
    sections.push(
      <RegulationDigestSection
        key="confrontation"
        id="regulation-confrontation-heading"
        sectionId="tile-confrontation"
        title={t('fiche.tiles.confrontation')}
        icon={<ConfrontationIcon size="lg" />}
        editLabel={t('fiche.edit')}
        onEdit={canEditTie ? onEditConfrontation : undefined}
        tokens={buildConfrontationDigestTokens(stage, tReg)}
        factsAriaLabel={t('fiche.tiles.confrontation')}
        issues={confrontationIssues}
      />,
    );
  }

  return (
    <div
      className="structure-phase-regulation-bar"
      aria-label={t('fiche.section.regulationFrame')}
    >
      <GradientSeparator
        orientation="horizontal"
        className="structure-phase-regulation__top-sep"
      />
      <RegulationDigestStrip>{sections}</RegulationDigestStrip>
      <div className="structure-regulation-strip__bar-footer">
        <TextLink to={regulationHref}>
          {t('hub.overview.openRegulation')}
        </TextLink>
      </div>
    </div>
  );
}
