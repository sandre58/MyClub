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
/// Inspired Groups capacity demo (8×4, 32 clubs) — approximates a classic CL group stage, not today's UEFA League Phase.
/// </summary>
/// <remarks>
/// Groups-only on purpose. Do not wire Swiss (≠ League Phase). Knockout / League Phase = other seeds.
/// </remarks>
public sealed class ChampionsLeagueTemplate : ICompetitionTemplate
{
    /// <inheritdoc />
    public string Id => "champions-league";

    /// <inheritdoc />
    public string Name => "UEFA Champions League";

    /// <inheritdoc />
    public string Description =>
        "Groups 8×4 · 32 clubs (JSON) — capacity demo for Groups (classic CL group-stage shape). Not UEFA League Phase; not Swiss; no KO.";

    /// <inheritdoc />
    public CompetitionRecipe Recipe { get; } = new()
    {
        DisplayName = "UEFA Champions League",
        Format = RecipeFormat.Groups,
        TeamCount = 32,
        GroupCount = 8,
        PlacesPerGroup = 4,
        StageName = "Phase de groupes",
        TeamNames = TeamNameSource.Dataset,
        DatasetCompetitionKey = "champions-league"
    };

    /// <inheritdoc />
    public Task ExecuteAsync(ScenarioContext context, CancellationToken cancellationToken = default) =>
        ScenarioOrchestration.BuildStructuredAsync(context, Recipe, cancellationToken: cancellationToken);
}
