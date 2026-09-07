// -----------------------------------------------------------------------
// <copyright file="RegulationDemoTemplate.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Development.Orchestration;
using MyClub.PlayUp.Development.Recipes;
using MyClub.PlayUp.Development.Runtime;

namespace MyClub.PlayUp.Development.Templates;

/// <summary>
/// QA seed for the Règlement hub: classifying poules + KO finale, ET+TAB, stays Draft.
/// </summary>
public sealed class RegulationDemoTemplate : ICompetitionTemplate
{
    /// <inheritdoc />
    public string Id => "regulation-demo";

    /// <inheritdoc />
    public string Name => "Démo Règlement";

    /// <inheritdoc />
    public string Description =>
        "Poules 2×4 → Finale · ET+TAB · Draft (ReplaceRegulation). :progress ignored.";

    /// <inheritdoc />
    public CompetitionRecipe Recipe { get; } = new()
    {
        DisplayName = "Démo Règlement",
        Format = RecipeFormat.Groups,
        TeamCount = 8,
        GroupCount = 2,
        ParticipantsPerGroup = 4,
        StageName = "Poules",
        TeamNames = TeamNameSource.Generated
    };

    /// <inheritdoc />
    public Task ExecuteAsync(ScenarioContext context, CancellationToken cancellationToken = default) =>
        ScenarioOrchestration.BuildRegulationHubDemoAsync(context, cancellationToken);
}
