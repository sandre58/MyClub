// -----------------------------------------------------------------------
// <copyright file="RegulationTieHomogeneousScenario.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Development.Orchestration;
using MyClub.PlayUp.Development.Recipes;
using MyClub.PlayUp.Development.Runtime;

namespace MyClub.PlayUp.Development.Scenarios;

/// <summary>
/// Regulation Confrontation tokens QA: one KO round with a rich TwoLegs TieFormat.
/// </summary>
public sealed class RegulationTieHomogeneousScenario : IScenario
{
    /// <inheritdoc />
    public string Id => "regulation-tie-homogeneous";

    /// <inheritdoc />
    public string Name => "Démo Confrontation homogène";

    /// <inheritdoc />
    public string Description =>
        "Finale 1 round · A/R riche (agregat, buts ext., ET/TAB) · Draft — jetons Confrontation.";

    /// <inheritdoc />
    public ScenarioCategory Category => ScenarioCategory.Construction;

    /// <inheritdoc />
    public bool AcceptsProgress => false;

    /// <inheritdoc />
    public CompetitionRecipe? Recipe => null;

    /// <inheritdoc />
    public Task ExecuteAsync(ScenarioContext context, CancellationToken cancellationToken = default) =>
        ScenarioOrchestration.BuildRegulationTieHomogeneousDemoAsync(context, cancellationToken);
}
