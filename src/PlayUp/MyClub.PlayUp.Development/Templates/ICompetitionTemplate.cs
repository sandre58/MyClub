// -----------------------------------------------------------------------
// <copyright file="ICompetitionTemplate.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Development.Recipes;
using MyClub.PlayUp.Development.Runtime;

namespace MyClub.PlayUp.Development.Templates;

/// <summary>
/// Inspired competition seed (real-world names / logos when a dataset exists).
/// Approximates structure modulo Domain capacity — not a UX mid-state catalog (see <see cref="IScenario"/>).
/// </summary>
public interface ICompetitionTemplate
{
    /// <summary>Gets the stable template identity.</summary>
    string Id { get; }

    /// <summary>Gets the display name.</summary>
    string Name { get; }

    /// <summary>Gets a short description including Domain approximation limits.</summary>
    string Description { get; }

    /// <summary>Gets the structure recipe (often dataset-backed).</summary>
    CompetitionRecipe Recipe { get; }

    /// <summary>
    /// Executes the template with <see cref="ScenarioContext.Progress"/>.
    /// </summary>
    /// <param name="context">Execution context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when the template is persisted.</returns>
    Task ExecuteAsync(ScenarioContext context, CancellationToken cancellationToken = default);
}
