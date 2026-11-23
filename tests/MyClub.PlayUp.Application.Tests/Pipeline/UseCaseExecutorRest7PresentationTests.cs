// -----------------------------------------------------------------------
// <copyright file="UseCaseExecutorRest7PresentationTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using MyClub.PlayUp.Application.Abstractions;
using MyClub.PlayUp.Application.Competitions;
using MyClub.PlayUp.Application.Pipeline;
using MyClub.PlayUp.Application.Tests.Common;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Stages;
using Xunit;

namespace MyClub.PlayUp.Application.Tests.Pipeline;

/// <summary>
/// A10-REST-7 — presentation, declared members, regulation, and backfill under competition lock envelopes.
/// </summary>
public sealed class UseCaseExecutorRest7PresentationTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 11, 23, 20, 0, 0, TimeSpan.Zero));

    [Fact]
    public async Task UpdateCompetitionPresentationAsync_persists_once_under_competition_lockAsync()
    {
        var competition = CreateCompetition.Execute("Presentation", _clock);
        var (executor, unitOfWork) = CreateExecutor(competition);

        _ = await executor.UpdateCompetitionPresentationAsync(competition.Id, shortName: "PU", logoMediaId: null);

        unitOfWork.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        unitOfWork.Verify(
            uow => uow.ExecuteExclusiveAsync(
                competition.Id,
                It.IsAny<Func<CancellationToken, Task>>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateCompetitionPresentationAsync_when_missing_throws_after_entering_exclusive_write_without_structure_viewAsync()
    {
        var missingId = CompetitionId.New();
        var (executor, unitOfWork, matches, _) = CreateExecutor(competition: null, competitionId: missingId);

        var act = () => executor.UpdateCompetitionPresentationAsync(missingId, "PU", null);

        await AssertMissingCompetitionAsync(act, missingId, unitOfWork, matches);
    }

    [Fact]
    public async Task SetCompetitionScheduleAsync_persists_once_under_competition_lockAsync()
    {
        var competition = CreateCompetition.Execute("Schedule", _clock);
        var (executor, unitOfWork) = CreateExecutor(competition);
        var start = _clock.UtcNow.AddDays(1);

        _ = await executor.SetCompetitionScheduleAsync(competition.Id, start, start.AddDays(7));

        unitOfWork.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        unitOfWork.Verify(
            uow => uow.ExecuteExclusiveAsync(
                competition.Id,
                It.IsAny<Func<CancellationToken, Task>>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SetCompetitionScheduleAsync_when_missing_throws_after_entering_exclusive_write_without_structure_viewAsync()
    {
        var missingId = CompetitionId.New();
        var (executor, unitOfWork, matches, _) = CreateExecutor(competition: null, competitionId: missingId);

        var act = () => executor.SetCompetitionScheduleAsync(missingId, null, null);

        await AssertMissingCompetitionAsync(act, missingId, unitOfWork, matches);
    }

    [Fact]
    public async Task UpdateEntryPresentationAsync_persists_once_under_competition_lockAsync()
    {
        var competition = CreateCompetition.Execute("Entry presentation", _clock);
        var entry = competition.AddEntry(TeamId.New(), "A", _clock);
        var (executor, unitOfWork) = CreateExecutor(competition);

        _ = await executor.UpdateEntryPresentationAsync(
            competition.Id,
            entry.Id,
            shortName: "A",
            logoMediaId: null,
            primaryColor: null,
            secondaryColor: null);

        unitOfWork.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        unitOfWork.Verify(
            uow => uow.ExecuteExclusiveAsync(
                competition.Id,
                It.IsAny<Func<CancellationToken, Task>>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateEntryPresentationAsync_when_missing_throws_after_entering_exclusive_write_without_structure_viewAsync()
    {
        var missingId = CompetitionId.New();
        var (executor, unitOfWork, matches, _) = CreateExecutor(competition: null, competitionId: missingId);

        var act = () => executor.UpdateEntryPresentationAsync(
            missingId,
            EntryId.New(),
            "A",
            null,
            null,
            null);

        await AssertMissingCompetitionAsync(act, missingId, unitOfWork, matches);
    }

    [Fact]
    public async Task RenameEntryAsync_persists_once_under_competition_lockAsync()
    {
        var competition = CreateCompetition.Execute("Rename entry", _clock);
        var entry = competition.AddEntry(TeamId.New(), "A", _clock);
        var (executor, unitOfWork) = CreateExecutor(competition);

        _ = await executor.RenameEntryAsync(competition.Id, entry.Id, "Alpha");

        unitOfWork.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        unitOfWork.Verify(
            uow => uow.ExecuteExclusiveAsync(
                competition.Id,
                It.IsAny<Func<CancellationToken, Task>>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task RenameEntryAsync_when_missing_throws_after_entering_exclusive_write_without_structure_viewAsync()
    {
        var missingId = CompetitionId.New();
        var (executor, unitOfWork, matches, _) = CreateExecutor(competition: null, competitionId: missingId);

        var act = () => executor.RenameEntryAsync(missingId, EntryId.New(), "Alpha");

        await AssertMissingCompetitionAsync(act, missingId, unitOfWork, matches);
    }

    [Fact]
    public async Task RemoveDeclaredMembersAsync_persists_once_under_competition_lockAsync()
    {
        var competition = CreateCompetition.Execute("Remove members", _clock);
        var entry = competition.AddEntry(TeamId.New(), "A", _clock);
        var member = competition.AddDeclaredMember(entry.Id, "Dupont", DeclaredMemberRole.Player, _clock);
        var (executor, unitOfWork) = CreateExecutor(competition);

        _ = await executor.RemoveDeclaredMembersAsync(competition.Id, entry.Id, [member.Id]);

        unitOfWork.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        unitOfWork.Verify(
            uow => uow.ExecuteExclusiveAsync(
                competition.Id,
                It.IsAny<Func<CancellationToken, Task>>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task RemoveDeclaredMembersAsync_when_missing_throws_after_entering_exclusive_write_without_structure_viewAsync()
    {
        var missingId = CompetitionId.New();
        var (executor, unitOfWork, matches, _) = CreateExecutor(competition: null, competitionId: missingId);

        var act = () => executor.RemoveDeclaredMembersAsync(missingId, EntryId.New(), [MemberId.New()]);

        await AssertMissingCompetitionAsync(act, missingId, unitOfWork, matches);
    }

    [Fact]
    public async Task AddDeclaredMemberAsync_persists_once_under_competition_lockAsync()
    {
        var competition = CreateCompetition.Execute("Add member", _clock);
        var entry = competition.AddEntry(TeamId.New(), "A", _clock);
        var (executor, unitOfWork) = CreateExecutor(competition);

        _ = await executor.AddDeclaredMemberAsync(competition.Id, entry.Id, "Dupont", DeclaredMemberRole.Player);

        unitOfWork.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        unitOfWork.Verify(
            uow => uow.ExecuteExclusiveAsync(
                competition.Id,
                It.IsAny<Func<CancellationToken, Task>>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task AddDeclaredMemberAsync_when_missing_throws_after_entering_exclusive_write_without_structure_viewAsync()
    {
        var missingId = CompetitionId.New();
        var (executor, unitOfWork, matches, _) = CreateExecutor(competition: null, competitionId: missingId);

        var act = () => executor.AddDeclaredMemberAsync(
            missingId,
            EntryId.New(),
            "Dupont",
            DeclaredMemberRole.Player);

        await AssertMissingCompetitionAsync(act, missingId, unitOfWork, matches);
    }

    [Fact]
    public async Task RemoveDeclaredMemberAsync_persists_once_under_competition_lockAsync()
    {
        var competition = CreateCompetition.Execute("Remove member", _clock);
        var entry = competition.AddEntry(TeamId.New(), "A", _clock);
        var member = competition.AddDeclaredMember(entry.Id, "Dupont", DeclaredMemberRole.Player, _clock);
        var (executor, unitOfWork) = CreateExecutor(competition);

        _ = await executor.RemoveDeclaredMemberAsync(competition.Id, entry.Id, member.Id);

        unitOfWork.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        unitOfWork.Verify(
            uow => uow.ExecuteExclusiveAsync(
                competition.Id,
                It.IsAny<Func<CancellationToken, Task>>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task RemoveDeclaredMemberAsync_when_missing_throws_after_entering_exclusive_write_without_structure_viewAsync()
    {
        var missingId = CompetitionId.New();
        var (executor, unitOfWork, matches, _) = CreateExecutor(competition: null, competitionId: missingId);

        var act = () => executor.RemoveDeclaredMemberAsync(missingId, EntryId.New(), MemberId.New());

        await AssertMissingCompetitionAsync(act, missingId, unitOfWork, matches);
    }

    [Fact]
    public async Task RenameDeclaredMemberAsync_persists_once_under_competition_lockAsync()
    {
        var competition = CreateCompetition.Execute("Rename member", _clock);
        var entry = competition.AddEntry(TeamId.New(), "A", _clock);
        var member = competition.AddDeclaredMember(entry.Id, "Dupont", DeclaredMemberRole.Player, _clock);
        var (executor, unitOfWork) = CreateExecutor(competition);

        _ = await executor.RenameDeclaredMemberAsync(competition.Id, entry.Id, member.Id, "Martin");

        unitOfWork.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        unitOfWork.Verify(
            uow => uow.ExecuteExclusiveAsync(
                competition.Id,
                It.IsAny<Func<CancellationToken, Task>>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task RenameDeclaredMemberAsync_when_missing_throws_after_entering_exclusive_write_without_structure_viewAsync()
    {
        var missingId = CompetitionId.New();
        var (executor, unitOfWork, matches, _) = CreateExecutor(competition: null, competitionId: missingId);

        var act = () => executor.RenameDeclaredMemberAsync(missingId, EntryId.New(), MemberId.New(), "Martin");

        await AssertMissingCompetitionAsync(act, missingId, unitOfWork, matches);
    }

    [Fact]
    public async Task ReplaceRegulationAsync_persists_once_under_competition_lockAsync()
    {
        var competition = CreateCompetition.Execute("Regulation", _clock);
        var stage = ConfigureStructure.Execute(competition, null, StructureIntent.Championship("L"), _clock).Stage;
        var (executor, unitOfWork, _, _) = CreateExecutor(competition, [stage]);

        _ = await executor.ReplaceRegulationAsync(competition.Id, regulation => regulation);

        unitOfWork.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        unitOfWork.Verify(
            uow => uow.ExecuteExclusiveAsync(
                competition.Id,
                It.IsAny<Func<CancellationToken, Task>>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ReplaceRegulationAsync_when_missing_throws_after_entering_exclusive_write_without_structure_viewAsync()
    {
        var missingId = CompetitionId.New();
        var (executor, unitOfWork, matches, _) = CreateExecutor(competition: null, competitionId: missingId);

        var act = () => executor.ReplaceRegulationAsync(missingId, regulation => regulation);

        await AssertMissingCompetitionAsync(act, missingId, unitOfWork, matches);
    }

    [Fact]
    public async Task BackfillResolutionProvenanceAsync_persists_once_under_competition_lockAsync()
    {
        var competition = CreateCompetition.Execute("Backfill", _clock);
        var (executor, unitOfWork) = CreateExecutor(competition);

        var report = await executor.BackfillResolutionProvenanceAsync(competition.Id);

        report.Should().NotBeNull();
        unitOfWork.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        unitOfWork.Verify(
            uow => uow.ExecuteExclusiveAsync(
                competition.Id,
                It.IsAny<Func<CancellationToken, Task>>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task BackfillResolutionProvenanceAsync_when_missing_throws_after_entering_exclusive_writeAsync()
    {
        var missingId = CompetitionId.New();
        var (executor, unitOfWork, matches, stages) = CreateExecutor(competition: null, competitionId: missingId);

        var act = () => executor.BackfillResolutionProvenanceAsync(missingId);

        await act.Should().ThrowAsync<ApplicationFailureException>()
            .Where(ex => ex.Code == ApplicationErrorCodes.CompetitionNotFound);
        unitOfWork.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        unitOfWork.Verify(
            uow => uow.ExecuteExclusiveAsync(
                missingId,
                It.IsAny<Func<CancellationToken, Task>>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
        matches.Verify(
            repository => repository.ListByStageIdsForUpdateAsync(
                It.IsAny<IReadOnlyList<StageId>>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
        stages.Verify(
            repository => repository.GetByIdsForUpdateAsync(
                It.IsAny<IReadOnlyList<StageId>>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private static void SetupStructureReads(
        Mock<IMatchRepository> matches,
        Mock<IStageRepository> stages,
        CompetitionId competitionId,
        IReadOnlyList<Stage> loadedStages)
    {
        matches
            .Setup(repository => repository.ListSheetMemberRefsByCompetitionReadOnlyAsync(
                competitionId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        matches
            .Setup(repository => repository.ListSummaryRowsByStageIdsReadOnlyAsync(
                It.IsAny<IReadOnlyList<StageId>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<StageId, IReadOnlyList<MatchSummaryRow>>());
        stages
            .Setup(repository => repository.GetByIdsReadOnlyAsync(
                It.IsAny<IReadOnlyList<StageId>>(),
                It.IsAny<StageReadCapabilities>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(loadedStages);
    }

    private static async Task AssertMissingCompetitionAsync(
        Func<Task> act,
        CompetitionId missingId,
        Mock<IUnitOfWork> unitOfWork,
        Mock<IMatchRepository> matches)
    {
        await act.Should().ThrowAsync<ApplicationFailureException>()
            .Where(ex => ex.Code == ApplicationErrorCodes.CompetitionNotFound);
        unitOfWork.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        unitOfWork.Verify(
            uow => uow.ExecuteExclusiveAsync(
                missingId,
                It.IsAny<Func<CancellationToken, Task>>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
        matches.Verify(
            repository => repository.ListSheetMemberRefsByCompetitionReadOnlyAsync(
                missingId,
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private (
        UseCaseExecutor Executor,
        Mock<IUnitOfWork> UnitOfWork,
        Mock<IMatchRepository> Matches,
        Mock<IStageRepository> Stages) CreateExecutor(
        Competition? competition,
        IReadOnlyList<Stage>? extraStages = null,
        CompetitionId? competitionId = null)
    {
        var resolvedId = competition?.Id ?? competitionId ?? CompetitionId.New();
        var stages = new Mock<IStageRepository>(MockBehavior.Strict);
        var matches = new Mock<IMatchRepository>(MockBehavior.Strict);
        var competitions = new Mock<ICompetitionRepository>(MockBehavior.Strict);
        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);
        unitOfWork
            .Setup(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        unitOfWork
            .Setup(uow => uow.ExecuteExclusiveAsync(
                resolvedId,
                It.IsAny<Func<CancellationToken, Task>>(),
                It.IsAny<CancellationToken>()))
            .Returns((CompetitionId _, Func<CancellationToken, Task> write, CancellationToken token) => write(token));

        if (competition is not null)
        {
            competitions
                .Setup(repository => repository.GetByIdForUpdateAsync(competition.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(competition);
            var loadedStages = extraStages?.ToList() ?? [];
            SetupStructureReads(matches, stages, competition.Id, loadedStages);
            stages
                .Setup(repository => repository.GetByIdsForUpdateAsync(
                    It.IsAny<IReadOnlyList<StageId>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(loadedStages);
            IReadOnlyDictionary<StageId, IReadOnlyList<Domain.Matches.Match>> emptyMatches =
                new Dictionary<StageId, IReadOnlyList<Domain.Matches.Match>>();
            matches
                .Setup(repository => repository.ListByStageIdsForUpdateAsync(
                    It.IsAny<IReadOnlyList<StageId>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(emptyMatches);
        }
        else
        {
            competitions
                .Setup(repository => repository.GetByIdForUpdateAsync(resolvedId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Competition?)null);
        }

        var executor = new UseCaseExecutor(
            stages.Object,
            matches.Object,
            competitions.Object,
            unitOfWork.Object,
            _clock,
            AlwaysExistingMedia.Instance,
            NullLogger<UseCaseExecutor>.Instance);
        return (executor, unitOfWork, matches, stages);
    }

    private (UseCaseExecutor Executor, Mock<IUnitOfWork> UnitOfWork) CreateExecutor(Competition competition)
    {
        var created = CreateExecutor(competition, extraStages: null);
        return (created.Executor, created.UnitOfWork);
    }
}
