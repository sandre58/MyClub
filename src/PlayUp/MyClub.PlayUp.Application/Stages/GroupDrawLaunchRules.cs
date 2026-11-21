// -----------------------------------------------------------------------
// <copyright file="GroupDrawLaunchRules.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Stages;

/// <summary>
/// Pure launch rule for a Groups draw (no I/O). Callers pass the stage and the competition phases.
/// </summary>
/// <remarks>
/// <see cref="DrawInputsIntent.Default"/> fixes every current occupant. The first failing check wins:
/// inbound seats, occupant outside the composition, two fixed entries of the same pot in one group,
/// occupants above draw capacity, missing pots, then a shape the generator cannot draw.
/// An empty grid is allowed when pots and shape are valid. A full grid is not a refusal of this rule.
/// <c>entriesBeingReleased</c> are treated as vacant, so a redo can test the grid it is about to free.
/// Inbound seats must leave the same drawable count in every group, equal to <c>NumberOfPots</c> and to
/// entries ÷ groups. When <c>NumberOfPots</c> equals <see cref="Stage.PlacesPerGroup"/>, any unresolved feed refuses.
/// </remarks>
public static class GroupDrawLaunchRules
{
    /// <summary>
    /// Evaluates whether a Groups draw may start from the current occupants.
    /// </summary>
    /// <param name="stage">Phase whose groups, composition, and draw rules are read.</param>
    /// <param name="competitionStages">Competition phases, used to count unresolved inbound feeds.</param>
    /// <param name="entriesBeingReleased">Occupants treated as already vacant. Null when nothing is being released.</param>
    /// <returns>The fixed placements and pots, or the first refusal.</returns>
    public static GroupDrawLaunchDecision Evaluate(
        Stage stage,
        IReadOnlyList<Stage> competitionStages,
        IReadOnlySet<EntryId>? entriesBeingReleased = null)
    {
        ArgumentNullException.ThrowIfNull(stage);
        ArgumentNullException.ThrowIfNull(competitionStages);

        var groups = stage.Groups;
        var composition = stage.CompositionEntries.Select(entry => entry.EntryId).ToArray();
        var compositionSet = composition.ToHashSet();
        var numberOfPots = stage.Regulation.DrawRules?.PotRules?.NumberOfPots;
        var placesPerGroup = stage.PlacesPerGroup;
        var pendingByGroup = CountPending(stage, competitionStages, groups);
        var occupantsByGroup = groups.ToDictionary(
            group => group.Id, IReadOnlyList<EntryId> (group) =>
            [
                .. group.EntryIds
                    .Where(entryId => entriesBeingReleased?.Contains(entryId) != true)
            ]);

        if (TryRefuseInbound(groups, placesPerGroup, numberOfPots, composition.Length, pendingByGroup) is { } inbound)
        {
            return inbound;
        }

        if (TryRefuseOutsideComposition(groups, occupantsByGroup, compositionSet) is { } outside)
        {
            return outside;
        }

        var pots = TryComputePots(composition, numberOfPots);
        var capacity = groups.Count > 0 && composition.Length % groups.Count == 0
            ? composition.Length / groups.Count
            : (int?)null;

        if (pots is not null && TryRefusePotConflict(groups, occupantsByGroup, pots) is { } conflict)
        {
            return conflict;
        }

        if (capacity is { } drawCapacity
            && TryRefuseCapacity(groups, occupantsByGroup, drawCapacity) is { } overCapacity)
        {
            return overCapacity;
        }

        if (numberOfPots is null or < 2 || (pots is null && composition.Length == 0))
        {
            return GroupDrawLaunchDecision.Deny(
                ApplicationErrorCodes.DrawPotsMissing,
                "Draw pots are missing or incomplete. Complete the pots before drawing.",
                ["pots"]);
        }

        if (pots is null
            || groups.Count < 1
            || capacity is null
            || numberOfPots != capacity
            || !PotsMatchGroupCount(pots, numberOfPots.Value, groups.Count))
        {
            return GroupDrawLaunchDecision.Deny(
                ApplicationErrorCodes.DrawGroupShapeInvalid,
                "The group shape cannot be drawn. Align the number of teams, groups, and pots before drawing.",
                ["shape"]);
        }

        var fixedGroups = new List<GroupDrawPlacement>();
        foreach (var group in groups)
        {
            fixedGroups.AddRange(occupantsByGroup[group.Id].Select(entryId => new GroupDrawPlacement(entryId, group.Id)));
        }

        return GroupDrawLaunchDecision.Allow(fixedGroups, pots);
    }

