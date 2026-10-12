// -----------------------------------------------------------------------
// <copyright file="HostStageHttpHelpers.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.PlayUp.Host.Tests;

/// <summary>
/// HTTP helpers for stage lifecycle in integration tests (phase V2).
/// </summary>
internal static class HostStageHttpHelpers
{
    public static Task<HttpResponseMessage> PostPrepareAsync(HttpClient client, Guid stageId) =>
        client.PostAsync($"/stages/{stageId}/prepare", content: null);

    public static Task<HttpResponseMessage> PostStartAsync(HttpClient client, Guid stageId) =>
        client.PostAsync($"/stages/{stageId}/start", content: null);
}
