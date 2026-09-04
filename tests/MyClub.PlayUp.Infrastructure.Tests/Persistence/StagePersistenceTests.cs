// -----------------------------------------------------------------------
// <copyright file="StagePersistenceTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using MyClub.PlayUp.Application.Abstractions;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;
using MyClub.PlayUp.Infrastructure.Persistence.Repositories;
using MyClub.PlayUp.Infrastructure.Tests.Common;
using Xunit;

namespace MyClub.PlayUp.Infrastructure.Tests.Persistence;

public sealed class StagePersistenceTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 8, 14, 15, 0, 0, TimeSpan.Zero));

    [Fact]
    public async Task A22_Prepare_Complete_preserves_status_and_regulation_after_reloadAsync()
    {
        var databaseName = Guid.NewGuid().ToString();
        var competitionId = CompetitionId.New();
        var initial = StageRegulation.MaterializeFrom(SampleRegulations.WithExtraTimeAndShootout());
        StageId id;

        await using (var context = PlayUpInMemory.CreateContext(databaseName))
        {
            var stage = Stage.Create(competitionId, new StageName("Knockout"), initial, _clock);
            stage.AddRound("Quarter-finals", _clock);
            stage.Prepare(_clock);
            stage.Start(_clock);
            stage.Complete(_clock);
            id = stage.Id;

            new StageRepository(context).Add(stage);
            await ((IUnitOfWork)context).SaveChangesAsync();
        }

        await using (var context = PlayUpInMemory.CreateContext(databaseName))
        {
            var loaded = await new StageRepository(context).GetByIdForUpdateAsync(id);
            loaded.Should().NotBeNull();
            loaded.Status.Should().Be(StageStatus.Completed);
            loaded.Status.Should().NotBe(StageStatus.Draft);
            loaded.Regulation.Should().Be(initial);
            loaded.Rounds.Should().ContainSingle().Which.Name.Should().Be("Quarter-finals");
        }
    }

    [Fact]
    public async Task ReplaceRegulation_marks_property_modified_and_persistsAsync()
    {
        var databaseName = Guid.NewGuid().ToString();
        var initial = StageRegulation.MaterializeFrom(SampleRegulations.Standard());
        var replacement = StageRegulation.MaterializeFrom(SampleRegulations.WithExtraTimeAndShootout());
        StageId id;

        await using (var context = PlayUpInMemory.CreateContext(databaseName))
        {
            var stage = Stage.Create(CompetitionId.New(), new StageName("Group stage"), initial, _clock);
            id = stage.Id;
            new StageRepository(context).Add(stage);
            await ((IUnitOfWork)context).SaveChangesAsync();
        }

        await using (var context = PlayUpInMemory.CreateContext(databaseName))
        {
            var repository = new StageRepository(context);
            var loaded = await repository.GetByIdForUpdateAsync(id);
            loaded.Should().NotBeNull();
            loaded.ReplaceRegulation(replacement, _clock);

            context.Entry(loaded).Property(candidate => candidate.Regulation).IsModified.Should().BeTrue();
            await ((IUnitOfWork)context).SaveChangesAsync();
        }

        await using (var context = PlayUpInMemory.CreateContext(databaseName))
        {
            var reloaded = await new StageRepository(context).GetByIdForUpdateAsync(id);
            reloaded.Should().NotBeNull();
            reloaded.Regulation.Should().Be(replacement);
            reloaded.Regulation.Should().NotBe(initial);
        }
    }

    [Fact]
    public async Task ReplaceRoundTieFormat_marks_property_modified_and_persistsAsync()
    {
        var databaseName = Guid.NewGuid().ToString();
        var replacement = new TieFormat(2, true, new AwayGoalsRule(), new ExtraTimeRule());
        StageId stageId;
        RoundId roundId;

        await using (var context = PlayUpInMemory.CreateContext(databaseName))
        {
            var stage = Stage.Create(CompetitionId.New(), new StageName("Cup"), SampleRegulations.Standard(), _clock);
            var round = stage.AddRound("Final", _clock);
            stageId = stage.Id;
            roundId = round.Id;
            new StageRepository(context).Add(stage);
            await ((IUnitOfWork)context).SaveChangesAsync();
        }

        await using (var context = PlayUpInMemory.CreateContext(databaseName))
        {
            var repository = new StageRepository(context);
            var loaded = await repository.GetByIdForUpdateAsync(stageId);
            loaded.Should().NotBeNull();
            loaded.ReplaceRoundTieFormat(roundId, replacement, _clock);

            context.Entry(loaded.Rounds.Single(round => round.Id == roundId))
                .Property(round => round.TieFormat)
                .IsModified.Should().BeTrue();
            await ((IUnitOfWork)context).SaveChangesAsync();
        }

        await using (var context = PlayUpInMemory.CreateContext(databaseName))
        {
            var reloaded = await new StageRepository(context).GetByIdForUpdateAsync(stageId);
            reloaded.Should().NotBeNull();
            reloaded.Rounds.Single(round => round.Id == roundId).TieFormat.Should().Be(replacement);
        }
    }

    [Fact]
    public async Task Fixture_XOR_round_parent_materializes_with_matchday_fk_nullAsync()
    {
        var databaseName = Guid.NewGuid().ToString();
        StageId stageId;
        RoundId roundId;
        FixtureId fixtureId;

        await using (var context = PlayUpInMemory.CreateContext(databaseName))
        {
            var stage = Stage.Create(CompetitionId.New(), new StageName("Cup"), SampleRegulations.Standard(), _clock);
            var round = stage.AddRound("Semi-finals", new TieFormat(2, true), _clock);
            var fixture = stage.AddFixture(round.Id, _clock);
            stageId = stage.Id;
            roundId = round.Id;
            fixtureId = fixture.Id;

            new StageRepository(context).Add(stage);
            await ((IUnitOfWork)context).SaveChangesAsync();
        }

        await using (var context = PlayUpInMemory.CreateContext(databaseName))
        {
            var loaded = await new StageRepository(context).GetByIdForUpdateAsync(stageId);
            loaded.Should().NotBeNull();
            loaded.Rounds.Should().ContainSingle();
            loaded.Matchdays.Should().BeEmpty();
            loaded.Rounds[0].Id.Should().Be(roundId);
            loaded.Rounds[0].Fixtures.Should().ContainSingle().Which.Id.Should().Be(fixtureId);
            loaded.Rounds[0].TieFormat.Should().Be(new TieFormat(2, true));

            var fixtureEntry = context.Entry(loaded.Rounds[0].Fixtures[0]);
            fixtureEntry.Property<RoundId?>("round_id").CurrentValue.Should().Be(roundId);
            fixtureEntry.Property<MatchdayId?>("matchday_id").CurrentValue.Should().BeNull();
        }
    }

    [Fact]
    public async Task Fixture_XOR_matchday_parent_materializes_with_round_fk_nullAsync()
    {
        var databaseName = Guid.NewGuid().ToString();
        StageId stageId;
        MatchdayId matchdayId;
        FixtureId fixtureId;

        await using (var context = PlayUpInMemory.CreateContext(databaseName))
        {
            var stage = Stage.Create(CompetitionId.New(), new StageName("League"), SampleRegulations.Standard(), _clock);
            var matchday = stage.AddMatchday(1, _clock);
            var fixture = stage.AddFixture(matchday.Id, _clock);
            stageId = stage.Id;
            matchdayId = matchday.Id;
            fixtureId = fixture.Id;

            new StageRepository(context).Add(stage);
            await ((IUnitOfWork)context).SaveChangesAsync();
        }

        await using (var context = PlayUpInMemory.CreateContext(databaseName))
        {
            var loaded = await new StageRepository(context).GetByIdForUpdateAsync(stageId);
            loaded.Should().NotBeNull();
            loaded.Matchdays.Should().ContainSingle();
            loaded.Rounds.Should().BeEmpty();
            loaded.Matchdays[0].Id.Should().Be(matchdayId);
            loaded.Matchdays[0].Fixtures.Should().ContainSingle().Which.Id.Should().Be(fixtureId);

            var fixtureEntry = context.Entry(loaded.Matchdays[0].Fixtures[0]);
            fixtureEntry.Property<MatchdayId?>("matchday_id").CurrentValue.Should().Be(matchdayId);
            fixtureEntry.Property<RoundId?>("round_id").CurrentValue.Should().BeNull();
        }
    }

    [Fact]
    public async Task Groups_entries_rounds_matchdays_slots_assignments_and_attachments_round_tripAsync()
    {
        var databaseName = Guid.NewGuid().ToString();
        var entryA = EntryId.New();
        var entryB = EntryId.New();
        var matchId = MatchId.New();
        StageId stageId;
        GroupId groupId;
        FixtureId fixtureId;

        await using (var context = PlayUpInMemory.CreateContext(databaseName))
        {
            var stage = Stage.Create(CompetitionId.New(), new StageName("Poules"), SampleRegulations.Standard(), _clock);
            var group = stage.AddGroup("Group A", _clock);
            stage.AssignEntryToGroup(group.Id, entryA);
            stage.AssignEntryToGroup(group.Id, entryB);
            stage.AddGroup("Group B", _clock);
            stage.ArrangeGroups([stage.Groups[1].Id, stage.Groups[0].Id]);

            var matchday = stage.AddMatchday(1, _clock);
            stage.AddMatchday(2, _clock);
            stage.ArrangeMatchdays([stage.Matchdays[1].Id, stage.Matchdays[0].Id]);

            var fixture = stage.AddFixture(matchday.Id, _clock);
            stage.AttachMatch(fixture.Id, matchId, legIndex: 1, _clock);

            stage.AddSlot("W1");
            stage.AssignEntryToSlot("W1", entryA);

            stageId = stage.Id;
            groupId = group.Id;
            fixtureId = fixture.Id;
            new StageRepository(context).Add(stage);
            await ((IUnitOfWork)context).SaveChangesAsync();
        }

        await using (var context = PlayUpInMemory.CreateContext(databaseName))
        {
            var loaded = await new StageRepository(context).GetByIdForUpdateAsync(stageId);
            loaded.Should().NotBeNull();
            loaded.Groups.Select(group => group.Name).Should().Equal("Group B", "Group A");
            loaded.Groups.Single(group => group.Id == groupId).EntryIds.Should().Equal(entryA, entryB);
            loaded.Matchdays.Select(matchday => matchday.Number).Should().Equal(2, 1);
            loaded.Matchdays.Single(matchday => matchday.Number == 1).Fixtures.Should().ContainSingle()
                .Which.Id.Should().Be(fixtureId);
            loaded.Matchdays.Single(matchday => matchday.Number == 1).Fixtures[0].Attachments.Should().ContainSingle()
                .Which.Should().Be(new MatchAttachment(matchId, 1));
            loaded.Slots.Should().ContainSingle().Which.SlotKey.Should().Be("W1");
            loaded.Slots[0].EntryId.Should().Be(entryA);
            loaded.DirectAssignments.Should().ContainSingle()
                .Which.Should().Be(new DirectAssignment("W1", entryA));
            loaded.Draws.Should().BeEmpty();
            loaded.Penalties.Should().BeEmpty();
            loaded.MatchPlacements.Should().BeEmpty();
        }
    }

    [Fact]
    public async Task Arrange_groups_while_unchanged_persists_sort_orderAsync()
    {
        var databaseName = Guid.NewGuid().ToString();
        StageId stageId;
        GroupId first;
        GroupId second;

        await using (var context = PlayUpInMemory.CreateContext(databaseName))
        {
            var stage = Stage.Create(CompetitionId.New(), new StageName("Poules"), SampleRegulations.Standard(), _clock);
            first = stage.AddGroup("A", _clock).Id;
            second = stage.AddGroup("B", _clock).Id;
            stageId = stage.Id;
            new StageRepository(context).Add(stage);
            await ((IUnitOfWork)context).SaveChangesAsync();
        }

        await using (var context = PlayUpInMemory.CreateContext(databaseName))
        {
            var repository = new StageRepository(context);
            var loaded = await repository.GetByIdForUpdateAsync(stageId);
            loaded.Should().NotBeNull();
            loaded.ArrangeGroups([second, first]);
            context.Entry(loaded).State.Should().Be(EntityState.Unchanged);
            await ((IUnitOfWork)context).SaveChangesAsync();
        }

        await using (var context = PlayUpInMemory.CreateContext(databaseName))
        {
            var reloaded = await new StageRepository(context).GetByIdForUpdateAsync(stageId);
            reloaded.Should().NotBeNull();
            reloaded.Groups.Select(group => group.Id).Should().Equal(second, first);
        }
    }

    [Fact]
    public async Task Model_maps_draws_penalties_and_match_placementsAsync()
    {
        await using var context = PlayUpInMemory.CreateContext();
        var entity = context.Model.FindEntityType(typeof(Stage));
        entity.Should().NotBeNull();
        entity.FindNavigation(nameof(Stage.Draws)).Should().NotBeNull();
        entity.FindNavigation(nameof(Stage.Penalties)).Should().NotBeNull();
        entity.FindNavigation(nameof(Stage.MatchPlacements)).Should().NotBeNull();
        entity.FindNavigation(nameof(Stage.SwissByeHistory)).Should().NotBeNull();
        entity.FindNavigation(nameof(Stage.DomainEvents)).Should().BeNull();

        context.Model.GetEntityTypes().Select(type => type.GetTableName())
            .Should().Contain(["draws", "penalties", "match_placements", "stage_swiss_byes"]);
        context.Model.GetEntityTypes().Select(type => type.GetTableName())
            .Should().NotContain(["qualification_results", "progression_results", "standings"]);
    }

    [Fact]
    public async Task Swiss_settings_and_bye_history_round_tripAsync()
    {
        var databaseName = Guid.NewGuid().ToString();
        var byeEntry = EntryId.New();
        StageId stageId;

        await using (var context = PlayUpInMemory.CreateContext(databaseName))
        {
            var stage = Stage.Create(CompetitionId.New(), new StageName("Swiss"), SampleRegulations.Standard(), _clock);
            stage.SetSwissSettings(new SwissSettings(3));
            stage.AddMatchday(1, _clock);
            stage.RecordSwissBye(1, byeEntry);
            stageId = stage.Id;
            new StageRepository(context).Add(stage);
            await ((IUnitOfWork)context).SaveChangesAsync();
        }

        await using (var context = PlayUpInMemory.CreateContext(databaseName))
        {
            var loaded = await new StageRepository(context).GetByIdForUpdateAsync(stageId);
            loaded.Should().NotBeNull();
            loaded.IsSwiss.Should().BeTrue();
            loaded.SwissSettings.Should().Be(new SwissSettings(3));
            loaded.SwissByeHistory.Should().ContainSingle().Which.Should().Be(new SwissBye(1, byeEntry));
            loaded.CountSwissByes(byeEntry).Should().Be(1);
            loaded.Matchdays.Should().ContainSingle().Which.Number.Should().Be(1);
        }
    }

    [Fact]
    public async Task SaveChanges_does_not_clear_domain_eventsAsync()
    {
        await using var context = PlayUpInMemory.CreateContext();
        var stage = Stage.Create(CompetitionId.New(), new StageName("Cup"), SampleRegulations.Standard(), _clock);
        new StageRepository(context).Add(stage);
        await ((IUnitOfWork)context).SaveChangesAsync();

        stage.DomainEvents.Should().NotBeEmpty();
    }
}
