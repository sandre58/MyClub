// -----------------------------------------------------------------------
// <copyright file="CoupeDeFranceTemplate.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Development.Orchestration;
using MyClub.PlayUp.Development.Recipes;
using MyClub.PlayUp.Development.Runtime;

namespace MyClub.PlayUp.Development.Templates;

/// <summary>
/// Coupe de France inspired multi-stage cup (R32 → R16 → QF → SF → Final).
/// </summary>
/// <remarks>
/// Progression Sorties = Winner → Population intents; each downstream Place fill = Slot Draw
/// (plus R32 pairing draw). PlacementAwards ranks 1–2; Completed + Outcome.
/// Mid-bracket from-slots demo = <c>cup-qf-sf</c>. <c>:progress</c> ignored.
/// </remarks>
public sealed class CoupeDeFranceTemplate : ICompetitionTemplate
{
    /// <inheritdoc />
    public string Id => "coupe-de-france";

    /// <inheritdoc />
    public string Name => "Coupe de France";

    /// <inheritdoc />
    public string Description =>
        "Cup 32 (JSON) · R32 pairing draw · Winner→Population intents · Slot draws R16→…→Final · PlacementAwards 1–2 · Completed + Outcome. :progress ignored.";

    /// <inheritdoc />
    public CompetitionRecipe Recipe { get; } = new()
    {
        DisplayName = "Coupe de France",
        Format = RecipeFormat.Cup,
        TeamCount = 32,
        BracketSize = 32,
        StageName = "32es de finale",
        TeamNames = TeamNameSource.Dataset,
        DatasetCompetitionKey = "coupe-de-france"
    };

    /// <inheritdoc />
    public Task ExecuteAsync(ScenarioContext context, CancellationToken cancellationToken = default) =>
        ScenarioOrchestration.BuildCoupeDeFranceMultiStageAsync(context, cancellationToken);
}
