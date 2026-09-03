// -----------------------------------------------------------------------
// <copyright file="DeleteEntryTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Application.Competitions;
using MyClub.PlayUp.Application.Tests.Common;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Matches;
using Xunit;

namespace MyClub.PlayUp.Application.Tests.Competitions;

public sealed class DeleteEntryTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 9, 3, 10, 0, 0, TimeSpan.Zero));

    [Fact]
    public void DeleteEntry_without_matches_removes_the_row()
    {
        var competition = CreateCompetition.Execute("Cup", _clock);
        var entry = AddEntry.Execute(competition, "Alpha", _clock);

        DeleteEntry.Execute(competition, entry.Id, [], _clock);

        competition.Entries.Should().BeEmpty();
    }

    [Fact]
    public void DeleteEntry_with_match_is_refused()
    {
        var competition = CreateCompetition.Execute("Cup", _clock);
        var home = AddEntry.Execute(competition, "Alpha", _clock);
        var away = AddEntry.Execute(competition, "Beta", _clock);
        var match = Match.Create(competition.Id, StageId.New(), home.Id, away.Id, _clock);

        var act = () => DeleteEntry.Execute(competition, home.Id, [match], _clock);

        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.EntryHasSportingHistory);
        competition.GetEntry(home.Id).Should().NotBeNull();
    }

    [Fact]
    public void DeleteEntries_is_atomic_when_one_has_history()
    {
        var competition = CreateCompetition.Execute("Cup", _clock);
        var home = AddEntry.Execute(competition, "Alpha", _clock);
        var away = AddEntry.Execute(competition, "Beta", _clock);
        var extra = AddEntry.Execute(competition, "Gamma", _clock);
        var match = Match.Create(competition.Id, StageId.New(), home.Id, away.Id, _clock);

        var act = () => DeleteEntries.Execute(
            competition,
            [extra.Id, home.Id],
            [match],
            _clock);

        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.EntryHasSportingHistory);
        competition.Entries.Should().HaveCount(3);
    }

    [Fact]
    public void DeleteEntries_deletes_all_when_none_have_history()
    {
        var competition = CreateCompetition.Execute("Cup", _clock);
        var first = AddEntry.Execute(competition, "Alpha", _clock);
        var second = AddEntry.Execute(competition, "Beta", _clock);

        DeleteEntries.Execute(competition, [first.Id, second.Id], [], _clock);

        competition.Entries.Should().BeEmpty();
    }

    [Fact]
    public void WithdrawEntries_is_atomic_when_one_is_already_withdrawn()
    {
        var competition = CreateCompetition.Execute("Cup", _clock);
        var first = AddEntry.Execute(competition, "Alpha", _clock);
        var second = AddEntry.Execute(competition, "Beta", _clock);
        WithdrawEntry.Execute(competition, second.Id, _clock);

        var act = () => WithdrawEntries.Execute(competition, [first.Id, second.Id], _clock);

        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.EntryNotWithdrawable);
        competition.GetEntry(first.Id).Status.Should().Be(EntryStatus.Active);
    }

    [Fact]
    public void Empty_lot_is_refused()
    {
        var competition = CreateCompetition.Execute("Cup", _clock);

        var act = () => DeleteEntries.Execute(competition, [], [], _clock);

        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.EmptyEntryLot);
    }
}
