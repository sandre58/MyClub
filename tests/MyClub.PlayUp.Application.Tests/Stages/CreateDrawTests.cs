// -----------------------------------------------------------------------
// <copyright file="CreateDrawTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Application;
using MyClub.PlayUp.Application.Competitions;
using MyClub.PlayUp.Application.Stages;
using MyClub.PlayUp.Application.Tests.Common;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;
using Xunit;

namespace MyClub.PlayUp.Application.Tests.Stages;

public sealed class CreateDrawTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 9, 23, 10, 0, 0, TimeSpan.Zero));

    [Fact]
    public void Create_without_draw_rules_is_rejected()
    {
        var competition = CreateCompetition.Execute("NoRules", _clock);
        for (var i = 0; i < 4; i++)
        {
            AddEntry.Execute(competition, $"T{i}", _clock);
        }

        var stage = ConfigureStructure.Execute(
            competition,
            null,
            StructureIntent.Cup(4),
            _clock).Stage;
        stage.ReplaceCompositionEntries([.. competition.Entries.Select(e => e.Id)], _clock);

        var act = () => CreateDraw.Execute(stage, DrawResolutionKind.Pairing, _clock);

        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.DrawRulesRequired);
    }

    [Fact]
    public void Create_with_draw_rules_succeeds()
    {
        var competition = CreateCompetition.Execute("WithRules", _clock);
        for (var i = 0; i < 4; i++)
        {
            AddEntry.Execute(competition, $"T{i}", _clock);
        }

        var stage = ConfigureStructure.Execute(
            competition,
            null,
            StructureIntent.Cup(4),
            _clock).Stage;
        stage.ReplaceCompositionEntries([.. competition.Entries.Select(e => e.Id)], _clock);
        ReplaceStageDrawRules.Execute(stage, new DrawRules(DrawMode.Random), _clock);

        var draw = CreateDraw.Execute(stage, DrawResolutionKind.Pairing, _clock);

        draw.Status.Should().Be(Domain.Common.DrawStatus.Draft);
        draw.Kind.Should().Be(DrawResolutionKind.Pairing);
    }
}
