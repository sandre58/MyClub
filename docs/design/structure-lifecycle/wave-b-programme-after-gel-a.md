# Programme B — après recette gel A (2026-10-16)

**Contexte** — Gel **A** clôturé. Programme B : slices §11C.4 **un par un** — voir [programme-b-arbitration-2026-10-16.md](./programme-b-arbitration-2026-10-16.md).

---

## État livré (ne pas rouvrir sans anomalie)

| ID | Statut |
|----|--------|
| **G-B-3** / **SLICE-1-G-B-3-ENF** | **Closed** — pool `EntryId` + Applied draw |

---

## Arbitrage 2026-10-16 (non = livré)

| ID | Statut gouvernance | Notes |
|----|-------------------|--------|
| **G-B-1** / **SLICE-2-G-B-1-ENF** | **Authorized** — pas **Closed** | B-Q3a ; diff local non commité |
| **G-OPS3** reliquat qual/prog / **SLICE-3** | **Authorized** — **G-OPS3** §12 **Partial** | Gate qual/prog ; pas Done |
| **C-ENF** (Detect / Apply / Mat-Start) | **Closed** (`279b6235`, `85360d3`, `ee0fc2e6`) | **G-C-INV** §12 **Done** |
| **G-MAT3 / G-MAT4** | **Deferred** | Inchangé |

---

## Ordre recommandé (produit)

```mermaid
flowchart LR
  P2[SLICE-2 G-B-1]
  P3[SLICE-3 G-OPS3 qual/prog]
  D[C-ENF Detect]
  A[C-ENF Apply]
  MS[C-ENF Mat-Start]
  P2 --> P3
  D --> A
  D --> MS
```

| Priorité | Backlog | Slice | Statut |
|----------|---------|-------|--------|
| **P2** | **G-B-1-ENF** | [slice-2](./slice-2-g-b-1-enf.md) | **Authorized** |
| **P2b** | **G-OPS3** qual/prog | [slice-3](./slice-3-g-ops3-qual-prog-enf.md) | **Authorized** ; G-OPS3 **Partial** |
| **P3a** | **C-ENF-Detect** | [slice-c-enf-detect](./slice-c-enf-detect.md) | **Closed** (`279b6235`) |
| **P3b** | **C-ENF-Apply** | [slice-c-enf-apply](./slice-c-enf-apply.md) | **Closed** (`85360d3`) |
| **P3c** | **C-ENF-Mat + Start** | [slice-c-enf-mat-start](./slice-c-enf-mat-start.md) | **Closed** (`ee0fc2e6`) |

---

## Working tree (proposition technique — non ratifiée en bloc)

Branche `feature/new_version` : **C-ENF** livré (3 commits slices). **G-C-INV** **Done**. Stash WIP optionnel — non requis.

---

## Références

[programme-b-arbitration-2026-10-16.md](./programme-b-arbitration-2026-10-16.md) · [implementation-plan-post-11c.md](./implementation-plan-post-11c.md) · [phase-mutability-matrix.md](./phase-mutability-matrix.md) §11C.4 · §12
