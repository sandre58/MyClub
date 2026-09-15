// -----------------------------------------------------------------------
// <copyright file="ConfrontationMultiRoundTemplate.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Development.Orchestration;
using MyClub.PlayUp.Development.Recipes;
using MyClub.PlayUp.Development.Runtime;

namespace MyClub.PlayUp.Development.Templates;

/// <summary>
/// QA seed for Structure Confrontation: one phase, three rounds with distinct TieFormats.
/// </summary>
public sealed class ConfrontationMultiRoundTemplate : ICompetitionTemplate
{
    /// <inheritdoc />
    public string Id => "confrontation-multi-round";

    /// <inheritdoc />
    public string Name => "Démo Confrontation multi-tours";

    /// <inheritdoc />
    public string Description =>
        "Cup 8 · Tableau QF (1 manche) / SF (A/R + buts ext. + ET) / Finale (1 manche + ET/TAB) · défaut A/R · Draft. :progress ignored.";

    /// <inheritdoc />
    public CompetitionRecipe Recipe { get; } = new()
    {
        DisplayName = "Démo Confrontation multi-tours",
        Format = RecipeFormat.Cup,
        TeamCount = 8,
        BracketSize = 8,
        StageName = "Tableau",
        TeamNames = TeamNameSource.Generated
    };

    /// <inheritdoc />
    public Task ExecuteAsync(ScenarioContext context, CancellationToken cancellationToken = default) =>
        ScenarioOrchestration.BuildConfrontationMultiRoundDemoAsync(context, cancellationToken);
}
