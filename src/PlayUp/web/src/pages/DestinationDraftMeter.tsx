// -----------------------------------------------------------------------
// Destination occupancy meter — Qual / Prog authoring tiles (shared chrome).
// -----------------------------------------------------------------------

/** Expected destination occupancy (X) vs Places N (Y); +Z = current intent only. */
export function DestinationDraftMeter({
  count,
  capacity,
  draft,
  label,
  draftLabel,
  ariaLabel,
}: {
  count: number;
  capacity: number;
  draft: number;
  label: string;
  draftLabel: string;
  ariaLabel: string;
}) {
  const tone =
    count === capacity ? 'exact' : count < capacity ? 'short' : 'over';
  const ratio =
    capacity > 0
      ? Math.min(1, Math.max(0, count / capacity))
      : count > 0
        ? 1
        : 0;

  return (
    <span
      className={`structure-qualification__tile-meter structure-qualification__tile-meter--${tone}`}
      role="img"
      aria-label={ariaLabel}
    >
      <span
        className="structure-qualification__tile-meter-label"
        aria-hidden="true"
      >
        {label}
      </span>
      <span
        className="structure-qualification__tile-meter-track"
        aria-hidden="true"
      >
        <span
          className="structure-qualification__tile-meter-fill"
          style={{ width: `${ratio * 100}%` }}
        />
      </span>
      <span
        className={[
          'structure-qualification__tile-meter-draft',
          draft > 0
            ? 'structure-qualification__tile-meter-draft--active'
            : null,
        ]
          .filter(Boolean)
          .join(' ')}
        aria-hidden="true"
      >
        {draftLabel}
      </span>
    </span>
  );
}
