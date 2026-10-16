# §11C E5 — Proposed `phase-mutability-matrix.md` diff (§9–§11)

| Field | Value |
|-------|--------|
| **Status** | **Accepted & applied** 2026-10-15 — matrix [phase-mutability-matrix.md](./phase-mutability-matrix.md) §9.1.1, §10, §11C, §12 |
| **Purpose** | Verifiable Git artifact for full §11C sign; Notion mirror **after** this is accepted |
| **Source decisions** | [section-11c-addendum](./section-11c-addendum-clarification-arbitration.md) — A/B/C workbooks signed 2026-10-15 |
| **Target file** | [phase-mutability-matrix.md](./phase-mutability-matrix.md) |
| **Date** | 2026-10-15 |

**Legend (per change):**

| Tag | Meaning |
|-----|---------|
| **NORM** | Stays as today — no edit in this proposal |
| **AMEND** | Text/table change at §11C full sign (product truth in Git) |
| **BACKLOG** | New or updated §12 row — contract visible; **not** claimed shipped |
| **META** | Document header / sign-off mechanics only |

**§11C full sign (Git):** 2026-10-15. **§11A** / **§11B** unchanged; draw/obsolescence interpretation published via matrix §9.1.1. **SLICE-0-none** — no ENF implementation authorized.

---

## 0. Header (lines 1–6) — **META**

| Tag | Current | Proposed |
|-----|---------|----------|
| **AMEND** | `**Version:** §11A + **§11B signed 2026-10-15**; …` | Add: `§11C workbooks signed 2026-10-15 (annex); **§11C matrix amend pending E5** (this doc).` |
| **NORM** | Rule: §5.2 target; §5 current; §12 gaps | Unchanged |

---

## 1. §1 D4 — draw nuance (optional cross-ref only)

| Tag | Note |
|-----|------|
| **NORM** | D4 line “Draw remains operational” — **no wording change** in §1 at E5 |
| **AMEND** | **§9.1** carries the **B** nuance (`ReplaceStageDrawRules` stage Draft-only; entity configure/generate Draft-only; Ready = execute per B-Q2) so §11A A2 is not retroactively rewritten |

**Justification:** B §4.9 checklist — `operationalDraw` Allow Ready **partial**: rules authoring exception, not contradiction of A2.

---

## 2. §3 `operationalDraw` row — **AMEND** (feeds §9.1; listed because E5 reviewers expect it)

| Tag | Current (§3 table) | Proposed footnote or cell note |
|-----|-------------------|--------------------------------|
| **AMEND** | `operationalDraw` \| Cup/Groups \| Allow \| **Allow (target)** \| … | Keep **Allow** in Ready (no production). Add footnote under §3 table: *Draw **execution** and pool/affectation paths in Ready per **§9.1.1**; stage-level `ReplaceStageDrawRules` and draw-entity configure/generate remain **Draft-only** (B-Q1b, B-Q3b). Uniqueness ≤1 non-cancelled draw = **product contract** (B-Q3a) — **G-B-1-ENF** backlog.* |

**Justification:** **A-keep** unchanged for placement; **B** narrows which draw **authoring** commands are Ready-allowed.

---

## 3. §9 — Commands inventory

### 3.1 Main table (§9 lines 203–253) — **NORM**

| Tag | Action |
|-----|--------|
| **NORM** | Keep table as **current code** inventory (Draft-only guards, etc.) |
| **AMEND** | Add one line **above** the table: *§9 table = **runtime today**. Target draw policy = **§9.1.1**. Do not read Allow in §9.1 as “already enforced” where §9 shows Draft-only.* |

**Justification:** E5 control — matrix must not imply implementation without proof.

### 3.2 §9.1 Target command policy — **AMEND**

**Replace / extend** closing paragraph and add **§9.1.1**.

**Current (tail of §9.1):**

```markdown
| Entrées, sorties, attribution, placement, draw | Allow + **D1** + draw-refuse rules | Deny | Deny (use progressive APIs only) |
```

**Proposed §9.1** (same table rows; amend last column text for draw family only in narrative below table):

After the existing §9.1 table, **insert**:

