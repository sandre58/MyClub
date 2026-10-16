# SLICE-C-ENF-MAT-START (pending)

| Field | Value |
|-------|--------|
| **Status** | **Pending** — not authorized for implementation or commit |
| **Slice ID** | `SLICE-C-ENF-MAT-START` |
| **Backlog** | **C-ENF-Mat** (G-C-3) + **C-ENF-Deps** (G-C-4) — voie Start |
| **Depends on** | **SLICE-C-ENF-DETECT** |

## Périmètre cible (à autoriser)

| In scope (proposal) | Out of scope |
|---------------------|--------------|
| `StructureDrawConsumptionGate` sur `MaterializeMatches`, `MaterializeCupFromOccupiedSlots` | Detect predicate (slice Detect) |
| Blocage **Start** via `StageStartReadinessGuard` lorsque tirage obsolète sur la phase | Apply (slice Apply) |

**Note** — Gates en working tree ; **non ratifiés**.
