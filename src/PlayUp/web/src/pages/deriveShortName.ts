/** Mirrors Domain `ShortName.FromDisplayName` (max 5; spaces only). */
export const SHORT_NAME_MAX_LENGTH = 5;

export function deriveShortName(displayName: string): string {
  const parts = displayName.trim().split(/\s+/).filter(Boolean);
  if (parts.length === 0) {
    return '';
  }
  if (parts.length === 1) {
    const only = parts[0]!;
    return only.length <= SHORT_NAME_MAX_LENGTH
      ? only.toUpperCase()
      : only.slice(0, SHORT_NAME_MAX_LENGTH).toUpperCase();
  }
  return parts
    .slice(0, 3)
    .map((part) => part[0]!.toUpperCase())
    .join('');
}
