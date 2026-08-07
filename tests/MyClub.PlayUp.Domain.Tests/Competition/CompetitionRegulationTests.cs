// -----------------------------------------------------------------------
// <copyright file="CompetitionRegulationTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competition;
using MyClub.PlayUp.Domain.Competition.Events;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Tests.Common;
using Xunit;
using CompetitionAggregate = MyClub.PlayUp.Domain.Competition.Competition;

namespace MyClub.PlayUp.Domain.Tests.Competition;

public sealed class CompetitionRegulationTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 8, 6, 10, 0, 0, TimeSpan.Zero));

    [Fact]
    public void Create_requires_regulation_and_exposes_it()
    {
        // Arrange
        var regulation = SampleRegulations.Standard();

        // Act
        var competition = CompetitionAggregate.Create(new CompetitionName("League"), regulation, _clock);

        // Assert
        competition.Regulation.Should().Be(regulation);
        competition.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<CompetitionCreated>();
    }

    [Fact]
    public void Create_rejects_null_regulation()
    {
        // Arrange & Act
        var act = () => CompetitionAggregate.Create(new CompetitionName("League"), null!, _clock);

        // Assert
        act.Should().Throw<ArgumentNullException>().Which.ParamName.Should().Be("regulation");
    }

    [Fact]
    public void ReplaceRegulation_replaces_completely_in_Draft()
    {
        // Arrange
        var competition = CompetitionAggregate.Create(
            new CompetitionName("League"),
            SampleRegulations.Standard(),
            _clock);
        competition.ClearDomainEvents();
        var replacement = new Regulation(
            new EntryRules(4, 8),
            competition.Regulation.MatchRules,
            competition.Regulation.StandingRules);

        // Act
        competition.ReplaceRegulation(replacement, _clock);

        // Assert
        competition.Regulation.Should().Be(replacement);
        competition.Regulation.EntryRules.MinimumTeams.Should().Be(4);
        competition.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<CompetitionRegulationReplaced>();
    }

    [Fact]
    public void ReplaceRegulation_on_Ready_demotes_to_Draft()
    {
        // Arrange
        var competition = CreateReady();
        competition.ClearDomainEvents();
        var replacement = SampleRegulations.Standard();

        // Act
        competition.ReplaceRegulation(replacement, _clock);

        // Assert
        competition.Status.Should().Be(CompetitionStatus.Draft);
        competition.Regulation.Should().Be(replacement);
        competition.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<CompetitionRegulationReplaced>();
    }

    [Fact]
    public void ReplaceRegulation_after_Start_is_rejected()
    {
        // Arrange
        var competition = CreateReady();
        competition.Start(_clock);

        // Act
        var act = () => competition.ReplaceRegulation(SampleRegulations.Standard(), _clock);

        // Assert
        act.Should().Throw<DomainException>().Which.Code.Should().Be(CompetitionErrorCodes.InvalidTransition);
    }

    private CompetitionAggregate CreateReady()
    {
        var competition = CompetitionAggregate.Create(
            new CompetitionName("League"),
            SampleRegulations.Standard(),
            _clock);
        competition.AddEntry(TeamId.New(), "Team A", _clock);
        competition.AddStage(StageId.New(), _clock);
        competition.Prepare(_clock);
        return competition;
    }
}
