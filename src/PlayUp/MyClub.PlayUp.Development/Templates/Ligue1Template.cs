// -----------------------------------------------------------------------
// <copyright file="Ligue1Template.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Development.Orchestration;
using MyClub.PlayUp.Development.Recipes;
using MyClub.PlayUp.Development.Runtime;
using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Development.Templates;

/// <summary>
/// Ligue 1 inspired championship — Double RR / PairMirror capacity demonstrator (not a real calendar).
/// </summary>
public sealed class Ligue1Template : ICompetitionTemplate
{
    /// <inheritdoc />
    public string Id => "ligue-1";

    /// <inheritdoc />
    public string Name => "Ligue 1";

    /// <inheritdoc />
    public string Description =>
        "Championship · 18 clubs (JSON) · DoubleRoundRobin / 34 matchdays (PairMirror) — capacity demo, not a real Ligue 1 calendar.";

    /// <inheritdoc />
    public CompetitionRecipe Recipe { get; } = new()
    {
        DisplayName = "Ligue 1",
        Format = RecipeFormat.Championship,
        TeamCount = 18,
        MatchdayCount = 34,
        MatchGenerationFormat = MatchGenerationFormat.DoubleRoundRobin,
        StageName = "Championnat",
        TeamNames = TeamNameSource.Dataset,
        DatasetCompetitionKey = "ligue-1"
    };

    /// <inheritdoc />
    public Task ExecuteAsync(ScenarioContext context, CancellationToken cancellationToken = default) =>
        ScenarioOrchestration.BuildStructuredAsync(context, Recipe, cancellationToken: cancellationToken);
}
