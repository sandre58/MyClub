// -----------------------------------------------------------------------
// <copyright file="ScenarioOrchestration.DeferredParticipantsRecette.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Application.Stages;
using MyClub.PlayUp.Application.Standings;
using MyClub.PlayUp.Development.Runtime;
using MyClub.PlayUp.Development.Scenarios;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;
using MyClub.PlayUp.Domain.Standings;
using MyClub.PlayUp.TestKit;

namespace MyClub.PlayUp.Development.Orchestration;

internal static partial class ScenarioOrchestration
{
    /// <summary>
    /// Seeds four <c>RECETTE T5 — …</c> competitions.
    /// The both-sides competition applies one slot, then the other, without a coherence conflict.
    /// </summary>
    public static async Task BuildDeferredParticipantsRecetteAsync(
        ScenarioContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();

        await SeedDeferredCupAsync(context, DeferredParticipantsRecetteNames.Planned, "t5-planned", DeferredCupStop.Planned, cancellationToken)
            .ConfigureAwait(false);
        await context.UnitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await SeedDeferredCupAsync(context, DeferredParticipantsRecetteNames.OneSide, "t5-one-side", DeferredCupStop.OneSide, cancellationToken)
            .ConfigureAwait(false);
        await context.UnitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await SeedDeferredCupAsync(context, DeferredParticipantsRecetteNames.BothSides, "t5-both", DeferredCupStop.BothSides, cancellationToken)
            .ConfigureAwait(false);
        await context.UnitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await SeedDeferredPoolStandingAsync(context, cancellationToken).ConfigureAwait(false);
        await context.UnitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private enum DeferredCupStop
    {
        Planned,
        OneSide,
        BothSides
    }

    private static async Task SeedDeferredCupAsync(
        ScenarioContext context,
        string competitionName,
        string competitionLocalKey,
        DeferredCupStop stop,
        CancellationToken cancellationToken)
    {
        var competition = await CreateCompetitionAsync(
                context,
                competitionName,
                competitionLocalKey: competitionLocalKey,
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        var home = competition.AddEntry(TeamId.New(), "Alpha", context.Clock).Id;
        var away = competition.AddEntry(TeamId.New(), "Beta", context.Clock).Id;
        var quarter = new[]
        {
            competition.AddEntry(TeamId.New(), "Gamma", context.Clock).Id,
            competition.AddEntry(TeamId.New(), "Delta", context.Clock).Id,
            competition.AddEntry(TeamId.New(), "Epsilon", context.Clock).Id,
            competition.AddEntry(TeamId.New(), "Zeta", context.Clock).Id,
        };
        var league = Stage.Create(competition.Id, new StageName("Poule"), RegulationPacks.Standard(), context.Clock);
        var cup = Stage.Create(competition.Id, new StageName("Demi-finale"), RegulationPacks.Standard(), context.Clock);
        competition.AddStage(league.Id, context.Clock);
        competition.AddStage(cup.Id, context.Clock);
        league.AddMatchday(1, context.Clock);
        var played = Match.Create(competition.Id, league.Id, home, away, context.Clock);
        played.Start(context.Clock);
        played.Finish(new MatchResult(ResultType.Played, new Score(2, 0)), context.Clock);
        var stages = new[] { league, cup };
        InstallQuarterToSemi(cup, quarter, context.Clock);
        var standing = CalculateStanding.Execute(
            [home, away],
            [played],
            competition.Regulation.StandingRules);
        if (stop == DeferredCupStop.OneSide)
        {
            league.ReplaceQualificationRules(
                new QualificationRules([DeferredQualPath(1, 1, cup, "SF-A")]),
                context.Clock);
        }
        else
        {
            league.ReplaceQualificationRules(DeferredSemiRules(cup), context.Clock);
        }

        PrepareStage.Execute(cup, stages, context.Clock);
        var created = MaterializeCupFromOccupiedSlots.Execute(
            competition,
            cup,
            ["Q1", "Q2", "S1"],
            [],
            context.Clock,
            stages);
        var match = SemiMatch(cup, created.CreatedMatches);

        if (stop != DeferredCupStop.Planned)
        {
            ApplyQualification.Execute(
                league,
                standing,
                new Dictionary<GroupId, Standing>(),
                matches: null,
                stages,
                context.Clock,
                destinationMatches: [match]);
            if (stop == DeferredCupStop.OneSide)
            {
                league.ReplaceQualificationRules(DeferredSemiRules(cup), context.Clock);
            }
        }

        SeedLifecycle.PrepareAndStart(context, competition, league, stages);
        context.Stages.Add(league);
        context.Stages.Add(cup);
        context.Matches.Add(played);
        foreach (var createdMatch in created.CreatedMatches)
        {
            context.Matches.Add(createdMatch);
        }
    }

    private static async Task SeedDeferredPoolStandingAsync(
        ScenarioContext context,
        CancellationToken cancellationToken)
    {
        var competition = await CreateCompetitionAsync(
                context,
                DeferredParticipantsRecetteNames.PoolStanding,
                competitionLocalKey: "t5-pool",
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        var home = competition.AddEntry(TeamId.New(), "Poule A", context.Clock).Id;
        var away = competition.AddEntry(TeamId.New(), "Poule B", context.Clock).Id;
        var third = competition.AddEntry(TeamId.New(), "Poule C", context.Clock).Id;
        var fourth = competition.AddEntry(TeamId.New(), "Poule D", context.Clock).Id;
        var quarter = new[]
        {
            competition.AddEntry(TeamId.New(), "Quart A", context.Clock).Id,
            competition.AddEntry(TeamId.New(), "Quart B", context.Clock).Id,
            competition.AddEntry(TeamId.New(), "Quart C", context.Clock).Id,
            competition.AddEntry(TeamId.New(), "Quart D", context.Clock).Id,
        };
        var groups = Stage.Create(competition.Id, new StageName("Poules"), RegulationPacks.Standard(), context.Clock);
        var cup = Stage.Create(competition.Id, new StageName("Demi-finale"), RegulationPacks.Standard(), context.Clock);
        competition.AddStage(groups.Id, context.Clock);
        competition.AddStage(cup.Id, context.Clock);
        var group = groups.AddGroup("A", context.Clock);
        groups.AssignEntryToGroup(group.Id, home);
        groups.AssignEntryToGroup(group.Id, away);
        groups.AssignEntryToGroup(group.Id, third);
        groups.AssignEntryToGroup(group.Id, fourth);
        groups.AddMatchday(1, context.Clock);
        var played = Match.Create(competition.Id, groups.Id, home, away, context.Clock);
        played.Start(context.Clock);
        played.Finish(new MatchResult(ResultType.Played, new Score(1, 0)), context.Clock);
        var stages = new[] { groups, cup };
        InstallQuarterToSemi(cup, quarter, context.Clock);
        groups.ReplaceQualificationRules(
            new QualificationRules(
            [
                new QualificationPath(
                    1,
                    QualificationSource.FromGroup(group.Id),
                    new QualificationSelection(SelectionMode.Position, 1),
                    QualificationDestination.ForSlot(cup.Id, "SF-A")),
                new QualificationPath(
                    2,
                    QualificationSource.FromGroup(group.Id),
                    new QualificationSelection(SelectionMode.Position, 2),
                    QualificationDestination.ForSlot(cup.Id, "SF-B")),
            ]),
            context.Clock);
        PrepareStage.Execute(cup, stages, context.Clock);
        var created = MaterializeCupFromOccupiedSlots.Execute(
            competition,
            cup,
            ["Q1", "Q2", "S1"],
            [],
            context.Clock,
            stages);
        SeedLifecycle.PrepareAndStart(context, competition, groups, stages);

        context.Stages.Add(groups);
        context.Stages.Add(cup);
        context.Matches.Add(played);
        foreach (var createdMatch in created.CreatedMatches)
        {
            context.Matches.Add(createdMatch);
        }
    }

    private static void InstallQuarterToSemi(Stage cup, EntryId[] quarterTeams, IClock clock)
    {
        var quarter = cup.AddRound("QF", new TieFormat(TieFormat.SingleLeg, aggregateScoring: false), clock);
        var semi = cup.AddRound("SF", new TieFormat(TieFormat.SingleLeg, aggregateScoring: false), clock);
        cup.AddSlot("QF-A");
        cup.AddSlot("QF-B");
        cup.AddSlot("QF-C");
        cup.AddSlot("QF-D");
        cup.AddSlot("SF-A");
        cup.AddSlot("SF-B");
        cup.ReplaceBracketPairs(
        [
            new BracketPair("Q1", "QF-A", "QF-B"),
            new BracketPair("Q2", "QF-C", "QF-D"),
            new BracketPair("S1", "SF-A", "SF-B"),
        ]);
        cup.AddFixture(quarter.Id, clock, "QF-A", "QF-B", "Q1");
        cup.AddFixture(quarter.Id, clock, "QF-C", "QF-D", "Q2");
        cup.AddFixture(semi.Id, clock, "SF-A", "SF-B", "S1");
        string[] slots = ["QF-A", "QF-B", "QF-C", "QF-D"];
        for (var index = 0; index < slots.Length; index++)
        {
            cup.AddResolvedPopulationEntry(quarterTeams[index], clock);
            cup.AssignEntryToSlot(slots[index], quarterTeams[index]);
        }
    }

    private static QualificationRules DeferredSemiRules(Stage cup) =>
        new(
        [
            DeferredQualPath(1, 1, cup, "SF-A"),
            DeferredQualPath(2, 2, cup, "SF-B"),
        ]);

    private static Match SemiMatch(Stage cup, IReadOnlyList<Match> created)
    {
        var fixture = cup.FindFixtureByBracketPairKey("S1");
        return created.Single(match => fixture!.MatchIds.Any(id => id.Equals(match.Id)));
    }

    private static QualificationPath DeferredQualPath(int order, int position, Stage cup, string slotKey) =>
        new(
            order,
            QualificationSource.Overall(),
            new QualificationSelection(SelectionMode.Position, position),
            QualificationDestination.ForSlot(cup.Id, slotKey));
}
