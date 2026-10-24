# Phase 3 — IC lot AUD-0013 / AUD-0014

**Findings:** orphan structure-fiche CSS (wave 3) and unused `structure.json` leaf keys.

**Method:** `npm run css:audit` + `npm run i18n:audit` @ branch ; repo grep ; no TS/TSX consumers.

## AUD-0013 — removed CSS (4 tokens)

- `structure-fiche__header-end`
- `structure-fiche__header-remediation`
- `structure-fiche__title-row`
- `structure-fiche__title-row--primary`

File: `src/PlayUp/web/src/pages/structure/structure-fiche.css`

Post-change: `css:audit` → **0 unused / 118** tokens.

## AUD-0014 — removed i18n (6 leaves, en+fr)

- `fiche.binding.general` / `personalized` (whole `binding` object)
- `composition.tile.strip.draw*` (4 keys; whole `strip` object)

Files: `src/PlayUp/web/src/i18n/locales/{en,fr}/structure.json`

Post-change: `i18n:audit` → **0 suspect** ; `npm run i18n:check` → 16/16.

## Acceptance

- [x] Audit scripts clean for structure fiche scope.
- [x] Parity en/fr preserved.
- [x] Registre Notion → **Vérifié** (merge `bbb76630`).
