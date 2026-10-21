// -----------------------------------------------------------------------
// <copyright file="CupSlotCoverage.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Application.Stages;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Reads;

/// <summary>
/// Shared Read helper: which Cup slot keys are already covered by a complete Fixture (expected legs).
/// </summary>
/// <remarks>
/// Used by Overview from-slots opportunity and Stage overview Confrontations UI — same rule, no SPA heuristic.
/// </remarks>
public static class CupSlotCoverage
{
    /// <summary>
    /// Slot keys that appear on a Fixture with attachments count ≥ expected legs for that round.
    /// </summary>
    public static HashSet<string> GetSlotsCoveredByCompleteFixtures(Stage stage)
    {
        ArgumentNullException.ThrowIfNull(stage);

        var covered = new HashSet<string>(StringComparer.Ordinal);
        foreach (var round in stage.Rounds)
        {
            var expectedLegs = CupConfrontationMaterializer.ExpectedLegsForRound(round);
            foreach (var fixture in round.Fixtures)
            {
                if (fixture.Attachments.Count < expectedLegs)
                {
                    continue;
                }

                if (fixture.SlotAKey is not null)
                {
                    covered.Add(fixture.SlotAKey);
                }

                if (fixture.SlotBKey is not null)
                {
                    covered.Add(fixture.SlotBKey);
                }
            }
        }

        return covered;
    }
}