```markdown
**Draw family (§11C — product target, partially enforced):** In Ready without production, **draw workflow** commands (`PublishDraw`, `ApplyDraw`, `CancelDraw`, create second draw when allowed) are in scope; **not** unrestricted editing of all draw parameters. See **§9.1.1**.

### 9.1.1 Draw commands — target policy (**AMEND** at §11C full sign)

**Status:** Product contract from Arbitrage **B** (+ **C** consumption gates as backlog). **Code** may differ — see §9 table and §12.

| Command / concern | Stage **Draft** | Stage **Ready**, no production | **Applied** draw (entity) | Enforcement today |
|-------------------|-----------------|-------------------------------|---------------------------|-------------------|
| `ReplaceStageDrawRules` | Allow per guards | **Deny** (B-Q1b) — use cancel → recreate (P2) if needed | **Deny** | **Aligned** — `EnsureStructureMutable` |
| `CreateDraw` | Allow | Allow if **≤1 non-cancelled** draw (B-Q3a) | — | **Δ** — SPA partial; **G-B-1-ENF** |
| `ConfigureDrawInputs` / `GenerateDraw` | Draw entity **Draft** only | **Deny** on entity (B-Q3b) | **Deny** | **Aligned** — `Draw.EnsureDraftMutable` |
| `PublishDraw` / `ApplyDraw` | Per lifecycle | **Allow** per B-Q2 (not universal deny post-Applied) | Idempotent **Apply** only if conform | **Partial** — no obsolete gate (**C-ENF-***) |
| `ReplaceStageAffectationAuthoring` (pool) | Allow + D1/D2 | Allow | Same **EntryId** set (**R-Draw1-N2**); pool **change** → **refuse** (**R-Draw1+** / G-B-3) | **Δ** — N2 tested; **G-B-3-ENF** / G-OPS3 |
| Obsolete draw **consumption** | — | Deny **Apply** / **Materialize** / dependent **Start** when **D** obsolete (**C**) | — | **Δ** — **C-ENF-*** backlog |

**Invariant (B-Q3a):** At most **one** draw with `Status != Cancelled` per phase; cancelled history retained.

**Three contracts (L3 — do not merge):** (a) **G-B-1** uniqueness, (b) **G-B-3** pool-change **refuse** after Applied, (c) **C** obsolete-for-consumption after **allowed** mutation.

**Annex:** [section-11c-addendum](./section-11c-addendum-clarification-arbitration.md) §4.10, §5.4–§5.5.
```

**Justification:** **B** B-Q1b, B-Q2, B-Q3a–b; **C** C1–C2, L3 vs G-B-3; explicit **enforcement today** column avoids false “signed = shipped”.

---

## 4. §10 — Reference business cases — **AMEND**

### 4.1 Amend existing row

| ID | Tag | Change |
|----|-----|--------|
| **R-Draw1** | **AMEND** | Rename/clarify in **Scenario** column to: *Legacy umbrella — see **R-Draw1+** (refuse) and **R-Draw1-N2** (allow same set).* Keep **Verification** pointing at G-OPS3 + GOps3 tests. |

### 4.2 Insert rows (after **R-Draw1** or grouped at end)

| ID | Scenario | Expected (signed policy) | Verification | Tag |
|----|----------|--------------------------|--------------|-----|
| **R-Draw1+** | Shrink or change affectation **EntryId** set with **Applied** draw | **Refuse mutation** (G-B-3 / G-OPS3) — **not** allow-then-obsolete (§5.2.9) | **Gap** — **G-B-3-ENF**; negatives N1/N3 pattern in GOps3 tests | **AMEND** |
| **R-Draw1-N2** | Replace affectation with **same** `EntryId` set, Applied draw | **Allow** — not obsolete | `ReplaceAffectationAuthoring_allows_replace_with_same_entries_when_group_draw_applied` | **AMEND** |
| **R-Inv-1** | Draft form change orphans placements; draw **D** obsolete for consumption | Allow mutation; **Deny** `ApplyDraw` / **Materialize** on obsolete **D**; independent ops Allow | **Gap** — **C-ENF-*** (contract §5.2.8) | **AMEND** |
| **R-C-R3** | Recovery blocked by **D2** (matches produced) | **R3** message — no false recreate promise; no C bypass | Related **R-Mat2** / D2 ops tests | **AMEND** |
| **R-Mat-block** | Ready; assignments invalid or draw obsolete for materialize path | **Materialize** denied with explicit reason (F4 + **C**) | **Gap** — **C-ENF-Mat** | **AMEND** |

**Justification:** **C** E3 / addendum §6; **B** R-Draw1+ vs N2; §5.2.9 scenarios.

**NORM:** All other §10 rows unchanged.

---

## 5. §11 — Sign-off scope — **AMEND**

### 5.1 Section title (line ~294)

| Tag | Proposed |
|-----|----------|
| **AMEND** | `## 11. Sign-off scope (three layers)` — or keep “two layers” text but add explicit third subsection (below). |