    /// <summary>
    /// Teams the resolution placed that are not fixed. A redo releases only these.
    /// </summary>
    /// <param name="draw">Draw whose resolution and fixed groups are read.</param>
    /// <returns>Resolution entries absent from <see cref="DrawInputs.FixedGroups"/>.</returns>
    public static IReadOnlySet<EntryId> DrawnEntries(Draw draw)
    {
        ArgumentNullException.ThrowIfNull(draw);
        var fixedIds = draw.Inputs?.FixedGroups.Select(placement => placement.EntryId).ToHashSet()
            ?? [];
        var drawn = new HashSet<EntryId>();
        foreach (var placement in draw.Resolution.GroupResults)
        {
            if (!fixedIds.Contains(placement.EntryId))
            {
                drawn.Add(placement.EntryId);
            }
        }

        return drawn;
    }

    private static Dictionary<GroupId, int> CountPending(
        Stage stage,
        IReadOnlyList<Stage> competitionStages,
        IReadOnlyList<Group> groups)
    {
        var feeds = GroupPendingFeeds.List(stage, competitionStages);
        return groups.ToDictionary(
            group => group.Id,
            group => feeds.Count(feed => feed.GroupId.Equals(group.Id)));
    }

    private static GroupDrawLaunchDecision? TryRefuseInbound(
        IReadOnlyList<Group> groups,
        int? placesPerGroup,
        int? numberOfPots,
        int compositionCount,
        IReadOnlyDictionary<GroupId, int> pendingByGroup)
    {
        if (groups.Count == 0 || !pendingByGroup.Values.Any(count => count > 0))
        {
            return null;
        }

        if (placesPerGroup is not { } places)
        {
            return RefuseInbound(groups, pendingByGroup);
        }

        var drawable = groups.Select(group => places - pendingByGroup[group.Id]).ToArray();
        var uniform = drawable.Distinct().Count() == 1;
        var capacity = compositionCount % groups.Count == 0 ? compositionCount / groups.Count : (int?)null;
        var holds = uniform
            && numberOfPots is { } pots
            && drawable[0] == pots
            && capacity == pots;
        return holds ? null : RefuseInbound(groups, pendingByGroup);
    }

    private static GroupDrawLaunchDecision RefuseInbound(
        IReadOnlyList<Group> groups,
        IReadOnlyDictionary<GroupId, int> pendingByGroup)
    {
        var reasons = groups
            .Where(group => pendingByGroup[group.Id] > 0)
            .Select(group => $"group:{group.Id.Value:D}:reserved:{pendingByGroup[group.Id]}")
            .ToArray();
        return GroupDrawLaunchDecision.Deny(
            ApplicationErrorCodes.DrawGroupSeatsReservedByInboundFeed,
            "Some places are reserved for qualified teams. Wait for the qualifications to resolve or place the teams by hand.",
            reasons);
    }

    private static GroupDrawLaunchDecision? TryRefuseOutsideComposition(
        IReadOnlyList<Group> groups,
        Dictionary<GroupId, IReadOnlyList<EntryId>> occupantsByGroup,
        HashSet<EntryId> composition)
    {
        var reasons = new List<string>();
        foreach (var group in groups)
        {
            reasons.AddRange(from entryId in occupantsByGroup[@group.Id] where !composition.Contains(entryId) select $"group:{@group.Id.Value:D}:entry:{entryId.Value:D}");
        }

        return reasons.Count == 0
            ? null
            : GroupDrawLaunchDecision.Deny(
                ApplicationErrorCodes.DrawFixedOutsideComposition,
                "A placed team is no longer in the phase composition. Remove it from the group or restore the composition before drawing.",
                reasons);
    }

