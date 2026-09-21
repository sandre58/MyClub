// -----------------------------------------------------------------------
// <copyright file="StructureFluxFullTemplate.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Development.Orchestration;
using MyClub.PlayUp.Development.Recipes;
using MyClub.PlayUp.Development.Runtime;

namespace MyClub.PlayUp.Development.Templates;

/// <summary>
/// Structure QA — Qualif + Progression Sorties on the same graph.
/// </summary>
public sealed class StructureFluxFullTemplate : ICompetitionTemplate
{
    /// <inheritdoc />
    public string Id => "structure-flux-full";

    /// <inheritdoc />
    public string Name => "Structure — Sorties Qualif + Prog";

    /// <inheritdoc />
    public string Description =>
        "Groups → Demis Places (Qual) → Finale/3ᵉ Places (Prog) + Attribution · Draft. Les deux dialogs Sorties. :progress ignored.";

    /// <inheritdoc />
    public CompetitionRecipe Recipe { get; } = new()
    {
        DisplayName = "Structure — Sorties Qualif + Prog",
        Format = RecipeFormat.Groups,
        TeamCount = 8,
        GroupCount = 2,
        PlacesPerGroup = 4,
        StageName = "Phase de groupes",
        TeamNames = TeamNameSource.Generated
    };

    /// <inheritdoc />
    public Task ExecuteAsync(ScenarioContext context, CancellationToken cancellationToken = default) =>
        ScenarioOrchestration.BuildFluxFullGraphDraftAsync(context, cancellationToken);
}
