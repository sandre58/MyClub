// -----------------------------------------------------------------------
// <copyright file="CompetitionPersistenceTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using MyClub.PlayUp.Application.Abstractions;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Infrastructure.Persistence.Repositories;
using MyClub.PlayUp.Infrastructure.Tests.Common;
using Xunit;

namespace MyClub.PlayUp.Infrastructure.Tests.Persistence;

public sealed class CompetitionPersistenceTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 8, 14, 11, 0, 0, TimeSpan.Zero));

    [Fact]
    public async Task SaveChanges_does_not_clear_domain_eventsAsync()
    {
        await using var context = PlayUpInMemory.CreateContext();
        var repository = new CompetitionRepository(context);
        IUnitOfWork unitOfWork = context;

        var competition = Competition.Create(new CompetitionName("League"), SampleRegulations.Standard(), _clock);
        repository.Add(competition);
        await unitOfWork.SaveChangesAsync();

        competition.DomainEvents.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Reload_materializes_empty_domain_eventsAsync()
    {
        var databaseName = Guid.NewGuid().ToString();
        var competition = Competition.Create(new CompetitionName("League"), SampleRegulations.Standard(), _clock);
        var id = competition.Id;

        await using (var context = PlayUpInMemory.CreateContext(databaseName))
        {
            new CompetitionRepository(context).Add(competition);
            await ((IUnitOfWork)context).SaveChangesAsync();
        }

        await using (var context = PlayUpInMemory.CreateContext(databaseName))
        {
            var loaded = await new CompetitionRepository(context).GetByIdAsync(id);
            loaded.Should().NotBeNull();
            loaded.DomainEvents.Should().BeEmpty();
        }
    }

    [Fact]
    public async Task Regulation_round_trips_including_optional_match_policiesAsync()
    {
        var databaseName = Guid.NewGuid().ToString();
        var regulation = SampleRegulations.WithExtraTimeAndShootout();
        var competition = Competition.Create(new CompetitionName("Cup"), regulation, _clock);
        var id = competition.Id;

        await using (var context = PlayUpInMemory.CreateContext(databaseName))
        {
            new CompetitionRepository(context).Add(competition);
            await ((IUnitOfWork)context).SaveChangesAsync();
        }

        await using (var context = PlayUpInMemory.CreateContext(databaseName))
        {
            var loaded = await new CompetitionRepository(context).GetByIdAsync(id);
            loaded.Should().NotBeNull();
            loaded.Regulation.Should().Be(regulation);
            loaded.Regulation.MatchRules.ExtraTimePolicy.Should().NotBeNull();
            loaded.Regulation.MatchRules.PenaltyShootoutPolicy.Should().NotBeNull();
        }
    }

    [Fact]
    public async Task Delete_start_withdraw_complete_preserves_statuses_after_reloadAsync()
    {
        var databaseName = Guid.NewGuid().ToString();
        var id = await SeedLifecycleAsync(databaseName);

        await using var context = PlayUpInMemory.CreateContext(databaseName);
        var loaded = await new CompetitionRepository(context).GetByIdAsync(id);

        loaded.Should().NotBeNull();
        loaded.Status.Should().Be(CompetitionStatus.Completed);
        loaded.CompletionMode.Should().Be(CompletionMode.Administrative);
        loaded.Entries.Should().HaveCount(2);
        loaded.Entries[0].Status.Should().Be(EntryStatus.Active);
        loaded.Entries[1].Status.Should().Be(EntryStatus.Withdrawn);
    }

    [Fact]
    public async Task Entries_keep_insertion_order_after_reloadAsync()
    {
        var databaseName = Guid.NewGuid().ToString();
        var competition = Competition.Create(new CompetitionName("League"), SampleRegulations.Standard(), _clock);
        var first = competition.AddEntry(TeamId.New(), "Alpha", _clock);
        var second = competition.AddEntry(TeamId.New(), "Beta", _clock);
        var third = competition.AddEntry(TeamId.New(), "Gamma", _clock);
        var id = competition.Id;

        await using (var context = PlayUpInMemory.CreateContext(databaseName))
        {
            new CompetitionRepository(context).Add(competition);
            await ((IUnitOfWork)context).SaveChangesAsync();
        }

        await using (var context = PlayUpInMemory.CreateContext(databaseName))
        {
            var loaded = await new CompetitionRepository(context).GetByIdAsync(id);
            loaded.Should().NotBeNull();
            loaded.Entries.Select(entry => entry.Id).Should().Equal(first.Id, second.Id, third.Id);
        }
    }

    [Fact]
    public async Task RemoveStage_persists_after_reloadAsync()
    {
        var databaseName = Guid.NewGuid().ToString();
        var stageA = StageId.New();
        var stageB = StageId.New();
        var competition = Competition.Create(new CompetitionName("League"), SampleRegulations.Standard(), _clock);
        competition.AddStage(stageA, _clock);
        competition.AddStage(stageB, _clock);
        var id = competition.Id;

        await using (var context = PlayUpInMemory.CreateContext(databaseName))
        {
            new CompetitionRepository(context).Add(competition);
            await ((IUnitOfWork)context).SaveChangesAsync();
        }

        await using (var context = PlayUpInMemory.CreateContext(databaseName))
        {
            var repository = new CompetitionRepository(context);
            var loaded = await repository.GetByIdAsync(id);
            loaded.Should().NotBeNull();
            loaded.RemoveStage(stageB, _clock);
            await ((IUnitOfWork)context).SaveChangesAsync();
        }

        await using (var context = PlayUpInMemory.CreateContext(databaseName))
        {
            var reloaded = await new CompetitionRepository(context).GetByIdAsync(id);
            reloaded.Should().NotBeNull();
            reloaded.StageIds.Should().Equal(stageA);
        }
    }

    [Fact]
    public async Task Add_and_RemoveStage_in_same_unit_of_work_persistsAsync()
    {
        var databaseName = Guid.NewGuid().ToString();
        var stageA = StageId.New();
        var stageB = StageId.New();
        var stageC = StageId.New();
        var competition = Competition.Create(new CompetitionName("League"), SampleRegulations.Standard(), _clock);
        competition.AddStage(stageA, _clock);
        competition.AddStage(stageB, _clock);
        competition.AddStage(stageC, _clock);
        competition.RemoveStage(stageB, _clock);
        var id = competition.Id;

        await using (var context = PlayUpInMemory.CreateContext(databaseName))
        {
            new CompetitionRepository(context).Add(competition);
            await ((IUnitOfWork)context).SaveChangesAsync();
        }

        await using (var context = PlayUpInMemory.CreateContext(databaseName))
        {
            var reloaded = await new CompetitionRepository(context).GetByIdAsync(id);
            reloaded.Should().NotBeNull();
            reloaded.StageIds.Should().Equal(stageA, stageC);
        }
    }

    [Fact]
    public async Task ReplaceRegulation_marks_property_modified_and_persistsAsync()
    {
        var databaseName = Guid.NewGuid().ToString();
        var initial = SampleRegulations.Standard();
        var replacement = SampleRegulations.WithExtraTimeAndShootout();
        var competition = Competition.Create(new CompetitionName("Cup"), initial, _clock);
        var id = competition.Id;

        await using (var context = PlayUpInMemory.CreateContext(databaseName))
        {
            new CompetitionRepository(context).Add(competition);
            await ((IUnitOfWork)context).SaveChangesAsync();
        }

        await using (var context = PlayUpInMemory.CreateContext(databaseName))
        {
            var repository = new CompetitionRepository(context);
            var loaded = await repository.GetByIdAsync(id);
            loaded.Should().NotBeNull();
            loaded.ReplaceRegulation(replacement, _clock);

            context.Entry(loaded).Property(candidate => candidate.Regulation).IsModified.Should().BeTrue();
            await ((IUnitOfWork)context).SaveChangesAsync();
        }

        await using (var context = PlayUpInMemory.CreateContext(databaseName))
        {
            var reloaded = await new CompetitionRepository(context).GetByIdAsync(id);
            reloaded.Should().NotBeNull();
            reloaded.Regulation.Should().Be(replacement);
            reloaded.Regulation.Should().NotBe(initial);
        }
    }

    [Fact]
    public async Task Withdrawn_team_can_reenter_with_new_entry_idAsync()
    {
        var databaseName = Guid.NewGuid().ToString();
        var teamId = TeamId.New();
        var competition = Competition.Create(new CompetitionName("League"), SampleRegulations.Standard(), _clock);
        var first = competition.AddEntry(teamId, "Team A", _clock);
        competition.WithdrawEntry(first.Id, _clock);
        var id = competition.Id;

        await using (var context = PlayUpInMemory.CreateContext(databaseName))
        {
            new CompetitionRepository(context).Add(competition);
            await ((IUnitOfWork)context).SaveChangesAsync();
        }

        EntryId secondId;
        await using (var context = PlayUpInMemory.CreateContext(databaseName))
        {
            var repository = new CompetitionRepository(context);
            var loaded = await repository.GetByIdAsync(id);
            loaded.Should().NotBeNull();
            var second = loaded.AddEntry(teamId, "Team A return", _clock);
            secondId = second.Id;
            await ((IUnitOfWork)context).SaveChangesAsync();
        }

        await using (var context = PlayUpInMemory.CreateContext(databaseName))
        {
            var reloaded = await new CompetitionRepository(context).GetByIdAsync(id);
            reloaded.Should().NotBeNull();
            reloaded.Entries.Should().HaveCount(2);
            reloaded.Entries.Select(entry => entry.Id).Should().BeEquivalentTo([first.Id, secondId]);
            reloaded.Entries.Should().OnlyContain(entry => entry.TeamId == teamId);
            reloaded.Entries.Single(entry => entry.Id == first.Id).Status.Should().Be(EntryStatus.Withdrawn);
            reloaded.Entries.Single(entry => entry.Id == secondId).Status.Should().Be(EntryStatus.Active);
        }
    }

    [Fact]
    [SuppressMessage("ReSharper", "AccessToDisposedClosure", Justification = "Test")]
    public async Task SaveChanges_throws_when_stage_ids_were_not_hydratedAsync()
    {
        var databaseName = Guid.NewGuid().ToString();
        var stageA = StageId.New();
        var competition = Competition.Create(new CompetitionName("League"), SampleRegulations.Standard(), _clock);
        competition.AddStage(stageA, _clock);
        var id = competition.Id;

        await using (var context = PlayUpInMemory.CreateContext(databaseName))
        {
            new CompetitionRepository(context).Add(competition);
            await ((IUnitOfWork)context).SaveChangesAsync();
        }

        await using (var context = PlayUpInMemory.CreateContext(databaseName))
        {
            var loadedWithoutHydration = await context.Set<Competition>()
                .SingleAsync(candidate => candidate.Id == id);
            loadedWithoutHydration.StageIds.Should().BeEmpty();

            var act = async () => await ((IUnitOfWork)context).SaveChangesAsync();
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*StageIds were not hydrated*");
        }

        await using (var context = PlayUpInMemory.CreateContext(databaseName))
        {
            var reloaded = await new CompetitionRepository(context).GetByIdAsync(id);
            reloaded.Should().NotBeNull();
            reloaded.StageIds.Should().Equal(stageA);
        }
    }

    [Fact]
    public async Task Declared_members_round_trip_with_order_role_and_mutationsAsync()
    {
        var databaseName = Guid.NewGuid().ToString();
        var competition = Competition.Create(new CompetitionName("League"), SampleRegulations.Standard(), _clock);
        var entry = competition.AddEntry(TeamId.New(), "FC Local", _clock);
        var player = competition.AddDeclaredMember(entry.Id, "Dupont", DeclaredMemberRole.Player, _clock);
        var staff = competition.AddDeclaredMember(entry.Id, "Coach", DeclaredMemberRole.Staff, _clock);
        var removed = competition.AddDeclaredMember(entry.Id, "Temp", DeclaredMemberRole.Player, _clock);
        competition.RemoveDeclaredMember(entry.Id, removed.Id, _clock);
        var id = competition.Id;
        var entryId = entry.Id;

        await using (var context = PlayUpInMemory.CreateContext(databaseName))
        {
            new CompetitionRepository(context).Add(competition);
            await ((IUnitOfWork)context).SaveChangesAsync();
        }

        await using (var context = PlayUpInMemory.CreateContext(databaseName))
        {
            var repository = new CompetitionRepository(context);
            var loaded = await repository.GetByIdAsync(id);
            loaded.Should().NotBeNull();
            var loadedEntry = loaded.Entries.Should().ContainSingle(e => e.Id == entryId).Subject;
            loadedEntry.DeclaredMembers.Select(m => m.Id).Should().Equal(player.Id, staff.Id);
            loadedEntry.DeclaredMembers[0].DisplayName.Should().Be("Dupont");
            loadedEntry.DeclaredMembers[0].Role.Should().Be(DeclaredMemberRole.Player);
            loadedEntry.DeclaredMembers[1].DisplayName.Should().Be("Coach");
            loadedEntry.DeclaredMembers[1].Role.Should().Be(DeclaredMemberRole.Staff);

            loaded.RenameDeclaredMember(entryId, player.Id, "J. Dupont", _clock);
            loaded.ChangeDeclaredMemberRole(entryId, staff.Id, DeclaredMemberRole.Player, _clock);
            await ((IUnitOfWork)context).SaveChangesAsync();
        }

        await using (var context = PlayUpInMemory.CreateContext(databaseName))
        {
            var reloaded = await new CompetitionRepository(context).GetByIdAsync(id);
            reloaded.Should().NotBeNull();
            var members = reloaded.Entries.Single(e => e.Id == entryId).DeclaredMembers;
            members.Should().HaveCount(2);
            members.Single(m => m.Id == player.Id).DisplayName.Should().Be("J. Dupont");
            members.Single(m => m.Id == staff.Id).Role.Should().Be(DeclaredMemberRole.Player);
        }
    }

    [Fact]
    public async Task Reentry_starts_with_empty_declared_members_after_reloadAsync()
    {
        var databaseName = Guid.NewGuid().ToString();
        var teamId = TeamId.New();
        var competition = Competition.Create(new CompetitionName("League"), SampleRegulations.Standard(), _clock);
        var first = competition.AddEntry(teamId, "Team A", _clock);
        competition.AddDeclaredMember(first.Id, "Dupont", DeclaredMemberRole.Player, _clock);
        competition.WithdrawEntry(first.Id, _clock);
        var id = competition.Id;

        await using (var context = PlayUpInMemory.CreateContext(databaseName))
        {
            new CompetitionRepository(context).Add(competition);
            await ((IUnitOfWork)context).SaveChangesAsync();
        }

        EntryId secondId;
        await using (var context = PlayUpInMemory.CreateContext(databaseName))
        {
            var repository = new CompetitionRepository(context);
            var loaded = await repository.GetByIdAsync(id);
            loaded.Should().NotBeNull();
            var second = loaded.AddEntry(teamId, "Team A return", _clock);
            secondId = second.Id;
            second.DeclaredMembers.Should().BeEmpty();
            await ((IUnitOfWork)context).SaveChangesAsync();
        }

        await using (var context = PlayUpInMemory.CreateContext(databaseName))
        {
            var reloaded = await new CompetitionRepository(context).GetByIdAsync(id);
            reloaded.Should().NotBeNull();
            reloaded.Entries.Single(e => e.Id == first.Id).DeclaredMembers.Should().ContainSingle()
                .Which.DisplayName.Should().Be("Dupont");
            reloaded.Entries.Single(e => e.Id == secondId).DeclaredMembers.Should().BeEmpty();
        }
    }

    [Fact]
    public async Task Presentation_metadata_round_trips_after_reloadAsync()
    {
        var databaseName = Guid.NewGuid().ToString();
        var competition = Competition.Create(new CompetitionName("Ligue 1"), SampleRegulations.Standard(), _clock);
        var competitionLogo = new LogoMediaId(Guid.CreateVersion7());
        var entryLogo = new LogoMediaId(Guid.CreateVersion7());
        competition.UpdatePresentation(ShortName.Create("L1"), competitionLogo, _clock);
        var start = new DateTimeOffset(2026, 8, 15, 0, 0, 0, TimeSpan.Zero);
        var end = new DateTimeOffset(2027, 5, 30, 0, 0, 0, TimeSpan.Zero);
        competition.SetSchedule(start, end, _clock);
        competition.AddEntry(
            TeamId.New(),
            "Paris Saint-Germain",
            _clock,
            new EntryPresentation(
                ShortName.Create("PSG"),
                entryLogo,
                TeamColor.Create("#004170"),
                TeamColor.Create("#DA291C")));
        var id = competition.Id;

        await using (var context = PlayUpInMemory.CreateContext(databaseName))
        {
            new CompetitionRepository(context).Add(competition);
            await ((IUnitOfWork)context).SaveChangesAsync();
        }

        await using (var context = PlayUpInMemory.CreateContext(databaseName))
        {
            var loaded = await new CompetitionRepository(context).GetByIdAsync(id);
            loaded.Should().NotBeNull();
            loaded.ShortName!.Value.Should().Be("L1");
            loaded.LogoMediaId.Should().Be(competitionLogo);
            loaded.ScheduledStart.Should().Be(start);
            loaded.ScheduledEnd.Should().Be(end);
            var entry = loaded.Entries.Should().ContainSingle().Subject;
            entry.ShortName!.Value.Should().Be("PSG");
            entry.LogoMediaId.Should().Be(entryLogo);
            entry.PrimaryColor!.Value.Should().Be("#004170");
            entry.SecondaryColor!.Value.Should().Be("#DA291C");
        }
    }

    private async Task<CompetitionId> SeedLifecycleAsync(string databaseName)
    {
        var competition = Competition.Create(new CompetitionName("League"), SampleRegulations.Standard(), _clock);
        competition.AddEntry(TeamId.New(), "Team A", _clock);
        competition.AddEntry(TeamId.New(), "Team B", _clock);
        var removed = competition.AddEntry(TeamId.New(), "Team C", _clock);
        competition.AddStage(StageId.New(), _clock);
        competition.Prepare(_clock);
        competition.DeleteEntry(removed.Id, _clock);
        competition.Start(_clock);
        competition.WithdrawEntry(competition.Entries[1].Id, _clock);
        competition.Complete(CompletionMode.Administrative, _clock);

        await using var context = PlayUpInMemory.CreateContext(databaseName);
        new CompetitionRepository(context).Add(competition);
        await ((IUnitOfWork)context).SaveChangesAsync();
        return competition.Id;
    }
}
