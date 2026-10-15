import type { ReactNode } from 'react';
import { useTranslation } from 'react-i18next';

import { Chip } from '../../design-system/components/Chip';
import { Status } from '../../design-system/components/Status';
import { TextLink } from '../../design-system/components/TextLink';
import { Tooltip } from '../../design-system/components/Tooltip';
import {
  ConfrontationIcon,
  MatchRulesIcon,
  PencilIcon,
  StandingRulesIcon,
} from '../../design-system/icons/contentIcons';
import {
  MatchRulesStripPanel,
  StandingRulesStripPanel,
} from '../regulation/regulationRulePanels';
import type { StructureStageHubSummary } from '../../types';
import { statusToneFromBadgeTone } from '../../design-system/components/statusTone';
import { ConfrontationPanel } from './StructurePhaseRails';
import {
  joinOperationalStripSections,
  operationalStripGridColumns,
} from './structureOperationalStripLayout';
import { StripSection } from './structureOperationalStripSection';

const compactIcon =
  'ds-btn ds-btn--ghost ds-icon-button ds-icon-button--compact';

function RegulationEditToolbar({
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
    <div className="structure-regulation-strip__toolbar">
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

  type SectionKind = 'match' | 'standing' | 'confrontation';
  const visible: SectionKind[] = [
    'match',
    ...(showStanding ? (['standing'] as const) : []),
    ...(showConfrontation ? (['confrontation'] as const) : []),
  ];

  const sectionIndex = (kind: SectionKind) => visible.indexOf(kind) + 1;

  const matchSection = (
    <StripSection
      key="match"
      sectionId="tile-match"
      idPrefix="regulation-strip"
      index={sectionIndex('match')}
      className="structure-regulation-strip__section"
      title={t('fiche.tiles.match')}
      titleIcon={<MatchRulesIcon size="sm" />}
      toolbar={
        <RegulationEditToolbar
          editLabel={t('fiche.edit')}
          onEdit={
            canEditMatch || canRebind ? onEditMatch : undefined
          }
          badge={matchBadge}
        />
      }
    >
      <MatchRulesStripPanel
        numberOfPeriods={stage.numberOfPeriods}
        durationPerPeriod={stage.durationPerPeriod}
        halfTimeDuration={stage.halfTimeDuration}
        hasExtraTime={stage.hasExtraTime}
        extraTimeNumberOfPeriods={stage.extraTimeNumberOfPeriods}
        extraTimeDurationPerPeriod={stage.extraTimeDurationPerPeriod}
        hasPenaltyShootout={stage.hasPenaltyShootout}
        penaltyInitialKicksPerTeam={stage.penaltyInitialKicksPerTeam}
      />
    </StripSection>
  );

  const standingSection = showStanding ? (
    <StripSection
      key="standing"
      sectionId="tile-standing"
      idPrefix="regulation-strip"
      index={sectionIndex('standing')}
      className="structure-regulation-strip__section"
      title={t('fiche.tiles.standing')}
      titleIcon={<StandingRulesIcon size="sm" />}
      toolbar={
        <RegulationEditToolbar
          editLabel={t('fiche.editStanding')}
          onEdit={
            canEditStanding || canRebind ? onEditStanding : undefined
          }
          badge={standingBadge ?? undefined}
        />
      }
    >
      <StandingRulesStripPanel
        winPoints={stage.winPoints ?? 0}
        drawPoints={stage.drawPoints ?? 0}
        lossPoints={stage.lossPoints ?? 0}
        rankingCriteria={stage.rankingCriteria}
        forfeitWinnerGoals={stage.forfeitWinnerGoals}
        forfeitLoserGoals={stage.forfeitLoserGoals}
      />
      {standingIssues}
    </StripSection>
  ) : null;

  const confrontationSection = showConfrontation ? (
    <StripSection
      key="confrontation"
      sectionId="tile-confrontation"
      idPrefix="regulation-strip"
      index={sectionIndex('confrontation')}
      className="structure-regulation-strip__section"
      title={t('fiche.tiles.confrontation')}
      titleIcon={<ConfrontationIcon size="sm" />}
      toolbar={
        <RegulationEditToolbar
          editLabel={t('fiche.edit')}
          onEdit={canEditTie ? onEditConfrontation : undefined}
        />
      }
    >
      <ConfrontationPanel stage={stage} />
      {confrontationIssues}
    </StripSection>
  ) : null;

  const stripSections = joinOperationalStripSections([
    { key: 'match', node: matchSection },
    { key: 'standing', node: standingSection },
    { key: 'confrontation', node: confrontationSection },
  ]);

  return (
    <div className="structure-phase-regulation-bar">
      <div
        className="structure-regulation-divider"
        role="separator"
        aria-label={t('fiche.section.regulationFrame')}
      >
        <span className="structure-regulation-divider__label">
          {t('fiche.regulationDividerLabel')}
        </span>
      </div>
      <section
        className="structure-fiche-section structure-fiche-section--regulation"
        aria-label={t('fiche.section.regulationFrame')}
      >
        <div
          className="structure-operational-strip structure-operational-strip--regulation-readout"
          data-section-count={visible.length}
          style={{
            gridTemplateColumns: operationalStripGridColumns(visible.length),
          }}
        >
          {stripSections}
        </div>
      </section>
      <div className="structure-regulation-strip__bar-footer">
        <TextLink to={regulationHref}>{t('hub.overview.openRegulation')}</TextLink>
      </div>
    </div>
  );
}
