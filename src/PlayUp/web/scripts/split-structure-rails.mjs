import fs from 'fs';
import path from 'path';
import { fileURLToPath } from 'url';

const dir = path.join(
  path.dirname(fileURLToPath(import.meta.url)),
  '../src/pages/structure',
);
const lines = fs.readFileSync(path.join(dir, 'StructurePhaseRails.tsx'), 'utf8').split('\n');

const fluxHeader = `// Structure flux rails — Sorties / population list chrome.
import { useMemo, type ReactNode } from 'react';
import { Chip } from '../../design-system/components/Chip';
`;

const fluxBody = lines.slice(38, 160).join('\n');
const fluxRail = lines.slice(485, 537).join('\n');

fs.writeFileSync(
  path.join(dir, 'structureFluxRails.tsx'),
  `${fluxHeader}\n${fluxBody}\n${fluxRail}\n`,
);

const entriesHeader = `// Population (Équipes) rail — affectation list + cardinality meter.
import { useMemo, type ReactNode } from 'react';
import { useTranslation } from 'react-i18next';
import { Tooltip } from '../../design-system/components/Tooltip';
import {
  EmptySelectionIcon,
  PencilIcon,
  PlusIcon,
  StructureIssueIcon,
} from '../../design-system/icons/contentIcons';
import { TeamCrest } from '../../design-system/TeamCrest';
import { EmptyState } from '../../ui';
import type { StructureEntry, StructureStageHubSummary } from '../../types';
import { resolvePlacesN } from './structurePlaces';
import { OccupancyMeter, occupancyMeterState } from './OccupancyMeter';

const compactIcon =
  'ds-btn ds-btn--ghost ds-icon-button ds-icon-button--compact';

const EMPTY_ENTRY_IDS: string[] = [];

`;

const entriesBody =
  lines.slice(342, 410).join('\n') +
  '\n' +
  lines.slice(160, 341).join('\n') +
  '\n';

fs.writeFileSync(
  path.join(dir, 'structurePhaseEntriesRail.tsx'),
  entriesHeader + entriesBody,
);

const panelsHeader = `// Phase rail panels — exit editor menu + confrontation readout.
import { useRef, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Popover } from '../../design-system/components/Popover';
import { Tooltip } from '../../design-system/components/Tooltip';
import {
  AggregateIcon,
  ArrowRightIcon,
  LegsStatIcon,
  PenaltiesIcon,
  PencilIcon,
  TimerIcon,
} from '../../design-system/icons/contentIcons';
import { PinIcon } from '../../design-system/icons/metaIcons';
import type {
  StructureConfrontationSegment,
  StructureStageHubSummary,
} from '../../types';
import { type ExitKind } from './structurePhaseFeeds';

const compactIcon =
  'ds-btn ds-btn--ghost ds-icon-button ds-icon-button--compact';

`;

const panelsBody =
  lines.slice(411, 484).join('\n') + '\n' + lines.slice(538).join('\n');

fs.writeFileSync(
  path.join(dir, 'structurePhaseRailPanels.tsx'),
  panelsHeader + panelsBody,
);

const barrel = `// Re-export barrel — keep imports stable (StructurePhaseFiche, RegulationStrip).
export {
  FluxRuleRow,
  PlacementAwardRow,
  FluxGroupList,
  FluxRail,
} from './structureFluxRails';

export { CompositionMeter, RootEntriesRail } from './structurePhaseEntriesRail';

export { ExitKindMenu, ConfrontationPanel } from './structurePhaseRailPanels';
`;

fs.writeFileSync(path.join(dir, 'StructurePhaseRails.tsx'), barrel);
console.log('Split StructurePhaseRails into flux / entries / panels modules.');
