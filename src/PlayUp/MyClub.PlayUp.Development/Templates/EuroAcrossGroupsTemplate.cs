// -----------------------------------------------------------------------
// <copyright file="EuroAcrossGroupsTemplate.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Development.Orchestration;
using MyClub.PlayUp.Development.Recipes;
using MyClub.PlayUp.Development.Runtime;

namespace MyClub.PlayUp.Development.Templates;

/// <summary>
/// UEFA Euro inspired Groups → AcrossGroups best thirds → KO (no bronze).
/// </summary>
/// <remarks>
/// 24 teams / 6×4: EachGroup Top1–2 + AcrossGroups(P=3) ranks 1–4 → R16 population → Slot Draw → Final.
/// Demonstrates Domain AcrossGroups (best thirds). Pairing table by which thirds qualify = not modeled
/// (same product choice as World Cup Case 1: population + draw). <c>:progress</c> ignored.
/// </remarks>
public sealed class EuroAcrossGroupsTemplate : ICompetitionTemplate
{
    /// <inheritdoc />
    public string Id => "euro-across-groups";

    /// <inheritdoc />
    public string Name => "UEFA European Championship";

    /// <inheritdoc />
    public string Description =>
        "Groups 6×4 → Top2 + best 4 thirds (AcrossGroups P=3) → R16 population → Slot Draw → QF→SF→Final · PlacementAwards 1–2 · Completed + Outcome. :progress ignored.";

    /// <inheritdoc />
    public CompetitionRecipe Recipe { get; } = new()
    {
        DisplayName = "UEFA European Championship",
        Format = RecipeFormat.Groups,
        TeamCount = 24,
        GroupCount = 6,
        PlacesPerGroup = 4,
        StageName = "Phase de groupes",
        TeamNames = TeamNameSource.Dataset,
        DatasetCompetitionKey = "euro"
    };

    /// <inheritdoc />
    public Task ExecuteAsync(ScenarioContext context, CancellationToken cancellationToken = default) =>
        ScenarioOrchestration.BuildEuroAcrossGroupsAsync(context, cancellationToken);
}
