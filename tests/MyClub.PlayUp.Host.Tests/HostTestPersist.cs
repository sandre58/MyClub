// -----------------------------------------------------------------------
// <copyright file="HostTestPersist.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;
using MyClub.PlayUp.Application.Abstractions;
using MyClub.PlayUp.TestKit;

namespace MyClub.PlayUp.Host.Tests;

/// <summary>
/// Persists a <see cref="TestCompetition"/> graph via Host DI ports.
/// </summary>
internal static class HostTestPersist
{
    public static async Task PersistAsync(
        IServiceProvider services,
        TestCompetition situation,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(situation);

        var competitions = services.GetRequiredService<ICompetitionRepository>();
        var stages = services.GetRequiredService<IStageRepository>();
        var matches = services.GetRequiredService<IMatchRepository>();
        var unitOfWork = services.GetRequiredService<IUnitOfWork>();

        competitions.Add(situation.Competition);

        if (situation.Stages.Count > 0)
        {
            foreach (var stage in situation.Stages)
            {
                stages.Add(stage);
            }
        }
        else if (situation.PrimaryStage is not null)
        {
            stages.Add(situation.PrimaryStage);
        }

        foreach (var match in situation.Matches)
        {
            matches.Add(match);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