### 5.2 §11A — **NORM** (checkboxes)

| Tag | Action |
|-----|--------|
| **NORM** | **Do not** edit A1–A6 checkboxes at §11C full sign |
| **AMEND** | Add **maintainer note** under signatory note: *§11C full sign **narrows interpretation** of operational draw via §9.1.1; does **not** reopen §11A A1–A6 without new arbitration.* |

### 5.3 §11B — **AMEND** (stop line text only)

| Tag | Proposed addition after existing stop line |
|-----|---------------------------------------------|
| **AMEND** | *§11C adds **G-B-1-ENF**, **G-B-3-ENF**, and **C-ENF-*** backlog rows (§12). Implementing them requires **§11C full sign** plus an explicit **slice** record (§11C.4 below) — not workbook sign alone.* |

**Justification:** Separate product sign from coding authorization.

### 5.4 §11C subsection — **AMEND** (replace current short annex block)

**Proposed §11C body** (replaces lines 330–334):

```markdown
### 11C — Clarification & arbitration

| Layer | Status |
|-------|--------|
| Workbooks **A**, **B**, **C** | **Signed** 2026-10-15 — [addendum](./section-11c-addendum-clarification-arbitration.md) |
| **Matrix amend (E5)** | **Pending** — [diff proposal](./section-11c-e5-matrix-diff-proposal.md) |
| **§11C full sign (Git)** | **Not signed** until E5 accepted + DM record below |

#### 11C.1 Product decisions published at full sign

| ID | Summary | Source |
|----|---------|--------|
| **A-keep** | `operationalPlacement` Draft + Ready | Addendum §3.4 |
| **B-Q1..B-Q3** | Publish commitment; draw rules Draft-only; ≤1 non-cancelled draw; post-Applied per-command; G-B-3 refuse pool change | Addendum §4.10, §4.13 |
| **C1–C4** | Obsolete unit **D**; per-op deny on consumption; R1–R3; W-* witnesses; L3 ≠ G-B-3 | Addendum §5.2, §5.5 |

#### 11C.2 Enforcement — explicit non-authorization

Workbook and full **§11C** sign record **contracts** and **backlogs**. They do **not** imply runtime gates unless §12 row is **Done** with tests.

| Backlog | Contract | Authorized by C workbook? | Authorized by §11C full sign alone? |
|---------|----------|---------------------------|-------------------------------------|
| **G-B-1-ENF** | ≤1 non-cancelled draw | **No** | **No** — needs **slice** |
| **G-B-3-ENF** | Pool change refuse (G-B-3) | **No** | **No** — needs **slice** |
| **C-ENF-Detect** | G-C-1 | **No** | **No** — needs **slice** |
| **C-ENF-Apply** | G-C-2 | **No** | **No** — needs **slice** |
| **C-ENF-Mat** | G-C-3 | **No** | **No** — needs **slice** |
| **C-ENF-Deps** | G-C-4 | **No** | **No** — needs **slice** |

#### 11C.3 Full sign checklist (DM — at E5)

- [ ] E5: Accept [matrix diff proposal](./section-11c-e5-matrix-diff-proposal.md) §9–§11 (+ §12 rows)
- [ ] E3: Accept §10 new/amended case rows
- [ ] E4: Confirm **B** not reopened
- [ ] **First implementation slice:** `None` | `G-B-1-ENF` | `G-B-3-ENF` | `C-ENF-*` (single ID only) — **one choice**

#### 11C.4 First slice record (**fill at full sign only**)

| Field | Value |
|-------|--------|
| **Slice ID** | *(e.g. `SLICE-0-none` or `SLICE-1-G-B-3-ENF`)* |
| **Allowed backlog IDs** | *(empty if slice zero)* |
| **Out of scope** | All other ENF rows remain forbidden |
| **Date / signatory** | |

**Default until filled:** **SLICE-0-none** — no new implementation authorized.
```

### 5.5 New §11C sign block (after 11C.4) — **META** at full sign only

When DM accepts E5, add dated line: `**§11C full sign (Git):** YYYY-MM-DD — matrix §9.1.1, §10, §12 rows per E5 proposal.`

---

## 6. §12 — Proposed backlog rows (**BACKLOG**)

Apply **with** §11C full sign. Does not change **Done** rows.

