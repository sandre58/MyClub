// -----------------------------------------------------------------------
// <copyright file="DrawInputsJson.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Text.Json;
using System.Text.Json.Serialization;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Infrastructure.Persistence.Converters;

/// <summary>
/// Deterministic JSON for <see cref="DrawInputs"/> via an Infrastructure document and Domain factories.
/// </summary>
internal static class DrawInputsJson
{
    internal static readonly JsonSerializerOptions Options = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    internal static string Serialize(DrawInputs inputs) =>
        JsonSerializer.Serialize(DrawInputsDocument.From(inputs), Options);

    internal static DrawInputs Deserialize(string json)
    {
        var document = JsonSerializer.Deserialize<DrawInputsDocument>(json, Options)
            ?? throw new InvalidOperationException("DrawInputs JSON is empty.");
        return document.ToDomain();
    }

    private sealed record EntryNumberDocument(Guid EntryId, int Value);

    private sealed record SlotPlacementDocument(Guid EntryId, string SlotKey);

    private sealed record GroupPlacementDocument(Guid EntryId, Guid GroupId);

    private sealed record DrawInputsDocument(
        Guid[] Entries,
        EntryNumberDocument[]? Seeds,
        EntryNumberDocument[]? Pots,
        SlotPlacementDocument[]? FixedSlots,
        GroupPlacementDocument[]? FixedGroups)
    {
        internal static DrawInputsDocument From(DrawInputs inputs)
        {
            Guid[] entries = [.. inputs.Entries.Select(entry => entry.Value)];
            EntryNumberDocument[]? seeds = inputs.SeedMap is null
                ? null
                : [.. inputs.SeedMap.Seeds.Select(pair => new EntryNumberDocument(pair.Key.Value, pair.Value))];
            EntryNumberDocument[]? pots = inputs.PotMembership is null
                ? null
                : [.. inputs.PotMembership.Pots.Select(pair => new EntryNumberDocument(pair.Key.Value, pair.Value))];
            SlotPlacementDocument[]? fixedSlots = inputs.FixedSlots.Count == 0
                ? null
                : [.. inputs.FixedSlots.Select(p => new SlotPlacementDocument(p.EntryId.Value, p.SlotKey))];
            GroupPlacementDocument[]? fixedGroups = inputs.FixedGroups.Count == 0
                ? null
                : [.. inputs.FixedGroups.Select(p => new GroupPlacementDocument(p.EntryId.Value, p.GroupId.Value))];
            return new DrawInputsDocument(entries, seeds, pots, fixedSlots, fixedGroups);
        }

        internal DrawInputs ToDomain()
        {
            var entries = Entries.Select(value => new EntryId(value)).ToArray();
            var seedMap = Seeds is { Length: > 0 }
                ? new SeedMap(Seeds.ToDictionary(row => new EntryId(row.EntryId), row => row.Value))
                : null;
            var pots = Pots is { Length: > 0 }
                ? new PotMembership(Pots.ToDictionary(row => new EntryId(row.EntryId), row => row.Value))
                : null;

            if (FixedSlots is { Length: > 0 })
            {
                return DrawInputs.ForSlot(
                    entries,
                    seedMap,
                    pots,
                    [.. FixedSlots.Select(p => new SlotDrawPlacement(new EntryId(p.EntryId), p.SlotKey))]);
            }

            if (FixedGroups is { Length: > 0 })
            {
                return DrawInputs.ForGroup(
                    entries,
                    seedMap,
                    pots,
                    [
                        .. FixedGroups.Select(p =>
                            new GroupDrawPlacement(new EntryId(p.EntryId), new GroupId(p.GroupId)))
                    ]);
            }

            // Empty fixed lists: ForSlot/ForGroup are equivalent for persistence.
            return DrawInputs.ForSlot(entries, seedMap, pots);
        }
    }
}
