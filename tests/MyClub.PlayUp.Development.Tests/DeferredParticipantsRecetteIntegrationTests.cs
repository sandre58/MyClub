// -----------------------------------------------------------------------
// <copyright file="DeferredParticipantsRecetteIntegrationTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using MyClub.PlayUp.Application.Abstractions;
using MyClub.PlayUp.Application.Standings;
using MyClub.PlayUp.Development.Runtime;
using MyClub.PlayUp.Development.Scenarios;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Matches;
using Xunit;

namespace MyClub.PlayUp.Development.Tests;

[Collection("DevelopmentPostgres")]
[Trait("Category", "Integration")]
public sealed class DeferredParticipantsRecetteIntegrationTests(DevelopmentPostgresFixture fixture)
{
    [Fact]
    public async Task Apply_fills_one_side_then_the_other_and_the_deferred_match_stays_out_of_the_pool_standingAsync()
    {
        var runner = fixture.Services.GetRequiredService<ScenarioRunner>();
        await runner.ResetAndRunAsync([SeedSpec.Parse(DeferredParticipantsRecetteNames.ScenarioId)]);

        using var scope = fixture.Services.CreateScope();
        var competitions = scope.ServiceProvider.GetRequiredService<ICompetitionRepository>();
        var stages = scope.ServiceProvider.GetRequiredService<IStageRepository>();
        var matches = scope.ServiceProvider.GetRequiredService<IMatchRepository>();
        var list = await competitions.ListAsync();
        list.Select(item => item.Name.Value).Should().BeEquivalentTo(
        [
            DeferredParticipantsRecetteNames.Planned,
            DeferredParticipantsRecetteNames.OneSide,
            DeferredParticipantsRecetteNames.BothSides,
            DeferredParticipantsRecetteNames.PoolStanding
        ]);

        var planned = await CupMatchAsync(list, DeferredParticipantsRecetteNames.Planned);
        planned.HomeEntryId.Should().BeNull();
        planned.AwayEntryId.Should().BeNull();

        var oneSide = await CupMatchAsync(list, DeferredParticipantsRecetteNames.OneSide);
        oneSide.ParticipantsResolved.Should().BeFalse();
        (oneSide.HomeEntryId is not null || oneSide.AwayEntryId is not null).Should().BeTrue();

        var both = await CupMatchAsync(list, DeferredParticipantsRecetteNames.BothSides);
        both.HomeEntryId.Should().NotBeNull();
        both.AwayEntryId.Should().NotBeNull();
        both.HomeEntryId.Should().NotBe(both.AwayEntryId);

        var pool = list.Single(item => item.Name.Value == DeferredParticipantsRecetteNames.PoolStanding);
        var competition = (await competitions.GetByIdForUpdateAsync(pool.Id))!;
        var loadedStages = await stages.GetByIdsForUpdateAsync(competition.StageIds);
        var allMatches = new List<Match>();
        foreach (var stage in loadedStages)
        {
            allMatches.AddRange(await matches.ListByStageForUpdateAsync(stage.Id));
        }

        var deferred = allMatches.Should().ContainSingle(match => !match.ParticipantsResolved).Subject;
        var groupMatch = allMatches.Should().ContainSingle(match => match.ParticipantsResolved).Subject;
        var standing = CalculateStanding.Execute(
            [groupMatch.HomeEntryId!.Value, groupMatch.AwayEntryId!.Value],
            allMatches,
            competition.Regulation.StandingRules);
        standing.Rows.Should().OnlyContain(row => row.Played == 1);
        deferred.Status.Should().Be(MatchStatus.Scheduled);

        async Task<Match> CupMatchAsync(
            IReadOnlyList<Competition> summaries,
            string name)
        {
            var summary = summaries.Single(item => item.Name.Value == name);
            var competition = (await competitions.GetByIdForUpdateAsync(summary.Id))!;
            var cup = (await stages.GetByIdsForUpdateAsync(competition.StageIds))
                .Single(stage => stage.Name.Value == "Demi-finale");
            var stageMatches = await matches.ListByStageForUpdateAsync(cup.Id);
            return stageMatches.Should().ContainSingle().Subject;
        }
    }
}
