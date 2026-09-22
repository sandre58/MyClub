// -----------------------------------------------------------------------
// <copyright file="ApplyQualificationForGroupTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Application.Stages;
using MyClub.PlayUp.Application.Tests.Common;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;
using MyClub.PlayUp.Domain.Standings;
using Xunit;

namespace MyClub.PlayUp.Application.Tests.Stages;

/// <summary>
/// A1 reference: Qual Place → Groups poule (dual-write Population + group membership).
/// </summary>
public sealed class ApplyQualificationForGroupTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 9, 22, 12, 0, 0, TimeSpan.Zero));

    [Fact]
    public void Execute_places_top_of_each_group_into_destination_poules()
    {
        var competitionId = CompetitionId.New();
        var source = Stage.Create(competitionId, new StageName("Poules"), SampleRegulations.Standard(), _clock);
        var groupA = source.AddGroup("A", _clock);
        var groupB = source.AddGroup("B", _clock);
        var eA1 = EntryId.New();
        var eA2 = EntryId.New();
        var eB1 = EntryId.New();
        var eB2 = EntryId.New();
        source.AssignEntryToGroup(groupA.Id, eA1);
        source.AssignEntryToGroup(groupA.Id, eA2);
        source.AssignEntryToGroup(groupB.Id, eB1);
        source.AssignEntryToGroup(groupB.Id, eB2);
        source.AddMatchday(1, _clock);

        var dest = Stage.Create(competitionId, new StageName("Poules 2"), SampleRegulations.Standard(), _clock);
        var destA = dest.AddGroup("Poule A", _clock);
        var destB = dest.AddGroup("Poule B", _clock);
        dest.AddMatchday(1, _clock);

        var standingA = ManualStanding([(eA1, 1, 6), (eA2, 2, 3)]);
        var standingB = ManualStanding([(eB1, 1, 6), (eB2, 2, 3)]);

        var intent = new QualificationIntent(
            IntentId.New(),
            order: 1,
            QualificationIntentSourceKind.EachGroup,
            positionFrom: 1,
            positionTo: 1,
            dest.Id,
            destinationGroupIds: [destA.Id, destB.Id]);
        source.ReplaceQualificationRules(
            QualificationRules.FromIntents([intent], [groupA.Id, groupB.Id]),
            _clock);

        var applied = ApplyQualification.Execute(
            source,
            overallStanding: null,
            new Dictionary<GroupId, Standing>
            {
                [groupA.Id] = standingA,
                [groupB.Id] = standingB
            },
            [source, dest],
            _clock);

        applied.Should().HaveCount(2);
        dest.FindGroup(destA.Id)!.EntryIds.Should().Contain(eA1);
        dest.FindGroup(destB.Id)!.EntryIds.Should().Contain(eB1);
        dest.CompositionEntries.Select(e => e.EntryId).Should().Contain(eA1).And.Contain(eB1);
    }

    private static Standing ManualStanding(IReadOnlyList<(EntryId EntryId, int Position, int Points)> rows) =>
        new(
        [
            .. rows.Select(r => new StandingRow(
                r.EntryId,
                r.Position,
                played: 0,
                wins: 0,
                draws: 0,
                losses: 0,
                goalsFor: 0,
                goalsAgainst: 0,
                points: r.Points))
        ]);
}
