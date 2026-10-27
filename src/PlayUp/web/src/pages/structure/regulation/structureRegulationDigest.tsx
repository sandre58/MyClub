import type { RegulationDigestToken } from './structureRegulationDigest.build';

export type { RegulationDigestToken } from './structureRegulationDigest.build';

export function RegulationDigestFacts({
  tokens,
  ariaLabel,
}: {
  tokens: RegulationDigestToken[];
  ariaLabel?: string;
}) {
  if (tokens.length === 0) {
    return null;
  }

  return (
    <ul className="structure-regulation-digest__facts" aria-label={ariaLabel}>
      {tokens.map((token) => (
        <li key={token.key} className="structure-regulation-digest__fact">
          <span
            className="structure-regulation-digest__fact-icon"
            aria-hidden="true"
          >
            {token.icon}
          </span>
          <span className="structure-regulation-digest__fact-content">
            {token.content}
          </span>
        </li>
      ))}
    </ul>
  );
}