| ID | Gap | Product policy | Current code | §11B status | Notes |
|----|-----|----------------|--------------|-------------|-------|
| **G-B-1** | ≤1 non-cancelled draw per phase | B-Q3a invariant | Domain allows multiple; SPA partial | **Deferred** | Delivery: **G-B-1-ENF** — not authorized until slice |
| **G-B-3** | Refuse pool membership change with Applied draw | G-B-3 / G-OPS3; **R-Draw1+** | No command gate | **Deferred** | Delivery: **G-B-3-ENF**; distinct from **C**; N2 preserved |
| **G-C-INV** | Obsolete draw **consumption** gates | C1–C4, **R-Inv-1**, **R-Mat-block** | No unified obsolete predicate | **Deferred** | Delivery split: **C-ENF-Detect**, **Apply**, **Mat**, **Deps** — slice may take **one** ID only |

**AMEND G-OPS3 row (notes column only):**

- Add: *Product refuse contract **G-B-3** signed workbook B §4.13. Implementation track **G-B-3-ENF** (may align with G-OPS3 reopen). **Not** obsolete-first path (**C** separate).*

**NORM:** G-OPS3 remains **Deferred** until slice — do not mark **Done**.

**Optional (maintainer):** Sub-rows under G-C-INV in implementation plan only — matrix stays one row to avoid implying partial ship.

---

## 7. Slice catalogue — dependencies (for E5 §11.3 review)

| Backlog ID | Product contract | Depends on | Typical order | Slice-ready? |
|------------|------------------|------------|---------------|--------------|
| **G-B-1-ENF** | Uniqueness | — | Independent | Yes — isolated invariant |
| **G-B-3-ENF** | Refuse pool change | Clarifies G-OPS3 / **R-Draw1+**; **not** C obsolete | Often before or parallel to C; **do not bundle** with C-ENF by default | Yes — bounded command gate |
| **C-ENF-Detect** | G-C-1 predicate | C4 technical open (derived vs flag) | **Before** Apply/Mat/Deps in design | Partial — predicate design still open |
| **C-ENF-Apply** | G-C-2 | Detect (or minimal inline predicate) | After Detect | No as first slice unless Detect in same slice (avoid — prefer one ID) |
| **C-ENF-Mat** | G-C-3 | Detect; **R-Inv-1** scope | After Detect | No — broader prerequisites |
| **C-ENF-Deps** | G-C-4 | Detect; format matrix | Last among C | No |

**Rule:** **C workbook sign** authorizes **none** of the above. **§11C full sign** authorizes **none** unless **§11C.4** names exactly one backlog ID (or **slice zero**).

---

## 8. E5 — Decision points for decision maker

| # | Question | Options | Recommendation |
|---|----------|---------|----------------|
| **E5-1** | Accept §9.1.1 draw target table + §9 code/target disclaimer? | Accept / Revise / Defer | Accept — makes B/C verifiable in Git |
| **E5-2** | Accept §3 footnote for `operationalDraw`? | Accept / Skip footnote (§9.1.1 only) | Accept — reduces §11A misread |
| **E5-3** | Accept §10 rows (§4.2) + **R-Draw1** clarify? | Accept / Subset | Accept full set per addendum §6 |
| **E5-4** | Accept §12 **G-B-1**, **G-B-3**, **G-C-INV** + G-OPS3 note amend? | Accept / Merge G-B-3 into G-OPS3 only | Accept separate G-B-3 row — matches B sign |
| **E5-5** | Accept §11C.1–11C.4 structure + default **SLICE-0-none**? | Accept / Revise | Accept — slice zero valid |
| **E5-6** | **First slice** at full sign | **None** (recommended default) / one ID from §7 | **None** — contract sign without dev trigger |
| **E5-7** | Notion mirror | After Git commit of accepted matrix | Per your workflow |

**If E5-6 = None:** Full §11C sign still valuable — publishes product truth and §10 cases; §12 rows visible as **Deferred**.

**If E5-6 = one ID:** Record in §11C.4; implementation plan must cite slice; no other ENF row in same PR tranche without new decision.

---

## 9. Application checklist (maintainer, after DM accepts E5)

1. Apply edits to `phase-mutability-matrix.md` per sections 2–6 above.
2. Set addendum header **Status** and §11 maintainer checklist §11C full sign.
3. Changelog row in addendum + matrix header version.
4. **Do not** merge application code under slice unless §11C.4 names backlog ID.
5. Notion **Décision §11C** — copy from **signed Git** only.

---

## 10. Changelog

| Date | Change |
|------|--------|
| 2026-10-15 | Initial E5 diff proposal — §9–§11 + §12 backlog rows |
| 2026-10-15 | **DM accepted E5** — applied to matrix; slice **SLICE-0-none** |
