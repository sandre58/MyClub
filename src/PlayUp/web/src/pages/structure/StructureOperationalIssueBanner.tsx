import { useTranslation } from 'react-i18next';
import {
  structureDiagnosticTitleKey,
  structureDiagnosticTone,
} from './structureDiagnosticPresentation';
import { StructureDiagnosticIssueIcon } from './StructureDiagnosticIssueIcon';

export function StructureOperationalIssueBanner({ codes }: { codes: string[] }) {
  const { t } = useTranslation('structure');

  if (codes.length === 0) {
    return null;
  }

  return (
    <div className="structure-operational-tile__diagnostics">
      <ul className="structure-phase-rail__issues" role="list">
        {codes.map((code) => {
          const tone = structureDiagnosticTone(code);
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
                {t(structureDiagnosticTitleKey(code), {
                  defaultValue: code,
                })}
              </span>
            </li>
          );
        })}
      </ul>
    </div>
  );
}
