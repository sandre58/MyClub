// -----------------------------------------------------------------------
// <copyright file="StructureCompositionGaugeAssemblerTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Application.Competitions;
using MyClub.PlayUp.Application.Reads;
using MyClub.PlayUp.Application.Stages;
using MyClub.PlayUp.Application.Tests.Common;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;
using MyClub.PlayUp.Domain.Standings;
using MyClub.PlayUp.TestKit;
using Xunit;

namespace MyClub.PlayUp.Application.Tests.Reads;

public sealed class StructureCompositionGaugeAssemblerTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 10, 18, 14, 0, 0, TimeSpan.Zero));

    [Fact]
    public void Resolve_root_championship_withdrawn_member_eligible_below_resolved()
    {
        var situation = TestCompetition.Create("Ch gauge", _clock)
            .WithTeams("A", "B", "C", "D", "E", "F", "G")
            .WithStructure(StructureIntent.Championship())
            .PreparePrimaryStage();
        situation.Competition.Prepare(_clock);
        situation.Competition.Start(_clock);
        var withdrawn = situation.Competition.Entries.OrderBy(e => e.Id.Value).First().Id;
        WithdrawEntry.Execute(situation.Competition, withdrawn, situation.Stages, [], _clock);

        var view = StructureViewAssembler.Assemble(situation.Competition, situation.Stages);
        var hub = view.Stages.Single();

        hub.CompositionEntryCount.Should().Be(7);
        hub.CompositionCapacity.Should().Be(7);
        hub.CompositionIneligibleCount.Should().Be(1);
        hub.CompositionBandResolvedCount.Should().Be(7);
    }

    [Fact]
    public void Resolve_qual_fed_championship_after_withdraw_keeps_inbound_resolved_band()
    {
        var competition = Competition.Create(
            new CompetitionName("Qual gauge"),
            SampleRegulations.Standard(),
            _clock);
        var (source, groupA, groupB, tops, standings) = CreateGroupsSource(competition);
        var champ = Stage.Create(competition.Id, new StageName("Championnat"), SampleRegulations.Standard(), _clock);
        champ.AddMatchday(1, _clock);
        competition.AddStage(source.Id, _clock);
        competition.AddStage(champ.Id, _clock);
        ReplaceEachGroupTop1Form(source, groupA.Id, groupB.Id, champ.Id);

        ApplyQualification.Execute(source, null, standings, [source, champ], _clock);
        competition.Prepare(_clock);
        competition.Start(_clock);
        WithdrawEntry.Execute(competition, tops[0], [source, champ], [], _clock);

        var view = StructureViewAssembler.Assemble(competition, [source, champ]);
        var hub = view.Stages.Single(s => s.StageId == champ.Id.Value);

        hub.InboundComposition.Should().NotBeNull();
        hub.InboundComposition.ResolvedCount.Should().Be(2);
        hub.CompositionBandResolvedCount.Should().Be(2);
        hub.CompositionIneligibleCount.Should().Be(1);
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

    private (Stage Source, Group GroupA, Group GroupB, EntryId[] Tops, Dictionary<GroupId, Standing> Standings)
        CreateGroupsSource(Competition competition)
    {
        var source = Stage.Create(competition.Id, new StageName("Poules"), SampleRegulations.Standard(), _clock);
        var groupA = source.AddGroup("A", _clock);
        var groupB = source.AddGroup("B", _clock);
        var eA1 = competition.AddEntry(TeamId.New(), "A1", _clock).Id;
        var eA2 = competition.AddEntry(TeamId.New(), "A2", _clock).Id;
        var eB1 = competition.AddEntry(TeamId.New(), "B1", _clock).Id;
        var eB2 = competition.AddEntry(TeamId.New(), "B2", _clock).Id;
        source.AssignEntryToGroup(groupA.Id, eA1);
        source.AssignEntryToGroup(groupA.Id, eA2);
        source.AssignEntryToGroup(groupB.Id, eB1);
        source.AssignEntryToGroup(groupB.Id, eB2);
        source.AddMatchday(1, _clock);

        var standingA = ManualStanding([(eA1, 1, 6), (eA2, 2, 3)]);
        var standingB = ManualStanding([(eB1, 1, 6), (eB2, 2, 3)]);
        return (
            source,
            groupA,
            groupB,
            [eA1, eB1],
            new Dictionary<GroupId, Standing>
            {
                [groupA.Id] = standingA,
                [groupB.Id] = standingB
            });
    }

    private void ReplaceEachGroupTop1Form(
        Stage source,
        GroupId groupA,
        GroupId groupB,
        StageId destinationStageId)
    {
        var intent = new QualificationIntent(
            IntentId.New(),
            order: 1,
            QualificationIntentSourceKind.EachGroup,
            positionFrom: 1,
            positionTo: 1,
            destinationStageId,
            destinationForm: true);
        source.ReplaceQualificationRules(
            QualificationRules.FromIntents([intent], [groupA, groupB]),
            _clock);
    }
}
