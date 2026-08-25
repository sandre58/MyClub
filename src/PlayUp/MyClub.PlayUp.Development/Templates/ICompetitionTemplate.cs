// -----------------------------------------------------------------------
// <copyright file="ICompetitionTemplate.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Development.Recipes;
using MyClub.PlayUp.Development.Runtime;

namespace MyClub.PlayUp.Development.Templates;

/// <summary>
/// Inspired competition template (names + V1 single-stage structure). Domain approximation only.
/// </summary>
public interface ICompetitionTemplate
{
    /// <summary>Gets the stable template identity.</summary>
    string Id { get; }

    /// <summary>Gets the display name.</summary>
    string Name { get; }

    /// <summary>Gets a short description including Domain V1 limits.</summary>
    string Description { get; }

    /// <summary>Gets the structure recipe (dataset-backed).</summary>
    CompetitionRecipe Recipe { get; }

    /// <summary>
    /// Executes the template with <see cref="ScenarioContext.Progress"/>.
    /// </summary>
    /// <param name="context">Execution context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when the template is persisted.</returns>
    Task ExecuteAsync(ScenarioContext context, CancellationToken cancellationToken = default);
}
