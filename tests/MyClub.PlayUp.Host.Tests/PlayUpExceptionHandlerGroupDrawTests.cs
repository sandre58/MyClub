// -----------------------------------------------------------------------
// <copyright file="PlayUpExceptionHandlerGroupDrawTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using MyClub.PlayUp.Application;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Stages;
using Xunit;

namespace MyClub.PlayUp.Host.Tests;

public sealed class PlayUpExceptionHandlerGroupDrawTests
{
    [Theory]
    [InlineData(ApplicationErrorCodes.DrawGroupSeatsReservedByInboundFeed, "Group places reserved by an inbound feed")]
    [InlineData(ApplicationErrorCodes.DrawFixedPotConflict, "Fixed teams share a pot in one group")]
    [InlineData(ApplicationErrorCodes.DrawFixedOutsideComposition, "Fixed team is outside the composition")]
    [InlineData(ApplicationErrorCodes.DrawFixedExceedsCapacity, "Fixed teams exceed the draw capacity")]
    [InlineData(ApplicationErrorCodes.DrawGroupShapeInvalid, "Group draw shape is invalid")]
    [InlineData(ApplicationErrorCodes.DrawPotsMissing, "Group draw pots are missing")]
    public async Task Group_draw_refusals_are_409_with_a_dedicated_title_code_and_reasonsAsync(string code, string title)
    {
        var handler = new PlayUpExceptionHandler(NullLogger<PlayUpExceptionHandler>.Instance);
        var context = new DefaultHttpContext { Response = { Body = new MemoryStream() } };
        var reasons = new[] { "group:reason" };

        var handled = await handler.TryHandleAsync(
            context,
            new ApplicationFailureException("detail", code, reasons),
            CancellationToken.None);

        handled.Should().BeTrue();
        context.Response.StatusCode.Should().Be(StatusCodes.Status409Conflict);
        context.Response.Body.Position = 0;
        using var document = await JsonDocument.ParseAsync(context.Response.Body);
        document.RootElement.GetProperty("title").GetString().Should().Be(title);
        document.RootElement.GetProperty("code").GetString().Should().Be(code);
        document.RootElement.GetProperty("reasons").EnumerateArray().Select(item => item.GetString())
            .Should().Equal(reasons);
    }

    [Fact]
    public async Task Domain_generator_codes_stay_on_the_generic_domain_titleAsync()
    {
        var handler = new PlayUpExceptionHandler(NullLogger<PlayUpExceptionHandler>.Instance);
        var context = new DefaultHttpContext { Response = { Body = new MemoryStream() } };

        await handler.TryHandleAsync(
            context,
            new DomainException("detail", StageErrorCodes.DrawFixedPotConflict),
            CancellationToken.None);

        context.Response.StatusCode.Should().Be(StatusCodes.Status409Conflict);
        context.Response.Body.Position = 0;
        using var document = await JsonDocument.ParseAsync(context.Response.Body);
        document.RootElement.GetProperty("title").GetString().Should().Be("Domain rule violation");
        document.RootElement.GetProperty("code").GetString().Should().Be(StageErrorCodes.DrawFixedPotConflict);
    }
}
