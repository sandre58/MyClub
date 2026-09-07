// -----------------------------------------------------------------------
// <copyright file="GenerateNextRound.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Application.Standings;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Stages;
using MyClub.PlayUp.Domain.Standings;

namespace MyClub.PlayUp.Application.Stages;

/// <summary>
/// Application use case: generate the next Swiss round (pairings + Matchday materialization).
/// </summary>
/// <remarks>
/// Allowed while Stage is Running without globally unlocking <c>StructureLocked</c>
/// (Domain Swiss progressive APIs only). Competition may be Running.
/// </remarks>
public static class GenerateNextRound
{
    /// <summary>
    /// Builds pairings from current standings/history and materializes one Matchday.
    /// </summary>
    /// <param name="competition">Owning competition (active entries).</param>
    /// <param name="stage">Swiss stage.</param>
    /// <param name="existingMatches">Matches already loaded for the stage.</param>
    /// <param name="clock">Clock for domain events.</param>
    /// <returns>Created matches and round metadata.</returns>
    public static GenerateNextRoundResult Execute(
        Competition competition,
        Stage stage,
        IReadOnlyList<Match> existingMatches,
        IClock clock)
    {
        ArgumentNullException.ThrowIfNull(competition);
        ArgumentNullException.ThrowIfNull(stage);
        ArgumentNullException.ThrowIfNull(existingMatches);
        ArgumentNullException.ThrowIfNull(clock);

        if (!stage.CompetitionId.Equals(competition.Id))
        {
            throw new ApplicationFailureException(
                $"Stage '{stage.Id}' does not belong to competition '{competition.Id}'.",
                ApplicationErrorCodes.StageNotInCompetition);
        }

        EnsureSwissRunning(competition, stage);

        var settings = stage.SwissSettings!;
        var participants = GetActiveEntries(competition);
        if (participants.Count < 2)
        {
            throw new ApplicationFailureException(
                "Swiss GenerateNextRound requires at least two active entries.",
                ApplicationErrorCodes.SwissRoundGenerationFailure);
        }

        var nextRound = stage.Matchdays.Count == 0
            ? 1
            : stage.Matchdays.Max(matchday => matchday.Number) + 1;

        if (nextRound > settings.RoundCount)
        {
            throw new ApplicationFailureException(
                $"Swiss stage already has all {settings.RoundCount} rounds.",
                ApplicationErrorCodes.SwissRoundGenerationFailure);
        }

        var existingRound = stage.Matchdays.FirstOrDefault(matchday => matchday.Number == nextRound);
        if (existingRound is not null)
        {
            return EnsureAlreadyComplete(stage, existingRound, participants.Count, existingMatches);
        }

        if (nextRound > 1)
        {
            EnsurePreviousRoundFinished(stage, nextRound - 1, existingMatches);
        }

        var attached = IndexAttachedMatches(stage, existingMatches);
        var standingRules = stage.Regulation.StandingRules
            ?? throw new ApplicationFailureException(
                "Standing rules are required to generate a Swiss round.",
                StandingErrorCodes.RulesRequired);
        var standing = CalculateStanding.Execute(
            participants,
            attached.Values,
            standingRules,
            penalties: CalculateStanding.ToStandingPenalties(stage.Penalties));

        var swissStandings = standing.Rows
            .Select(row => new SwissParticipantStanding(row.EntryId, row.Points, row.Position))
            .ToArray();

        var playedPairs = CollectPlayedPairs(attached);
        var byeCounts = participants.ToDictionary(id => id, stage.CountSwissByes);
        var homeCounts = CollectHomeCounts(attached);

        var pairing = SwissPairingEngine.BuildPairings(
            new SwissPairingRequest(swissStandings, playedPairs, byeCounts, homeCounts));

        if (pairing.IsNoSolution)
        {
            throw new ApplicationFailureException(
                $"No complete legal Swiss pairing for round {nextRound}.",
                ApplicationErrorCodes.SwissRoundGenerationFailure);
        }

        var matchday = stage.AddSwissRoundMatchday(clock);
        var created = new List<Match>(pairing.Pairings.Count);
        foreach (var pair in pairing.Pairings)
        {
            var fixture = stage.AddSwissRoundFixture(matchday.Id, clock);
            var match = Match.Create(
                competition.Id,
                stage.Id,
                pair.HomeEntryId,
                pair.AwayEntryId,
                clock);
            stage.AttachSwissRoundMatch(fixture.Id, match.Id, legIndex: 1, clock);
            created.Add(match);
        }

        if (pairing.ByeEntryId is { } bye)
        {
            stage.RecordSwissBye(matchday.Number, bye);
        }

        return new GenerateNextRoundResult(matchday.Number, created, pairing.ByeEntryId, AlreadyComplete: false);
    }

