// -----------------------------------------------------------------------
// <copyright file="HostStageHttpHelpers.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;
using MyClub.PlayUp.Application.Abstractions;
using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Host.Tests;

/// <summary>
/// HTTP helpers for stage lifecycle in integration tests (phase V2).
/// </summary>
internal static class HostStageHttpHelpers
{
    public static Task<HttpResponseMessage> PostPrepareAsync(HttpClient client, Guid stageId) =>
        client.PostAsync($"/stages/{stageId}/prepare", content: null);

    public static Task<HttpResponseMessage> PostStartAsync(HttpClient client, Guid stageId) =>
        client.PostAsync($"/stages/{stageId}/start", content: null);

    /// <summary>
    /// Fills root championship/groups composition so Prepare passes the places gate (Host has no public composition API yet).
    /// </summary>
    public static async Task FillRootCompositionFromActiveEntriesAsync(
        PlayUpWebApplicationFactory factory,
        Guid competitionId,
        Guid stageId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(factory);

        await using var scope = factory.Services.CreateAsyncScope();
        var competitions = scope.ServiceProvider.GetRequiredService<ICompetitionRepository>();
        var stages = scope.ServiceProvider.GetRequiredService<IStageRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var clock = scope.ServiceProvider.GetRequiredService<IClock>();

        var competition = await competitions
            .GetByIdForUpdateAsync(new CompetitionId(competitionId), cancellationToken)
            .ConfigureAwait(false);
        var stage = await stages
            .GetByIdForUpdateAsync(new StageId(stageId), cancellationToken)
            .ConfigureAwait(false);

        if (competition is null || stage is null)
        {
            throw new InvalidOperationException("Competition or stage not found for composition seed.");
        }

        foreach (var entry in competition.Entries.Where(entry => entry.Status == EntryStatus.Active))
        {
            stage.AddResolvedPopulationEntry(entry.Id, clock);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
