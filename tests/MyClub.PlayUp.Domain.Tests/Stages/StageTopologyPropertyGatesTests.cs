// -----------------------------------------------------------------------
// <copyright file="StageTopologyPropertyGatesTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Stages;
using MyClub.PlayUp.Domain.Tests.Common;
using Xunit;

namespace MyClub.PlayUp.Domain.Tests.Stages;

/// <summary>
/// Topology-derived gates for MatchGenerationFormat / PlacesPerGroup (Option G — no product format enum).
/// </summary>
public sealed class StageTopologyPropertyGatesTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 9, 29, 12, 0, 0, TimeSpan.Zero));
    private readonly CompetitionId _competitionId = CompetitionId.New();

    [Fact]
    public void SetMatchGenerationFormat_on_matchdays_only_succeeds()
    {
        var stage = Stage.Create(_competitionId, new StageName("Champ"), SampleRegulations.Standard(), _clock);
        stage.AddMatchday(1, _clock);

        stage.SetMatchGenerationFormat(MatchGenerationFormat.DoubleRoundRobin);

        stage.MatchGenerationFormat.Should().Be(MatchGenerationFormat.DoubleRoundRobin);
    }

    [Fact]
    public void SetMatchGenerationFormat_on_groups_with_matchdays_succeeds()
    {
        var stage = Stage.Create(_competitionId, new StageName("Poules"), SampleRegulations.Standard(), _clock);
        stage.AddGroup("A", _clock);
        stage.AddMatchday(1, _clock);

        stage.SetMatchGenerationFormat(MatchGenerationFormat.DoubleRoundRobin);

        stage.MatchGenerationFormat.Should().Be(MatchGenerationFormat.DoubleRoundRobin);
    }

    [Fact]
    public void SetMatchGenerationFormat_on_unstructured_draft_succeeds()
    {
        var stage = Stage.Create(_competitionId, new StageName("Empty"), SampleRegulations.Standard(), _clock);

        stage.SetMatchGenerationFormat(MatchGenerationFormat.DoubleRoundRobin);

        stage.MatchGenerationFormat.Should().Be(MatchGenerationFormat.DoubleRoundRobin);
    }

    [Fact]
    public void SetMatchGenerationFormat_with_rounds_throws()
    {
        var stage = Stage.Create(_competitionId, new StageName("Cup"), SampleRegulations.Standard(), _clock);
        stage.AddRound("QF", _clock);

        var act = () => stage.SetMatchGenerationFormat(MatchGenerationFormat.DoubleRoundRobin);

        act.Should().Throw<DomainException>()
            .Which.Code.Should().Be(StageErrorCodes.MatchGenerationFormatNotApplicable);
    }

    [Fact]
    public void SetMatchGenerationFormat_with_swiss_throws()
    {
        var stage = Stage.Create(_competitionId, new StageName("Swiss"), SampleRegulations.Standard(), _clock);
        stage.SetSwissSettings(new SwissSettings(3));

        var act = () => stage.SetMatchGenerationFormat(MatchGenerationFormat.DoubleRoundRobin);

        act.Should().Throw<DomainException>()
            .Which.Code.Should().Be(StageErrorCodes.MatchGenerationFormatNotApplicable);
    }

    [Fact]
    public void SetMatchGenerationFormat_noop_when_unchanged_even_with_rounds()
    {
        var stage = Stage.Create(_competitionId, new StageName("Cup"), SampleRegulations.Standard(), _clock);
        stage.AddRound("QF", _clock);
        var current = stage.MatchGenerationFormat;

        stage.SetMatchGenerationFormat(current);

        stage.MatchGenerationFormat.Should().Be(current);
    }

    [Fact]
    public void SetPlacesPerGroup_with_groups_succeeds()
    {
        var stage = Stage.Create(_competitionId, new StageName("Poules"), SampleRegulations.Standard(), _clock);
        stage.AddGroup("A", _clock);

        stage.SetPlacesPerGroup(4);

        stage.PlacesPerGroup.Should().Be(4);
    }

    [Fact]
    public void SetPlacesPerGroup_without_groups_throws()
    {
        var stage = Stage.Create(_competitionId, new StageName("Empty"), SampleRegulations.Standard(), _clock);

        var act = () => stage.SetPlacesPerGroup(4);

        act.Should().Throw<DomainException>()
            .Which.Code.Should().Be(StageErrorCodes.PlacesPerGroupNotApplicable);
    }

    [Fact]
    public void SetPlacesPerGroup_null_clears_without_groups()
    {
        var stage = Stage.Create(_competitionId, new StageName("Empty"), SampleRegulations.Standard(), _clock);

        stage.SetPlacesPerGroup(null);

        stage.PlacesPerGroup.Should().BeNull();
    }

    [Fact]
    public void SetPlacesPerGroup_null_clears_after_groups_removed()
    {
        var stage = Stage.Create(_competitionId, new StageName("Poules"), SampleRegulations.Standard(), _clock);
        var group = stage.AddGroup("A", _clock);
        stage.SetPlacesPerGroup(3);
        stage.RemoveGroup(group.Id, _clock);

        stage.SetPlacesPerGroup(null);

        stage.PlacesPerGroup.Should().BeNull();
    }

    [Fact]
    public void SetPlacesPerGroup_below_minimum_throws()
    {
        var stage = Stage.Create(_competitionId, new StageName("Poules"), SampleRegulations.Standard(), _clock);
        stage.AddGroup("A", _clock);

        var act = () => stage.SetPlacesPerGroup(1);

        act.Should().Throw<DomainException>()
            .Which.Code.Should().Be(StageErrorCodes.InvalidConfiguration);
    }

    [Fact]
    public void SetMatchGenerationFormat_unknown_enum_throws()
    {
        var stage = Stage.Create(_competitionId, new StageName("Champ"), SampleRegulations.Standard(), _clock);

        var act = () => stage.SetMatchGenerationFormat((MatchGenerationFormat)99);

        act.Should().Throw<DomainException>()
            .Which.Code.Should().Be(StageErrorCodes.InvalidMatchGenerationFormat);
    }
}
