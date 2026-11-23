// -----------------------------------------------------------------------
// <copyright file="UseCaseExecutorMatchLockTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using MyClub.PlayUp.Application.Abstractions;
using MyClub.PlayUp.Application.Competitions;
using MyClub.PlayUp.Application.Matches;
using MyClub.PlayUp.Application.Pipeline;
using MyClub.PlayUp.Application.Tests.Common;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;
using Xunit;
using DomainMatch = MyClub.PlayUp.Domain.Matches.Match;
using DomainMatchResult = MyClub.PlayUp.Domain.Matches.MatchResult;
using DomainScore = MyClub.PlayUp.Domain.Matches.Score;

namespace MyClub.PlayUp.Application.Tests.Pipeline;

/// <summary>
/// Competition lock around the seventeen match write commands.
/// </summary>
public sealed class UseCaseExecutorMatchLockTests
{
    public enum MatchWriteOp
    {
        StartMatch,
        FinishMatch,
        CorrectMatchResult,
        AddDeclaredParticipation,
        RemoveDeclaredParticipation,
        ChangeDeclaredParticipationCompositionStatus,
        SetDeclaredParticipationJerseyNumber,
        SetRunningScore,
        RecordGoal,
        CorrectRecordedGoal,
        RemoveRecordedGoal,
        RecordSubstitution,
        CorrectRecordedSubstitution,
        RemoveRecordedSubstitution,
        RecordDisciplinaryEvent,
        CorrectRecordedDisciplinaryEvent,
        RemoveRecordedDisciplinaryEvent,
    }

    public static IEnumerable<object[]> AllMatchWriteOps() =>
        Enum.GetValues<MatchWriteOp>().Select(op => new object[] { op });

