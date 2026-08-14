// -----------------------------------------------------------------------
// <copyright file="StageOrderedCollectionsAccessor.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Reflection;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Infrastructure.Persistence;

/// <summary>
/// Reflection helpers to reorder Stage structure collections without Domain persistence APIs.
/// </summary>
internal static class StageOrderedCollectionsAccessor
{
    private static readonly FieldInfo GroupsField = RequireField(typeof(Stage), "_groups");
    private static readonly FieldInfo RoundsField = RequireField(typeof(Stage), "_rounds");
    private static readonly FieldInfo MatchdaysField = RequireField(typeof(Stage), "_matchdays");
    private static readonly FieldInfo SlotsField = RequireField(typeof(Stage), "_slots");
    private static readonly FieldInfo DirectAssignmentsField = RequireField(typeof(Stage), "_directAssignments");
    private static readonly FieldInfo GroupEntryIdsField = RequireField(typeof(Group), "_entryIds");
    private static readonly FieldInfo RoundFixturesField = RequireField(typeof(Round), "_fixtures");
    private static readonly FieldInfo MatchdayFixturesField = RequireField(typeof(Matchday), "_fixtures");
    private static readonly FieldInfo FixtureAttachmentsField = RequireField(typeof(Fixture), "_attachments");

    internal static List<Group> GetGroups(Stage stage) => GetList<Group>(GroupsField, stage);

    internal static List<Round> GetRounds(Stage stage) => GetList<Round>(RoundsField, stage);

    internal static List<Matchday> GetMatchdays(Stage stage) => GetList<Matchday>(MatchdaysField, stage);

    internal static List<Slot> GetSlots(Stage stage) => GetList<Slot>(SlotsField, stage);

    internal static List<DirectAssignment> GetDirectAssignments(Stage stage) =>
        GetList<DirectAssignment>(DirectAssignmentsField, stage);

    internal static List<EntryId> GetEntryIds(Group group) => GetList<EntryId>(GroupEntryIdsField, group);

    internal static List<Fixture> GetRoundFixtures(Round round) => GetList<Fixture>(RoundFixturesField, round);

    internal static List<Fixture> GetMatchdayFixtures(Matchday matchday) =>
        GetList<Fixture>(MatchdayFixturesField, matchday);

    internal static List<MatchAttachment> GetAttachments(Fixture fixture) =>
        GetList<MatchAttachment>(FixtureAttachmentsField, fixture);

    internal static void ReplaceAttachments(Fixture fixture, IEnumerable<MatchAttachment> attachments) =>
        Replace(GetAttachments(fixture), attachments);

    internal static void ReorderGroups(Stage stage, IEnumerable<Group> ordered) =>
        ReorderInPlace(GetGroups(stage), ordered);

    internal static void ReorderRounds(Stage stage, IEnumerable<Round> ordered) =>
        ReorderInPlace(GetRounds(stage), ordered);

    internal static void ReorderMatchdays(Stage stage, IEnumerable<Matchday> ordered) =>
        ReorderInPlace(GetMatchdays(stage), ordered);

    internal static void ReorderEntryIds(Group group, IEnumerable<EntryId> ordered) =>
        Replace(GetEntryIds(group), ordered);

    internal static void ReorderRoundFixtures(Round round, IEnumerable<Fixture> ordered) =>
        ReorderInPlace(GetRoundFixtures(round), ordered);

    internal static void ReorderMatchdayFixtures(Matchday matchday, IEnumerable<Fixture> ordered) =>
        ReorderInPlace(GetMatchdayFixtures(matchday), ordered);

    private static List<T> GetList<T>(FieldInfo field, object instance) =>
        (List<T>)field.GetValue(instance)!;

    /// <summary>
    /// Reorders an EF-tracked navigation list without Clear (Clear marks children Deleted).
    /// </summary>
    private static void ReorderInPlace<T>(List<T> list, IEnumerable<T> ordered)
        where T : class
    {
        var items = ordered as IList<T> ?? [.. ordered];
        if (list.Count == items.Count && list.SequenceEqual(items)) return;
        for (var index = 0; index < items.Count; index++)
        {
            var item = items[index];
            var currentIndex = list.IndexOf(item);
            if (currentIndex < 0)
            {
                throw new InvalidOperationException(
                    $"Cannot reorder {typeof(T).Name}: item was not found in the tracked collection.");
            }

            if (currentIndex == index)
            {
                continue;
            }

            list.RemoveAt(currentIndex);
            list.Insert(index, item);
        }
    }

    private static void Replace<T>(List<T> list, IEnumerable<T> ordered)
    {
        var items = ordered as IList<T> ?? [.. ordered];
        if (list.Count == items.Count && list.SequenceEqual(items))
        {
            return;
        }

        list.Clear();
        list.AddRange(items);
    }

    private static FieldInfo RequireField(Type type, string name) =>
        type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)
        ?? throw new InvalidOperationException($"{type.Name}.{name} backing field was not found.");
}
