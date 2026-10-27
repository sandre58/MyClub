import { useTranslation } from 'react-i18next';
import type { StructureView } from '../../../types';
import { readinessBlockerCodesForStage } from './structureReadinessBlockerDetails';
import { StripOperationalDiagnosticMessage } from '../fiche/structureOperationalStripUi';

const READINESS_FICHE_BLOCKER_CODES = ['CupBracketInvalid'] as const;

export function StructureReadinessAttributedBlockerBanner({
  data,
  stageId,
}: {
  data: StructureView;
  stageId: string;
}) {
  const { t } = useTranslation('enums');
  const codes = readinessBlockerCodesForStage(data, stageId).filter((code) =>
    (READINESS_FICHE_BLOCKER_CODES as readonly string[]).includes(code),
  );

  if (codes.length === 0) {
    return null;
  }

  return (
    <div
      className="structure-operational-strip__message-stack structure-fiche__readiness-blockers"
      data-testid="structure-readiness-attributed-blockers"
    >
      {codes.map((code) => (
        <StripOperationalDiagnosticMessage key={code} tone="danger">
          {t(`attentionSource.${code}`, { defaultValue: code })}
        </StripOperationalDiagnosticMessage>
      ))}
    </div>
  );
}
