// -----------------------------------------------------------------------
// <copyright file="StageLifecyclePersistenceTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MyClub.PlayUp.Application.Abstractions;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;
using MyClub.PlayUp.Infrastructure.Persistence;
using MyClub.PlayUp.Infrastructure.Tests.Common;
using Npgsql;
using Xunit;

namespace MyClub.PlayUp.Infrastructure.Tests.Persistence;

[Collection("postgres")]
[Trait("Category", "Integration")]
public sealed class StageLifecyclePersistenceTests(PostgresFixture fixture)
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 8, 14, 16, 0, 0, TimeSpan.Zero));

    [IntegrationFact]
    public async Task Create_reload_preserves_regulation_and_completed_statusAsync()
    {
        StageId stageId;
        var regulation = StageRegulation.MaterializeFrom(SampleRegulations.WithExtraTimeAndShootout());

        using (var scope = fixture.CreateScope())
        {
            var competitions = scope.ServiceProvider.GetRequiredService<ICompetitionRepository>();
            var stages = scope.ServiceProvider.GetRequiredService<IStageRepository>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var competition = Competition.Create(new CompetitionName("Cup"), SampleRegulations.Standard(), _clock);
            competitions.Add(competition);

            var stage = Stage.Create(competition.Id, new StageName("Knockout"), regulation, _clock);
            stage.AddRound("Final", _clock);
            stage.Prepare(_clock);
            stage.Start(_clock);
            stage.Complete(_clock);
            stageId = stage.Id;
            stages.Add(stage);
            await unitOfWork.SaveChangesAsync();
        }

        using (var scope = fixture.CreateScope())
        {
            var loaded = await scope.ServiceProvider.GetRequiredService<IStageRepository>().GetByIdForUpdateAsync(stageId);
            loaded.Should().NotBeNull();
            loaded.Status.Should().Be(StageStatus.Completed);
            loaded.Regulation.Should().Be(regulation);
        }
    }

    [IntegrationFact]
    public async Task ReplaceRegulation_persists_after_reloadAsync()
    {
        StageId stageId;
        var replacement = StageRegulation.MaterializeFrom(SampleRegulations.WithExtraTimeAndShootout());

        using (var scope = fixture.CreateScope())
        {
            var competitions = scope.ServiceProvider.GetRequiredService<ICompetitionRepository>();
            var stages = scope.ServiceProvider.GetRequiredService<IStageRepository>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var competition = Competition.Create(new CompetitionName("League"), SampleRegulations.Standard(), _clock);
            competitions.Add(competition);

            var stage = Stage.Create(
                competition.Id,
                new StageName("Groups"),
                StageRegulation.MaterializeFrom(SampleRegulations.Standard()),
                _clock);
            stageId = stage.Id;
            stages.Add(stage);
            await unitOfWork.SaveChangesAsync();
        }

        using (var scope = fixture.CreateScope())
        {
            var stages = scope.ServiceProvider.GetRequiredService<IStageRepository>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var loaded = await stages.GetByIdForUpdateAsync(stageId);
            loaded.Should().NotBeNull();
            loaded.ReplaceRegulation(replacement, _clock);
            await unitOfWork.SaveChangesAsync();
        }

        using (var scope = fixture.CreateScope())
        {
            var reloaded = await scope.ServiceProvider.GetRequiredService<IStageRepository>().GetByIdForUpdateAsync(stageId);
            reloaded.Should().NotBeNull();
            reloaded.Regulation.Should().Be(replacement);
        }
    }

    [IntegrationFact]
    public async Task StageRegulation_nested_families_round_trip_on_postgresAsync()
    {
        StageId stageId;
        var groupId = GroupId.New();
        var destinationStageId = StageId.New();
        var fixtureId = FixtureId.New();
        var progressionStageId = StageId.New();
        var regulation = StageRegulation.MaterializeFrom(SampleRegulations.WithExtraTimeAndShootout())
            .WithTieFormat(new TieFormat(2, true, new AwayGoalsRule()))
            .WithDrawRules(
                new DrawRules(
                    DrawMode.Random,
                    new SeedingRules(4),
                    constraints:
                    [
                        new DrawConstraint(DrawConstraintType.SameGroupAvoidance),
                        DrawConstraint.MaxSameAssociationPerGroup(1)
                    ]))
            .WithQualificationRules(
                new QualificationRules(
                [
                    new QualificationPath(
                        1,
                        QualificationSource.FromGroup(groupId),
                        new QualificationSelection(SelectionMode.Position, 1),
                        new QualificationDestination(destinationStageId, "QF1"),
                        QualificationCondition.PointsAtLeast(4))
                ]))
            .WithProgressionRules(
                new ProgressionRules(
                [
                    new ProgressionPath(
                        fixtureId,
                        ProgressionOutcome.Winner,
                        new ProgressionDestination(progressionStageId, "SF1-A"))
                ]));

        using (var scope = fixture.CreateScope())
        {
            var competitions = scope.ServiceProvider.GetRequiredService<ICompetitionRepository>();
            var stages = scope.ServiceProvider.GetRequiredService<IStageRepository>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var competition = Competition.Create(new CompetitionName("Cup"), SampleRegulations.Standard(), _clock);
            competitions.Add(competition);

            var stage = Stage.Create(competition.Id, new StageName("Knockout"), regulation, _clock);
            stageId = stage.Id;
            stages.Add(stage);
            await unitOfWork.SaveChangesAsync();
        }

        using (var scope = fixture.CreateScope())
        {
            var loaded = await scope.ServiceProvider.GetRequiredService<IStageRepository>().GetByIdForUpdateAsync(stageId);
            loaded.Should().NotBeNull();
            loaded.Regulation.Should().Be(regulation);
            loaded.Regulation.DrawRules!.Constraints.Should().Contain(c =>
                c.ConstraintType == DrawConstraintType.MaxSameAssociationPerGroup && c.MaxPerGroup == 1);
            loaded.Regulation.QualificationRules!.Paths[0].Condition!.MinimumPoints.Should().Be(4);
            loaded.Regulation.ProgressionRules!.Paths.Should().ContainSingle();
        }
    }

    [IntegrationFact]
    public async Task Round_TieFormat_null_and_non_null_round_trip_on_postgresAsync()
    {
        StageId stageId;
        RoundId nullTieFormatRoundId;
        RoundId richTieFormatRoundId;
        var richTieFormat = new TieFormat(
            2,
            true,
            new AwayGoalsRule(),
            new ExtraTimeRule(),
            new PenaltyShootoutRule());

        using (var scope = fixture.CreateScope())
        {
            var competitions = scope.ServiceProvider.GetRequiredService<ICompetitionRepository>();
            var stages = scope.ServiceProvider.GetRequiredService<IStageRepository>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var competition = Competition.Create(new CompetitionName("Cup"), SampleRegulations.Standard(), _clock);
            competitions.Add(competition);

            var stage = Stage.Create(competition.Id, new StageName("Knockout"), SampleRegulations.Standard(), _clock);
            nullTieFormatRoundId = stage.AddRound("Final", _clock).Id;
            richTieFormatRoundId = stage.AddRound("Semi-finals", richTieFormat, _clock).Id;
            stageId = stage.Id;
            stages.Add(stage);
            await unitOfWork.SaveChangesAsync();
        }

        using (var scope = fixture.CreateScope())
        {
            var loaded = await scope.ServiceProvider.GetRequiredService<IStageRepository>().GetByIdForUpdateAsync(stageId);
            loaded.Should().NotBeNull();
            loaded.Rounds.Single(round => round.Id == nullTieFormatRoundId).TieFormat.Should().BeNull();
            loaded.Rounds.Single(round => round.Id == richTieFormatRoundId).TieFormat.Should().Be(richTieFormat);
        }
    }

    [IntegrationFact]
    public async Task Structure_ordres_fixtures_xor_slots_and_attachments_round_tripAsync()
    {
        StageId stageId;
        var entryA = EntryId.New();
        var entryB = EntryId.New();
        MatchId matchId;

        using (var scope = fixture.CreateScope())
        {
            var competitions = scope.ServiceProvider.GetRequiredService<ICompetitionRepository>();
            var stages = scope.ServiceProvider.GetRequiredService<IStageRepository>();
            var matches = scope.ServiceProvider.GetRequiredService<IMatchRepository>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var competition = Competition.Create(new CompetitionName("Poules"), SampleRegulations.Standard(), _clock);
            competitions.Add(competition);

            var stage = Stage.Create(competition.Id, new StageName("Poules"), SampleRegulations.Standard(), _clock);
            var groupA = stage.AddGroup("A", _clock);
            stage.AddGroup("B", _clock);
            stage.AssignEntryToGroup(groupA.Id, entryA, _clock);
            stage.AssignEntryToGroup(groupA.Id, entryB, _clock);
            stage.ArrangeGroups([stage.Groups[1].Id, stage.Groups[0].Id], _clock);

            var matchday1 = stage.AddMatchday(1, _clock);
            stage.AddMatchday(2, _clock);
            stage.ArrangeMatchdays([stage.Matchdays[1].Id, stage.Matchdays[0].Id], _clock);

            _ = stage.AddFixture(matchday1.Id, _clock);
            stage.AddSlot("W1", _clock);
            stage.AssignEntryToSlot("W1", entryA, _clock);

            stageId = stage.Id;
            stages.Add(stage);
            await unitOfWork.SaveChangesAsync();

            var match = Match.Create(competition.Id, stage.Id, entryA, entryB, _clock);
            matchId = match.Id;
            matches.Add(match);
            await unitOfWork.SaveChangesAsync();
        }

        using (var scope = fixture.CreateScope())
        {
            var stages = scope.ServiceProvider.GetRequiredService<IStageRepository>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var tracked = await stages.GetByIdForUpdateAsync(stageId);
            tracked.Should().NotBeNull();
            var fixtureId = tracked.Matchdays.SelectMany(matchday => matchday.Fixtures).Single().Id;
            tracked.AttachMatch(fixtureId, matchId, 1, _clock);
            await unitOfWork.SaveChangesAsync();
        }

        using (var scope = fixture.CreateScope())
        {
            var loaded = await scope.ServiceProvider.GetRequiredService<IStageRepository>().GetByIdForUpdateAsync(stageId);
            loaded.Should().NotBeNull();
            loaded.Groups.Select(group => group.Name).Should().Equal("B", "A");
            loaded.Groups[1].EntryIds.Should().Equal(entryA, entryB);
            loaded.Matchdays.Select(matchday => matchday.Number).Should().Equal(2, 1);
            loaded.Matchdays[1].Fixtures.Should().ContainSingle()
                .Which.Attachments.Should().ContainSingle()
                .Which.Should().Be(new MatchAttachment(matchId, 1));
            loaded.Slots.Should().ContainSingle().Which.EntryId.Should().Be(entryA);
            loaded.DirectAssignments.Should().ContainSingle();
        }
    }

    [IntegrationFact]
    public async Task Round_fixture_xor_and_matchday_fixture_xor_persist_parentsAsync()
    {
        StageId knockoutId;
        StageId leagueId;
        RoundId roundId;
        MatchdayId matchdayId;
        FixtureId roundFixtureId;
        FixtureId matchdayFixtureId;

        using (var scope = fixture.CreateScope())
        {
            var competitions = scope.ServiceProvider.GetRequiredService<ICompetitionRepository>();
            var stages = scope.ServiceProvider.GetRequiredService<IStageRepository>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var competition = Competition.Create(new CompetitionName("Season"), SampleRegulations.Standard(), _clock);
            competitions.Add(competition);

            var knockout = Stage.Create(competition.Id, new StageName("Cup"), SampleRegulations.Standard(), _clock);
            var round = knockout.AddRound("SF", new TieFormat(2, true), _clock);
            var roundFixture = knockout.AddFixture(round.Id, _clock);
            knockoutId = knockout.Id;
            roundId = round.Id;
            roundFixtureId = roundFixture.Id;
            stages.Add(knockout);

            var league = Stage.Create(competition.Id, new StageName("League"), SampleRegulations.Standard(), _clock);
            var matchday = league.AddMatchday(1, _clock);
            var matchdayFixture = league.AddFixture(matchday.Id, _clock);
            leagueId = league.Id;
            matchdayId = matchday.Id;
            matchdayFixtureId = matchdayFixture.Id;
            stages.Add(league);

            await unitOfWork.SaveChangesAsync();
        }

        using (var scope = fixture.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<PlayUpDbContext>();
            var stages = scope.ServiceProvider.GetRequiredService<IStageRepository>();

            var knockout = await stages.GetByIdForUpdateAsync(knockoutId);
            knockout.Should().NotBeNull();
            knockout.Rounds[0].Fixtures.Should().ContainSingle().Which.Id.Should().Be(roundFixtureId);
            context.Entry(knockout.Rounds[0].Fixtures[0]).Property<RoundId?>("round_id").CurrentValue.Should().Be(roundId);
            context.Entry(knockout.Rounds[0].Fixtures[0]).Property<MatchdayId?>("matchday_id").CurrentValue.Should().BeNull();

            var league = await stages.GetByIdForUpdateAsync(leagueId);
            league.Should().NotBeNull();
            league.Matchdays[0].Fixtures.Should().ContainSingle().Which.Id.Should().Be(matchdayFixtureId);
            context.Entry(league.Matchdays[0].Fixtures[0]).Property<MatchdayId?>("matchday_id").CurrentValue.Should().Be(matchdayId);
            context.Entry(league.Matchdays[0].Fixtures[0]).Property<RoundId?>("round_id").CurrentValue.Should().BeNull();
        }
    }

    [IntegrationFact]
    public async Task Delete_stage_with_match_is_restrictedAsync()
    {
        StageId stageId;

        using (var scope = fixture.CreateScope())
        {
            var competitions = scope.ServiceProvider.GetRequiredService<ICompetitionRepository>();
            var stages = scope.ServiceProvider.GetRequiredService<IStageRepository>();
            var matches = scope.ServiceProvider.GetRequiredService<IMatchRepository>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var competition = Competition.Create(new CompetitionName("League"), SampleRegulations.Standard(), _clock);
            competitions.Add(competition);

            var stage = StageSeed.CreateDraft(competition.Id, _clock);
            stageId = stage.Id;
            stages.Add(stage);
            await unitOfWork.SaveChangesAsync();

            matches.Add(Match.Create(competition.Id, stageId, EntryId.New(), EntryId.New(), _clock));
            await unitOfWork.SaveChangesAsync();
        }

        using (var scope = fixture.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<PlayUpDbContext>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var stage = await context.Set<Stage>().SingleAsync(candidate => candidate.Id == stageId);
            context.Remove(stage);

            var act = async () => await unitOfWork.SaveChangesAsync();
            await act.Should().ThrowAsync<DbUpdateException>();
        }
    }

    [IntegrationFact]
    public async Task Competition_stage_ref_requires_stage_row_and_restricts_deleteAsync()
    {
        CompetitionId competitionId;
        StageId stageId;

        using (var scope = fixture.CreateScope())
        {
            var competitions = scope.ServiceProvider.GetRequiredService<ICompetitionRepository>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var competition = Competition.Create(new CompetitionName("League"), SampleRegulations.Standard(), _clock);
            stageId = StageId.New();
            competition.AddStage(stageId, _clock);
            competitions.Add(competition);

            var act = async () => await unitOfWork.SaveChangesAsync();
            await act.Should().ThrowAsync<DbUpdateException>();
        }

        using (var scope = fixture.CreateScope())
        {
            var competitions = scope.ServiceProvider.GetRequiredService<ICompetitionRepository>();
            var stages = scope.ServiceProvider.GetRequiredService<IStageRepository>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var competition = Competition.Create(new CompetitionName("League"), SampleRegulations.Standard(), _clock);
            competitionId = competition.Id;
            competitions.Add(competition);
            await unitOfWork.SaveChangesAsync();

            var stage = StageSeed.CreateDraft(competitionId, _clock);
            stageId = stage.Id;
            stages.Add(stage);
            await unitOfWork.SaveChangesAsync();

            var tracked = await competitions.GetByIdForUpdateAsync(competitionId);
            tracked.Should().NotBeNull();
            tracked.AddStage(stageId, _clock);
            await unitOfWork.SaveChangesAsync();
        }

        using (var scope = fixture.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<PlayUpDbContext>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var stage = await context.Set<Stage>().SingleAsync(candidate => candidate.Id == stageId);
            context.Remove(stage);

            var act = async () => await unitOfWork.SaveChangesAsync();
            await act.Should().ThrowAsync<DbUpdateException>();
        }

        using (var scope = fixture.CreateScope())
        {
            var competitions = scope.ServiceProvider.GetRequiredService<ICompetitionRepository>();
            var loaded = await competitions.GetByIdForUpdateAsync(competitionId);
            loaded.Should().NotBeNull();
            loaded.StageIds.Should().Equal(stageId);
        }
    }

    [IntegrationFact]
    public async Task Delete_competition_with_stage_is_restrictedAsync()
    {
        CompetitionId competitionId;

        using (var scope = fixture.CreateScope())
        {
            var competitions = scope.ServiceProvider.GetRequiredService<ICompetitionRepository>();
            var stages = scope.ServiceProvider.GetRequiredService<IStageRepository>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var competition = Competition.Create(new CompetitionName("League"), SampleRegulations.Standard(), _clock);
            competitionId = competition.Id;
            competitions.Add(competition);
            stages.Add(StageSeed.CreateDraft(competitionId, _clock));
            await unitOfWork.SaveChangesAsync();
        }

        using (var scope = fixture.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<PlayUpDbContext>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var competition = await context.Set<Competition>().SingleAsync(candidate => candidate.Id == competitionId);
            context.Remove(competition);

            var act = async () => await unitOfWork.SaveChangesAsync();
            await act.Should().ThrowAsync<DbUpdateException>();
        }
    }

    [IntegrationFact]
    public async Task Cascade_stage_deletes_structure_childrenAsync()
    {
        StageId stageId;
        GroupId groupId;

        using (var scope = fixture.CreateScope())
        {
            var competitions = scope.ServiceProvider.GetRequiredService<ICompetitionRepository>();
            var stages = scope.ServiceProvider.GetRequiredService<IStageRepository>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var competition = Competition.Create(new CompetitionName("League"), SampleRegulations.Standard(), _clock);
            competitions.Add(competition);

            var stage = StageSeed.CreateDraft(competition.Id, _clock);
            var group = stage.AddGroup("A", _clock);
            stage.AssignEntryToGroup(group.Id, EntryId.New(), _clock);
            stage.AddMatchday(1, _clock);
            stage.AddFixture(stage.Matchdays[0].Id, _clock);
            stage.AddSlot("S1", _clock);
            stageId = stage.Id;
            groupId = group.Id;
            stages.Add(stage);
            await unitOfWork.SaveChangesAsync();
        }

        using (var scope = fixture.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<PlayUpDbContext>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            context.Remove(await context.Set<Stage>().SingleAsync(candidate => candidate.Id == stageId));
            await unitOfWork.SaveChangesAsync();
        }

        using (var scope = fixture.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<PlayUpDbContext>();
            (await context.Set<Stage>().CountAsync(candidate => candidate.Id == stageId)).Should().Be(0);
            (await context.Set<Group>().CountAsync(candidate => candidate.Id == groupId)).Should().Be(0);
            (await context.Set<GroupEntryRef>().CountAsync(row => row.GroupId == groupId)).Should().Be(0);
        }
    }

    [IntegrationFact]
    public async Task Fixture_xor_check_rejects_invalid_rowAsync()
    {
        using var scope = fixture.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<PlayUpDbContext>();

        var act = async () => await context.Database.ExecuteSqlRawAsync(
            """
            INSERT INTO fixtures (id, round_id, matchday_id, slot_a_key, slot_b_key, sort_order)
            VALUES ({0}, NULL, NULL, NULL, NULL, 0)
            """,
            Guid.CreateVersion7());

        await act.Should().ThrowAsync<PostgresException>();
    }

    [IntegrationFact]
    public async Task Fixture_xor_check_rejects_both_parents_setAsync()
    {
        Guid roundId;
        Guid matchdayId;

        using (var scope = fixture.CreateScope())
        {
            var competitions = scope.ServiceProvider.GetRequiredService<ICompetitionRepository>();
            var stages = scope.ServiceProvider.GetRequiredService<IStageRepository>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var competition = Competition.Create(new CompetitionName("Season"), SampleRegulations.Standard(), _clock);
            competitions.Add(competition);

            var knockout = Stage.Create(competition.Id, new StageName("Cup"), SampleRegulations.Standard(), _clock);
            var round = knockout.AddRound("SF", _clock);
            roundId = round.Id.Value;
            stages.Add(knockout);

            var league = Stage.Create(competition.Id, new StageName("League"), SampleRegulations.Standard(), _clock);
            var matchday = league.AddMatchday(1, _clock);
            matchdayId = matchday.Id.Value;
            stages.Add(league);

            await unitOfWork.SaveChangesAsync();
        }

        using (var scope = fixture.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<PlayUpDbContext>();

            var act = async () => await context.Database.ExecuteSqlRawAsync(
                """
                INSERT INTO fixtures (id, round_id, matchday_id, slot_a_key, slot_b_key, sort_order)
                VALUES ({0}, {1}, {2}, NULL, NULL, 0)
                """,
                Guid.CreateVersion7(),
                roundId,
                matchdayId);

            var exception = await act.Should().ThrowAsync<PostgresException>();
            exception.Which.SqlState.Should().Be(PostgresErrorCodes.CheckViolation);
            exception.Which.ConstraintName.Should().Be("CK_fixtures_round_xor_matchday");
        }
    }

    [IntegrationFact]
    public async Task Arrange_groups_while_unchanged_persists_sort_order_on_postgresAsync()
    {
        StageId stageId;
        GroupId first;
        GroupId second;
        GroupId third;

        using (var scope = fixture.CreateScope())
        {
            var competitions = scope.ServiceProvider.GetRequiredService<ICompetitionRepository>();
            var stages = scope.ServiceProvider.GetRequiredService<IStageRepository>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var competition = Competition.Create(new CompetitionName("Poules"), SampleRegulations.Standard(), _clock);
            competitions.Add(competition);

            var stage = StageSeed.CreateDraft(competition.Id, _clock, "Groups");
            first = stage.AddGroup("A", _clock).Id;
            second = stage.AddGroup("B", _clock).Id;
            third = stage.AddGroup("C", _clock).Id;
            stageId = stage.Id;
            stages.Add(stage);
            await unitOfWork.SaveChangesAsync();
        }

        using (var scope = fixture.CreateScope())
        {
            var stages = scope.ServiceProvider.GetRequiredService<IStageRepository>();
            var context = scope.ServiceProvider.GetRequiredService<PlayUpDbContext>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var loaded = await stages.GetByIdForUpdateAsync(stageId);
            loaded.Should().NotBeNull();
            loaded.Groups.Select(group => group.Id).Should().Equal(first, second, third);
            context.Entry(loaded).State.Should().Be(EntityState.Unchanged);

            loaded.ArrangeGroups([third, first, second], _clock);
            context.Entry(loaded).State.Should().Be(EntityState.Unchanged);

            await unitOfWork.SaveChangesAsync();
        }

        using (var scope = fixture.CreateScope())
        {
            var reloaded = await scope.ServiceProvider.GetRequiredService<IStageRepository>().GetByIdForUpdateAsync(stageId);
            reloaded.Should().NotBeNull();
            reloaded.Groups.Select(group => group.Id).Should().Equal(third, first, second);
        }
    }

    [IntegrationFact]
    public async Task Arrange_matchdays_while_unchanged_persists_sort_order_on_postgresAsync()
    {
        StageId stageId;
        MatchdayId first;
        MatchdayId second;
        MatchdayId third;

        using (var scope = fixture.CreateScope())
        {
            var competitions = scope.ServiceProvider.GetRequiredService<ICompetitionRepository>();
            var stages = scope.ServiceProvider.GetRequiredService<IStageRepository>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var competition = Competition.Create(new CompetitionName("League"), SampleRegulations.Standard(), _clock);
            competitions.Add(competition);

            var stage = StageSeed.CreateDraft(competition.Id, _clock, "Matchdays");
            first = stage.AddMatchday(1, _clock).Id;
            second = stage.AddMatchday(2, _clock).Id;
            third = stage.AddMatchday(3, _clock).Id;
            stageId = stage.Id;
            stages.Add(stage);
            await unitOfWork.SaveChangesAsync();
        }

        using (var scope = fixture.CreateScope())
        {
            var stages = scope.ServiceProvider.GetRequiredService<IStageRepository>();
            var context = scope.ServiceProvider.GetRequiredService<PlayUpDbContext>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var loaded = await stages.GetByIdForUpdateAsync(stageId);
            loaded.Should().NotBeNull();
            context.Entry(loaded).State.Should().Be(EntityState.Unchanged);

            loaded.ArrangeMatchdays([third, first, second], _clock);
            context.Entry(loaded).State.Should().Be(EntityState.Unchanged);

            await unitOfWork.SaveChangesAsync();
        }

        using (var scope = fixture.CreateScope())
        {
            var reloaded = await scope.ServiceProvider.GetRequiredService<IStageRepository>().GetByIdForUpdateAsync(stageId);
            reloaded.Should().NotBeNull();
            reloaded.Matchdays.Select(matchday => matchday.Id).Should().Equal(third, first, second);
            reloaded.Matchdays.Select(matchday => matchday.Number).Should().Equal(3, 1, 2);
        }
    }

    [IntegrationFact]
    public async Task Arrange_rounds_while_unchanged_persists_sort_order_on_postgresAsync()
    {
        StageId stageId;
        RoundId first;
        RoundId second;
        RoundId third;

        using (var scope = fixture.CreateScope())
        {
            var competitions = scope.ServiceProvider.GetRequiredService<ICompetitionRepository>();
            var stages = scope.ServiceProvider.GetRequiredService<IStageRepository>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var competition = Competition.Create(new CompetitionName("Cup"), SampleRegulations.Standard(), _clock);
            competitions.Add(competition);

            var stage = StageSeed.CreateDraft(competition.Id, _clock, "Rounds");
            first = stage.AddRound("QF", _clock).Id;
            second = stage.AddRound("SF", _clock).Id;
            third = stage.AddRound("Final", _clock).Id;
            stageId = stage.Id;
            stages.Add(stage);
            await unitOfWork.SaveChangesAsync();
        }

        using (var scope = fixture.CreateScope())
        {
            var stages = scope.ServiceProvider.GetRequiredService<IStageRepository>();
            var context = scope.ServiceProvider.GetRequiredService<PlayUpDbContext>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var loaded = await stages.GetByIdForUpdateAsync(stageId);
            loaded.Should().NotBeNull();
            context.Entry(loaded).State.Should().Be(EntityState.Unchanged);

            loaded.ArrangeRounds([third, first, second], _clock);
            context.Entry(loaded).State.Should().Be(EntityState.Unchanged);

            await unitOfWork.SaveChangesAsync();
        }

        using (var scope = fixture.CreateScope())
        {
            var reloaded = await scope.ServiceProvider.GetRequiredService<IStageRepository>().GetByIdForUpdateAsync(stageId);
            reloaded.Should().NotBeNull();
            reloaded.Rounds.Select(round => round.Id).Should().Equal(third, first, second);
        }
    }

    [IntegrationFact]
    public async Task Delete_match_with_fixture_attachment_is_restrictedAsync()
    {
        MatchId matchId;

        using (var scope = fixture.CreateScope())
        {
            var competitions = scope.ServiceProvider.GetRequiredService<ICompetitionRepository>();
            var stages = scope.ServiceProvider.GetRequiredService<IStageRepository>();
            var matches = scope.ServiceProvider.GetRequiredService<IMatchRepository>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var competition = Competition.Create(new CompetitionName("Cup"), SampleRegulations.Standard(), _clock);
            competitions.Add(competition);

            var stage = Stage.Create(competition.Id, new StageName("Knockout"), SampleRegulations.Standard(), _clock);
            var round = stage.AddRound("Final", _clock);
            var addFixture = stage.AddFixture(round.Id, _clock);
            var stageId = stage.Id;
            stages.Add(stage);
            await unitOfWork.SaveChangesAsync();

            var match = Match.Create(competition.Id, stage.Id, EntryId.New(), EntryId.New(), _clock);
            matchId = match.Id;
            matches.Add(match);
            await unitOfWork.SaveChangesAsync();

            var tracked = await stages.GetByIdForUpdateAsync(stageId);
            tracked.Should().NotBeNull();
            tracked.AttachMatch(addFixture.Id, matchId, legIndex: 1, _clock);
            await unitOfWork.SaveChangesAsync();
        }

        using (var scope = fixture.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<PlayUpDbContext>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var match = await context.Set<Match>().SingleAsync(candidate => candidate.Id == matchId);
            context.Remove(match);

            var act = async () => await unitOfWork.SaveChangesAsync();
            await act.Should().ThrowAsync<DbUpdateException>();
        }

        using (var scope = fixture.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<PlayUpDbContext>();
            (await context.Set<Match>().CountAsync(candidate => candidate.Id == matchId)).Should().Be(1);
            (await context.Set<FixtureAttachmentRef>().CountAsync(row => row.MatchId == matchId)).Should().Be(1);
        }
    }
}
