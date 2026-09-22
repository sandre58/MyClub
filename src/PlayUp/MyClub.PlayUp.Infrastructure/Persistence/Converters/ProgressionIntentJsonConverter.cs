// -----------------------------------------------------------------------
// <copyright file="ProgressionIntentJsonConverter.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Text.Json;
using System.Text.Json.Serialization;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Rules;

namespace MyClub.PlayUp.Infrastructure.Persistence.Converters;

/// <summary>
/// Persists <see cref="ProgressionIntent"/> with <c>DestinationStageId</c> + <c>DestinationSlotKeys</c>.
/// Dual-reads legacy singular <c>DestinationSlotKey</c> and nested <c>Destination</c> objects.
/// </summary>
internal sealed class ProgressionIntentJsonConverter : JsonConverter<ProgressionIntent>
{
    /// <inheritdoc />
    public override ProgressionIntent Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var document = JsonDocument.ParseValue(ref reader);
        var root = document.RootElement;

        var id = JsonSerializer.Deserialize<IntentId>(root.GetProperty("Id").GetRawText(), options);
        var order = root.GetProperty("Order").GetInt32();
        var roundId = JsonSerializer.Deserialize<RoundId>(root.GetProperty("RoundId").GetRawText(), options);
        var outcome = JsonSerializer.Deserialize<ProgressionOutcome>(
            root.GetProperty("Outcome").GetRawText(),
            options);

        StageId destinationStageId;
        IReadOnlyList<string>? slotKeys;
        IReadOnlyList<GroupId>? groupIds = null;

        if (root.TryGetProperty("DestinationStageId", out var stageElement))
        {
            destinationStageId = JsonSerializer.Deserialize<StageId>(stageElement.GetRawText(), options);
            slotKeys = QualificationIntentJsonConverter.ReadDestinationSlotKeys(root);
            groupIds = QualificationIntentJsonConverter.ReadDestinationGroupIds(root, options);
        }
        else if (root.TryGetProperty("Destination", out var destinationElement)
                 && destinationElement.ValueKind == JsonValueKind.Object)
        {
            destinationStageId = JsonSerializer.Deserialize<StageId>(
                destinationElement.GetProperty("StageId").GetRawText(),
                options);
            slotKeys = null;
            if (!destinationElement.TryGetProperty("SlotKey", out var slotElement)
                || slotElement.ValueKind != JsonValueKind.String)
            {
                return new ProgressionIntent(id, order, roundId, outcome, destinationStageId, slotKeys, groupIds);
            }

            var key = slotElement.GetString();
            if (!string.IsNullOrWhiteSpace(key))
            {
                slotKeys = [key];
            }
        }
        else
        {
            throw new JsonException("ProgressionIntent JSON requires DestinationStageId or Destination.");
        }

        return new ProgressionIntent(id, order, roundId, outcome, destinationStageId, slotKeys, groupIds);
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, ProgressionIntent value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WritePropertyName("Id");
        JsonSerializer.Serialize(writer, value.Id, options);
        writer.WriteNumber("Order", value.Order);
        writer.WritePropertyName("RoundId");
        JsonSerializer.Serialize(writer, value.RoundId, options);
        writer.WritePropertyName("Outcome");
        JsonSerializer.Serialize(writer, value.Outcome, options);
        writer.WritePropertyName("DestinationStageId");
        JsonSerializer.Serialize(writer, value.DestinationStageId, options);
        writer.WritePropertyName("DestinationSlotKeys");
        JsonSerializer.Serialize(writer, value.DestinationSlotKeys, options);
        writer.WritePropertyName("DestinationGroupIds");
        JsonSerializer.Serialize(writer, value.DestinationGroupIds, options);
        writer.WriteEndObject();
    }
}