    private static UseCaseExecutor CreateExecutor(
        Mock<IStageRepository> stages,
        Mock<IMatchRepository> matches,
        Mock<ICompetitionRepository> competitions,
        Mock<IUnitOfWork> unitOfWork) =>
        new(
            stages.Object,
            matches.Object,
            competitions.Object,
            unitOfWork.Object,
            new FakeClock(new DateTimeOffset(2026, 11, 23, 12, 0, 0, TimeSpan.Zero)),
            AlwaysExistingMedia.Instance,
            NullLogger<UseCaseExecutor>.Instance);

    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 11, 23, 12, 0, 0, TimeSpan.Zero));

    [Fact]
    public async Task Match_write_when_missing_does_not_acquire_exclusive_lockAsync()
    {
        var matchId = MatchId.New();
        var matchRepo = new Mock<IMatchRepository>(MockBehavior.Strict);
        var stages = new Mock<IStageRepository>(MockBehavior.Strict);
        var competitions = new Mock<ICompetitionRepository>(MockBehavior.Strict);
        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);

        matchRepo
            .Setup(repository => repository.GetCompetitionIdReadOnlyAsync(matchId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((CompetitionId?)null);

        var executor = CreateExecutor(stages, matchRepo, competitions, unitOfWork);

        var act = async () => await executor.StartMatchAsync(matchId);

        (await act.Should().ThrowAsync<ApplicationFailureException>()).Which.Code
            .Should().Be(ApplicationErrorCodes.MatchNotFound);
        unitOfWork.Verify(
            uow => uow.ExecuteExclusiveAsync(
                It.IsAny<CompetitionId>(),
                It.IsAny<Func<CancellationToken, Task>>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Theory]
    [MemberData(nameof(AllMatchWriteOps))]
    public async Task Match_writes_acquire_competition_lock_before_match_loadAsync(MatchWriteOp writeOp)
    {
        var harness = new Harness(_clock, writeOp);
        harness.ArmExclusive();

        await harness.InvokeWriteAsync();

        harness.EnteredLock.Should().BeTrue();
        harness.UnitOfWork.Verify(
            uow => uow.ExecuteExclusiveAsync(
                harness.Competition.Id,
                It.IsAny<Func<CancellationToken, Task>>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
        harness.UnitOfWork.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    private sealed class Harness
    {
        private readonly bool[] _inside = [false];
        private readonly MatchWriteOp _writeOp;
        private readonly MemberId _dupont;
        private readonly MemberId _martin;
        private readonly MemberId _bernard;
        private readonly MemberId _awayPlayer;
        private GoalId? _goalId;
        private SubstitutionId? _substitutionId;
        private DisciplinaryEventId? _disciplinaryEventId;

        public Harness(FakeClock clock, MatchWriteOp writeOp)
        {
            _writeOp = writeOp;
            var regulation = new Regulation(
                SampleRegulations.Standard().EntryRules,
                SampleRegulations.Standard().MatchRules,
                SampleRegulations.Standard().StandingRules,
                new DisciplinaryRules([DisciplinaryType.Yellow, DisciplinaryType.Red]));
            Competition = CreateCompetition.Execute("Match-Lock", regulation, clock);
            var home = AddEntry.Execute(Competition, "Home", clock);
            var away = AddEntry.Execute(Competition, "Away", clock);
            _dupont = AddDeclaredMember.Execute(Competition, home.Id, "Dupont", DeclaredMemberRole.Player, clock).Id;
            _martin = AddDeclaredMember.Execute(Competition, home.Id, "Martin", DeclaredMemberRole.Player, clock).Id;
            _bernard = AddDeclaredMember.Execute(Competition, home.Id, "Bernard", DeclaredMemberRole.Player, clock).Id;
            _awayPlayer = AddDeclaredMember.Execute(Competition, away.Id, "Away", DeclaredMemberRole.Player, clock).Id;

            Stage = Stage.Create(Competition.Id, new StageName("MD1"), Competition.Regulation, clock);
            Competition.AddStage(Stage.Id, clock);
            Stage.AddMatchday(1, clock);
            Stage.Prepare(clock);
            Stage.Start(clock);
            Match = DomainMatch.Create(Competition.Id, Stage.Id, home.Id, away.Id, clock);
            PrepareMatchState(clock);

            Stages = new Mock<IStageRepository>(MockBehavior.Strict);
            Matches = new Mock<IMatchRepository>(MockBehavior.Strict);
            Competitions = new Mock<ICompetitionRepository>(MockBehavior.Strict);
            UnitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);

            Matches
                .Setup(repository => repository.GetCompetitionIdReadOnlyAsync(Match.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Competition.Id);
            Matches
                .Setup(repository => repository.GetByIdForUpdateAsync(Match.Id, It.IsAny<CancellationToken>()))
                .Returns(() =>
                {
                    _inside[0].Should().BeTrue();
                    return Task.FromResult<DomainMatch?>(Match);
                });
            Stages
                .Setup(repository => repository.GetByIdForUpdateAsync(Stage.Id, It.IsAny<CancellationToken>()))
                .Returns(() =>
                {
                    _inside[0].Should().BeTrue();
                    return Task.FromResult<Stage?>(Stage);
                });
            Competitions
                .Setup(repository => repository.GetByIdForUpdateAsync(Competition.Id, It.IsAny<CancellationToken>()))
                .Returns(() =>
                {
                    _inside[0].Should().BeTrue();
                    return Task.FromResult<Competition?>(Competition);
                });
            UnitOfWork
                .Setup(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            Executor = CreateExecutor(Stages, Matches, Competitions, UnitOfWork);
        }

        public Competition Competition { get; }

        public Stage Stage { get; }

        public DomainMatch Match { get; }

        public Mock<IStageRepository> Stages { get; }

        public Mock<IMatchRepository> Matches { get; }

        public Mock<ICompetitionRepository> Competitions { get; }

        public Mock<IUnitOfWork> UnitOfWork { get; }

        public UseCaseExecutor Executor { get; }

        public bool EnteredLock { get; private set; }

        public void ArmExclusive() =>
            UnitOfWork
                .Setup(uow => uow.ExecuteExclusiveAsync(
                    Competition.Id,
                    It.IsAny<Func<CancellationToken, Task>>(),
                    It.IsAny<CancellationToken>()))
                .Returns((CompetitionId _, Func<CancellationToken, Task> write, CancellationToken token) =>
                {
                    EnteredLock = true;
                    _inside[0] = true;
                    return write(token);
                });

        public Task InvokeWriteAsync()
        {
            return _writeOp switch
            {
                MatchWriteOp.StartMatch => Executor.StartMatchAsync(Match.Id),
                MatchWriteOp.FinishMatch => Executor.FinishMatchAsync(
                    Match.Id,
                    new DomainMatchResult(ResultType.Played, new DomainScore(1, 0))),
                MatchWriteOp.CorrectMatchResult => Executor.CorrectMatchResultAsync(
                    Match.Id,
                    new DomainMatchResult(ResultType.Played, new DomainScore(2, 1))),
                MatchWriteOp.AddDeclaredParticipation => Executor.AddDeclaredParticipationAsync(
                    Match.Id,
                    _awayPlayer,
                    Side.Away,
                    CompositionStatus.Starter),
                MatchWriteOp.RemoveDeclaredParticipation => Executor.RemoveDeclaredParticipationAsync(
                    Match.Id,
                    _awayPlayer),
                MatchWriteOp.ChangeDeclaredParticipationCompositionStatus => Executor
                    .ChangeDeclaredParticipationCompositionStatusAsync(
                        Match.Id,
                        _dupont,
                        CompositionStatus.Bench),
                MatchWriteOp.SetDeclaredParticipationJerseyNumber => Executor.SetDeclaredParticipationJerseyNumberAsync(
                    Match.Id,
                    _dupont,
                    11),
                MatchWriteOp.SetRunningScore => Executor.SetRunningScoreAsync(Match.Id, 2, 1),
                MatchWriteOp.RecordGoal => Executor.RecordGoalAsync(Match.Id, _dupont, Side.Home),
                MatchWriteOp.CorrectRecordedGoal => Executor.CorrectRecordedGoalAsync(
                    Match.Id,
                    _goalId!.Value,
                    _martin,
                    Side.Home),
                MatchWriteOp.RemoveRecordedGoal => Executor.RemoveRecordedGoalAsync(Match.Id, _goalId!.Value),
                MatchWriteOp.RecordSubstitution => Executor.RecordSubstitutionAsync(
                    Match.Id,
                    _dupont,
                    _martin,
                    Side.Home),
                MatchWriteOp.CorrectRecordedSubstitution => Executor.CorrectRecordedSubstitutionAsync(
                    Match.Id,
                    _substitutionId!.Value,
                    _dupont,
                    _bernard,
                    Side.Home),
                MatchWriteOp.RemoveRecordedSubstitution => Executor.RemoveRecordedSubstitutionAsync(
                    Match.Id,
                    _substitutionId!.Value),
                MatchWriteOp.RecordDisciplinaryEvent => Executor.RecordDisciplinaryEventAsync(
                    Match.Id,
                    _dupont,
                    DisciplinaryType.Yellow),
                MatchWriteOp.CorrectRecordedDisciplinaryEvent => Executor.CorrectRecordedDisciplinaryEventAsync(
                    Match.Id,
                    _disciplinaryEventId!.Value,
                    _dupont,
                    DisciplinaryType.Red),
                MatchWriteOp.RemoveRecordedDisciplinaryEvent => Executor.RemoveRecordedDisciplinaryEventAsync(
                    Match.Id,
                    _disciplinaryEventId!.Value),
                _ => throw new ArgumentOutOfRangeException(nameof(_writeOp)),
            };
        }

        private void PrepareMatchState(FakeClock clock)
        {
            switch (_writeOp)
            {
                case MatchWriteOp.StartMatch:
                    return;
                case MatchWriteOp.AddDeclaredParticipation:
                    Match.AddDeclaredParticipation(_dupont, Side.Home, CompositionStatus.Starter, clock);
                    return;
                case MatchWriteOp.RemoveDeclaredParticipation:
                case MatchWriteOp.ChangeDeclaredParticipationCompositionStatus:
                case MatchWriteOp.SetDeclaredParticipationJerseyNumber:
                    Match.AddDeclaredParticipation(_dupont, Side.Home, CompositionStatus.Starter, clock);
                    Match.AddDeclaredParticipation(_awayPlayer, Side.Away, CompositionStatus.Starter, clock);
                    return;
                case MatchWriteOp.FinishMatch:
                    Match.AddDeclaredParticipation(_dupont, Side.Home, CompositionStatus.Starter, clock);
                    Match.Start(clock);
                    return;
                case MatchWriteOp.CorrectMatchResult:
                    Match.AddDeclaredParticipation(_dupont, Side.Home, CompositionStatus.Starter, clock);
                    Match.Start(clock);
                    Match.Finish(new DomainMatchResult(ResultType.Played, new DomainScore(1, 0)), clock);
                    return;
                default:
                    Match.AddDeclaredParticipation(_dupont, Side.Home, CompositionStatus.Starter, clock);
                    Match.AddDeclaredParticipation(_martin, Side.Home, CompositionStatus.Bench, clock);
                    Match.AddDeclaredParticipation(_bernard, Side.Home, CompositionStatus.Bench, clock);
                    Match.Start(clock);

                    if (_writeOp is MatchWriteOp.CorrectRecordedGoal
                        or MatchWriteOp.RemoveRecordedGoal)
                    {
                        _goalId = RecordGoal.Execute(Match, _dupont, Side.Home, clock).Id;
                        return;
                    }

                    if (_writeOp is MatchWriteOp.CorrectRecordedSubstitution
                        or MatchWriteOp.RemoveRecordedSubstitution)
                    {
                        _substitutionId = RecordSubstitution.Execute(Match, _dupont, _martin, Side.Home, clock).Id;
                        return;
                    }

                    if (_writeOp is MatchWriteOp.CorrectRecordedDisciplinaryEvent
                        or MatchWriteOp.RemoveRecordedDisciplinaryEvent)
                    {
                        _disciplinaryEventId = RecordDisciplinaryEvent.Execute(
                            Match,
                            Competition,
                            _dupont,
                            DisciplinaryType.Yellow,
                            clock).Id;
                    }

                    break;
            }
        }
    }
}
