// -----------------------------------------------------------------------
// <copyright file="ScenarioOrchestration.ResolutionVisualRecette.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Application.Competitions;
using MyClub.PlayUp.Application.Stages;
using MyClub.PlayUp.Application.Standings;
using MyClub.PlayUp.Development.Runtime;
using MyClub.PlayUp.Development.Scenarios;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Qualification;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;
using MyClub.PlayUp.Domain.Standings;
using MyClub.PlayUp.TestKit;

namespace MyClub.PlayUp.Development.Orchestration;

internal static partial class ScenarioOrchestration
{
    /// <summary>
    /// Seeds ten <c>RECETTE T3 — …</c> competitions for Structure resolution freshness visual recette.
    /// </summary>
    public static async Task BuildResolutionVisualRecetteAsync(
        ScenarioContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();

        await SeedVisualRecetteQualRuleSlotAsync(context, cancellationToken).ConfigureAwait(false);
        await context.UnitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await SeedVisualRecetteUpstreamScoreSlotAsync(context, cancellationToken).ConfigureAwait(false);
        await context.UnitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await SeedVisualRecetteAmontCascadeAsync(context, cancellationToken).ConfigureAwait(false);
        await context.UnitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await SeedVisualRecetteIndeterminateAsync(context, cancellationToken).ConfigureAwait(false);
        await context.UnitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await SeedVisualRecetteNonVerifieeAsync(context, cancellationToken).ConfigureAwait(false);
        await context.UnitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await SeedVisualRecetteQualPopulationAsync(context, cancellationToken).ConfigureAwait(false);
        await context.UnitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await SeedVisualRecetteQualGroupAsync(context, cancellationToken).ConfigureAwait(false);
        await context.UnitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await SeedVisualRecetteStaleDrawActiveAsync(context, cancellationToken).ConfigureAwait(false);
        await context.UnitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await SeedVisualRecetteStaleDrawBlockedAsync(context, cancellationToken).ConfigureAwait(false);
        await context.UnitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await SeedVisualRecetteWithdrawnOccupantAsync(context, cancellationToken).ConfigureAwait(false);
        await context.UnitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async Task SeedVisualRecetteWithdrawnOccupantAsync(
        ScenarioContext context,
        CancellationToken cancellationToken)
    {
        var competition = await CreateCompetitionAsync(
                context,
                ResolutionVisualRecetteNames.WithdrawnOccupant,
                competitionLocalKey: "vis-t3-withdrawn",
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        var entries = AddNamedEntries(context, competition, "Forfait FC", "Beta", "Gamma", "Delta");
        var league = Stage.Create(
            competition.Id,
            new StageName("Championnat"),
            RegulationPacks.Standard(),
            context.Clock);
        league.AddMatchday(1, context.Clock);
        competition.AddStage(league.Id, context.Clock);
        league.ReplaceAffectationAuthoring(entries, context.Clock);
        var matches = RoundRobinFinished(context, league, entries);
        SeedLifecycle.PrepareAndStart(context, competition, league, [league]);
        competition.WithdrawEntry(entries[0], context.Clock);
        context.Stages.Add(league);
        foreach (var match in matches)
        {
            context.Matches.Add(match);
        }
    }

    private static async Task SeedVisualRecetteQualRuleSlotAsync(
        ScenarioContext context,
        CancellationToken cancellationToken)
    {
        var competition = await CreateCompetitionAsync(
                context,
                ResolutionVisualRecetteNames.QualRuleSlot,
                competitionLocalKey: "vis-t3-qual-slot",
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        var (league, terminal, home, away, matches) = CreateLeagueTerminalPair(context, competition, "Champ");
        ApplyOverallQualToSlot(context, league, terminal, [home, away], matches, position: 1, slotKey: "Champ");
        var stages = new[] { league, terminal };
        SeedLifecycle.PrepareAndStart(context, competition, league, stages);
        ReplaceStageQualificationRules.Execute(
            league,
            [
                new QualificationPathSpec(
                    1,
                    SelectionMode.Position,
                    2,
                    terminal.Id.Value,
                    RankingScope.Overall,
                    DestinationSlotKey: "Champ"),
            ],
            context.Clock,
            stages,
            matches);
        TrackLeagueTerminal(context, league, terminal, matches);
    }

    private static async Task SeedVisualRecetteUpstreamScoreSlotAsync(
        ScenarioContext context,
        CancellationToken cancellationToken)
    {
        var competition = await CreateCompetitionAsync(
                context,
                ResolutionVisualRecetteNames.UpstreamScoreSlot,
                competitionLocalKey: "vis-t3-upstream-score",
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        var (league, terminal, home, away, matches) = CreateLeagueTerminalPair(context, competition, "Champ");
        ApplyOverallQualToSlot(context, league, terminal, [home, away], matches, position: 1, slotKey: "Champ");
        var stages = new[] { league, terminal };
        SeedLifecycle.PrepareAndStart(context, competition, league, stages);
        matches.Single().CorrectMatchResult(new MatchResult(ResultType.Played, new Score(0, 3)), context.Clock);
        TrackLeagueTerminal(context, league, terminal, matches);
    }

    private static async Task SeedVisualRecetteAmontCascadeAsync(
        ScenarioContext context,
        CancellationToken cancellationToken)
    {
        var competition = await CreateCompetitionAsync(
                context,
                ResolutionVisualRecetteNames.AmontPerimeCascade,
                competitionLocalKey: "vis-t3-cascade",
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        var entries = AddNamedEntries(context, competition, "A1", "A2", "A3");
        var phaseA = Stage.Create(competition.Id, new StageName("Phase A"), RegulationPacks.Standard(), context.Clock);
        var group = phaseA.AddGroup("A", context.Clock);
        phaseA.AssignEntryToGroup(group.Id, entries[0]);
        phaseA.AssignEntryToGroup(group.Id, entries[1]);
        phaseA.AddMatchday(1, context.Clock);
        competition.AddStage(phaseA.Id, context.Clock);
        var groupMatch = Match.Create(competition.Id, phaseA.Id, entries[0], entries[1], context.Clock);
        groupMatch.Start(context.Clock);
        groupMatch.Finish(new MatchResult(ResultType.Played, new Score(2, 0)), context.Clock);
        var groupMatches = new List<Match> { groupMatch };
        var groupStanding = CalculateStanding.Execute(
            [entries[0], entries[1]],
            groupMatches,
            phaseA.Regulation.StandingRules!);

        var phaseB = Stage.Create(competition.Id, new StageName("Phase B"), RegulationPacks.Standard(), context.Clock);
        phaseB.AddRound("Tour", new TieFormat(TieFormat.SingleLeg, aggregateScoring: false), context.Clock);
        phaseB.AddSlot("QF-A");
        phaseB.AddSlot("QF-B");
        phaseB.ReplaceBracketPairs([new BracketPair("P1", "QF-A", "QF-B")]);
        competition.AddStage(phaseB.Id, context.Clock);

        var phaseC = Stage.Create(competition.Id, new StageName("Phase C"), RegulationPacks.Standard(), context.Clock);
        phaseC.AddSlot("Finale");
        competition.AddStage(phaseC.Id, context.Clock);

        phaseA.ReplaceQualificationRules(
            new QualificationRules(
            [
                new QualificationPath(
                    1,
                    QualificationSource.FromGroup(group.Id),
                    new QualificationSelection(SelectionMode.Position, 1),
                    QualificationDestination.ForSlot(phaseB.Id, "QF-A")),
            ]),
            context.Clock);
        ApplyQualification.Execute(
            phaseA,
            overallStanding: null,
            new Dictionary<GroupId, Standing> { [group.Id] = groupStanding },
            [phaseA, phaseB, phaseC],
            context.Clock);
        var qualifier = phaseB.FindSlot("QF-A")!.EntryId!.Value;

        phaseB.AddResolvedPopulationEntry(entries[2], context.Clock);
        phaseB.AssignEntryToSlot("QF-B", entries[2]);
        var fixture = phaseB.AddFixture(phaseB.Rounds[0].Id, context.Clock, "QF-A", "QF-B", "P1");
        var cupMatch = Match.Create(competition.Id, phaseB.Id, qualifier, entries[2], context.Clock);
        phaseB.AttachMatch(fixture.Id, cupMatch.Id, legIndex: 1, context.Clock);
        cupMatch.Start(context.Clock);
        cupMatch.Finish(new MatchResult(ResultType.Played, new Score(1, 0)), context.Clock);
        phaseB.ReplaceProgressionRules(
            new ProgressionRules(
            [
                new ProgressionPath(
                    "P1",
                    ProgressionOutcome.Winner,
                    new ProgressionDestination(phaseC.Id, "Finale")),
            ]),
            context.Clock);
        ApplyProgressionOutcome.Execute(
            phaseB,
            fixture.Id,
            [cupMatch],
            [phaseA, phaseB, phaseC],
            context.Clock);

        groupMatch.CorrectMatchResult(new MatchResult(ResultType.Played, new Score(0, 2)), context.Clock);
        SeedLifecycle.PrepareAndStart(context, competition, phaseA, [phaseA, phaseB, phaseC]);
        context.Stages.Add(phaseA);
        context.Stages.Add(phaseB);
        context.Stages.Add(phaseC);
        context.Matches.Add(groupMatch);
        context.Matches.Add(cupMatch);
    }

    private static async Task SeedVisualRecetteIndeterminateAsync(
        ScenarioContext context,
        CancellationToken cancellationToken)
    {
        var competition = await CreateCompetitionAsync(
                context,
                ResolutionVisualRecetteNames.IndeterminateQual,
                competitionLocalKey: "vis-t3-indet",
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        var e0 = competition.AddEntry(TeamId.New(), "T0", context.Clock);
        var e1 = competition.AddEntry(TeamId.New(), "T1", context.Clock);
        var e2 = competition.AddEntry(TeamId.New(), "T2", context.Clock);
        var league = Stage.Create(competition.Id, new StageName("League"), RegulationPacks.Standard(), context.Clock);
        league.AddMatchday(1, context.Clock);
        competition.AddStage(league.Id, context.Clock);
        var terminal = Stage.Create(competition.Id, new StageName("Terminal"), RegulationPacks.Standard(), context.Clock);
        terminal.AddSlot("Place");
        competition.AddStage(terminal.Id, context.Clock);
        var matches = RoundRobinFinished(context, league, [e0.Id, e1.Id, e2.Id]);
        ApplyOverallQualToSlot(
            context,
            league,
            terminal,
            [e0.Id, e1.Id, e2.Id],
            matches,
            position: 3,
            slotKey: "Place");
        var stages = new[] { league, terminal };
        SeedLifecycle.PrepareAndStart(context, competition, league, stages);
        WithdrawEntry.Execute(competition, e2.Id, stages, matches, context.Clock);
        TrackLeagueTerminal(context, league, terminal, matches);
    }

    private static async Task SeedVisualRecetteNonVerifieeAsync(
        ScenarioContext context,
        CancellationToken cancellationToken)
    {
        var competition = await CreateCompetitionAsync(
                context,
                ResolutionVisualRecetteNames.NonVerifieeSlot,
                competitionLocalKey: "vis-t3-non-verif",
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        var home = competition.AddEntry(TeamId.New(), "Alpha", context.Clock);
        var away = competition.AddEntry(TeamId.New(), "Beta", context.Clock);
        var league = Stage.Create(competition.Id, new StageName("League"), RegulationPacks.Standard(), context.Clock);
        league.AddMatchday(1, context.Clock);
        competition.AddStage(league.Id, context.Clock);
        var terminal = Stage.Create(competition.Id, new StageName("Terminal"), RegulationPacks.Standard(), context.Clock);
        terminal.AddSlot("NV");
        competition.AddStage(terminal.Id, context.Clock);
        terminal.AddResolvedPopulationEntry(home.Id, context.Clock);
        terminal.ApplyResolvedEntry("NV", home.Id, context.Clock);
        var match = Match.Create(competition.Id, league.Id, home.Id, away.Id, context.Clock);
        match.Start(context.Clock);
        match.Finish(new MatchResult(ResultType.Played, new Score(1, 0)), context.Clock);
        SeedLifecycle.PrepareAndStart(context, competition, league, [league, terminal]);
        context.Stages.Add(league);
        context.Stages.Add(terminal);
        context.Matches.Add(match);
    }

    private static async Task SeedVisualRecetteQualPopulationAsync(
        ScenarioContext context,
        CancellationToken cancellationToken)
    {
        var competition = await CreateCompetitionAsync(
                context,
                ResolutionVisualRecetteNames.QualRulePopulation,
                competitionLocalKey: "vis-t3-pop-qual",
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        var entries = AddNamedEntries(context, competition, "E1", "E2", "E3");
        var league = Stage.Create(competition.Id, new StageName("League"), RegulationPacks.Standard(), context.Clock);
        league.AddMatchday(1, context.Clock);
        competition.AddStage(league.Id, context.Clock);
        var downstream = Stage.Create(
            competition.Id,
            new StageName("Knockout"),
            RegulationPacks.Standard(),
            context.Clock);
        downstream.AddRound("R1", context.Clock);
        competition.AddStage(downstream.Id, context.Clock);
        var matches = RoundRobinFinished(context, league, entries);
        var standing = CalculateStanding.Execute(entries, matches, league.Regulation.StandingRules!);
        league.ReplaceQualificationRules(
            new QualificationRules(
            [
                new QualificationPath(
                    1,
                    QualificationSource.Overall(),
                    new QualificationSelection(SelectionMode.Position, 1),
                    QualificationDestination.ForPopulation(downstream.Id)),
                new QualificationPath(
                    2,
                    QualificationSource.Overall(),
                    new QualificationSelection(SelectionMode.Position, 2),
                    QualificationDestination.ForPopulation(downstream.Id)),
            ]),
            context.Clock);
        ApplyQualification.Execute(league, standing, [league, downstream], context.Clock);
        var stages = new[] { league, downstream };
        SeedLifecycle.PrepareAndStart(context, competition, league, stages);
        ReplaceStageQualificationRules.Execute(
            league,
            [
                new QualificationPathSpec(
                    1,
                    SelectionMode.Position,
                    3,
                    downstream.Id.Value,
                    RankingScope.Overall),
            ],
            context.Clock,
            stages,
            matches);
        context.Stages.Add(league);
        context.Stages.Add(downstream);
        foreach (var played in matches)
        {
            context.Matches.Add(played);
        }
    }

    private static async Task SeedVisualRecetteQualGroupAsync(
        ScenarioContext context,
        CancellationToken cancellationToken)
    {
        var competition = await CreateCompetitionAsync(
                context,
                ResolutionVisualRecetteNames.QualRuleGroup,
                competitionLocalKey: "vis-t3-group-qual",
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        var entries = AddNamedEntries(context, competition, "G1", "G2");
        var source = Stage.Create(competition.Id, new StageName("Poules source"), RegulationPacks.Standard(), context.Clock);
        var sourceGroup = source.AddGroup("A", context.Clock);
        source.AssignEntryToGroup(sourceGroup.Id, entries[0]);
        source.AssignEntryToGroup(sourceGroup.Id, entries[1]);
        source.AddMatchday(1, context.Clock);
        competition.AddStage(source.Id, context.Clock);
        var match = Match.Create(competition.Id, source.Id, entries[0], entries[1], context.Clock);
        match.Start(context.Clock);
        match.Finish(new MatchResult(ResultType.Played, new Score(3, 1)), context.Clock);
        var matches = new List<Match> { match };
        var standing = CalculateStanding.Execute(entries, matches, source.Regulation.StandingRules!);

        var dest = Stage.Create(competition.Id, new StageName("Poules destination"), RegulationPacks.Standard(), context.Clock);
        var destGroup = dest.AddGroup("B", context.Clock);
        dest.SetPlacesPerGroup(4);
        competition.AddStage(dest.Id, context.Clock);

        source.ReplaceQualificationRules(
            new QualificationRules(
            [
                new QualificationPath(
                    1,
                    QualificationSource.FromGroup(sourceGroup.Id),
                    new QualificationSelection(SelectionMode.Position, 1),
                    QualificationDestination.ForGroup(dest.Id, destGroup.Id)),
            ]),
            context.Clock);
        ApplyQualification.Execute(
            source,
            overallStanding: null,
            new Dictionary<GroupId, Standing> { [sourceGroup.Id] = standing },
            [source, dest],
            context.Clock);
        var stages = new[] { source, dest };
        SeedLifecycle.PrepareAndStart(context, competition, source, stages);
        ReplaceStageQualificationRules.Execute(
            source,
            [
                new QualificationPathSpec(
                    1,
                    SelectionMode.Position,
                    2,
                    dest.Id.Value,
                    GroupId: sourceGroup.Id.Value,
                    DestinationGroupId: destGroup.Id.Value),
            ],
            context.Clock,
            stages,
            matches);
        context.Stages.Add(source);
        context.Stages.Add(dest);
        context.Matches.Add(match);
    }

    private static async Task SeedVisualRecetteStaleDrawActiveAsync(
        ScenarioContext context,
        CancellationToken cancellationToken)
    {
        var competition = await CreateCompetitionAsync(
                context,
                ResolutionVisualRecetteNames.StaleDrawActive,
                competitionLocalKey: "vis-t3-draw-active",
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        var entries = AddNamedEntries(context, competition, "D1", "D2");
        var cup = Stage.Create(competition.Id, new StageName("Cup"), RegulationPacks.Standard(), context.Clock);
        cup.AddRound("R1", new TieFormat(TieFormat.SingleLeg, aggregateScoring: false), context.Clock);
        cup.AddSlot("S1");
        cup.AddSlot("S2");
        cup.SeedEntryRoundBracketPairs();
        competition.AddStage(cup.Id, context.Clock);
        cup.ReplaceAffectationAuthoring(entries, context.Clock);
        SeedSlotDraw.EngageRandomDrawRules(cup, context.Clock);
        SeedSlotDraw.RecordAndApplySlotDraw(context, cup, entries, ["S1", "S2"]);
        cup.AddResolvedPopulationEntry(EntryId.New(), context.Clock);
        SeedLifecycle.PrepareOnly(context, competition, cup, [cup]);
        context.Stages.Add(cup);
    }

    private static async Task SeedVisualRecetteStaleDrawBlockedAsync(
        ScenarioContext context,
        CancellationToken cancellationToken)
    {
        var competition = await CreateCompetitionAsync(
                context,
                ResolutionVisualRecetteNames.StaleDrawBlocked,
                competitionLocalKey: "vis-t3-draw-blocked",
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        var entries = AddNamedEntries(context, competition, "B1", "B2");
        var cup = Stage.Create(competition.Id, new StageName("Cup"), RegulationPacks.Standard(), context.Clock);
        var round = cup.AddRound("R1", new TieFormat(TieFormat.SingleLeg, aggregateScoring: false), context.Clock);
        cup.AddSlot("S1");
        cup.AddSlot("S2");
        cup.SeedEntryRoundBracketPairs();
        competition.AddStage(cup.Id, context.Clock);
        cup.ReplaceAffectationAuthoring(entries, context.Clock);
        SeedSlotDraw.EngageRandomDrawRules(cup, context.Clock);
        SeedSlotDraw.RecordAndApplySlotDraw(context, cup, entries, ["S1", "S2"]);
        var fixture = cup.AddFixture(round.Id, context.Clock, "S1", "S2", "P1");
        var match = Match.Create(competition.Id, cup.Id, entries[0], entries[1], context.Clock);
        cup.AttachMatch(fixture.Id, match.Id, legIndex: 1, context.Clock);
        cup.AddResolvedPopulationEntry(EntryId.New(), context.Clock);
        SeedLifecycle.PrepareOnly(context, competition, cup, [cup]);
        context.Stages.Add(cup);
        context.Matches.Add(match);
    }

    private static EntryId[] AddNamedEntries(ScenarioContext context, Competition competition, params string[] names)
    {
        var ids = new EntryId[names.Length];
        for (var index = 0; index < names.Length; index++)
        {
            ids[index] = competition.AddEntry(TeamId.New(), names[index], context.Clock).Id;
        }

        return ids;
    }

    private static (
        Stage League,
        Stage Terminal,
        EntryId Home,
        EntryId Away,
        List<Match> Matches) CreateLeagueTerminalPair(
        ScenarioContext context,
        Competition competition,
        string slotKey)
    {
        var home = competition.AddEntry(TeamId.New(), "Alpha", context.Clock);
        var away = competition.AddEntry(TeamId.New(), "Beta", context.Clock);
        var league = Stage.Create(competition.Id, new StageName("League"), RegulationPacks.Standard(), context.Clock);
        league.AddMatchday(1, context.Clock);
        competition.AddStage(league.Id, context.Clock);
        var terminal = Stage.Create(competition.Id, new StageName("Terminal"), RegulationPacks.Standard(), context.Clock);
        terminal.AddSlot(slotKey);
        competition.AddStage(terminal.Id, context.Clock);
        var matches = RoundRobinFinished(context, league, [home.Id, away.Id]);
        return (league, terminal, home.Id, away.Id, matches);
    }

    private static List<Match> RoundRobinFinished(
        ScenarioContext context,
        Stage league,
        EntryId[] entries)
    {
        var matches = new List<Match>();
        for (var home = 0; home < entries.Length; home++)
        {
            for (var away = home + 1; away < entries.Length; away++)
            {
                var match = Match.Create(league.CompetitionId, league.Id, entries[home], entries[away], context.Clock);
                match.Start(context.Clock);
                match.Finish(
                    new MatchResult(ResultType.Played, new Score(entries.Length - home, 0)),
                    context.Clock);
                matches.Add(match);
            }
        }

        return matches;
    }

    private static void ApplyOverallQualToSlot(
        ScenarioContext context,
        Stage league,
        Stage terminal,
        EntryId[] entries,
        IReadOnlyList<Match> matches,
        int position,
        string slotKey)
    {
        var standing = CalculateStanding.Execute(entries, matches, league.Regulation.StandingRules!);
        league.ReplaceQualificationRules(
            new QualificationRules(
            [
                new QualificationPath(
                    1,
                    QualificationSource.Overall(),
                    new QualificationSelection(SelectionMode.Position, position),
                    QualificationDestination.ForSlot(terminal.Id, slotKey)),
            ]),
            context.Clock);
        ApplyQualification.Execute(league, standing, [league, terminal], context.Clock);
    }

    private static void TrackLeagueTerminal(
        ScenarioContext context,
        Stage league,
        Stage terminal,
        IReadOnlyList<Match> matches)
    {
        context.Stages.Add(league);
        context.Stages.Add(terminal);
        foreach (var match in matches)
        {
            context.Matches.Add(match);
        }
    }
}
