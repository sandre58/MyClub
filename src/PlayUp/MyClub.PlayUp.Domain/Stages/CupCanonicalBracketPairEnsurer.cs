// -----------------------------------------------------------------------
// <copyright file="CupCanonicalBracketPairEnsurer.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.PlayUp.Domain.Stages;

/// <summary>
/// Idempotent add-only repair of missing canonical entry-round <see cref="BracketPair"/>s.
/// </summary>
public static class CupCanonicalBracketPairEnsurer
{
    /// <summary>
    /// Appends missing expected pairs when classification is <see cref="CupBracketTopologyKind.CanonicalIncomplete"/>.
    /// </summary>
    /// <returns>Number of pairs added (0 when nothing to do or policy does not apply).</returns>
    public static int EnsureMissing(Stage stage)
    {
        ArgumentNullException.ThrowIfNull(stage);

        if (CupBracketTopologyClassifier.Classify(stage) is not CupBracketTopologyKind.CanonicalIncomplete)
        {
            return 0;
        }

        var expected = CupBracketTopologyClassifier.ExpectedCanonicalPairs(stage);
        var missing = new List<BracketPair>(expected.Count);
        missing.AddRange(expected.Where(pair => stage.FindBracketPair(pair.PairKey) is null));

        if (missing.Count == 0)
        {
            return 0;
        }

        stage.AppendCanonicalBracketPairs(missing);
        return missing.Count;
    }
}
