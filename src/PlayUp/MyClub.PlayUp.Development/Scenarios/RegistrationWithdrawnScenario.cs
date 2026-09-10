// -----------------------------------------------------------------------
// <copyright file="RegistrationWithdrawnScenario.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Development.Orchestration;
using MyClub.PlayUp.Development.Recipes;
using MyClub.PlayUp.Development.Runtime;

namespace MyClub.PlayUp.Development.Scenarios;

/// <summary>
/// Full roster then one withdrawal — active count below EntryRules minimum.
/// </summary>
public sealed class RegistrationWithdrawnScenario : IScenario
{
    /// <inheritdoc />
    public string Id => "registration-withdrawn";

    /// <inheritdoc />
    public string Name => "Registration withdrawn";

    /// <inheritdoc />
    public string Description => "Championship Running mid-results then 1 withdrawn (forfait) — operational problem.";

    /// <inheritdoc />
    public ScenarioCategory Category => ScenarioCategory.Operational;

    /// <inheritdoc />
    public bool AcceptsProgress => false;

    /// <inheritdoc />
    public CompetitionRecipe? Recipe => null;

    /// <inheritdoc />
    public Task ExecuteAsync(ScenarioContext context, CancellationToken cancellationToken = default) =>
        ScenarioOrchestration.BuildRegistrationWithdrawnAsync(context, cancellationToken);
}
