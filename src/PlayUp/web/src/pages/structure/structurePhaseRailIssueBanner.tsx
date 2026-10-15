import { useTranslation } from 'react-i18next';

import type {
  StructureStageHubSummary,
  StructureView,
} from '../../types';
import { StructureDiagnosticIssueIcon } from './StructureDiagnosticIssueIcon';
import { structureDiagnosticContextFromStage } from './structureDiagnosticContext';
import { structureDiagnosticTone } from './structureDiagnosticPresentation';
import { resolveStructureDiagnosticIssueTitleKey } from './structurePausedDiagnosticCopy';

export function StructurePhaseRailIssueBanner({
  codes,
  data,
  stage,
}: {
  codes: readonly string[];
  data: StructureView;
  stage: StructureStageHubSummary;
}) {
  const { t } = useTranslation('structure');

  if (codes.length === 0) {
    return null;
  }

  const diagnosticContext = structureDiagnosticContextFromStage(data, stage);

  return (
    <ul className="structure-phase-rail__issues" role="list">
      {codes.map((code) => {
        const tone = structureDiagnosticTone(code, diagnosticContext);
        return (
          <li
            key={code}
            className="structure-phase-rail__issue"
            data-tone={tone}
            role="status"
          >
            <span
              className="structure-phase-rail__issue-icon"
              aria-hidden="true"
            >
              <StructureDiagnosticIssueIcon tone={tone} />
            </span>
            <span>
              {t(
                resolveStructureDiagnosticIssueTitleKey(
                  code,
                  diagnosticContext.stageStatus,
                ),
                {
                  defaultValue: code,
                },
              )}
            </span>
          </li>
        );
      })}
    </ul>
  );
}
