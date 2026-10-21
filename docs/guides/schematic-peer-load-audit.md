# Schematic peer load audit (PR4)

`StageSchematicAssembler` uses `competitionStages` for Qual/Prog **WhoFeeds**, pending group feeds, progression origin labels, and cross-stage population paths. It does **not** read peer **draw** aggregates for the target schematic.

| Assembler concern | Peer data needed | Profile |
| --- | --- | --- |
| Progression / Qual path origins | Rules + structure graph (groups, rounds, slots on **target**; rules on peers) | `Structure` |
| Pending group feeds | Groups + qualification rules on source peers | `Structure` |
| Cup slot feed snapshot | Slots + structure on target; source stage graph for paths | Target: `Structure` + draws + slots |
| Fixture / match connection on target | Target rounds/fixtures/slots | Target: `Structure` + slots |

**Loader change** (`GetStageSchematicAsync`):

- Target stage: `Structure` + `IncludeDraws` + `IncludeSlots` (aligned with Structure hub spec).
- Peer stages: `Structure` only (no draws/slots includes).

Functional equivalence for multi-phase progression is covered by `StageSchematicAssemblerTests.Schematic_multi_phase_progression_matches_full_stage_graph_reference`.
