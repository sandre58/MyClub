// -----------------------------------------------------------------------
// <copyright file="PublishAndApplyGroupReloadTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Net;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using MyClub.PlayUp.Application.Abstractions;
using MyClub.PlayUp.Application.Competitions;
using MyClub.PlayUp.Application.Reads;
using MyClub.PlayUp.Application.Stages;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;
using MyClub.PlayUp.Infrastructure.Persistence;
using Npgsql;
using Xunit;

namespace MyClub.PlayUp.Host.Tests;

/// <summary>
/// Publish-and-apply must read the stage again under the apply lock.
/// A membership removed after publish commits must not be written back.
/// </summary>
[Collection("host-postgres")]
[Trait("Category", "Integration")]
public sealed class PublishAndApplyGroupReloadTests(HostPostgresFixture fixture)
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 11, 18, 12, 30, 0, TimeSpan.Zero));

    [IntegrationFact]
    public async Task PublishAndApply_does_not_restore_a_member_removed_between_the_commitsAsync()
    {
        var removal = new MemberRemoval(fixture.ConnectionString);
        await using var factory = new PlayUpWebApplicationFactory(
            fixture.ConnectionString,
            configureServices: services =>
            {
                var existing = services.Single(descriptor => descriptor.ServiceType == typeof(IUnitOfWork));
                _ = services.Remove(existing);
                services.AddScoped<IUnitOfWork>(sp =>
                    new RemoveMemberAfterFirstExclusiveWrite(
                        sp.GetRequiredService<PlayUpDbContext>(),
                        removal));
            });
        var seed = await SeedResolvedGroupDrawWithOutsiderAsync(factory);
        removal.EntryId = seed.Outsider.Value;

        using var client = factory.CreateClient();
        using var response = await client.PostAsync(
            $"/stages/{seed.StageId.Value}/draws/{seed.DrawId.Value}/publish-and-apply",
            content: null);

        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.NoContent, body);
        removal.DeletedRows.Should().Be(1);

        using var scope = factory.Services.CreateScope();
        var stage = await scope.ServiceProvider.GetRequiredService<IStageRepository>()
            .GetByIdForUpdateAsync(seed.StageId) ?? throw new InvalidOperationException("Stage missing.");
        stage.Groups.SelectMany(group => group.EntryIds).Should().NotContain(seed.Outsider);
        foreach (var placement in seed.Resolution)
        {
            stage.GetGroup(placement.GroupId).EntryIds.Should().Contain(placement.EntryId);
        }

        DrawAppliedState.IsApplied(stage.GetDraw(seed.DrawId), stage).Should().BeTrue();
    }

    private async Task<Seed> SeedResolvedGroupDrawWithOutsiderAsync(PlayUpWebApplicationFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var competitions = scope.ServiceProvider.GetRequiredService<ICompetitionRepository>();
        var stages = scope.ServiceProvider.GetRequiredService<IStageRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var competition = CreateCompetition.Execute("Publish apply reload", _clock);
        var ids = Enumerable.Range(0, 4)
            .Select(index => AddEntry.Execute(competition, $"T{index}", _clock).Id)
            .ToArray();
        var stage = ConfigureStructure.Execute(
            competition,
            null,
            StructureIntent.Groups(2, 2),
            _clock).Stage;
        ReplaceStageDrawRules.Execute(
            stage,
            competition,
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
            groupTargets: [.. stage.Groups.Select(group => group.Id)]).IsResolved.Should().BeTrue();

        var outsider = AddEntry.Execute(competition, "Outsider", _clock).Id;
        stage.ReplaceAffectationAuthoring([.. ids, outsider], _clock);
        stage.AssignEntryToGroup(stage.Groups[0].Id, outsider);

        competitions.Add(competition);
        stages.Add(stage);
        await unitOfWork.SaveChangesAsync();

        return new Seed(stage.Id, draw.Id, outsider, [.. draw.Resolution.GroupResults]);
    }

    private sealed class MemberRemoval(string connectionString)
    {
        public Guid? EntryId { get; set; }

        public int DeletedRows { get; private set; }

        public async Task DeleteAsync()
        {
            if (EntryId is not Guid entryId)
            {
                return;
            }

            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync();
            await using var command = new NpgsqlCommand(
                "DELETE FROM group_entries WHERE entry_id = @id",
                connection);
            _ = command.Parameters.AddWithValue("id", entryId);
            DeletedRows += await command.ExecuteNonQueryAsync();
        }
    }

    private sealed class RemoveMemberAfterFirstExclusiveWrite(PlayUpDbContext context, MemberRemoval removal)
        : IUnitOfWork
    {
        private int _exclusiveWrites;

        public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
            ((IUnitOfWork)context).SaveChangesAsync(cancellationToken);

        public void ClearTrackedChanges() => ((IUnitOfWork)context).ClearTrackedChanges();

        public async Task ExecuteExclusiveAsync(
            CompetitionId competitionId,
            Func<CancellationToken, Task> write,
            CancellationToken cancellationToken = default)
        {
            await ((IUnitOfWork)context).ExecuteExclusiveAsync(competitionId, write, cancellationToken);
            if (Interlocked.Increment(ref _exclusiveWrites) == 1)
            {
                await removal.DeleteAsync();
            }
        }
    }

    private sealed record Seed(
        StageId StageId,
        DrawId DrawId,
        EntryId Outsider,
        IReadOnlyList<GroupDrawPlacement> Resolution);
}
