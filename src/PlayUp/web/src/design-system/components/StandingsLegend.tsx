export type StandingsLegendItem = {
  label: string
  color: string
}

export function StandingsLegend({
  items,
  className,
}: {
  items: StandingsLegendItem[]
  className?: string
}) {
  if (items.length === 0) {
    return null
  }

  const classes = ['ds-standings-legend', className].filter(Boolean).join(' ')

  return (
    <div className={classes}>
      {items.map((item) => (
        <span key={item.label} className="ds-standings-legend__item">
          <span
            className="ds-standings-legend__swatch"
            style={{ background: item.color }}
            aria-hidden="true"
          />
          {item.label}
        </span>
      ))}
    </div>
  )
}
