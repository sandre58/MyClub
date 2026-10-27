# Frontend perf — lazy Structure draw dialog (F-02, partial)

**Tip measured:** `03d5f7baabcf8125afafb4f1d39bcc95dd980dd9`  
**Action:** lazy-load `StructureDrawDialog` on first open (arbitrage 3A).  
**Finding:** F-02 stays open. This lot is one measured step. It does not add a bundle-size CI gate.

## Hypothesis

On tip `03d5f7ba`, opening Structure downloads `StructureDrawDialog` inside the Structure route chunk even though the dialog stays closed. Overview never needs that module. Loading the dialog with `React.lazy` at the first open should shrink the Structure chunk and the script bytes of the Structure hub, leave the Overview route chunk unchanged, and fetch the dialog script only when the draw dialog opens.

## Why this action

Before the change, the Structure hub and the draw-open parcours downloaded the **same 23 scripts** (288 634 B encoded). The dialog was already paid for on first paint. `StructurePhaseOperationalStrip` stays on that first paint: the fiche always mounts it through `StructurePhaseSchematicAndAside`. Splitting the strip file would not take it off the hub. React Profiler and Lighthouse were not available on the production preview used here, and the marker clock moved by hundreds of milliseconds with identical bytes, so there is no measurement that strip re-renders dominate the dialog’s weight. The single code action is the lazy dialog.

## Protocol

Machine: Linux cloud VM, Node 22.23.4, Vite 8.3.5 (`npm run build`), Playwright Chromium 156.0.8078.4. No Host and no Postgres. Lighthouse and the React Profiler were not run.

### Chunk sizes

```bash
cd src/PlayUp/web
npm ci
npm run build
```

Record the Vite lines for `index-*.js`, `StructurePage-*.js`, `StructureDrawDialog-*.js`, `CompetitionOverviewPage-*.js`, and `structure-*.css`. Vite’s gzip column is the build proof (same printer as the P7 route-split note). It is not a CI ceiling.

### Three parcours (runtime proxy)

Production preview from `playwright.config.ts` (`vite preview` on `127.0.0.1:4173`). API responses are the stub in `e2e/stub/stubApi.ts`. Each sample uses a new browser context (empty HTTP cache). Three runs. Clock: `Date.now()` from just before `page.goto(url, { waitUntil: 'commit' })` until the marker is visible. After the marker, sum `encodedBodySize` of script resources (`initiatorType === 'script'`, or an `/assets/*.js` URL) and of `fetch` / `xmlhttprequest` resources.

| Parcours | URL | Marker |
| --- | --- | --- |
| Structure hub | `/competitions/{id}/structure` | headings `Structure` and `League` |
| Overview | `/competitions/{id}` | `.overview` |
| Draw dialog | `/competitions/{id}/structure?draw=1` | dialog `Exécutions de tirage` |

The draw sample uses the stub structure payload with `structure.hasDrawRules` and `stages[0].hasDrawRules` set to true, which is what `initialDrawWorkflow` requires. After the timed sample, the header close control (`.ds-dialog__header` button `Fermer`) must remove the dialog. That probe is not part of the CI stub suite.

Marker times on this VM swung between roughly 400 ms and 950 ms with **identical** byte counts. They are listed below and are not the decision metric.

## Before (`03d5f7ba`, `npm run build`)

| Asset | Min | Gzip (Vite) |
| --- | ---: | ---: |
| `index-*.js` | 316.56 kB | 96.16 kB |
| `StructurePage-*.js` | 320.22 kB | 75.24 kB |
| `StructureDrawDialog-*.js` | — | — |
| `CompetitionOverviewPage-*.js` | 30.94 kB | 7.67 kB |
| `structure-*.css` | 81.36 kB | 10.75 kB |

Browser `encodedBodySize` of `StructurePage` on the hub: **74 880 B**. Draw-open used that same file and no other draw script.

| Parcours | Script bytes (median of 3) | Scripts | Fetch count | Fetch bytes | Marker ms (3 runs) |
| --- | ---: | ---: | ---: | ---: | --- |
| Structure hub | 288 634 | 23 | 4 | 2 132 | 461, 881, 433 |
| Overview | 211 176 | 14 | 4 | 3 519 | 874, 874, 399 |
| Draw dialog | 288 634 | 23 | 5 | 2 297 | 945, 451, 891 |

