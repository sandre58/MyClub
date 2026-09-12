import { useTranslation } from 'react-i18next';
import type { StructureFormatKind, StructureStageHubSummary } from '../types';
import { nextPowerOfTwo } from './structureFixtureLabels';
import './phase-schematic.css';

export function inferPhaseKind(
  stage: StructureStageHubSummary,
): 'groups' | 'cup' | 'championship' | 'swiss' {
  const kind: StructureFormatKind | null | undefined = stage.formatKind;
  if (kind === 'Swiss') {
    return 'swiss';
  }
  if (kind === 'Groups' || (stage.groupCount ?? 0) > 0) {
    return 'groups';
  }
  if (kind === 'Cup' || (stage.roundCount ?? 0) > 0 || stage.hasTieFormat) {
    return 'cup';
  }
  return 'championship';
}

export function PhaseSchematic({
  stage,
  density = 'compact',
  occupantLabels,
  showSlotPlaceholders = false,
}: {
  stage: StructureStageHubSummary;
  density?: 'compact' | 'hero';
  /** When set, Groups dots become short labels (Équipes view). */
  occupantLabels?: string[];
  /** Hero slots mode: empty name-sized placeholders instead of dots. */
  showSlotPlaceholders?: boolean;
}) {
  const { t } = useTranslation('regulation');
  const kind = inferPhaseKind(stage);
  const groupCount = stage.groupCount ?? 0;
  const roundCount = stage.roundCount ?? 0;
  const densityClass =
    density === 'hero' ? ' regulation-schematic--hero' : '';

  if (kind === 'groups' || groupCount > 0) {
    const shown =
      density === 'hero'
        ? Math.max(groupCount, 1)
        : Math.min(Math.max(groupCount, 1), 4);
    const perGroup = Math.max(
      1,
      Math.round(stage.teamCount / Math.max(groupCount, 1)),
    );
    const slotsPerCard =
      density === 'hero' ? perGroup : Math.min(perGroup, 4);
    return (
      <div
        className={`regulation-schematic regulation-schematic--groups${densityClass}`}
        aria-label={t('schematic.groups', { count: groupCount || shown })}
      >
        <div className="regulation-schematic__cards">
          {Array.from({ length: shown }, (_, i) => {
            const slice = occupantLabels?.slice(
              i * perGroup,
              i * perGroup + slotsPerCard,
            );
            return (
              <div
                key={i}
                className={`regulation-schematic__card regulation-schematic__card--${i % 4}`}
              >
                <span className="regulation-schematic__card-label">
                  {String.fromCharCode(65 + i)}
                </span>
                {slice && slice.length > 0 ? (
                  <div className="regulation-schematic__labels">
                    {slice.map((label, j) => (
                      <span key={j} className="regulation-schematic__label">
                        {label}
                      </span>
                    ))}
                  </div>
                ) : showSlotPlaceholders || density === 'hero' ? (
                  <div className="regulation-schematic__labels">
                    {Array.from({ length: slotsPerCard }, (_, j) => (
                      <span
                        key={j}
                        className="regulation-schematic__slot"
                        aria-hidden="true"
                      />
                    ))}
                  </div>
                ) : (
                  <div className="regulation-schematic__dots">
                    {Array.from({ length: slotsPerCard }, (_, j) => (
                      <span key={j} className="regulation-schematic__dot" />
                    ))}
                  </div>
                )}
              </div>
            );
          })}
        </div>
      </div>
    );
  }

  if (kind === 'swiss') {
    const knownRounds = stage.swissRoundCount;
    const teamCount = Math.max(stage.teamCount || stage.slotCount || 0, 2);
    const pairCount = Math.max(1, Math.floor(teamCount / 2));
    if (knownRounds == null || knownRounds <= 0) {
      return (
        <div
          className={`regulation-schematic regulation-schematic--swiss${densityClass}`}
          aria-label={t('schematic.swissGeneric')}
        >
          <p className="regulation-schematic__swiss-caption">
            {t('schematic.swissGeneric')}
          </p>
          <div className="regulation-schematic__swiss-rounds">
            <div className="regulation-schematic__swiss-round">
              <span className="regulation-schematic__swiss-round-label">
                R?
              </span>
              <div className="regulation-schematic__swiss-pairs">
                {Array.from({ length: Math.min(pairCount, 4) }, (_, j) => (
                  <span
                    key={j}
                    className="regulation-schematic__swiss-pair-slot"
                    aria-hidden="true"
                  >
                    <span className="regulation-schematic__slot" />
                    <span className="regulation-schematic__swiss-vs">·</span>
                    <span className="regulation-schematic__slot" />
                  </span>
                ))}
              </div>
            </div>
          </div>
        </div>
      );
    }
    const rounds =
      density === 'hero' ? knownRounds : Math.min(knownRounds, 5);
    const pairsShown =
      density === 'hero' ? pairCount : Math.min(pairCount, 4);
    return (
      <div
        className={`regulation-schematic regulation-schematic--swiss${densityClass}`}
        aria-label={t('schematic.swiss', { rounds: knownRounds })}
      >
        <div className="regulation-schematic__swiss-rounds">
          {Array.from({ length: rounds }, (_, i) => (
            <div key={i} className="regulation-schematic__swiss-round">
              <span className="regulation-schematic__swiss-round-label">
                {t('schematic.swissRound', { n: i + 1 })}
              </span>
              <div className="regulation-schematic__swiss-pairs">
                {Array.from({ length: pairsShown }, (_, j) => (
                  <span
                    key={j}
                    className="regulation-schematic__swiss-pair-slot"
                    aria-hidden="true"
                  >
                    <span className="regulation-schematic__slot" />
                    <span className="regulation-schematic__swiss-vs">·</span>
                    <span className="regulation-schematic__slot" />
                  </span>
                ))}
              </div>
            </div>
          ))}
        </div>
      </div>
    );
  }

  if (kind === 'cup' || roundCount > 0 || stage.hasTieFormat) {
    const hero = density === 'hero';
    /** C1: rounds from roundCount; leaves from slotCount (phase may exit with many winners). */
    const shownRounds = Math.max(roundCount || 1, 1);
    const roundsShown = hero ? Math.min(shownRounds, 6) : Math.min(shownRounds, 4);
    const slotCount = Math.max(stage.slotCount ?? 0, 0);
    const leafBase =
      slotCount > 0
        ? slotCount
        : Math.max(stage.teamCount || 0, 2 ** roundsShown);
    const wireLeaves = nextPowerOfTwo(Math.max(leafBase, 2));
    const leaves = hero ? wireLeaves : Math.min(wireLeaves, 8);
    const colGap = hero ? Math.max(40, Math.min(72, 520 / Math.max(roundsShown, 1))) : 28;
    const entryW = hero ? 52 : 0;
    const startX = hero ? entryW + 10 : 8;
    const slotH = hero ? Math.max(12, Math.min(18, 280 / leaves)) : 12;
    const top = hero ? slotH : 6;
    const span = hero
      ? Math.max(leaves * (slotH + 3), 140)
      : 52;
    const height = top * 2 + span;
    const width = startX + roundsShown * colGap + 36;
    const nodeR = 2.5;
    const roundSlot = hero ? 10 : 9;
    const columns: number[][] = [];
    for (let col = 0; col <= roundsShown; col += 1) {
      const count = leaves / 2 ** col;
      const ys: number[] = [];
      for (let i = 0; i < count; i += 1) {
        if (col === 0) {
          ys.push(top + i * (span / Math.max(leaves - 1, 1)));
        } else {
          const y1 = columns[col - 1]![i * 2]!;
          const y2 = columns[col - 1]![i * 2 + 1]!;
          ys.push((y1 + y2) / 2);
        }
      }
      columns.push(ys);
    }

    const showEntryColumn = hero;
    const leafYs = columns[0] ?? [];

    return (
      <div
        className={`regulation-schematic regulation-schematic--bracket${densityClass}${
          showEntryColumn ? ' regulation-schematic--cup-entry' : ''
        }`}
        aria-label={
          roundsShown > 1
            ? t('schematic.bracketRounds', { rounds: roundsShown })
            : t('schematic.bracket')
        }
      >
        <svg
          className="regulation-schematic__wire regulation-schematic__wire--fluid"
          viewBox={`0 0 ${width} ${height}`}
          width="100%"
          height={height}
          preserveAspectRatio="xMinYMid meet"
          aria-hidden="true"
        >
          {showEntryColumn
            ? leafYs.map((y, i) => {
                const occupied = occupantLabels?.[i];
                const isPad = slotCount > 0 && i >= slotCount;
                return (
                  <g key={`entry-${i}`}>
                    <rect
                      x={2}
                      y={y - slotH / 2}
                      width={entryW}
                      height={slotH}
                      rx={2}
                      className={
                        occupied
                          ? 'regulation-schematic__wire-slot'
                          : 'regulation-schematic__wire-slot regulation-schematic__wire-slot--empty'
                      }
                    />
                    {occupied ? (
                      <text
                        x={6}
                        y={y + 3}
                        className="regulation-schematic__wire-label"
                      >
                        {occupied.length > 8
                          ? `${occupied.slice(0, 7)}…`
                          : occupied}
                      </text>
                    ) : null}
                    {isPad ? (
                      <text
                        x={6}
                        y={y + 3}
                        className="regulation-schematic__wire-label regulation-schematic__wire-label--muted"
                      >
                        —
                      </text>
                    ) : null}
                  </g>
                );
              })
            : null}
          {columns.map((ys, col) => {
            const x = startX + col * colGap;
            const skipEntryNodes = showEntryColumn && col === 0;
            return (
              <g key={`col-${col}`}>
                {skipEntryNodes
                  ? null
                  : ys.map((y, i) =>
                      hero ? (
                        <rect
                          key={`n-${col}-${i}`}
                          x={x - roundSlot / 2}
                          y={y - roundSlot / 2}
                          width={roundSlot}
                          height={roundSlot}
                          rx={2}
                          className="regulation-schematic__wire-slot regulation-schematic__wire-slot--round"
                        />
                      ) : (
                        <circle
                          key={`n-${col}-${i}`}
                          cx={x}
                          cy={y}
                          r={nodeR}
                          className="regulation-schematic__wire-node"
                        />
                      ),
                    )}
                {col < roundsShown
                  ? Array.from({ length: ys.length / 2 }, (_, pair) => {
                      const y1 = ys[pair * 2]!;
                      const y2 = ys[pair * 2 + 1]!;
                      const mid = (y1 + y2) / 2;
                      const xMid = x + colGap / 2;
                      const xNext = startX + (col + 1) * colGap;
                      return (
                        <path
                          key={`e-${col}-${pair}`}
                          d={`M ${x} ${y1} H ${xMid} M ${x} ${y2} H ${xMid} M ${xMid} ${y1} V ${y2} M ${xMid} ${mid} H ${xNext}`}
                          className="regulation-schematic__wire-line"
                        />
                      );
                    })
                  : null}
              </g>
            );
          })}
          {hero
            ? Array.from({ length: leaves / 2 }, (_, i) => {
                const y1 = leafYs[i * 2];
                const y2 = leafYs[i * 2 + 1];
                if (y1 == null || y2 == null) return null;
                const midY = (y1 + y2) / 2;
                const labelX = startX + colGap * 0.35;
                return (
                  <text
                    key={`match-n-${i}`}
                    x={labelX}
                    y={midY + 3}
                    textAnchor="middle"
                    className="regulation-schematic__wire-match-n"
                  >
                    {`#${i + 1}`}
                  </text>
                );
              })
            : null}
        </svg>
      </div>
    );
  }

  const leagueRows =
    density === 'hero'
      ? Math.max(stage.teamCount, stage.slotCount ?? 0, 1)
      : Math.min(Math.max(stage.teamCount, 3), 6);
  return (
    <div
      className={`regulation-schematic regulation-schematic--championship${densityClass}`}
      aria-label={t('schematic.championship', { teams: stage.teamCount })}
    >
      <div className="regulation-schematic__league">
        {Array.from({ length: leagueRows }, (_, i) => (
          <span key={i} className="regulation-schematic__league-row">
            <span
              className="regulation-schematic__league-rank"
              aria-hidden="true"
            >
              {i + 1}
            </span>
            {occupantLabels?.[i] ? (
              <span className="regulation-schematic__league-name">
                {occupantLabels[i]}
              </span>
            ) : showSlotPlaceholders || density === 'hero' ? (
              <span className="regulation-schematic__slot regulation-schematic__slot--row" />
            ) : (
              <span
                className="regulation-schematic__league-bar"
                aria-hidden="true"
              />
            )}
          </span>
        ))}
      </div>
    </div>
  );
}
