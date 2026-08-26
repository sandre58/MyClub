// -----------------------------------------------------------------------
// <copyright file="StageSwissTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Stages;
using MyClub.PlayUp.Domain.Tests.Common;
using Xunit;

namespace MyClub.PlayUp.Domain.Tests.Stages;

public sealed class StageSwissTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 8, 26, 14, 0, 0, TimeSpan.Zero));
    private readonly CompetitionId _competitionId = CompetitionId.New();

    [Fact]
    public void SwissSettings_rejects_non_positive_round_count()
    {
        var act = () => new SwissSettings(0);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(StageErrorCodes.SwissSettingsInvalid);
    }

    [Fact]
    public void SetSwissSettings_marks_stage_as_swiss()
    {
        var stage = CreateDraft();

        stage.SetSwissSettings(new SwissSettings(3), _clock);

        stage.IsSwiss.Should().BeTrue();
        stage.SwissSettings!.RoundCount.Should().Be(3);
        stage.AddMatchday(1, _clock);
        stage.Matchdays.Should().ContainSingle();
    }

    [Fact]
    public void SetSwissSettings_rejects_when_groups_present()
    {
        var stage = CreateDraft();
        stage.AddGroup("A", _clock);

        var act = () => stage.SetSwissSettings(new SwissSettings(3), _clock);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(StageErrorCodes.InvalidComposition);
    }

    [Fact]
    public void SetSwissSettings_rejects_when_rounds_present()
    {
        var stage = CreateDraft();
        stage.AddRound("QF", _clock);

        var act = () => stage.SetSwissSettings(new SwissSettings(3), _clock);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(StageErrorCodes.InvalidComposition);
    }

    [Fact]
    public void SetSwissSettings_rejects_when_slots_present()
    {
        var stage = CreateDraft();
        stage.AddSlot("SF1-A", _clock);

        var act = () => stage.SetSwissSettings(new SwissSettings(3), _clock);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(StageErrorCodes.InvalidComposition);
    }

    [Fact]
    public void AddGroup_rejects_when_swiss()
    {
        var stage = CreateSwiss(3);

        var act = () => stage.AddGroup("A", _clock);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(StageErrorCodes.InvalidComposition);
    }

    [Fact]
    public void AddRound_rejects_when_swiss()
    {
        var stage = CreateSwiss(3);

        var act = () => stage.AddRound("QF", _clock);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(StageErrorCodes.InvalidComposition);
    }

    [Fact]
    public void AddSlot_rejects_when_swiss()
    {
        var stage = CreateSwiss(3);

        var act = () => stage.AddSlot("SF1-A", _clock);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(StageErrorCodes.InvalidComposition);
    }

    [Fact]
    public void ClearSwissSettings_succeeds_when_bye_history_empty()
    {
        var stage = CreateSwiss(3);

        stage.SetSwissSettings(null, _clock);

        stage.IsSwiss.Should().BeFalse();
        stage.SwissSettings.Should().BeNull();
    }

    [Fact]
    public void ClearSwissSettings_rejects_when_bye_history_present()
    {
        var stage = CreateSwiss(3);
        stage.RecordSwissBye(1, EntryId.New(), _clock);

        var act = () => stage.SetSwissSettings(null, _clock);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(StageErrorCodes.SwissSettingsInvalid);
    }

    [Fact]
    public void RecordSwissBye_persists_history_and_counts()
    {
        var stage = CreateSwiss(3);
        var entry = EntryId.New();

        stage.RecordSwissBye(1, entry, _clock);

        stage.SwissByeHistory.Should().ContainSingle()
            .Which.Should().Be(new SwissBye(1, entry));
        stage.CountSwissByes(entry).Should().Be(1);
        stage.CountSwissByes(EntryId.New()).Should().Be(0);
    }

    [Fact]
    public void RecordSwissBye_rejects_duplicate_round()
    {
        var stage = CreateSwiss(3);
        stage.RecordSwissBye(1, EntryId.New(), _clock);

        var act = () => stage.RecordSwissBye(1, EntryId.New(), _clock);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(StageErrorCodes.SwissByeInvalid);
    }

    [Fact]
    public void RecordSwissBye_rejects_out_of_range_round()
    {
        var stage = CreateSwiss(2);

        var act = () => stage.RecordSwissBye(3, EntryId.New(), _clock);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(StageErrorCodes.SwissByeInvalid);
    }

    [Fact]
    public void RecordSwissBye_rejects_when_not_swiss()
    {
        var stage = CreateDraft();

        var act = () => stage.RecordSwissBye(1, EntryId.New(), _clock);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(StageErrorCodes.SwissByeInvalid);
    }

    [Fact]
    public void RecordSwissBye_allowed_while_running()
    {
        var stage = CreateSwiss(3);
        stage.Prepare(_clock);
        stage.Start(_clock);

        var entry = EntryId.New();
        stage.RecordSwissBye(1, entry, _clock);

        stage.SwissByeHistory.Should().ContainSingle().Which.EntryId.Should().Be(entry);
        stage.Status.Should().Be(StageStatus.Running);
    }

    [Fact]
    public void Prepare_allows_swiss_without_matchdays()
    {
        var stage = CreateSwiss(3);

        stage.Prepare(_clock);

        stage.Status.Should().Be(StageStatus.Ready);
        stage.Matchdays.Should().BeEmpty();
    }

    [Fact]
    public void AddSwissRoundMatchday_allowed_while_running()
    {
        var stage = CreateSwiss(2);
        stage.Prepare(_clock);
        stage.Start(_clock);

        var matchday = stage.AddSwissRoundMatchday(_clock);

        matchday.Number.Should().Be(1);
        var blocked = () => stage.AddMatchday(2, _clock);
        blocked.Should().Throw<DomainException>().Which.Code.Should().Be(StageErrorCodes.StructureLocked);
    }

    private Stage CreateDraft() =>
        Stage.Create(_competitionId, new StageName("Swiss"), SampleRegulations.Standard(), _clock);

    private Stage CreateSwiss(int rounds)
    {
        var stage = CreateDraft();
        stage.SetSwissSettings(new SwissSettings(rounds), _clock);
        return stage;
    }
}
