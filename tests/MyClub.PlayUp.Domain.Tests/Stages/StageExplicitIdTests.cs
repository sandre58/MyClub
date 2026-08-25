// -----------------------------------------------------------------------
// <copyright file="StageExplicitIdTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Stages;
using MyClub.PlayUp.Domain.Stages.Events;
using MyClub.PlayUp.Domain.Tests.Common;
using Xunit;

namespace MyClub.PlayUp.Domain.Tests.Stages;

public sealed class StageExplicitIdTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 8, 6, 10, 0, 0, TimeSpan.Zero));

    [Fact]
    public void Create_with_explicit_id_uses_that_identity()
    {
        var competitionId = CompetitionId.New();
        var id = new StageId(Guid.Parse("44444444-4444-5444-8444-444444444444"));

        var stage = Stage.Create(
            competitionId,
            new StageName("Groups"),
            SampleRegulations.Standard(),
            id,
            _clock);

        stage.Id.Should().Be(id);
        stage.CompetitionId.Should().Be(competitionId);
        stage.Status.Should().Be(StageStatus.Draft);
        stage.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<StageCreated>();
    }

    [Fact]
    public void Create_with_empty_id_throws()
    {
        var act = () => Stage.Create(
            CompetitionId.New(),
            new StageName("Groups"),
            SampleRegulations.Standard(),
            new StageId(Guid.Empty),
            _clock);

        act.Should().Throw<DomainException>();
    }
}
