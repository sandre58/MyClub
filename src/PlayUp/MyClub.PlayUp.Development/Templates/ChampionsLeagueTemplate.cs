// -----------------------------------------------------------------------
// <copyright file="ChampionsLeagueTemplate.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Development.Orchestration;
using MyClub.PlayUp.Development.Recipes;
using MyClub.PlayUp.Development.Runtime;

namespace MyClub.PlayUp.Development.Templates;

/// <summary>
/// Champions League inspired group stage only (8×4) — no knockout / no Swiss / no League Phase.
/// </summary>
/// <remarks>
/// Swiss classique is a separate Domain Kind (Lot 2). UEFA League Phase is a future product track —
/// do not wire <c>champions-league</c> to Swiss; that would be misleading.
/// </remarks>
public sealed class ChampionsLeagueTemplate : ICompetitionTemplate
{
    /// <inheritdoc />
    public string Id => "champions-league";

    /// <inheritdoc />
    public string Name => "UEFA Champions League";

    /// <inheritdoc />
    public string Description =>
        "Groups 8×4 · 32 clubs (JSON). Groups-only capacity demo — UEFA League Phase is a future distinct track (not Swiss classique).";

    /// <inheritdoc />
    public CompetitionRecipe Recipe { get; } = new()
    {
        DisplayName = "UEFA Champions League",
        Format = RecipeFormat.Groups,
        TeamCount = 32,
        GroupCount = 8,
        ParticipantsPerGroup = 4,
        StageName = "Phase de groupes",
        TeamNames = TeamNameSource.Dataset,
        DatasetCompetitionKey = "champions-league"
    };

    /// <inheritdoc />
    public Task ExecuteAsync(ScenarioContext context, CancellationToken cancellationToken = default) =>
        ScenarioOrchestration.BuildStructuredAsync(context, Recipe, cancellationToken: cancellationToken);
}
