// -----------------------------------------------------------------------
// <copyright file="RegistrationOpenScenario.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Application.Competitions;
using MyClub.PlayUp.Development.Orchestration;
using MyClub.PlayUp.Development.Recipes;
using MyClub.PlayUp.Development.Runtime;
using MyClub.PlayUp.Domain.Rules;

namespace MyClub.PlayUp.Development.Scenarios;

/// <summary>
/// Championship intended for 16 teams with only 3 registered entries.
/// </summary>
public sealed class RegistrationOpenScenario : IScenario
{
    /// <inheritdoc />
    public string Id => "registration-open";

    /// <inheritdoc />
    public string Name => "Registration open";

    /// <inheritdoc />
    public string Description => "Partial registration (3/16) during construction.";

    /// <inheritdoc />
    public ScenarioCategory Category => ScenarioCategory.Construction;

    /// <inheritdoc />
    public bool AcceptsProgress => false;

    /// <inheritdoc />
    public CompetitionRecipe? Recipe { get; } = new()
    {
        DisplayName = "Inscriptions en cours",
        Format = RecipeFormat.Championship,
        TeamCount = 16,
        MatchdayCount = 1
    };

    /// <inheritdoc />
    public async Task ExecuteAsync(ScenarioContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();

        var regulation = new Regulation(
            new EntryRules(minimumTeams: 16, maximumTeams: 16),
            BootstrapRegulation.Standard().MatchRules,
            BootstrapRegulation.Standard().StandingRules);

        var competition = await ScenarioOrchestration.CreateCompetitionAsync(
            context,
            Recipe!.DisplayName,
            regulation,
            cancellationToken: cancellationToken).ConfigureAwait(false);
        await ScenarioOrchestration.RegisterTeamsAsync(
            context,
            competition,
            Recipe,
            countOverride: 3,
            cancellationToken: cancellationToken).ConfigureAwait(false);
        await context.UnitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
