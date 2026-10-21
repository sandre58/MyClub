export function entryCellListItemClassName(options: {
  ineligible?: boolean;
  unplaced?: boolean;
}): string {
  return [
    'structure-entries__team',
    options.ineligible ? 'structure-entries__team--ineligible' : null,
    options.unplaced ? 'structure-entries__team--unplaced' : null,
  ]
    .filter(Boolean)
    .join(' ');
}
