import {
  ChampionshipFormatIcon,
  CupFormatIcon,
  GroupsFormatIcon,
  StructureIcon,
  SwissFormatIcon,
} from '../../design-system/icons/contentIcons';
import type { IconSize } from '../../design-system/icons/Icon';
import type { StructureFormatKind } from '../../types';

export function StageFormatGlyph({
  kind,
  size = 'sm',
}: {
  kind?: StructureFormatKind | null;
  size?: IconSize;
}) {
  switch (kind) {
    case 'Cup':
      return <CupFormatIcon size={size} aria-hidden="true" />;
    case 'Championship':
      return <ChampionshipFormatIcon size={size} aria-hidden="true" />;
    case 'Groups':
      return <GroupsFormatIcon size={size} aria-hidden="true" />;
    case 'Swiss':
      return <SwissFormatIcon size={size} aria-hidden="true" />;
    default:
      return <StructureIcon size={size} aria-hidden="true" />;
  }
}