    private static GroupDrawLaunchDecision? TryRefusePotConflict(
        IReadOnlyList<Group> groups,
        Dictionary<GroupId, IReadOnlyList<EntryId>> occupantsByGroup,
        IReadOnlyDictionary<EntryId, int> pots)
    {
        foreach (var group in groups)
        {
            var seen = new Dictionary<int, EntryId>();
            foreach (var entryId in occupantsByGroup[group.Id])
            {
                if (!pots.TryGetValue(entryId, out var pot))
                {
                    continue;
                }

                if (!seen.TryAdd(pot, entryId))
                {
                    return GroupDrawLaunchDecision.Deny(
                        ApplicationErrorCodes.DrawFixedPotConflict,
                        "Two teams from the same pot are in the same group. Move one of them before drawing.",
                        [$"group:{group.Id.Value:D}:pot:{pot}:entries:{seen[pot].Value:D},{entryId.Value:D}"]);
                }
            }
        }

        return null;
    }

    private static GroupDrawLaunchDecision? TryRefuseCapacity(
        IReadOnlyList<Group> groups,
        Dictionary<GroupId, IReadOnlyList<EntryId>> occupantsByGroup,
        int capacity) =>
        (from @group in groups let count = occupantsByGroup[@group.Id].Count where count > capacity select GroupDrawLaunchDecision.Deny(ApplicationErrorCodes.DrawFixedExceedsCapacity, "A group holds more teams than the draw can place. Remove teams before drawing.", [$"group:{@group.Id.Value:D}:occupants:{count}:capacity:{capacity}"])).FirstOrDefault();

    private static IReadOnlyDictionary<EntryId, int>? TryComputePots(
        EntryId[] composition,
        int? numberOfPots)
    {
        if (numberOfPots is not { } pots || composition.Length == 0 || composition.Length % pots != 0)
        {
            return null;
        }

        return GroupDrawPots.Compute(composition, pots);
    }

    private static bool PotsMatchGroupCount(
        IReadOnlyDictionary<EntryId, int> pots,
        int numberOfPots,
        int groupCount)
    {
        for (var pot = 1; pot <= numberOfPots; pot++)
        {
            var size = pots.Values.Count(value => value == pot);

            if (size != groupCount)
            {
                return false;
            }
        }

        return true;
    }
}

/// <summary>
/// Outcome of <see cref="GroupDrawLaunchRules.Evaluate"/>.
/// </summary>
public sealed class GroupDrawLaunchDecision
{
    private GroupDrawLaunchDecision(
        bool isAllowed,
        IReadOnlyList<GroupDrawPlacement> fixedGroups,
        IReadOnlyDictionary<EntryId, int>? pots,
        string? code,
        string? message,
        IReadOnlyList<string> reasons)
    {
        IsAllowed = isAllowed;
        FixedGroups = fixedGroups;
        Pots = pots;
        Code = code;
        Message = message;
        Reasons = reasons;
    }

    /// <summary>Gets a value indicating whether the draw may start.</summary>
    public bool IsAllowed { get; }

    /// <summary>Gets the occupants that become fixed when the draw is allowed.</summary>
    public IReadOnlyList<GroupDrawPlacement> FixedGroups { get; }

    /// <summary>Gets the pot map when the draw is allowed; otherwise null.</summary>
    public IReadOnlyDictionary<EntryId, int>? Pots { get; }

    /// <summary>Gets the refusal code when the draw is not allowed; otherwise null.</summary>
    public string? Code { get; }

    /// <summary>Gets the refusal message when the draw is not allowed; otherwise null.</summary>
    public string? Message { get; }

    /// <summary>Gets groups, pots, and teams that explain the refusal. Empty when allowed.</summary>
    public IReadOnlyList<string> Reasons { get; }

    /// <summary>Throws the refusal. Does nothing when the draw is allowed.</summary>
    public void EnsureAllowed()
    {
        if (IsAllowed)
        {
            return;
        }

        throw new ApplicationFailureException(Message!, Code!, Reasons);
    }

    internal static GroupDrawLaunchDecision Allow(
        IReadOnlyList<GroupDrawPlacement> fixedGroups,
        IReadOnlyDictionary<EntryId, int> pots) =>
        new(true, fixedGroups, pots, null, null, []);

    internal static GroupDrawLaunchDecision Deny(
        string code,
        string message,
        IReadOnlyList<string> reasons) =>
        new(false, [], null, code, message, reasons);
}
