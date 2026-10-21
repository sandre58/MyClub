// -----------------------------------------------------------------------
// <copyright file="CompetitionListAssemblerTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Application.Reads;
using MyClub.PlayUp.Application.Tests.Common;
using MyClub.PlayUp.Domain.Competitions;
using Xunit;

namespace MyClub.PlayUp.Application.Tests.Reads;

public sealed class CompetitionListAssemblerTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 9, 2, 8, 0, 0, TimeSpan.Zero));

    [Fact]
    public void Assemble_maps_declared_schedule()
    {
        var competition = Competition.Create(
            new CompetitionName("Spring Cup"),
            SampleRegulations.Standard(),
            _clock);
        var start = new DateTimeOffset(2026, 9, 12, 0, 0, 0, TimeSpan.Zero);
        var end = new DateTimeOffset(2027, 3, 28, 0, 0, 0, TimeSpan.Zero);
        competition.SetSchedule(start, end, _clock);

        var items = CompetitionListAssembler.Assemble([competition]);

        items.Should().ContainSingle();
        items[0].Name.Should().Be("Spring Cup");
        items[0].ScheduledStart.Should().Be(start);
        items[0].ScheduledEnd.Should().Be(end);
    }

    [Fact]
    public void Assemble_leaves_schedule_null_when_unset()
    {
        var competition = Competition.Create(
            new CompetitionName("Draft Cup"),
            SampleRegulations.Standard(),
            _clock);

        var items = CompetitionListAssembler.Assemble([competition]);

        items[0].ScheduledStart.Should().BeNull();
        items[0].ScheduledEnd.Should().BeNull();
    }
}
