# SLICE-C-ENF-APPLY (pending)

| Field | Value |
|-------|--------|
| **Status** | **Pending** — not authorized for implementation or commit |
| **Slice ID** | `SLICE-C-ENF-APPLY` |
| **Backlog** | **C-ENF-Apply** (G-C-2) |
| **Depends on** | **SLICE-C-ENF-DETECT** (predicate stable) |

## Périmètre cible (à autoriser)

| In scope (proposal) | Out of scope |
|---------------------|--------------|
| Refus `ApplyDraw` après preflight slot/group, avant mutation | Materialize, Start |
| `Application.DrawObsoleteForConsumption` | Bundle avec Mat-Start |

**Note** — Raccordements dans `ApplyDraw` en working tree ; **non ratifiés**.
