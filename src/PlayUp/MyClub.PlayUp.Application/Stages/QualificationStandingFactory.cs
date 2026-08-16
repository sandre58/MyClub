// -----------------------------------------------------------------------
// <copyright file="QualificationStandingFactory.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Application.Standings;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;
using MyClub.PlayUp.Domain.Standings;

namespace MyClub.PlayUp.Application.Stages;

/// <summary>
/// Builds overall / group standings required by <see cref="ApplyQualification"/> from loaded aggregates.
/// </summary>
public static class QualificationStandingFactory
{
    /// <summary>
    /// Assembles standings for the qualification paths of <paramref name="sourceStage"/>.
    /// </summary>
    /// <param name="competition">Owning competition (active entries for overall).</param>
    /// <param name="sourceStage">Stage owning qualification rules.</param>
    /// <param name="matches">Matches of the source stage.</param>
    /// <returns>Overall standing (when needed) and per-group standings.</returns>
    public static (Standing? Overall, IReadOnlyDictionary<GroupId, Standing> Groups) Build(
        Competition competition,
        Stage sourceStage,
        IReadOnlyList<Match> matches)
    {
        ArgumentNullException.ThrowIfNull(competition);
        ArgumentNullException.ThrowIfNull(sourceStage);
        ArgumentNullException.ThrowIfNull(matches);

        var paths = sourceStage.Regulation.QualificationRules?.Paths ?? [];
        var penalties = CalculateStanding.ToStandingPenalties(sourceStage.Penalties);
        var rules = sourceStage.Regulation.StandingRules;

        static bool IsGroupScoped(QualificationPath path) =>
            path.Source.GroupId is not null || path.Source.Scope == RankingScope.Group;

        var needsOverall = paths.Any(path =>
            path.Source.Scope != RankingScope.AcrossGroups && !IsGroupScoped(path));
        var needsGroups = paths.Any(path =>
            IsGroupScoped(path) || path.Source.Scope == RankingScope.AcrossGroups);

        Standing? overall = null;
        if (needsOverall)
        {
            var participants = competition.Entries
                .Where(entry => entry.Status == EntryStatus.Active)
                .Select(entry => entry.Id)
                .ToArray();
            if (participants.Length == 0)
            {
                participants = [.. matches
                    .Where(match => match.Status == MatchStatus.Finished && match.Result is not null)
                    .SelectMany(match => new[] { match.HomeEntryId, match.AwayEntryId })
                    .Distinct()];
            }

            overall = CalculateStanding.Execute(participants, matches, rules, MatchFilter.All, penalties);
        }

        var groupStandings = new Dictionary<GroupId, Standing>();
        if (needsGroups)
        {
            foreach (var group in sourceStage.Groups)
            {
                groupStandings[group.Id] = CalculateStanding.Execute(
                    group.EntryIds,
                    matches,
                    rules,
                    MatchFilter.All,
                    penalties);
            }
        }

        return (overall, groupStandings);
    }
}
