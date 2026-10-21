// -----------------------------------------------------------------------
// <copyright file="IScenario.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Development.Recipes;

namespace MyClub.PlayUp.Development.Runtime;

/// <summary>
/// Named Domain-first scenario that builds an observable business situation.
/// </summary>
public interface IScenario
{
    /// <summary>Gets the stable scenario identity (catalog key).</summary>
    string Id { get; }

    /// <summary>Gets the display name.</summary>
    string Name { get; }

    /// <summary>Gets a short description.</summary>
    string Description { get; }

    /// <summary>Gets the scenario category.</summary>
    ScenarioCategory Category { get; }

    /// <summary>Gets a value indicating whether <see cref="SeedProgress"/> applies.</summary>
    bool AcceptsProgress { get; }

    /// <summary>Gets the structure recipe (may be empty for workspace-only scenarios).</summary>
    CompetitionRecipe? Recipe { get; }

    /// <summary>
    /// Executes the scenario against ports in <paramref name="context"/> and commits once via <see cref="ScenarioContext.UnitOfWork"/>.
    /// </summary>
    /// <param name="context">Execution context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when the scenario is persisted.</returns>
    Task ExecuteAsync(ScenarioContext context, CancellationToken cancellationToken = default);
}
