// -----------------------------------------------------------------------
// <copyright file="ExclusiveWriteTrackingGuardTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Application.Abstractions;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Stages;
using MyClub.PlayUp.Infrastructure.Persistence;
using MyClub.PlayUp.Infrastructure.Tests.Common;
using Xunit;

namespace MyClub.PlayUp.Infrastructure.Tests.Persistence;

public sealed class ExclusiveWriteTrackingGuardTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 11, 18, 12, 0, 0, TimeSpan.Zero));

    [Fact]
    public async Task ExecuteExclusive_throws_when_a_stage_is_already_tracked()
    {
        await using var context = PlayUpInMemory.CreateContext();
        var stage = Stage.Create(
            CompetitionId.New(),
            new StageName("Poules"),
            SampleRegulations.Standard(),
            _clock);
        _ = context.Attach(stage);

        var act = () => ((IUnitOfWork)context).ExecuteExclusiveAsync(
            stage.CompetitionId,
            _ => Task.CompletedTask);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task ExecuteExclusive_throws_when_a_match_is_already_tracked()
    {
        await using var context = PlayUpInMemory.CreateContext();
        var competitionId = CompetitionId.New();
        var match = Match.Create(competitionId, StageId.New(), null, null, _clock);
        _ = context.Attach(match);

        var act = () => ((IUnitOfWork)context).ExecuteExclusiveAsync(
            competitionId,
            _ => Task.CompletedTask);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task ExecuteExclusive_runs_when_only_a_competition_is_tracked()
    {
        await using var context = PlayUpInMemory.CreateContext();
        var competition = Competition.Create(
            new CompetitionName("Guard"),
            SampleRegulations.Standard(),
            _clock);
        _ = context.Attach(competition);
        var ran = false;

        await ((IUnitOfWork)context).ExecuteExclusiveAsync(
            competition.Id,
            _ =>
            {
                ran = true;
                return Task.CompletedTask;
            });

        ran.Should().BeTrue();
    }

    [Fact]
    public async Task ClearTrackedChanges_lets_the_next_exclusive_write_start()
    {
        await using var context = PlayUpInMemory.CreateContext();
        var stage = Stage.Create(
            CompetitionId.New(),
            new StageName("Poules"),
            SampleRegulations.Standard(),
            _clock);
        _ = context.Attach(stage);
        ((IUnitOfWork)context).ClearTrackedChanges();
        var ran = false;

        await ((IUnitOfWork)context).ExecuteExclusiveAsync(
            stage.CompetitionId,
            _ =>
            {
                ran = true;
                return Task.CompletedTask;
            });

        ran.Should().BeTrue();
    }
}
