// -----------------------------------------------------------------------
// <copyright file="PlayUpExceptionHandlerSlotEntryUniqueTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using MyClub.PlayUp.Application;
using MyClub.PlayUp.Host;
using Xunit;

namespace MyClub.PlayUp.Host.Tests;

public sealed class PlayUpExceptionHandlerSlotEntryUniqueTests
{
    [Fact]
    public async Task Slot_entry_unique_conflict_is_409_with_application_codeAsync()
    {
        var handler = new PlayUpExceptionHandler(NullLogger<PlayUpExceptionHandler>.Instance);
        var context = new DefaultHttpContext { Response = { Body = new MemoryStream() } };

        var handled = await handler.TryHandleAsync(
            context,
            new ApplicationFailureException(
                "This team already occupies another place in the phase.",
                ApplicationErrorCodes.SlotEntryUniqueConflict),
            CancellationToken.None);

        handled.Should().BeTrue();
        context.Response.StatusCode.Should().Be(StatusCodes.Status409Conflict);
        context.Response.Body.Position = 0;
        using var document = await JsonDocument.ParseAsync(context.Response.Body);
        document.RootElement.GetProperty("code").GetString()
            .Should().Be(ApplicationErrorCodes.SlotEntryUniqueConflict);
    }
}