    private static void EnsureSwissRunning(Competition competition, Stage stage)
    {
        if (!stage.IsSwiss)
        {
            throw new ApplicationFailureException(
                "GenerateNextRound requires a Swiss stage (SwissSettings).",
                ApplicationErrorCodes.SwissRoundGenerationFailure);
        }

        if (competition.Status is CompetitionStatus.Suspended
            or CompetitionStatus.Completed
            or CompetitionStatus.Archived)
        {
            throw new ApplicationFailureException(
                $"Swiss rounds cannot be generated while competition status is '{competition.Status}'.",
                ApplicationErrorCodes.OrganisationNotMutable);
        }

        if (stage.Status is not StageStatus.Running)
        {
            throw new ApplicationFailureException(
                $"GenerateNextRound requires stage status Running (current: '{stage.Status}').",
                ApplicationErrorCodes.SwissRoundGenerationFailure);
        }
    }

    private static GenerateNextRoundResult EnsureAlreadyComplete(
        Stage stage,
        Matchday matchday,
        int participantCount,
        IReadOnlyList<Match> existingMatches)
    {
        var expectedPairs = participantCount / 2;
        var expectsBye = participantCount % 2 == 1;
        var attached = IndexAttachedMatches(stage, existingMatches);
        var completeFixtures = matchday.Fixtures.Count(fixture => fixture.Attachments.Count >= 1);
        var byeRecorded = stage.SwissByeHistory.Any(bye => bye.RoundIndex == matchday.Number);

        if (completeFixtures == expectedPairs
            && matchday.Fixtures.Count == expectedPairs
            && expectsBye == byeRecorded
            && matchday.Fixtures.All(fixture => fixture.MatchIds.All(attached.ContainsKey)))
        {
            EntryId? byeEntryId = byeRecorded
                ? stage.SwissByeHistory.Single(bye => bye.RoundIndex == matchday.Number).EntryId
                : null;
            return new GenerateNextRoundResult(
                matchday.Number,
                [],
                byeEntryId,
                AlreadyComplete: true);
        }

        throw new ApplicationFailureException(
            $"Swiss round {matchday.Number} exists but is incomplete; cannot regenerate.",
            ApplicationErrorCodes.SwissRoundGenerationFailure);
    }

    private static void EnsurePreviousRoundFinished(
        Stage stage,
        int previousRound,
        IReadOnlyList<Match> existingMatches)
    {
        var matchday = stage.Matchdays.FirstOrDefault(candidate => candidate.Number == previousRound)
            ?? throw new ApplicationFailureException(
                $"Swiss previous round {previousRound} is missing.",
                ApplicationErrorCodes.SwissRoundGenerationFailure);

        var attached = IndexAttachedMatches(stage, existingMatches);
        foreach (var fixture in matchday.Fixtures)
        {
            if (fixture.Attachments.Count == 0)
            {
                throw new ApplicationFailureException(
                    $"Swiss round {previousRound} has a fixture without a match.",
                    ApplicationErrorCodes.SwissRoundGenerationFailure);
            }

            foreach (var matchId in fixture.MatchIds)
            {
                if (!attached.TryGetValue(matchId, out var match) || match.Status != MatchStatus.Finished)
                {
                    throw new ApplicationFailureException(
                        $"Swiss round {previousRound} must be fully finished before generating the next round.",
                        ApplicationErrorCodes.SwissRoundGenerationFailure);
                }
            }
        }
    }

    private static IReadOnlyList<EntryId> GetActiveEntries(Competition competition) =>
    [
        .. competition.Entries
            .Where(entry => entry.Status == EntryStatus.Active)
            .Select(entry => entry.Id)
            .OrderBy(id => id.Value)
    ];

    private static Dictionary<MatchId, Match> IndexAttachedMatches(
        Stage stage,
        IReadOnlyList<Match> existingMatches)
    {
        var attachedIds = stage.Matchdays
            .SelectMany(matchday => matchday.Fixtures)
            .SelectMany(fixture => fixture.MatchIds)
            .ToHashSet();

        return existingMatches
            .Where(match => attachedIds.Contains(match.Id))
            .ToDictionary(match => match.Id);
    }

    private static List<(EntryId First, EntryId Second)> CollectPlayedPairs(
        IReadOnlyDictionary<MatchId, Match> attached) =>
    [
        .. attached.Values.Select(match => (match.HomeEntryId, match.AwayEntryId))
    ];

    private static Dictionary<EntryId, int> CollectHomeCounts(IReadOnlyDictionary<MatchId, Match> attached)
    {
        var counts = new Dictionary<EntryId, int>();
        foreach (var match in attached.Values)
        {
            counts[match.HomeEntryId] = counts.GetValueOrDefault(match.HomeEntryId) + 1;
        }

        return counts;
    }
}
