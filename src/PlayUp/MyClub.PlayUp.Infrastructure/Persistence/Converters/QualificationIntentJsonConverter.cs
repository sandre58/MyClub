// -----------------------------------------------------------------------
// <copyright file="QualificationIntentJsonConverter.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Text.Json;
using System.Text.Json.Serialization;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Rules;

namespace MyClub.PlayUp.Infrastructure.Persistence.Converters;

/// <summary>
/// Persists <see cref="QualificationIntent"/> with <c>DestinationSlotKeys</c>.
/// Dual-reads legacy singular <c>DestinationSlotKey</c> as a one-element list.
/// </summary>
internal sealed class QualificationIntentJsonConverter : JsonConverter<QualificationIntent>
{
    /// <inheritdoc />
    public override QualificationIntent Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var document = JsonDocument.ParseValue(ref reader);
        var root = document.RootElement;

        var id = JsonSerializer.Deserialize<IntentId>(root.GetProperty("Id").GetRawText(), options);
        var order = root.GetProperty("Order").GetInt32();
        var sourceKind = JsonSerializer.Deserialize<QualificationIntentSourceKind>(
            root.GetProperty("SourceKind").GetRawText(),
            options);
        var positionFrom = root.GetProperty("PositionFrom").GetInt32();
        var positionTo = root.GetProperty("PositionTo").GetInt32();
        var destinationStageId = JsonSerializer.Deserialize<StageId>(
            root.GetProperty("DestinationStageId").GetRawText(),
            options);

        GroupId? groupId = null;
        if (root.TryGetProperty("GroupId", out var groupElement)
            && groupElement.ValueKind is not JsonValueKind.Null and not JsonValueKind.Undefined)
        {
            groupId = JsonSerializer.Deserialize<GroupId>(groupElement.GetRawText(), options);
        }

        int? acrossGroupsPosition = null;
        if (root.TryGetProperty("AcrossGroupsPosition", out var acrossElement)
            && acrossElement.ValueKind is not JsonValueKind.Null and not JsonValueKind.Undefined)
        {
            acrossGroupsPosition = acrossElement.GetInt32();
        }

        QualificationCondition? condition = null;
        if (root.TryGetProperty("Condition", out var conditionElement)
            && conditionElement.ValueKind is not JsonValueKind.Null and not JsonValueKind.Undefined)
        {
            condition = JsonSerializer.Deserialize<QualificationCondition>(conditionElement.GetRawText(), options);
        }

        var slotKeys = ReadDestinationSlotKeys(root);
        var groupIds = ReadDestinationGroupIds(root, options);
        var destinationForm = ReadDestinationForm(root);

        return new QualificationIntent(
            id,
            order,
            sourceKind,
            positionFrom,
            positionTo,
            destinationStageId,
            groupId,
            acrossGroupsPosition,
            condition,
            slotKeys,
            groupIds,
            destinationForm);
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, QualificationIntent value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WritePropertyName("Id");
        JsonSerializer.Serialize(writer, value.Id, options);
        writer.WriteNumber("Order", value.Order);
        writer.WritePropertyName("SourceKind");
        JsonSerializer.Serialize(writer, value.SourceKind, options);
        writer.WriteNumber("PositionFrom", value.PositionFrom);
        writer.WriteNumber("PositionTo", value.PositionTo);
        writer.WritePropertyName("DestinationStageId");
        JsonSerializer.Serialize(writer, value.DestinationStageId, options);

        if (value.GroupId is { } groupId)
        {
            writer.WritePropertyName("GroupId");
            JsonSerializer.Serialize(writer, groupId, options);
        }

        if (value.AcrossGroupsPosition is { } across)
        {
            writer.WriteNumber("AcrossGroupsPosition", across);
        }

        if (value.Condition is { } condition)
        {
            writer.WritePropertyName("Condition");
            JsonSerializer.Serialize(writer, condition, options);
        }

        writer.WritePropertyName("DestinationSlotKeys");
        JsonSerializer.Serialize(writer, value.DestinationSlotKeys, options);
        writer.WritePropertyName("DestinationGroupIds");
        JsonSerializer.Serialize(writer, value.DestinationGroupIds, options);
        writer.WriteBoolean("DestinationForm", value.DestinationForm);
        writer.WriteEndObject();
    }

    internal static IReadOnlyList<string>? ReadDestinationSlotKeys(JsonElement root)
    {
        if (root.TryGetProperty("DestinationSlotKeys", out var keysElement)
            && keysElement.ValueKind == JsonValueKind.Array)
        {
            var list = new List<string>(keysElement.GetArrayLength());
            list.AddRange(from item in keysElement.EnumerateArray() where item.ValueKind == JsonValueKind.String select item.GetString());

            return list.Count == 0 ? null : list;
        }

        if (!root.TryGetProperty("DestinationSlotKey", out var singular)
            || singular.ValueKind != JsonValueKind.String)
        {
            return null;
        }

        var key = singular.GetString();
        return string.IsNullOrWhiteSpace(key) ? null : [key];
    }

    internal static IReadOnlyList<GroupId>? ReadDestinationGroupIds(
        JsonElement root,
        JsonSerializerOptions options)
    {
        if (!root.TryGetProperty("DestinationGroupIds", out var keysElement)
            || keysElement.ValueKind != JsonValueKind.Array
            || keysElement.GetArrayLength() == 0)
        {
            return null;
        }

        var list = new List<GroupId>(keysElement.GetArrayLength());
        list.AddRange(from item in keysElement.EnumerateArray() where item.ValueKind is not (JsonValueKind.Null or JsonValueKind.Undefined) select JsonSerializer.Deserialize<GroupId>(item.GetRawText(), options));

        return list.Count == 0 ? null : list;
    }

    internal static bool ReadDestinationForm(JsonElement root) =>
        root.TryGetProperty("DestinationForm", out var formElement)
        && formElement.ValueKind is JsonValueKind.True;
}
