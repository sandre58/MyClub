// -----------------------------------------------------------------------
// <copyright file="ScenarioOrchestration.FreshnessActionsRecette.cs" company="Stéphane ANDRE">
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
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;
using MyClub.PlayUp.Domain.Standings;
using MyClub.PlayUp.TestKit;

namespace MyClub.PlayUp.Development.Orchestration;

internal static partial class ScenarioOrchestration
{
    /// <summary>
    /// Seeds one competition whose open phases show each freshness corrective action.
    /// </summary>
    public static async Task BuildFreshnessActionsRecetteAsync(
        ScenarioContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();

        var competition = await CreateCompetitionAsync(
                context,
                FreshnessActionsRecetteNames.Competition,
                competitionLocalKey: "freshness-actions",
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        var nord = competition.AddEntry(TeamId.New(), "Nord", context.Clock).Id;
        var est = competition.AddEntry(TeamId.New(), "Est", context.Clock).Id;
        var sud = competition.AddEntry(TeamId.New(), "Sud", context.Clock).Id;
        var ouest = competition.AddEntry(TeamId.New(), "Ouest", context.Clock).Id;
        var centre = competition.AddEntry(TeamId.New(), "Centre", context.Clock).Id;
        var four = new[] { nord, est, sud, ouest };

        var poules = Championship(context, competition, "Poules");
        var tableau = Cup(context, competition, "Tableau", "Tete", "Est", "Sud", "Ouest");
        var classement = Cup(context, competition, "Classement", "Dernier", "Relais", "Annexe", "Reserve");
        var aval = Cup(context, competition, "Aval", "Suite", "Est", "Sud", "NV");
        var barrage = Cup(context, competition, "Barrage", "A", "B", "C", "D");
        var aJouer = Cup(context, competition, "A jouer", "Jouer", "Est", "Sud", "Ouest");
        var nul = Cup(context, competition, "Nul", "A", "B", "C", "D");
        var aDepartager = Cup(context, competition, "A départager", "Egal", "Est", "Sud", "Ouest");
        var orphelin = Cup(context, competition, "Orphelin", "Fantome", "Est", "Sud", "Ouest");
        var stages = new List<Stage>
        {
            poules, tableau, classement, aval, barrage, aJouer, nul, aDepartager, orphelin,
        };

        var matches = RoundRobin(context, poules, [nord, est, sud, ouest, centre]);
        var standing = CalculateStanding.Execute(
            [nord, est, sud, ouest, centre],
            matches,
            poules.Regulation.StandingRules!);
        poules.ReplaceQualificationRules(
            new QualificationRules(
            [
                QualPath(1, 1, tableau.Id, "Tete"),
                QualPath(2, 5, classement.Id, "Dernier"),
            ]),
            context.Clock);
        ApplyQualification.Execute(poules, standing, stages, context.Clock);

        PlaceDirect(tableau, "Est", est, context.Clock);
        PlaceDirect(tableau, "Sud", sud, context.Clock);
        PlaceDirect(tableau, "Ouest", ouest, context.Clock);
        var upstream = Play(context, tableau, nord, est, "P1", "Tete", "Est", 2, 0);
        matches.Add(upstream.Match);
        matches.Add(Play(context, tableau, sud, ouest, "P2", "Sud", "Ouest", 1, 0).Match);
        tableau.ReplaceProgressionRules(
            new ProgressionRules(
            [
                new ProgressionPath(
                    "P1",
                    ProgressionOutcome.Winner,
                    new ProgressionDestination(aval.Id, "Suite")),
            ]),
            context.Clock);
        ApplyProgressionOutcome.Execute(tableau, upstream.FixtureId, [upstream.Match], stages, context.Clock);
        PlaceDirect(aval, "Est", est, context.Clock);
        PlaceDirect(aval, "Sud", sud, context.Clock);
        PlaceDirect(aval, "NV", ouest, context.Clock);
        aval.WriteSlotResolutionProvenance("NV", ResolutionProvenance.NonVerifiee());

        PlaceFour(barrage, four, context.Clock);
        matches.Add(Schedule(context, barrage, nord, est, "P1", "A", "B"));
        matches.Add(Play(context, barrage, sud, ouest, "P2", "C", "D", 1, 0).Match);
        StampProgression(context, barrage, "P1", aJouer, "Jouer", nord);
        PlaceDirect(aJouer, "Est", est, context.Clock);
        PlaceDirect(aJouer, "Sud", sud, context.Clock);
        PlaceDirect(aJouer, "Ouest", ouest, context.Clock);

        PlaceFour(nul, four, context.Clock);
        matches.Add(Play(context, nul, nord, est, "P1", "A", "B", 1, 1).Match);
        matches.Add(Play(context, nul, sud, ouest, "P2", "C", "D", 2, 0).Match);
        StampProgression(context, nul, "P1", aDepartager, "Egal", nord);
        PlaceDirect(aDepartager, "Est", est, context.Clock);
        PlaceDirect(aDepartager, "Sud", sud, context.Clock);
        PlaceDirect(aDepartager, "Ouest", ouest, context.Clock);

        PlaceDirect(orphelin, "Fantome", nord, context.Clock);
        PlaceDirect(orphelin, "Est", est, context.Clock);
        PlaceDirect(orphelin, "Sud", sud, context.Clock);
        PlaceDirect(orphelin, "Ouest", ouest, context.Clock);
        orphelin.WriteSlotResolutionProvenance(
            "Fantome",
            new ResolutionProvenance(
                ResolutionProvenanceKind.Progression,
                $"P|{Guid.NewGuid():N}|PX|Winner|{orphelin.Id.Value:N}|S|Fantome",
                "seed"));

        PlaceDirect(classement, "Relais", est, context.Clock);
        PlaceDirect(classement, "Annexe", sud, context.Clock);
        PlaceDirect(classement, "Reserve", ouest, context.Clock);

        SeedLifecycle.PrepareAndStart(context, competition, poules, stages);
        WithdrawEntry.Execute(competition, centre, stages, matches, context.Clock);
        matches.Single(match => match.HomeEntryId.Equals(nord) && match.AwayEntryId.Equals(est) && match.StageId.Equals(poules.Id))
            .CorrectMatchResult(new MatchResult(ResultType.Played, new Score(0, 4)), context.Clock);

        foreach (var stage in stages)
        {
            context.Stages.Add(stage);
        }

        foreach (var match in matches)
        {
            context.Matches.Add(match);
        }

        await context.UnitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private static QualificationPath QualPath(int order, int position, StageId destination, string slot) =>
        new(
            order,
            QualificationSource.Overall(),
            new QualificationSelection(SelectionMode.Position, position),
            QualificationDestination.ForSlot(destination, slot));

    private static Stage Championship(ScenarioContext context, Competition competition, string name)
    {
        var stage = Stage.Create(competition.Id, new StageName(name), RegulationPacks.Standard(), context.Clock);
        stage.AddMatchday(1, context.Clock);
        competition.AddStage(stage.Id, context.Clock);
        return stage;
    }

    private static Stage Cup(
        ScenarioContext context,
        Competition competition,
        string name,
        string slotA,
        string slotB,
        string slotC,
        string slotD)
    {
        var stage = Stage.Create(competition.Id, new StageName(name), RegulationPacks.Standard(), context.Clock);
        stage.AddRound("Tour", new TieFormat(TieFormat.SingleLeg, aggregateScoring: false), context.Clock);
        stage.AddSlot(slotA);
        stage.AddSlot(slotB);
        stage.AddSlot(slotC);
        stage.AddSlot(slotD);
        stage.ReplaceBracketPairs(
        [
            new BracketPair("P1", slotA, slotB),
            new BracketPair("P2", slotC, slotD),
        ]);
        competition.AddStage(stage.Id, context.Clock);
        return stage;
    }

    private static void PlaceDirect(Stage stage, string slot, EntryId entry, IClock clock)
    {
        stage.AddResolvedPopulationEntry(entry, clock, ResolutionProvenance.Directe(slot, entry));
        stage.AssignEntryToSlot(slot, entry);
    }

    private static void PlaceFour(Stage stage, EntryId[] entries, IClock clock)
    {
        PlaceDirect(stage, "A", entries[0], clock);
        PlaceDirect(stage, "B", entries[1], clock);
        PlaceDirect(stage, "C", entries[2], clock);
        PlaceDirect(stage, "D", entries[3], clock);
    }

    private static List<Match> RoundRobin(ScenarioContext context, Stage stage, EntryId[] entries)
    {
        var matches = new List<Match>();
        for (var home = 0; home < entries.Length; home++)
        {
            for (var away = home + 1; away < entries.Length; away++)
            {
                matches.Add(Finish(context, stage, entries[home], entries[away], entries.Length - home, 0));
            }
        }

        return matches;
    }

    private static Match Finish(
        ScenarioContext context,
        Stage stage,
        EntryId home,
        EntryId away,
        int homeGoals,
        int awayGoals)
    {
        var match = Match.Create(stage.CompetitionId, stage.Id, home, away, context.Clock);
        match.Start(context.Clock);
        match.Finish(new MatchResult(ResultType.Played, new Score(homeGoals, awayGoals)), context.Clock);
        return match;
    }

    private static (FixtureId FixtureId, Match Match) Play(
        ScenarioContext context,
        Stage stage,
        EntryId home,
        EntryId away,
        string pairKey,
        string slotA,
        string slotB,
        int homeGoals,
        int awayGoals)
    {
        var fixture = stage.AddFixture(stage.Rounds[0].Id, context.Clock, slotA, slotB, pairKey);
        var match = Finish(context, stage, home, away, homeGoals, awayGoals);
        stage.AttachMatch(fixture.Id, match.Id, legIndex: 1, context.Clock);
        return (fixture.Id, match);
    }

    private static Match Schedule(
        ScenarioContext context,
        Stage stage,
        EntryId home,
        EntryId away,
        string pairKey,
        string slotA,
        string slotB)
    {
        var fixture = stage.AddFixture(stage.Rounds[0].Id, context.Clock, slotA, slotB, pairKey);
        var match = Match.Create(stage.CompetitionId, stage.Id, home, away, context.Clock);
        stage.AttachMatch(fixture.Id, match.Id, legIndex: 1, context.Clock);
        return match;
    }

    private static void StampProgression(
        ScenarioContext context,
        Stage source,
        string pairKey,
        Stage destination,
        string slot,
        EntryId entry)
    {
        var path = new ProgressionPath(
            pairKey,
            ProgressionOutcome.Winner,
            new ProgressionDestination(destination.Id, slot));
        source.ReplaceProgressionRules(new ProgressionRules([path]), context.Clock);
        var provenance = new ResolutionProvenance(
            ResolutionProvenanceKind.Progression,
            ResolutionPathIdentifier.ForProgression(source.Id, path),
            "seed");
        destination.AddResolvedPopulationEntry(entry, context.Clock, provenance);
        destination.ApplyResolvedEntry(slot, entry, context.Clock, provenance);
    }
}
