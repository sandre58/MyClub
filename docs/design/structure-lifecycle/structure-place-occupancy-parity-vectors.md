# Structure place occupancy — FE ↔ Application parity vectors (O3-A)

**Authority:** `StructurePlaceOccupancyIssues.CountOccupiedPlaces` (Application) · `countSchematicOccupiedPlaces` (SPA).

**Tests:** `StructurePlaceOccupancyCountParityTests.cs` · `structurePlaceOccupancy.test.ts`.

| Id | Format | placesN | Expected | Notes |
| -- | ------ | ------- | -------- | ----- |
| cup-entry-round | Cup | 8 | 1 | Three CupSlot cases; only entry round (min roundOrder) counts; one with entry |
| cup-unknown-n | Cup | null | (skip) | FE returns null — not a backend CountOccupiedPlaces call |
| groups-qual-feed | Groups | 4 | 1 | GroupPlace with Qual feed counts; empty second place does not |
| championship-roster | Championship | 6 | 2 | RosterPlace: entry + qual feed; empty third |

Keep vectors in sync when either implementation changes.
