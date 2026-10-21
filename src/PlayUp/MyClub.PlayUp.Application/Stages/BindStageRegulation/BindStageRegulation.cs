// -----------------------------------------------------------------------
// <copyright file="BindStageRegulation.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Stages;

/// <summary>
/// Application use case: rebind Match or Standing heritable parts to Competition defaults.
/// </summary>
public static class BindStageRegulation
{
    /// <summary>
    /// Match bundle: duration, extra time, shootout, administrative result.
    /// </summary>
    public const string ScopeMatch = "Match";

    /// <summary>
    /// Standing bundle: points + ranking criteria.
    /// </summary>
    public const string ScopeStanding = "Standing";

    /// <summary>
    /// Rebinds the requested bundle from competition regulation into the stage.
    /// </summary>
    public static void Execute(
        Stage stage,
        Competition competition,
        string scope,
        IClock clock)
    {
        ArgumentNullException.ThrowIfNull(stage);
        ArgumentNullException.ThrowIfNull(competition);
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentException.ThrowIfNullOrWhiteSpace(scope);

        var parts = ResolveParts(scope);
        foreach (var part in parts)
        {
            stage.BindToCompetition(part, competition.Regulation, clock);
        }
    }

    private static IReadOnlyList<HeritableRegulationPart> ResolveParts(string scope) =>
        string.Equals(scope, ScopeMatch, StringComparison.OrdinalIgnoreCase)
            ? [
                HeritableRegulationPart.MatchDuration,
                HeritableRegulationPart.ExtraTime,
                HeritableRegulationPart.PenaltyShootout,
                HeritableRegulationPart.AdministrativeResult
            ]
            : string.Equals(scope, ScopeStanding, StringComparison.OrdinalIgnoreCase)
                ? [
                    HeritableRegulationPart.Points,
                    HeritableRegulationPart.RankingCriteria
                ]
                : throw new ApplicationFailureException(
                    $"Unknown bind scope '{scope}'. Expected '{ScopeMatch}' or '{ScopeStanding}'.",
                    ApplicationErrorCodes.InvalidStructureIntent);
}
