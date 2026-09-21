// -----------------------------------------------------------------------
// <copyright file="StructureFluxEmptyTemplate.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Development.Orchestration;
using MyClub.PlayUp.Development.Recipes;
using MyClub.PlayUp.Development.Runtime;

namespace MyClub.PlayUp.Development.Templates;

/// <summary>
/// Structure QA — multi-phases without Sorties (author from empty dialogs).
/// </summary>
public sealed class StructureFluxEmptyTemplate : ICompetitionTemplate
{
    /// <inheritdoc />
    public string Id => "structure-flux-empty";

    /// <inheritdoc />
    public string Name => "Structure — Sorties vides";

    /// <inheritdoc />
    public string Description =>
        "Groups 2×4 + QF aval (fixtures, sans arêtes) · Draft. Authoring Qualif (groupes) et Progression (QF). :progress ignored.";

    /// <inheritdoc />
    public CompetitionRecipe Recipe { get; } = new()
    {
        DisplayName = "Structure — Sorties vides",
        Format = RecipeFormat.Groups,
        TeamCount = 8,
        GroupCount = 2,
        PlacesPerGroup = 4,
        StageName = "Phase de groupes",
        TeamNames = TeamNameSource.Generated
    };

    /// <inheritdoc />
    public Task ExecuteAsync(ScenarioContext context, CancellationToken cancellationToken = default) =>
        ScenarioOrchestration.BuildFluxEmptyRelationsDraftAsync(context, cancellationToken);
}
