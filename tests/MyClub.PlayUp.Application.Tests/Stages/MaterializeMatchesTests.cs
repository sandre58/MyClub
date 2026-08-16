// -----------------------------------------------------------------------
// <copyright file="MaterializeMatchesTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Application.Competitions;
using MyClub.PlayUp.Application.Reads;
using MyClub.PlayUp.Application.Stages;
using MyClub.PlayUp.Application.Tests.Common;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Stages;
using Xunit;

namespace MyClub.PlayUp.Application.Tests.Stages;

public sealed class MaterializeMatchesTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 8, 16, 12, 0, 0, TimeSpan.Zero));

    [Fact]
    public void Championship_round_robin_creates_expected_matches_and_is_idempotent()
    {
        var competition = CreateCompetition.Execute("Champ", _clock);
        AddEntry.Execute(competition, "A", _clock);
        AddEntry.Execute(competition, "B", _clock);
        AddEntry.Execute(competition, "C", _clock);
        AddEntry.Execute(competition, "D", _clock);
        var configured = ConfigureStructure.Execute(
            competition,
            null,
            StructureIntent.Championship(1),
            _clock);

        var first = MaterializeMatches.Execute(competition, configured.Stage, [], _clock);
        first.CreatedMatches.Should().HaveCount(6); // 4*3/2
        first.AlreadyComplete.Should().BeFalse();

        var second = MaterializeMatches.Execute(
            competition,
            configured.Stage,
            first.CreatedMatches,
            _clock);
        second.CreatedMatches.Should().BeEmpty();
        second.AlreadyComplete.Should().BeTrue();
        second.AttachedMatchIds.Should().HaveCount(6);

        var view = OrganisationViewAssembler.Assemble(competition, [configured.Stage]);
        view.Readiness.ReadyForMatchOperation.Should().BeTrue();
        view.Readiness.AttachedMatchCount.Should().Be(6);
        view.Readiness.ReadyForSchedule.Should().BeTrue();
    }

    [Fact]
    public void Groups_materialize_after_manual_assignment()
    {
        var competition = CreateCompetition.Execute("Groups", _clock);
        var e1 = AddEntry.Execute(competition, "A", _clock);
        var e2 = AddEntry.Execute(competition, "B", _clock);
        var e3 = AddEntry.Execute(competition, "C", _clock);
        var e4 = AddEntry.Execute(competition, "D", _clock);
        var configured = ConfigureStructure.Execute(
            competition,
            null,
            StructureIntent.Groups(2, 2),
            _clock);
        var stage = configured.Stage;
        stage.AssignEntryToGroup(stage.Groups[0].Id, e1.Id, _clock);
        stage.AssignEntryToGroup(stage.Groups[0].Id, e2.Id, _clock);
        stage.AssignEntryToGroup(stage.Groups[1].Id, e3.Id, _clock);
        stage.AssignEntryToGroup(stage.Groups[1].Id, e4.Id, _clock);

        var result = MaterializeMatches.Execute(competition, stage, [], _clock);
        result.CreatedMatches.Should().HaveCount(2); // 1 pair per group of 2
    }

    [Fact]
    public void Cup_materialize_prepares_fixtures_without_matches_until_pairing_apply()
    {
        var competition = CreateCompetition.Execute("Cup", _clock);
        AddEntry.Execute(competition, "A", _clock);
        AddEntry.Execute(competition, "B", _clock);
        AddEntry.Execute(competition, "C", _clock);
        AddEntry.Execute(competition, "D", _clock);
        var configured = ConfigureStructure.Execute(
            competition,
            null,
            StructureIntent.Cup(4),
            _clock);

        var result = MaterializeMatches.Execute(competition, configured.Stage, [], _clock);
        result.CreatedMatches.Should().BeEmpty();
        configured.Stage.Rounds[0].Fixtures.Should().HaveCount(2);

        var view = OrganisationViewAssembler.Assemble(competition, [configured.Stage]);
        view.Readiness.ReadyForMatchOperation.Should().BeFalse();
    }

    [Fact]
    public void BuildRoundRobinPairs_count_is_n_times_n_minus_1_over_2()
    {
        var entries = Enumerable.Range(0, 5).Select(_ => EntryId.New()).ToArray();
        MaterializeMatches.BuildRoundRobinPairs(entries).Should().HaveCount(10);
    }
}
