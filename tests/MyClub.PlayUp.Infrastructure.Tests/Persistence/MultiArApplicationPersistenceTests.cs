// -----------------------------------------------------------------------
// <copyright file="MultiArApplicationPersistenceTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MyClub.PlayUp.Application.Abstractions;
using MyClub.PlayUp.Application.Stages;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;
using MyClub.PlayUp.Domain.Standings;
using MyClub.PlayUp.Infrastructure.Tests.Common;
using Npgsql;
using Xunit;

namespace MyClub.PlayUp.Infrastructure.Tests.Persistence;

/// <summary>
/// PostgreSQL proofs that Application Apply* compositions persist multiple ARs atomically
/// via one scoped <see cref="IUnitOfWork.SaveChangesAsync"/>.
/// </summary>
[Collection("postgres")]
[Trait("Category", "Integration")]
public sealed class MultiArApplicationPersistenceTests(PostgresFixture fixture)
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 8, 14, 19, 0, 0, TimeSpan.Zero));

    [IntegrationFact]
    public async Task ApplyDraw_slot_persists_slot_occupancy_atomicallyAsync()
    {
        StageId stageId;
        DrawId drawId;
        var entryA = EntryId.New();
        var entryB = EntryId.New();

        using (var scope = fixture.CreateScope())
        {
            var competitions = scope.ServiceProvider.GetRequiredService<ICompetitionRepository>();
            var stages = scope.ServiceProvider.GetRequiredService<IStageRepository>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var competition = Competition.Create(new CompetitionName("Cup"), SampleRegulations.Standard(), _clock);
            competitions.Add(competition);

            var stage = Stage.Create(competition.Id, new StageName("QF"), SampleRegulations.Standard(), _clock);
            stage.AddSlot("S1");
            stage.AddSlot("S2");
            var draw = stage.CreateDraw(DrawResolutionKind.Slot, _clock);
            stage.ConfigureDrawInputs(draw.Id, DrawInputs.ForSlot([entryA, entryB]));
            stage.RecordDrawResolution(
                draw.Id,
                DrawResolution.ResolvedSlots(
                [
                    new SlotDrawPlacement(entryA, "S1"),
                    new SlotDrawPlacement(entryB, "S2")
                ]),
                _clock);
            stage.PublishDraw(draw.Id, _clock);
            stageId = stage.Id;
            drawId = draw.Id;
            stages.Add(stage);
            await unitOfWork.SaveChangesAsync();
        }

        using (var scope = fixture.CreateScope())
        {
            var stages = scope.ServiceProvider.GetRequiredService<IStageRepository>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var stage = await stages.GetByIdForUpdateAsync(stageId);
            stage.Should().NotBeNull();

            var result = ApplyDraw.Execute(stage, drawId, _clock);

            result.SlotInstructions.Should().HaveCount(2);
            result.CreatedMatches.Should().BeEmpty();
            await unitOfWork.SaveChangesAsync();
        }

        using (var scope = fixture.CreateScope())
        {
            var stages = scope.ServiceProvider.GetRequiredService<IStageRepository>();

            var stage = await stages.GetByIdForUpdateAsync(stageId);
            stage.Should().NotBeNull();
            stage.FindSlot("S1")!.EntryId.Should().Be(entryA);
            stage.FindSlot("S2")!.EntryId.Should().Be(entryB);
        }
    }

    [IntegrationFact]
    public async Task ApplyQualification_persists_destination_slots_across_stages_atomicallyAsync()
    {
        StageId leagueId;
        StageId terminalId;
        var champ = EntryId.New();
        var europe = EntryId.New();

        using (var scope = fixture.CreateScope())
        {
            var competitions = scope.ServiceProvider.GetRequiredService<ICompetitionRepository>();
            var stages = scope.ServiceProvider.GetRequiredService<IStageRepository>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var competition = Competition.Create(new CompetitionName("League"), SampleRegulations.Standard(), _clock);
            competitions.Add(competition);

            var league = Stage.Create(competition.Id, new StageName("League"), SampleRegulations.Standard(), _clock);
            var terminal = Stage.Create(competition.Id, new StageName("Terminal"), SampleRegulations.Standard(), _clock);
            terminal.AddSlot("Champ");
            terminal.AddSlot("Europe1");
            league.ReplaceQualificationRules(
                new QualificationRules(
                [
                    new QualificationPath(
                        1,
                        QualificationSource.Overall(),
                        new QualificationSelection(SelectionMode.Position, 1),
                        QualificationDestination.ForPopulation(terminal.Id)),
                    new QualificationPath(
                        2,
                        QualificationSource.Overall(),
                        new QualificationSelection(SelectionMode.Position, 2),
                        QualificationDestination.ForPopulation(terminal.Id))
                ]),
                _clock);

            leagueId = league.Id;
            terminalId = terminal.Id;
            stages.Add(league);
            stages.Add(terminal);
            await unitOfWork.SaveChangesAsync();
        }

        using (var scope = fixture.CreateScope())
        {
            var stages = scope.ServiceProvider.GetRequiredService<IStageRepository>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var league = await stages.GetByIdForUpdateAsync(leagueId);
            var terminal = await stages.GetByIdForUpdateAsync(terminalId);
            league.Should().NotBeNull();
            terminal.Should().NotBeNull();

            var standing = new Standing(
            [
                new StandingRow(champ, position: 1, played: 2, wins: 2, draws: 0, losses: 0, goalsFor: 4, goalsAgainst: 0, points: 6),
                new StandingRow(europe, position: 2, played: 2, wins: 0, draws: 0, losses: 2, goalsFor: 0, goalsAgainst: 4, points: 0)
            ]);

            ApplyQualification.Execute(league, standing, [league, terminal], _clock);
            await unitOfWork.SaveChangesAsync();
        }

        using (var scope = fixture.CreateScope())
        {
            var terminal = await scope.ServiceProvider.GetRequiredService<IStageRepository>().GetByIdForUpdateAsync(terminalId);
            terminal.Should().NotBeNull();
            terminal.CompositionEntries.Select(e => e.EntryId).Should().BeEquivalentTo([champ, europe]);
            terminal.FindSlot("Champ")!.EntryId.Should().BeNull();
            terminal.FindSlot("Europe1")!.EntryId.Should().BeNull();
        }
    }

    [IntegrationFact]
    public async Task ApplyProgressionOutcome_persists_destination_slot_atomicallyAsync()
    {
        StageId sourceId;
        StageId destinationId;
        FixtureId fixtureId;
        MatchId matchId;
        var home = EntryId.New();
        var away = EntryId.New();

        using (var scope = fixture.CreateScope())
        {
            var competitions = scope.ServiceProvider.GetRequiredService<ICompetitionRepository>();
            var stages = scope.ServiceProvider.GetRequiredService<IStageRepository>();
            var matches = scope.ServiceProvider.GetRequiredService<IMatchRepository>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var competition = Competition.Create(new CompetitionName("Cup"), SampleRegulations.Standard(), _clock);
            competitions.Add(competition);

            var source = Stage.Create(competition.Id, new StageName("QF"), SampleRegulations.Standard(), _clock);
            source.AddRound("R1", new TieFormat(TieFormat.SingleLeg, aggregateScoring: false), _clock);
            source.AddSlot("QF1-A");
            source.AddSlot("QF1-B");

            var destination = Stage.Create(competition.Id, new StageName("SF"), SampleRegulations.Standard(), _clock);
            destination.AddRound("R1", new TieFormat(TieFormat.SingleLeg, aggregateScoring: false), _clock);
            destination.AddSlot("SF1-A");
            destination.AddSlot("SF1-B");

            var addFixture = source.AddFixture(source.Rounds[0].Id, _clock);
            fixtureId = addFixture.Id;
            var match = Match.Create(competition.Id, source.Id, home, away, _clock);
            matchId = match.Id;
            source.AttachMatch(addFixture.Id, match.Id, legIndex: 1, _clock);
            match.Start(_clock);
            match.Finish(new MatchResult(ResultType.Played, new Score(2, 0)), _clock);

            source.ReplaceProgressionRules(
                new ProgressionRules(
                [
                    new ProgressionPath(
                        fixtureId,
                        ProgressionOutcome.Winner,
                        ProgressionDestination.ForPopulation(destination.Id))
                ]),
                _clock);

            sourceId = source.Id;
            destinationId = destination.Id;
            stages.Add(source);
            stages.Add(destination);
            matches.Add(match);
            await unitOfWork.SaveChangesAsync();
        }

        using (var scope = fixture.CreateScope())
        {
            var stages = scope.ServiceProvider.GetRequiredService<IStageRepository>();
            var matches = scope.ServiceProvider.GetRequiredService<IMatchRepository>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var source = await stages.GetByIdForUpdateAsync(sourceId);
            var destination = await stages.GetByIdForUpdateAsync(destinationId);
            var match = await matches.GetByIdForUpdateAsync(matchId);
            source.Should().NotBeNull();
            destination.Should().NotBeNull();
            match.Should().NotBeNull();

            ApplyProgressionOutcome.Execute(source, fixtureId, [match], [source, destination], _clock);
            await unitOfWork.SaveChangesAsync();
        }

        using (var scope = fixture.CreateScope())
        {
            var destination = await scope.ServiceProvider.GetRequiredService<IStageRepository>().GetByIdForUpdateAsync(destinationId);
            destination.Should().NotBeNull();
            destination.CompositionEntries.Select(e => e.EntryId).Should().Equal(home);
            destination.FindSlot("SF1-A")!.EntryId.Should().BeNull();
            destination.FindSlot("SF1-B")!.EntryId.Should().BeNull();
        }
    }

    [IntegrationFact]
    public async Task MultiAr_save_failure_rolls_back_all_tracked_aggregatesAsync()
    {
        StageId stageId;
        MatchId orphanId;
        var entryId = EntryId.New();

        using (var scope = fixture.CreateScope())
        {
            var competitions = scope.ServiceProvider.GetRequiredService<ICompetitionRepository>();
            var stages = scope.ServiceProvider.GetRequiredService<IStageRepository>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var competition = Competition.Create(new CompetitionName("League"), SampleRegulations.Standard(), _clock);
            competitions.Add(competition);
            var stage = Stage.Create(competition.Id, new StageName("Groups"), SampleRegulations.Standard(), _clock);
            stageId = stage.Id;
            stages.Add(stage);
            await unitOfWork.SaveChangesAsync();
        }

        using (var scope = fixture.CreateScope())
        {
            var stages = scope.ServiceProvider.GetRequiredService<IStageRepository>();
            var matches = scope.ServiceProvider.GetRequiredService<IMatchRepository>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var stage = await stages.GetByIdForUpdateAsync(stageId);
            stage.Should().NotBeNull();
            stage.Penalties.Should().BeEmpty();
            stage.AddPenalty(entryId, 3, _clock, "Should roll back");

            // Invalid Match FKs force SaveChanges to fail while Stage is also dirty.
            var orphan = Match.Create(CompetitionId.New(), StageId.New(), EntryId.New(), EntryId.New(), _clock);
            orphanId = orphan.Id;
            matches.Add(orphan);

            var act = async () => await unitOfWork.SaveChangesAsync();
            var exception = await act.Should().ThrowAsync<DbUpdateException>();
            exception.Which.InnerException.Should().BeOfType<PostgresException>()
                .Which.SqlState.Should().Be(PostgresErrorCodes.ForeignKeyViolation);
        }

        using (var scope = fixture.CreateScope())
        {
            var stages = scope.ServiceProvider.GetRequiredService<IStageRepository>();
            var matches = scope.ServiceProvider.GetRequiredService<IMatchRepository>();
            var stage = await stages.GetByIdForUpdateAsync(stageId);
            stage.Should().NotBeNull();
            stage.Penalties.Should().BeEmpty();
            (await matches.GetByIdForUpdateAsync(orphanId)).Should().BeNull();
        }
    }
}
