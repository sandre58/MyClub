// -----------------------------------------------------------------------
// <copyright file="CompetitionOverviewAssemblerTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Application.Reads;
using MyClub.PlayUp.Application.Tests.Common;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Stages;
using Xunit;

namespace MyClub.PlayUp.Application.Tests.Reads;

public sealed class CompetitionOverviewAssemblerTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 8, 14, 22, 0, 0, TimeSpan.Zero));

    [Fact]
    public void Assemble_maps_competition_entries_and_stages()
    {
        var competition = Competition.Create(new CompetitionName("Cup"), SampleRegulations.Standard(), _clock);
        var home = competition.AddEntry(TeamId.New(), "Alpha FC", _clock);
        var away = competition.AddEntry(TeamId.New(), "Beta United", _clock);
        var qf = Stage.Create(competition.Id, new StageName("QF"), SampleRegulations.Standard(), _clock);
        var sf = Stage.Create(competition.Id, new StageName("SF"), SampleRegulations.Standard(), _clock);
        competition.AddStage(qf.Id, _clock);
        competition.AddStage(sf.Id, _clock);

        var overview = CompetitionOverviewAssembler.Assemble(competition, [qf, sf]);

        overview.Id.Should().Be(competition.Id.Value);
        overview.Name.Should().Be("Cup");
        overview.Status.Should().Be(CompetitionStatus.Draft);
        overview.Entries.Should().HaveCount(2);
        overview.Entries[0].EntryId.Should().Be(home.Id.Value);
        overview.Entries[0].DisplayName.Should().Be("Alpha FC");
        overview.Entries[1].EntryId.Should().Be(away.Id.Value);
        overview.Entries[1].DisplayName.Should().Be("Beta United");
        overview.Stages.Should().HaveCount(2);
        overview.Stages[0].StageId.Should().Be(qf.Id.Value);
        overview.Stages[0].Name.Should().Be("QF");
        overview.Stages[1].StageId.Should().Be(sf.Id.Value);
        overview.Stages[1].Name.Should().Be("SF");
    }

    [Fact]
    public void Assemble_when_stage_missing_throws_StageNotFound()
    {
        var competition = Competition.Create(new CompetitionName("Cup"), SampleRegulations.Standard(), _clock);
        var qf = Stage.Create(competition.Id, new StageName("QF"), SampleRegulations.Standard(), _clock);
        competition.AddStage(qf.Id, _clock);
        competition.AddStage(StageId.New(), _clock);

        var act = () => CompetitionOverviewAssembler.Assemble(competition, [qf]);

        act.Should().Throw<ApplicationFailureException>().Which.Code.Should().Be(ApplicationErrorCodes.StageNotFound);
    }
}
