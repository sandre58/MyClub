// -----------------------------------------------------------------------
// <copyright file="WritePerformanceBaselineTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Diagnostics;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using MyClub.PlayUp.Application;
using MyClub.PlayUp.Application.Abstractions;
using MyClub.PlayUp.Application.Pipeline;
using MyClub.PlayUp.Development.Runtime;
using MyClub.PlayUp.Development.Tests.Diagnostics;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Stages;
using Xunit;
using Xunit.Abstractions;

namespace MyClub.PlayUp.Development.Tests;

/// <summary>
/// Command-path SQL and latency baselines (AUD-0012 write guards).
/// </summary>
[Collection("DevelopmentPostgres")]
[Trait("Category", "Integration")]
[Trait("Category", "PerformanceBaseline")]
public sealed class WritePerformanceBaselineTests(
    DevelopmentPostgresFixture fixture,
    ITestOutputHelper output)
{
    [Fact]
    public async Task Baseline_replace_qualification_rules_refused_on_running_qf_seedAsync()
    {
        var runner = fixture.Services.GetRequiredService<ScenarioRunner>();
        await runner.ResetAndRunAsync([SeedSpec.Parse("groups:running")]);

        using var scope = fixture.Services.CreateScope();
        var competitions = scope.ServiceProvider.GetRequiredService<ICompetitionRepository>();
        var stages = scope.ServiceProvider.GetRequiredService<IStageRepository>();
        var competitionId = (await competitions.ListAsync()).Should().ContainSingle().Subject.Id;
        var competition = await competitions.GetByIdReadOnlyAsync(competitionId);
        competition.Should().NotBeNull();
        var stageId = competition!.StageIds.Should().ContainSingle().Subject;
        var executor = scope.ServiceProvider.GetRequiredService<UseCaseExecutor>();
        var stage = await stages.GetByIdForUpdateAsync(stageId);
        stage.Should().NotBeNull();
        if (stage!.Status is StageStatus.Ready)
        {
            await executor.StartStageAsync(stageId);
            stage = await stages.GetByIdForUpdateAsync(stageId);
        }

        stage!.Status.Should().Be(StageStatus.Running);

        var counter = scope.ServiceProvider.GetRequiredService<SqlCommandCounterInterceptor>();
        counter.Reset();
        var stopwatch = Stopwatch.StartNew();

        var act = async () => await executor.ReplaceStageQualificationRulesAsync(
            stageId,
            paths: null);

        await act.Should().ThrowAsync<ApplicationFailureException>()
            .Where(ex => ex.Code == ApplicationErrorCodes.StructureNotMutable);

        stopwatch.Stop();

        WriteBlock(
            "groups:running — ReplaceStageQualificationRulesAsync refused (Running stage)",
            new Dictionary<string, object>
            {
                ["SQL commands"] = counter.CommandCount,
                ["Elapsed ms"] = stopwatch.ElapsedMilliseconds
            });

        counter.CommandCount.Should().BeLessThanOrEqualTo(
            20,
            "S2A early status guard must not load QF match graphs before refusal");
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(
            150,
            "refusal on Running upstream must stay under product latency budget on seed");
    }

    private void WriteBlock(string header, IReadOnlyDictionary<string, object> rows)
    {
        output.WriteLine(string.Empty);
        output.WriteLine(header);
        output.WriteLine(new string('-', 72));
        foreach (var (label, value) in rows)
        {
            output.WriteLine($"{label,-52} {value}");
        }
    }
}
