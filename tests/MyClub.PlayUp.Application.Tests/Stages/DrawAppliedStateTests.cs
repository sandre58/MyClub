// -----------------------------------------------------------------------
// <copyright file="DrawAppliedStateTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Application.Competitions;
using MyClub.PlayUp.Application.Reads;
using MyClub.PlayUp.Application.Stages;
using MyClub.PlayUp.Application.Tests.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;
using Xunit;

namespace MyClub.PlayUp.Application.Tests.Stages;

public sealed class DrawAppliedStateTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 9, 22, 18, 30, 0, TimeSpan.Zero));

    [Fact]
    public void Group_is_applied_after_publish_and_apply()
    {
        var competition = CreateCompetition.Execute("AppliedG", _clock);
        var ids = new List<Domain.Common.EntryId>();
        for (var i = 0; i < 4; i++)
        {
            ids.Add(AddEntry.Execute(competition, $"T{i}", _clock).Id);
        }

        var stage = ConfigureStructure.Execute(
            competition,
            null,
            StructureIntent.Groups(2, 2),
            _clock).Stage;
        ReplaceStageDrawRules.Execute(
            stage,
            new DrawRules(DrawMode.Random, potRules: new PotRules(2)),
            _clock);
        stage.ReplaceCompositionEntries(ids, _clock);

        var draw = CreateDraw.Execute(stage, DrawResolutionKind.Group, _clock);
        ConfigureDrawInputs.Execute(
            stage,
            draw.Id,
            DrawInputsFactory.CreateDefault(stage, DrawResolutionKind.Group));
        GenerateDrawResolution.Execute(
            stage,
            draw.Id,
            _clock,
            groupTargets: [.. stage.Groups.Select(g => g.Id)]).IsResolved.Should().BeTrue();
        PublishDraw.Execute(stage, draw.Id, _clock);

        DrawAppliedState.IsApplied(stage.GetDraw(draw.Id), stage).Should().BeFalse();

        ApplyDraw.Execute(stage, draw.Id, _clock);

        DrawAppliedState.IsApplied(stage.GetDraw(draw.Id), stage).Should().BeTrue();
    }
}
