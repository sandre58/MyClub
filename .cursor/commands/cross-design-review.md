# Cross Design Review

Challenge a design or architecture proposal **before** implementation. Do **not** modify the repository unless the user explicitly asks for changes after the review.

## Goal

Surface problems and alternatives so the decision maker can decide. Not automatic consensus. Not a tutorial.

## Steps

1. Load the relevant design rules under `.cursor/rules/design/` and `design-system.mdc` when the subject is UI; load `architecture.mdc` when the subject is structure/boundaries.
2. Inspect the code, Design Lab (`/design-lab`), and existing components/patterns that the proposal touches.
3. Compare the proposal to the current system (code + Lab + rules).
4. Identify gaps: inconsistencies with codebase, feasibility, DDD/aggregate boundaries, missing invariants, misplaced responsibilities, edge cases, unnecessary complexity, contradictions with existing Notion Décisions.
5. When the phase is business-important: locate or request the 3–8 reference business cases; challenge the model against each.
6. Classify every finding:
   - **Critical** — should not be implemented as proposed
   - **Important** — arbitration required before proceeding
   - **Minor** — improvement without structural impact
   - **Observation** — informative
7. For Critical / Important: state the issue, evidence (code or decisions), and viable alternatives — do not silently pick one.
8. If business arbitration is needed: **signal** it; do not invent Décision content.

## Output

Group findings by severity. Stay concise and technical. No pedagogy, no syllabus, no “how React works” digressions.
