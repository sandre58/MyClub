// -----------------------------------------------------------------------
// <copyright file="HostJson.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Text.Json;
using System.Text.Json.Serialization;

namespace MyClub.PlayUp.Host.Tests;

/// <summary>
/// JSON options aligned with Host HTTP contract (camelCase + string enums).
/// </summary>
internal static class HostJson
{
    /// <summary>
    /// Gets serializer options matching Play'Up Host <c>ConfigureHttpJsonOptions</c>.
    /// </summary>
    public static JsonSerializerOptions Options { get; } = CreateOptions();

    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }
}
