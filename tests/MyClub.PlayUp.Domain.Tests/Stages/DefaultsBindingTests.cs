// -----------------------------------------------------------------------
// <copyright file="DefaultsBindingTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;
using MyClub.PlayUp.Domain.Tests.Common;
using Xunit;

namespace MyClub.PlayUp.Domain.Tests.Stages;

public sealed class DefaultsBindingTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 9, 8, 12, 0, 0, TimeSpan.Zero));

    [Fact]
    public void Create_from_competition_marks_all_classifying_parts_bound()
    {
        var competition = Competition.Create(
            new CompetitionName("League"),
            SampleRegulations.Standard(),
            _clock);

        var stage = Stage.Create(
            competition.Id,
            new StageName("Poules"),
            competition.Regulation,
            _clock);

        stage.DefaultsBinding.IsBound(HeritableRegulationPart.MatchDuration).Should().BeTrue();
        stage.DefaultsBinding.IsBound(HeritableRegulationPart.ExtraTime).Should().BeTrue();
        stage.DefaultsBinding.IsBound(HeritableRegulationPart.Points).Should().BeTrue();
        stage.DefaultsBinding.IsBound(HeritableRegulationPart.RankingCriteria).Should().BeTrue();
    }

    [Fact]
    public void Create_non_classifying_does_not_bind_standing_parts()
    {
        var competition = Competition.Create(
            new CompetitionName("Cup"),
            SampleRegulations.Standard(),
            _clock);
        var regulation = StageRegulation.MaterializeFrom(competition.Regulation, isClassifyingPhase: false);

        var stage = Stage.Create(
            competition.Id,
            new StageName("Finale"),
            regulation,
            DefaultsBinding.AllBound(isClassifyingPhase: false),
            _clock);

        stage.DefaultsBinding.IsBound(HeritableRegulationPart.MatchDuration).Should().BeTrue();
        stage.DefaultsBinding.IsBound(HeritableRegulationPart.Points).Should().BeFalse();
        stage.Regulation.StandingRules.Should().BeNull();
    }

    [Fact]
    public void Revealing_case_poules_finale_propagate_duration_keeps_extra_time_override()
    {
        var competition = Competition.Create(
            new CompetitionName("League"),
            SampleRegulations.Standard(),
            _clock);

        var poules = Stage.Create(
            competition.Id,
            new StageName("Poules"),
            competition.Regulation,
            _clock);

        var finale = Stage.Create(
            competition.Id,
            new StageName("Finale"),
            competition.Regulation,
            _clock);
        finale.ReplaceMatchRules(
            new MatchRules(
                finale.Regulation.MatchRules.Duration,
                finale.Regulation.MatchRules.AdministrativeResultPolicy,
                new ExtraTimePolicy(5, 2),
                finale.Regulation.MatchRules.PenaltyShootoutPolicy),
            _clock);

        finale.DefaultsBinding.IsBound(HeritableRegulationPart.MatchDuration).Should().BeTrue();
        finale.DefaultsBinding.IsBound(HeritableRegulationPart.ExtraTime).Should().BeFalse();

        var bindingBefore = finale.DefaultsBinding.Copy();
        var replacement = new Regulation(
            competition.Regulation.EntryRules,
            new MatchRules(
                new MatchDuration(40, 2, 15),
                competition.Regulation.MatchRules.AdministrativeResultPolicy),
            competition.Regulation.StandingRules,
            competition.Regulation.DisciplinaryRules);

        competition.ReplaceRegulation(replacement, _clock);
        poules.PropagateBoundDefaults(competition.Regulation, _clock);
        finale.PropagateBoundDefaults(competition.Regulation, _clock);

        poules.Regulation.MatchRules.Duration.DurationPerPeriod.Should().Be(40);
        finale.Regulation.MatchRules.Duration.DurationPerPeriod.Should().Be(40);
        finale.Regulation.MatchRules.ExtraTimePolicy.Should().Be(new ExtraTimePolicy(5, 2));
        finale.DefaultsBinding.Should().Be(bindingBefore);
    }

    [Fact]
    public void PropagateBoundDefaults_does_not_change_binding()
    {
        var competition = Competition.Create(
            new CompetitionName("League"),
            SampleRegulations.Standard(),
            _clock);
        var stage = Stage.Create(
            competition.Id,
            new StageName("Poules"),
            competition.Regulation,
            _clock);
        var bindingBefore = stage.DefaultsBinding.Copy();

        competition.ReplaceRegulation(
            new Regulation(
                competition.Regulation.EntryRules,
                new MatchRules(
                    new MatchDuration(40, 2, 10),
                    competition.Regulation.MatchRules.AdministrativeResultPolicy),
                competition.Regulation.StandingRules),
            _clock);
        stage.PropagateBoundDefaults(competition.Regulation, _clock);

        stage.DefaultsBinding.Should().Be(bindingBefore);
        stage.Regulation.MatchRules.Duration.DurationPerPeriod.Should().Be(40);
    }

    [Fact]
    public void PropagateBoundDefaults_is_noop_when_running()
    {
        var competition = Competition.Create(
            new CompetitionName("League"),
            SampleRegulations.Standard(),
            _clock);
        var stage = CreateRunning(competition);
        var snapshot = stage.Regulation;

        competition.ReplaceRegulation(
            new Regulation(
                competition.Regulation.EntryRules,
                new MatchRules(
                    new MatchDuration(40, 2, 10),
                    competition.Regulation.MatchRules.AdministrativeResultPolicy),
                competition.Regulation.StandingRules),
            _clock);
        stage.PropagateBoundDefaults(competition.Regulation, _clock);

        stage.Regulation.Should().Be(snapshot);
    }

    [Fact]
    public void BindToCompetition_rebinds_extra_time_from_defaults()
    {
        var competition = Competition.Create(
            new CompetitionName("League"),
            SampleRegulations.Standard(),
            _clock);
        var stage = Stage.Create(
            competition.Id,
            new StageName("Finale"),
            competition.Regulation,
            _clock);
        stage.ReplaceMatchRules(
            new MatchRules(
                stage.Regulation.MatchRules.Duration,
                stage.Regulation.MatchRules.AdministrativeResultPolicy,
                new ExtraTimePolicy(5, 2)),
            _clock);

        stage.BindToCompetition(HeritableRegulationPart.ExtraTime, competition.Regulation, _clock);

        stage.DefaultsBinding.IsBound(HeritableRegulationPart.ExtraTime).Should().BeTrue();
        stage.Regulation.MatchRules.ExtraTimePolicy.Should().BeNull();
    }

    private Stage CreateRunning(Competition competition)
    {
        var stage = Stage.Create(
            competition.Id,
            new StageName("Poules"),
            competition.Regulation,
            _clock);
        stage.AddMatchday(1, _clock);
        stage.Prepare(_clock);
        stage.Start(_clock);
        return stage;
    }
}
