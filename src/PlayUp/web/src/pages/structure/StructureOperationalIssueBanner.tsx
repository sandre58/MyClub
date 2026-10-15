import { useTranslation } from 'react-i18next';
import {
  structureDiagnosticPresentation,
  structureDiagnosticTone,
} from './structureDiagnosticPresentation';
import type { StructureDiagnosticSeverityContext } from './structureDiagnosticSeverity';
import { StripOperationalDiagnosticMessage } from './structureOperationalStripUi';

export function StructureOperationalIssueBanner({
  codes,
  diagnosticContext,
}: {
  codes: string[];
  diagnosticContext?: StructureDiagnosticSeverityContext;
}) {
  const { t } = useTranslation('structure');

  if (codes.length === 0) {
    return null;
  }

  const presentationContext = diagnosticContext
    ? {
        formatKind: diagnosticContext.formatKind,
        stageStatus: diagnosticContext.stageStatus,
        isEntryBaselineStage: diagnosticContext.isEntryBaselineStage,
        hasInboundQualProgFeed: diagnosticContext.hasInboundQualProgFeed,
        affectationEntryCount: diagnosticContext.affectationEntryCount,
      }
    : {};

  return (
    <div className="structure-operational-strip__message-stack">
      {codes.map((code) => {
        const tone = structureDiagnosticTone(code, diagnosticContext);
        const { titleKeyLong } = structureDiagnosticPresentation(
          code,
          presentationContext,
        );
        return (
          <StripOperationalDiagnosticMessage key={code} tone={tone}>
            {t(titleKeyLong, {
              defaultValue: code,
            })}
          </StripOperationalDiagnosticMessage>
        );
      })}
    </div>
  );
}
