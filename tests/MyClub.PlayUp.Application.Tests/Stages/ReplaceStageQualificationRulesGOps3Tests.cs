// -----------------------------------------------------------------------
// <copyright file="ReplaceStageQualificationRulesGOps3Tests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Application.Competitions;
using MyClub.PlayUp.Application.Stages;
using MyClub.PlayUp.Application.Tests.Common;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;
using Xunit;

namespace MyClub.PlayUp.Application.Tests.Stages;

/// <summary>
/// G-OPS3 reliquat — qualification/progression rules with Applied draw.
/// </summary>
public sealed class ReplaceStageQualificationRulesGOps3Tests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 10, 16, 12, 0, 0, TimeSpan.Zero));

    [Fact]
    public void ReplaceQualificationRules_refuses_new_rules_when_group_draw_applied()
    {
        var competition = CreateCompetition.Execute("G-OPS3-qual", _clock);
        var ids = Enumerable.Range(0, 4)
            .Select(i => AddEntry.Execute(competition, $"T{i}", _clock).Id)
            .ToArray();
        var downstream = ConfigureStructure.Execute(
            competition,
            null,
            StructureIntent.Championship(),
            _clock).Stage;
        var stage = AddCompetitionStage.Execute(competition, StructureIntent.Groups(2, 2), _clock);
        ReplaceStageDrawRules.Execute(
            stage,
            new DrawRules(DrawMode.Random, potRules: new PotRules(2)),
            _clock);
        stage.ReplaceAffectationAuthoring(ids, _clock);

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
        ApplyDraw.Execute(stage, draw.Id, _clock);
        PrepareStage.Execute(stage, [stage, downstream], _clock);

        var act = () => ReplaceStageQualificationRules.Execute(
            stage,
            [
                new QualificationIntentSpec(
                    Guid.NewGuid(),
                    1,
                    QualificationIntentSourceKind.EachGroup,
                    1,
                    1,
                    downstream.Id.Value,
                    DestinationForm: true)
            ],
            _clock,
            [stage, downstream],
            []);

        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.AppliedDrawFeedRulesChangeBlocked);
    }
}
