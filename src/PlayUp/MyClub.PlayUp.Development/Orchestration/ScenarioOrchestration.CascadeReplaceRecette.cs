// -----------------------------------------------------------------------
// <copyright file="ScenarioOrchestration.CascadeReplaceRecette.cs" company="Stéphane ANDRE">
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
    /// Seeds five <c>RECETTE T4 — …</c> competitions in the stale state, without confirming the cascade.
    /// </summary>
    public static async Task BuildCascadeReplaceRecetteAsync(
        ScenarioContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();

        await SeedSimpleAsync(context, cancellationToken).ConfigureAwait(false);
        await context.UnitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await SeedPlayedAsync(context, cancellationToken).ConfigureAwait(false);
        await context.UnitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await SeedTransitiveAsync(context, cancellationToken).ConfigureAwait(false);
        await context.UnitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await SeedCompletedAsync(context, cancellationToken).ConfigureAwait(false);
        await context.UnitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await SeedDirectAsync(context, cancellationToken).ConfigureAwait(false);
        await context.UnitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async Task SeedSimpleAsync(ScenarioContext context, CancellationToken cancellationToken)
    {
        var (competition, league, cup, leader, challenger, leagueMatch) = await LeagueCupAsync(
            context,
            CascadeReplaceRecetteNames.Simple,
            "t4-simple",
            cancellationToken).ConfigureAwait(false);
        ApplyRank(league, cup, [leader, challenger], [leagueMatch], 1, "F-A", context.Clock);
        var final = Match.Create(competition.Id, cup.Id, leader, awayEntryId: null, context.Clock);
        cup.AttachMatch(cup.Rounds[0].Fixtures[0].Id, final.Id, legIndex: 1, context.Clock);
        leagueMatch.CorrectMatchResult(new MatchResult(ResultType.Played, new Score(0, 2)), context.Clock);
        context.Stages.Add(league);
        context.Stages.Add(cup);
        context.Matches.Add(leagueMatch);
        context.Matches.Add(final);
    }

    private static async Task SeedPlayedAsync(ScenarioContext context, CancellationToken cancellationToken)
    {
        var (competition, league, cup, leader, challenger, leagueMatch) = await LeagueCupAsync(
            context,
            CascadeReplaceRecetteNames.Played,
            "t4-played",
            cancellationToken).ConfigureAwait(false);
        ApplyRank(league, cup, [leader, challenger], [leagueMatch], 1, "F-A", context.Clock);
        var opponent = competition.AddEntry(TeamId.New(), "Gamma", context.Clock).Id;
        cup.AddResolvedPopulationEntry(opponent, context.Clock);
        cup.AssignEntryToSlot("F-B", opponent);
        var final = Match.Create(competition.Id, cup.Id, leader, opponent, context.Clock);
        cup.AttachMatch(cup.Rounds[0].Fixtures[0].Id, final.Id, legIndex: 1, context.Clock);
        final.Finish(new MatchResult(ResultType.Played, new Score(1, 0)), context.Clock);
        var scorer = MemberId.New();
        final.AddDeclaredParticipation(scorer, Side.Home, CompositionStatus.Starter, context.Clock, jerseyNumber: 9);
        final.RecordGoal(scorer, Side.Home, context.Clock);
        leagueMatch.CorrectMatchResult(new MatchResult(ResultType.Played, new Score(0, 2)), context.Clock);
        context.Stages.Add(league);
        context.Stages.Add(cup);
        context.Matches.Add(leagueMatch);
        context.Matches.Add(final);
    }

    private static async Task SeedTransitiveAsync(ScenarioContext context, CancellationToken cancellationToken)
    {
        var competition = await CreateCompetitionAsync(
            context,
            CascadeReplaceRecetteNames.Transitive,
            competitionLocalKey: "t4-transitive",
            cancellationToken: cancellationToken).ConfigureAwait(false);
        var (leader, challenger) = TwoTeams(competition, context.Clock);
        var groups = Stage.Create(competition.Id, new StageName("Poules"), RegulationPacks.Standard(), context.Clock);
        var group = groups.AddGroup("A", context.Clock);
        groups.AssignEntryToGroup(group.Id, leader);
        groups.AssignEntryToGroup(group.Id, challenger);
        competition.AddStage(groups.Id, context.Clock);
        var groupMatch = Match.Create(competition.Id, groups.Id, leader, challenger, context.Clock);
        groupMatch.Finish(new MatchResult(ResultType.Played, new Score(2, 0)), context.Clock);
        var standing = CalculateStanding.Execute(
            [leader, challenger],
            [groupMatch],
            groups.Regulation.StandingRules!);
        var middle = Stage.Create(competition.Id, new StageName("Milieu"), RegulationPacks.Standard(), context.Clock);
        var middleGroup = middle.AddGroup("M", context.Clock);
        competition.AddStage(middle.Id, context.Clock);
        var final = Stage.Create(competition.Id, new StageName("Finale"), RegulationPacks.Standard(), context.Clock);
        final.AddSlot("Champ");
        competition.AddStage(final.Id, context.Clock);
        Stage[] stages = [groups, middle, final];
        groups.ReplaceQualificationRules(
            new QualificationRules(
            [
                new QualificationPath(
                    1,
                    QualificationSource.FromGroup(group.Id),
                    new QualificationSelection(SelectionMode.Position, 1),
                    QualificationDestination.ForGroup(middle.Id, middleGroup.Id)),
            ]),
            context.Clock);
        ApplyQualification.Execute(
            groups,
            overallStanding: null,
            new Dictionary<GroupId, Standing> { [group.Id] = standing },
            stages,
            context.Clock);
        var middleStanding = CalculateStanding.Execute(
            middleGroup.EntryIds.ToArray(),
            Array.Empty<Match>(),
            middle.Regulation.StandingRules!);
        middle.ReplaceQualificationRules(
            new QualificationRules(
            [
                new QualificationPath(
                    1,
                    QualificationSource.FromGroup(middleGroup.Id),
                    new QualificationSelection(SelectionMode.Position, 1),
                    QualificationDestination.ForSlot(final.Id, "Champ")),
            ]),
            context.Clock);
        ApplyQualification.Execute(
            middle,
            overallStanding: null,
            new Dictionary<GroupId, Standing> { [middleGroup.Id] = middleStanding },
            stages,
            context.Clock);
        groupMatch.CorrectMatchResult(new MatchResult(ResultType.Played, new Score(0, 2)), context.Clock);
        context.Stages.Add(groups);
        context.Stages.Add(middle);
        context.Stages.Add(final);
        context.Matches.Add(groupMatch);
    }

    private static async Task SeedCompletedAsync(ScenarioContext context, CancellationToken cancellationToken)
    {
        var competition = await CreateCompetitionAsync(
            context,
            CascadeReplaceRecetteNames.Completed,
            competitionLocalKey: "t4-completed",
            cancellationToken: cancellationToken).ConfigureAwait(false);
        var (leader, challenger) = TwoTeams(competition, context.Clock);
        var league = Stage.Create(competition.Id, new StageName("Championnat"), RegulationPacks.Standard(), context.Clock);
        league.AddMatchday(1, context.Clock);
        competition.AddStage(league.Id, context.Clock);
        var terminal = Stage.Create(competition.Id, new StageName("Finale"), RegulationPacks.Standard(), context.Clock);
        terminal.AddSlot("Champ");
        terminal.AddMatchday(1, context.Clock);
        competition.AddStage(terminal.Id, context.Clock);
        var leagueMatch = Match.Create(competition.Id, league.Id, leader, challenger, context.Clock);
        leagueMatch.Finish(new MatchResult(ResultType.Played, new Score(2, 0)), context.Clock);
        ApplyRank(league, terminal, [leader, challenger], [leagueMatch], 1, "Champ", context.Clock);
        PrepareStage.Execute(terminal, [league, terminal], context.Clock);
        terminal.Start(context.Clock);
        terminal.Complete(context.Clock);
        leagueMatch.CorrectMatchResult(new MatchResult(ResultType.Played, new Score(0, 2)), context.Clock);
        context.Stages.Add(league);
        context.Stages.Add(terminal);
        context.Matches.Add(leagueMatch);
    }

    private static async Task SeedDirectAsync(ScenarioContext context, CancellationToken cancellationToken)
    {
        var competition = await CreateCompetitionAsync(
            context,
            CascadeReplaceRecetteNames.Direct,
            competitionLocalKey: "t4-direct",
            cancellationToken: cancellationToken).ConfigureAwait(false);
        var entry = competition.AddEntry(TeamId.New(), "Directe", context.Clock).Id;
        var stage = Stage.Create(competition.Id, new StageName("Finale"), RegulationPacks.Standard(), context.Clock);
        competition.AddStage(stage.Id, context.Clock);
        stage.AddSlot("Direct");
        stage.AddResolvedPopulationEntry(entry, context.Clock);
        stage.AssignEntryToSlot("Direct", entry);
        context.Stages.Add(stage);
    }

    private static async Task<(Competition Competition, Stage League, Stage Cup, EntryId Leader, EntryId Challenger, Match LeagueMatch)>
        LeagueCupAsync(
            ScenarioContext context,
            string name,
            string key,
            CancellationToken cancellationToken)
    {
        var competition = await CreateCompetitionAsync(
            context,
            name,
            competitionLocalKey: key,
            cancellationToken: cancellationToken).ConfigureAwait(false);
        var (leader, challenger) = TwoTeams(competition, context.Clock);
        var league = Stage.Create(competition.Id, new StageName("Championnat"), RegulationPacks.Standard(), context.Clock);
        league.AddMatchday(1, context.Clock);
        competition.AddStage(league.Id, context.Clock);
        var cup = Stage.Create(competition.Id, new StageName("Finale"), RegulationPacks.Standard(), context.Clock);
        competition.AddStage(cup.Id, context.Clock);
        cup.AddRound("F", new TieFormat(TieFormat.SingleLeg, aggregateScoring: false), context.Clock);
        cup.AddSlot("F-A");
        cup.AddSlot("F-B");
        cup.ReplaceBracketPairs([new BracketPair("P1", "F-A", "F-B")]);
        cup.AddFixture(cup.Rounds[0].Id, context.Clock, "F-A", "F-B", "P1");
        var leagueMatch = Match.Create(competition.Id, league.Id, leader, challenger, context.Clock);
        leagueMatch.Finish(new MatchResult(ResultType.Played, new Score(2, 0)), context.Clock);
        return (competition, league, cup, leader, challenger, leagueMatch);
    }

    private static void ApplyRank(
        Stage league,
        Stage cup,
        EntryId[] entries,
        IReadOnlyList<Match> matches,
        int position,
        string slot,
        IClock clock)
    {
        var standing = CalculateStanding.Execute(entries, matches, league.Regulation.StandingRules!);
        league.ReplaceQualificationRules(
            new QualificationRules([RankPath(position, cup.Id, slot)]),
            clock);
        ApplyQualification.Execute(league, standing, [league, cup], clock);
    }

    private static QualificationPath RankPath(int position, StageId destinationId, string slot) =>
        new(
            position,
            QualificationSource.Overall(),
            new QualificationSelection(SelectionMode.Position, position),
            QualificationDestination.ForSlot(destinationId, slot));

    private static (EntryId Leader, EntryId Challenger) TwoTeams(Competition competition, IClock clock)
    {
        var leader = competition.AddEntry(TeamId.New(), "Alpha", clock).Id;
        var challenger = competition.AddEntry(TeamId.New(), "Beta", clock).Id;
        return leader.Value.CompareTo(challenger.Value) <= 0
            ? (leader, challenger)
            : (challenger, leader);
    }
}
