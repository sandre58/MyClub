// -----------------------------------------------------------------------
// <copyright file="LocaleStructureTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Application.Competitions;
using MyClub.PlayUp.Application.Stages;
using MyClub.PlayUp.Application.Tests.Common;
using MyClub.PlayUp.Domain.Common;
using Xunit;

namespace MyClub.PlayUp.Application.Tests.Stages;

/// <summary>
/// Structure Lot 3: locale skeleton mutations and rebuild impact.
/// </summary>
public sealed class LocaleStructureTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 9, 10, 14, 0, 0, TimeSpan.Zero));

    [Fact]
    public void Rebuild_reports_impact_and_replaces_skeleton_same_kind()
    {
        var competition = CreateCompetition.Execute("Rebuild", _clock);
        var first = ConfigureStructure.Execute(
            competition,
            primaryStage: null,
            StructureIntent.Championship(),
            _clock);
        first.StageCreated.Should().BeTrue();

        // Pre-Materialize seed.
        first.Stage.Matchdays.Should().HaveCount(1);

        var rebuilt = ConfigureStructure.Execute(
            competition,
            first.Stage,
            StructureIntent.Championship(stageName: "Saison"),
            _clock);

        rebuilt.StageCreated.Should().BeFalse();
        rebuilt.RebuildImpact.Should().NotBeNull();
        rebuilt.RebuildImpact!.ClearedMatchdays.Should().Be(1);
        rebuilt.Stage.Matchdays.Should().HaveCount(1);
        rebuilt.Stage.Name.Value.Should().Be("Saison");
    }

    [Fact]
    public void AddMatchday_and_Rename_are_locale()
    {
        var competition = CreateCompetition.Execute("Locale", _clock);
        var configured = ConfigureStructure.Execute(
            competition,
            null,
            StructureIntent.Championship(),
            _clock);

        RenameStage.Execute(configured.Stage, "Saison");
        configured.Stage.Name.Value.Should().Be("Saison");

        AddStageMatchday.Execute(configured.Stage, number: null, _clock);
        configured.Stage.Matchdays.Should().HaveCount(2);
    }

    [Fact]
    public void Rename_is_allowed_while_Running()
    {
        var competition = CreateCompetition.Execute("RenameRunning", _clock);
        var configured = ConfigureStructure.Execute(
            competition,
            null,
            StructureIntent.Championship(),
            _clock);
        configured.Stage.Prepare(_clock);
        configured.Stage.Start(_clock);

        RenameStage.Execute(configured.Stage, "Live name");

        configured.Stage.Name.Value.Should().Be("Live name");
        configured.Stage.Status.Should().Be(StageStatus.Running);
    }

    [Fact]
    public void AddGroup_and_SetMatchGenerationFormat_are_locale()
    {
        var competition = CreateCompetition.Execute("GroupsLocale", _clock);
        var configured = ConfigureStructure.Execute(
            competition,
            null,
            StructureIntent.Groups(2, 2),
            _clock);

        AddStageGroup.Execute(configured.Stage, name: null, _clock);
        configured.Stage.Groups.Should().HaveCount(3);

        ReplaceStageMatchGenerationFormat.Execute(
            configured.Stage,
            MatchGenerationFormat.DoubleRoundRobin);
        configured.Stage.MatchGenerationFormat.Should().Be(MatchGenerationFormat.DoubleRoundRobin);
    }

    [Fact]
    public void ReplaceSwissSettings_updates_K()
    {
        var competition = CreateCompetition.Execute("SwissLocale", _clock);
        var configured = ConfigureStructure.Execute(
            competition,
            null,
            StructureIntent.Swiss(3),
            _clock);

        ReplaceStageSwissSettings.Execute(configured.Stage, 5);
        configured.Stage.SwissSettings!.RoundCount.Should().Be(5);
    }
}
