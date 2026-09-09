// -----------------------------------------------------------------------
// <copyright file="DefaultsBindingJson.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Text.Json;
using System.Text.Json.Serialization;
using MyClub.PlayUp.Domain.Rules;

namespace MyClub.PlayUp.Infrastructure.Persistence.Converters;

/// <summary>
/// System.Text.Json helpers for <see cref="DefaultsBinding"/> jsonb persistence.
/// </summary>
internal static class DefaultsBindingJson
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    internal static string Serialize(DefaultsBinding binding) =>
        JsonSerializer.Serialize(new Dto([.. binding.BoundParts]), Options);

    internal static DefaultsBinding Deserialize(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return DefaultsBinding.AllUnbound();
        }

        var dto = JsonSerializer.Deserialize<Dto>(json, Options);
        if (dto?.Bound is null || dto.Bound.Length == 0)
        {
            return DefaultsBinding.AllUnbound();
        }

        var binding = DefaultsBinding.AllUnbound();

        return dto.Bound.Aggregate(binding, (current, part) => current.Bind(part));
    }

    private sealed record Dto(HeritableRegulationPart[] Bound);
}