The extra draw fetch is `GET /stages/{id}` (stage overview). JSON size is unchanged by this lot.

## After (this branch, same commands)

| Asset | Min | Gzip (Vite) |
| --- | ---: | ---: |
| `index-*.js` | 316.58 kB | 96.17 kB |
| `StructurePage-*.js` | **305.44 kB** | **71.86 kB** |
| `StructureDrawDialog-*.js` | **16.95 kB** | **4.96 kB** |
| `CompetitionOverviewPage-*.js` | 30.94 kB | 7.67 kB |
| `structure-*.css` | 81.36 kB | 10.75 kB |

| Parcours | Script bytes (median of 3) | Scripts | Fetch count | Fetch bytes | Marker ms (3 runs) |
| --- | ---: | ---: | ---: | ---: | --- |
| Structure hub | **285 045** | **22** | 4 | 2 132 | 937, 448, 868 |
| Overview | 211 253 | 14 | 4 | 3 519 | 890, 878, 870 |
| Draw dialog | 290 249 | 24 | 5 | 2 297 | 449, 876, 444 |

On the draw parcours the extra scripts are `StructureDrawDialog` (4 960 B encoded) and `deriveShortName` (244 B). `deriveShortName` used to load with the hub because the dialog module pulled it in; it now loads with the dialog. `drawUi` stays a shared chunk on the hub (the fiche still imports it).

## Delta

| Metric | Before | After | Delta |
| --- | ---: | ---: | ---: |
| `StructurePage` min (Vite) | 320.22 kB | 305.44 kB | **−14.78 kB** |
| `StructurePage` gzip (Vite) | 75.24 kB | 71.86 kB | **−3.38 kB** |
| Hub script bytes | 288 634 | 285 045 | **−3 589 B** |
| Hub script requests | 23 | 22 | **−1** |
| Draw-open script bytes | 288 634 | 290 249 | **+1 615 B** |
| Overview route chunk | 30.94 / 7.67 kB | 30.94 / 7.67 kB | 0 |
| Overview script bytes | 211 176 | 211 253 | +77 B |
| Structure / Overview CSS | unchanged | unchanged | 0 |
| Fetch counts and JSON bytes | unchanged | unchanged | 0 |

Hub savings are the dialog (and `deriveShortName`) leaving first paint. Draw-open costs more than the hub saves because the dialog gzipped better inside the large Structure chunk than as its own file (4 960 B on the wire versus about 3 427 B removed from `StructurePage`). That is the measured trade-off: Structure first paint is lighter; opening the draw fetches the module that used to be prepaid.

Overview’s +77 B is shared-chunk noise (`index` +15 B, one shared runtime chunk +58 B, and ±1–3 B on several others). `CompetitionOverviewPage` itself is unchanged.

All built JS (Node `zlib.gzipSync`, informative): 1 326 541 B → 1 328 869 B min (+2 328 B), gzip 360 590 B → 362 208 B (+1 618 B). The whole app grows by the split overhead. There is no bundle gate.

Marker times are not a TTI result. They moved in both directions across three runs.

## Change

- `StructureDrawDialogHost` dynamic-imports `StructureDrawDialog` on the first `open`.
- The host stays mounted after that so the dialog’s exit transition still runs.
- While the chunk is in flight and the dialog is open, the same title and the existing « Chargement des exécutions… » wait mark are shown.
- `StructurePhaseFiche` renders the host instead of a static dialog import.

## Checks

- `StructureDrawDialogHost.test.tsx` — closed fiche does not static-import the dialog; opening shows the dialog; closing keeps it through the exit, then it leaves.
- `StructureDrawDialog.test.tsx`, `StructurePage.test.tsx` — green.
- `npm run build` — green (`tsc -b` and Vite).
- Playwright stub smoke (Accueil, Overview, Structure fiche, Match) — green on this build.
- Draw smoke on the same preview: dialog `Exécutions de tirage` opens from `?draw=1`, header close removes it.

## Still open

F-02 is not done. The Structure route is still about 305 kB min. The strip, the fiche, and the schematic stay on first paint. No TanStack Query change in this lot. No SQL change.
