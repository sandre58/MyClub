// -----------------------------------------------------------------------
// <copyright file="SeedExecution.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;
using MyClub.PlayUp.Application.Abstractions;
using MyClub.PlayUp.Development.Datasets;

namespace MyClub.PlayUp.Development.Runtime;

/// <summary>
/// Shared scope + context wiring for scenario/template execution.
/// </summary>
internal static class SeedExecution
{
    private static readonly DateTimeOffset DefaultEpoch = new(2026, 8, 1, 12, 0, 0, TimeSpan.Zero);

    public static async Task ExecuteAsync(
        IServiceScopeFactory scopeFactory,
        DatasetCatalog datasets,
        int workspaceSeed,
        string catalogId,
        SeedProgress progress,
        Func<ScenarioContext, CancellationToken, Task> execute,
        CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var competitions = scope.ServiceProvider.GetRequiredService<ICompetitionRepository>();
        var stages = scope.ServiceProvider.GetRequiredService<IStageRepository>();
        var matches = scope.ServiceProvider.GetRequiredService<IMatchRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var logos = scope.ServiceProvider.GetRequiredService<SeedLogoImporter>();

        var clock = new ControlledClock(DefaultEpoch);
        var ids = new DeterministicIdFactory(catalogId, workspaceSeed);
        var entropy = new DeterministicEntropy(catalogId, workspaceSeed);
        var context = new ScenarioContext(
            catalogId,
            workspaceSeed,
            progress,
            competitions,
            stages,
            matches,
            unitOfWork,
            clock,
            ids,
            entropy,
            datasets,
            logos);

        await execute(context, cancellationToken).ConfigureAwait(false);
    }
}
