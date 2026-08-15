// -----------------------------------------------------------------------
// <copyright file="Program.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MyClub.PlayUp.Application.Abstractions;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;
using MyClub.PlayUp.Infrastructure.DependencyInjection;

// Optional override: dotnet run --project …DevSeed -- "Host=…;…"
// Default: same sources as Host — User Secrets (MyClub.PlayUp.Host) then env (ConnectionStrings__PlayUp).
var configuration = new ConfigurationBuilder()
    .AddUserSecrets(typeof(Program).Assembly)
    .AddEnvironmentVariables()
    .Build();

var connectionString = args.ElementAtOrDefault(0)
    ?? configuration.GetConnectionString("PlayUp")
    ?? throw new InvalidOperationException(
        "Connection string 'PlayUp' is not configured. "
        + "Set Host User Secrets (ConnectionStrings:PlayUp), env ConnectionStrings__PlayUp, "
        + "or pass the connection string as the first argument.");

var services = new ServiceCollection();
services.AddPlayUpInfrastructure(connectionString);
await using var provider = services.BuildServiceProvider();
using var scope = provider.CreateScope();

var competitions = scope.ServiceProvider.GetRequiredService<ICompetitionRepository>();
var stages = scope.ServiceProvider.GetRequiredService<IStageRepository>();
var matches = scope.ServiceProvider.GetRequiredService<IMatchRepository>();
var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
var clock = scope.ServiceProvider.GetRequiredService<IClock>();

var regulation = new Regulation(
    new EntryRules(minimumTeams: 2, maximumTeams: 64),
    new MatchRules(
        new MatchDuration(durationPerPeriod: 45, numberOfPeriods: 2, halfTimeDuration: 15),
        new AdministrativeResultPolicy(forfeitWinnerGoals: 3, forfeitLoserGoals: 0)),
    new StandingRules(
        new PointsPolicy(winPoints: 3, drawPoints: 1, lossPoints: 0),
        [
            RankingCriterion.Points,
            RankingCriterion.GoalDifference,
            RankingCriterion.GoalsFor,
            RankingCriterion.HeadToHead
        ]));

var competition = Competition.Create(new CompetitionName("Dev Seed Cup"), regulation, clock);
var home = competition.AddEntry(TeamId.New(), "Alpha", clock);
var away = competition.AddEntry(TeamId.New(), "Beta", clock);

var stage = Stage.Create(competition.Id, new StageName("QF"), regulation, clock);
stage.AddRound("R1", new TieFormat(TieFormat.SingleLeg, aggregateScoring: false), clock);
stage.AddFixture(stage.Rounds[0].Id, clock);
stage.AddSlot("SF1-A", clock);
var draw = stage.CreateDraw(DrawResolutionKind.Pairing, clock);
stage.ConfigureDrawInputs(draw.Id, DrawInputs.ForPairing([home.Id, away.Id]), clock);
stage.RecordDrawResolution(
    draw.Id,
    DrawResolution.ResolvedPairings([new PairingDrawResult(home.Id, away.Id)]),
    clock);

competition.AddStage(stage.Id, clock);

// One attached match so SPA navigation can reach Match detail (11.3.2).
var fixtureId = stage.Rounds[0].Fixtures[0].Id;
var match = Match.Create(competition.Id, stage.Id, home.Id, away.Id, clock);
stage.AttachMatch(fixtureId, match.Id, legIndex: 1, clock);

competitions.Add(competition);
stages.Add(stage);
matches.Add(match);
await unitOfWork.SaveChangesAsync().ConfigureAwait(false);

Console.WriteLine($"competitionId={competition.Id.Value}");
Console.WriteLine($"stageId={stage.Id.Value}");
Console.WriteLine($"matchId={match.Id.Value}");
