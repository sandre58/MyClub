import { mediaContentUrl } from './mediaContentUrl';

type CrestTone = 'a' | 'b' | 'c' | 'd' | 'e';

export type TeamCrestProps = {
  name: string;
  logoMediaId?: string | null;
  primaryColor?: string | null;
  className?: string;
  size?: 'sm' | 'md' | 'lg';
};

/**
 * Team crest: real logo via /media/{id}/content when logoMediaId is set,
 * otherwise letter-mark placeholder.
 * No native `title` — wrap with Tooltip only when the crest is the sole name cue.
 */
export function TeamCrest({
  name,
  logoMediaId,
  primaryColor,
  className = '',
  size = 'md',
}: TeamCrestProps) {
  const initial = crestInitials(name);
  const tone = crestTone(name);
  const sizeClass =
    size === 'sm'
      ? 'team-crest--sm'
      : size === 'lg'
        ? 'team-crest--lg'
        : 'team-crest--md';

  if (logoMediaId) {
    return (
      <span
        className={`team-crest team-crest--logo ${sizeClass} ${className}`.trim()}
      >
        <img
          src={mediaContentUrl(logoMediaId)}
          alt=""
          className="team-crest__img"
          loading="lazy"
        />
      </span>
    );
  }

  return (
    <span
      className={`team-crest team-crest--${tone} ${sizeClass} ${className}`.trim()}
      style={
        primaryColor
          ? { ['--team-crest-fill' as string]: primaryColor }
          : undefined
      }
    >
      <svg className="team-crest__shield" viewBox="0 0 24 28" focusable="false">
        <path d="M12 2 L22 6.5 V14.5 C22 20 17.5 25 12 27 C6.5 25 2 20 2 14.5 V6.5 Z" />
      </svg>
      <span className="team-crest__initial">{initial}</span>
    </span>
  );
}

function crestInitials(name: string): string {
  const parts = name.trim().split(/\s+/).filter(Boolean);
  if (parts.length === 0) return '?';
  if (parts.length === 1) return parts[0]!.slice(0, 2).toUpperCase();
  return `${parts[0]![0] ?? ''}${parts[1]![0] ?? ''}`.toUpperCase();
}

function crestTone(name: string): CrestTone {
  let hash = 0;
  for (let i = 0; i < name.length; i++) {
    hash = (hash + name.charCodeAt(i) * (i + 1)) % 5;
  }
  return (['a', 'b', 'c', 'd', 'e'] as const)[hash]!;
}
