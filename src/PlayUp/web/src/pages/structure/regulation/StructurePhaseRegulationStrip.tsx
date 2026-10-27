import {
  Children,
  Fragment,
  isValidElement,
  type ReactElement,
  type ReactNode,
} from 'react';
import { useTranslation } from 'react-i18next';

import { FormSection } from '../../../design-system/components/FormSection';
import { GradientSeparator } from '../../../design-system/components/GradientSeparator';
import { TextLink } from '../../../design-system/components/TextLink';
import { Tooltip } from '../../../design-system/components/Tooltip';
import {
  ConfrontationIcon,
  MatchRulesIcon,
  PencilIcon,
  StandingRulesIcon,
} from '../../../design-system/icons/contentIcons';
import type { StructureStageHubSummary } from '../../../types';
import { regulationFamilyBindingHintKey } from '../../regulation/regulationBindingHints';
import {
  buildConfrontationDigestTokens,
  buildMatchDigestTokens,
  buildStandingDigestTokens,
  confrontationFormatScopeHintKey,
} from './structureRegulationDigest.build';
import { RegulationDigestFacts } from './structureRegulationDigest';

const compactIcon =
  'ds-btn ds-btn--ghost ds-icon-button ds-icon-button--compact';

function RegulationEditButton({
  editLabel,
  onEdit,
}: {
  editLabel: string;
  onEdit: () => void;
}) {
  return (
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
  );
}

function RegulationDigestSection({
  sectionId,
  titleId,
  title,
  icon,
  editLabel,
  onEdit,
  bindingHint,
  bindingPhaseSpecific,
  tokens,
  factsAriaLabel,
  issues,
}: {
  sectionId: string;
  titleId: string;
  title: string;
  icon: ReactNode;
  editLabel: string;
  onEdit?: () => void;
  bindingHint?: string;
  bindingPhaseSpecific?: boolean;
  tokens: ReturnType<typeof buildMatchDigestTokens>;
  factsAriaLabel: string;
  issues?: ReactNode;
}) {
  const bindingHintClass = [
    'structure-regulation-digest__binding-hint',
    bindingPhaseSpecific
      ? 'structure-regulation-digest__binding-hint--phase'
      : null,
  ]
    .filter(Boolean)
    .join(' ');

  return (
    <article
      id={sectionId}
      className="structure-regulation-digest__section"
      aria-labelledby={`${titleId}-title`}
    >
      <FormSection
        id={titleId}
        variant="compact"
        title={title}
        description={bindingHint}
        descriptionClassName={bindingHint ? bindingHintClass : undefined}
        icon={icon}
        toolbar={
          onEdit ? (
            <RegulationEditButton editLabel={editLabel} onEdit={onEdit} />
          ) : undefined
        }
      >
        <RegulationDigestFacts tokens={tokens} ariaLabel={factsAriaLabel} />
        {issues}
      </FormSection>
    </article>
  );
}

function RegulationDigestStrip({ children }: { children: ReactNode }) {
  const sections = Children.toArray(children).filter(
    isValidElement,
  ) as ReactElement[];
  return (
    <div className="structure-regulation-digest">
      {sections.map((child, index) => (
        <Fragment key={child.key ?? `regulation-digest-${index}`}>
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

  const matchHint = tReg(regulationFamilyBindingHintKey(matchBound));
  const standingHint =
    standingBound === null
      ? undefined
      : tReg(regulationFamilyBindingHintKey(standingBound));

  const sections: ReactNode[] = [
    <RegulationDigestSection
      key="match"
      titleId="regulation-match-heading"
      sectionId="tile-match"
      title={t('fiche.tiles.match')}
      icon={<MatchRulesIcon size="lg" />}
      editLabel={t('fiche.edit')}
      onEdit={canEditMatch || canRebind ? onEditMatch : undefined}
      bindingHint={matchHint}
      bindingPhaseSpecific={!matchBound}
      tokens={buildMatchDigestTokens(stage, tReg)}
      factsAriaLabel={tReg('matchTimeline.aria')}
    />,
  ];

  if (showStanding) {
    sections.push(
      <RegulationDigestSection
        key="standing"
        titleId="regulation-standing-heading"
        sectionId="tile-standing"
        title={t('fiche.tiles.standing')}
        icon={<StandingRulesIcon size="lg" />}
        editLabel={t('fiche.editStanding')}
        onEdit={canEditStanding || canRebind ? onEditStanding : undefined}
        bindingHint={standingHint}
        bindingPhaseSpecific={standingBound === false}
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
        titleId="regulation-confrontation-heading"
        sectionId="tile-confrontation"
        title={t('fiche.tiles.confrontation')}
        icon={<ConfrontationIcon size="lg" />}
        editLabel={t('fiche.edit')}
        onEdit={canEditTie ? onEditConfrontation : undefined}
        bindingHint={t(confrontationFormatScopeHintKey(stage))}
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
