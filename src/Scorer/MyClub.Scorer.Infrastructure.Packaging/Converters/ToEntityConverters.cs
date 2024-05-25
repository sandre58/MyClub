// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using MyClub.Domain.Enums;
using MyClub.Scorer.Domain.CompetitionAggregate;
using MyClub.Scorer.Domain.Enums;
using MyClub.Scorer.Domain.MatchAggregate;
using MyClub.Scorer.Domain.PersonAggregate;
using MyClub.Scorer.Domain.ProjectAggregate;
using MyClub.Scorer.Domain.RankingAggregate;
using MyClub.Scorer.Domain.StadiumAggregate;
using MyClub.Scorer.Domain.TeamAggregate;
using MyClub.Scorer.Infrastructure.Packaging.Models;
using MyNet.Utilities;
using MyNet.Utilities.Geography;
using MyNet.Utilities.Sequences;

namespace MyClub.Scorer.Infrastructure.Packaging.Converters
{
    public static class ToEntityConverters
    {
        public static IProject CreateProject(this ProjectPackage source)
        {
            var type = (CompetitionType)source.Metadata!.Type;
            IProject project = type switch
            {
                CompetitionType.League => new LeagueProject(source.Metadata!.Name, source.Metadata!.StartDate, source.Metadata!.EndDate, source.Metadata!.Image, source.Metadata!.Id),
                CompetitionType.Cup => new CupProject(source.Metadata!.Name, source.Metadata!.StartDate, source.Metadata!.EndDate, source.Metadata!.Image, source.Metadata!.Id),
                CompetitionType.Tournament => new TournamentProject(source.Metadata!.Name, source.Metadata!.StartDate, source.Metadata!.EndDate, source.Metadata!.Image, source.Metadata!.Id),
                _ => throw new InvalidOperationException("Project type unknown"),
            };

            // Parameters
            project.Parameters.UseTeamVenues = source.Metadata!.Parameters!.UseTeamVenues;
            project.Parameters.MatchStartTime = source.Metadata!.Parameters!.MatchStartTime;
            project.Parameters.RotationTime = source.Metadata!.Parameters!.RotationTime;
            project.Parameters.MinimumRestTime = source.Metadata!.Parameters!.MinimumRestTime;

            var stadiums = source.Stadiums?.Select(x => x.CreateStadium()).ToArray() ?? [];
            var teams = source.Teams?.Select(x => x.CreateTeam(x.StadiumId.HasValue ? stadiums.GetByIdOrDefault(x.StadiumId.Value) : null)).ToArray() ?? [];

            stadiums.ForEach(x => project.AddStadium(x));
            teams.ForEach(x => project.AddTeam(x));
            project.MarkedAsCreated(source.Metadata!.CreatedAt, source.Metadata!.CreatedBy);
            project.MarkedAsModified(source.Metadata!.ModifiedAt, source.Metadata!.ModifiedBy);

            // Competition
            switch (project.Competition)
            {
                case League league:
                    var leaguePackage = (LeaguePackage)source.Competition!;
                    league.Labels.AddRange(leaguePackage.Labels?.ToDictionary(x => new AcceptableValueRange<int>(x.StartRank, x.EndRank), x => new RankLabel(x.Color, x.Name.OrEmpty(), x.ShortName.OrEmpty(), x.Description)));
                    league.RankingRules = leaguePackage.RankingRules?.CreateRankingRules() ?? RankingRules.Default;
                    league.MatchFormat = leaguePackage.MatchFormat?.CreateMatchFormat() ?? MatchFormat.Default;
                    leaguePackage.Matchdays?.ForEach(x => league.AddMatchday(x.CreateMatchday(league, teams, stadiums, teams.SelectMany(y => y.Players).ToList())));

                    league.MarkedAsCreated(leaguePackage.CreatedAt, leaguePackage.CreatedBy);
                    league.MarkedAsModified(leaguePackage.ModifiedAt, leaguePackage.ModifiedBy);
                    break;

                case Cup cup:
                    var cupPackage = (CupPackage)source.Competition!;
                    cup.MarkedAsCreated(cupPackage.CreatedAt, cupPackage.CreatedBy);
                    cup.MarkedAsModified(cupPackage.ModifiedAt, cupPackage.ModifiedBy);
                    break;

                case Tournament tournament:
                    var tournamentPackage = (TournamentPackage)source.Competition!;
                    tournament.MarkedAsCreated(tournamentPackage.CreatedAt, tournamentPackage.CreatedBy);
                    tournament.MarkedAsModified(tournamentPackage.ModifiedAt, tournamentPackage.ModifiedBy);
                    break;
            }

            return project;
        }

        public static Address CreateAddress(this AddressPackage source) => new(source.Street, source.PostalCode, source.City, source.Country.HasValue ? Country.FromValue(source.Country.Value) : null, source.Latitude, source.Longitude);

        public static Stadium CreateStadium(this StadiumPackage source) => new(source.Name, (Ground)source.Ground, id: source.Id)
        {
            Address = source.Address?.CreateAddress()
        };

        public static Team CreateTeam(this TeamPackage source, Stadium? stadium)
        {
            var result = new Team(source.Name, source.ShortName, id: source.Id)
            {
                AwayColor = source.AwayColor,
                HomeColor = source.HomeColor,
                Country = source.Country.HasValue ? Country.FromValue(source.Country.Value) : null,
                Logo = source.Logo,
                Stadium = stadium,
            };
            source.Players?.ForEach(x => result.AddPlayer(x.CreatePlayer(result)));
            source.Staff?.ForEach(x => result.AddManager(x.CreateManager(result)));

            result.MarkedAsCreated(source.CreatedAt, source.CreatedBy);
            result.MarkedAsModified(source.ModifiedAt, source.ModifiedBy);

            return result;
        }

        public static Player CreatePlayer(this PlayerPackage source, Team team)
        {
            var result = new Player(team, firstName: source.FirstName, lastName: source.LastName, id: source.Id)
            {
                Country = source.Country.HasValue ? Country.FromValue(source.Country.Value) : null,
                Photo = source.Photo,
                Gender = (GenderType)source.Gender,
                LicenseNumber = source.LicenseNumber,
                Email = source.Email,
            };

            result.MarkedAsCreated(source.CreatedAt, source.CreatedBy);
            result.MarkedAsModified(source.ModifiedAt, source.ModifiedBy);

            return result;
        }

        public static Manager CreateManager(this ManagerPackage source, Team team)
        {
            var result = new Manager(team, firstName: source.FirstName, lastName: source.LastName, id: source.Id)
            {
                Country = source.Country.HasValue ? Country.FromValue(source.Country.Value) : null,
                Photo = source.Photo,
                Gender = (GenderType)source.Gender,
                LicenseNumber = source.LicenseNumber,
                Email = source.Email,
            };

            result.MarkedAsCreated(source.CreatedAt, source.CreatedBy);
            result.MarkedAsModified(source.ModifiedAt, source.ModifiedBy);

            return result;
        }

        private static HalfFormat CreateHalfFormat(this HalfFormatPackage source) => new(source.Number, source.Duration, source.HalfTimeDuration);

        private static MatchFormat CreateMatchFormat(this MatchFormatPackage source) => new(source.RegulationTime?.CreateHalfFormat() ?? HalfFormat.Default, source.ExtraTime?.CreateHalfFormat(), source.NumberOfPenaltyShootouts);

        private static RankingRules CreateRankingRules(this RankingRulesPackage source)
            => new(source.Points?.ToDictionary(x => (MatchResultDetailled)x.Result, x => x.Points) ?? [],
                   new RankingComparer(source.Comparers?.Split(';').Select(x => RankingComparer.AllAvailableComparers.GetOrDefault(x)).NotNull() ?? RankingComparer.Default),
                   source.Computers?.Split(';').ToDictionary(x => x, x => RankingRules.CreateComputer(Enum.Parse<DefaultRankingColumn>(x))) ?? RankingRules.DefaultComputers);

        private static Match CreateMatch(this MatchPackage source, MatchFormat matchFormat, IEnumerable<Team> teams, IEnumerable<Stadium> stadiums, IEnumerable<Player> players)
        {
            var matchFormatInMatch = source.Format?.CreateMatchFormat() ?? MatchFormat.Default;
            var result = new Match(source.OriginDate, teams.GetById(source.Home!.TeamId), teams.GetById(source.Away!.TeamId), matchFormatInMatch == matchFormat ? matchFormat : matchFormatInMatch, source.Id)
            {
                AfterExtraTime = source.AfterExtraTime,
                NeutralVenue = source.NeutralVenue,
                Stadium = source.StadiumId.HasValue ? stadiums.GetById(source.StadiumId.Value) : null,
            };

            source.Home.Goals?.ForEach(x => result.Home.AddGoal(new Goal((GoalType)x.Type, x.ScorerId.HasValue ? players.GetById(x.ScorerId.Value) : null, x.AssistId.HasValue ? players.GetById(x.AssistId.Value) : null, x.Minute, x.Id)));
            source.Home.Cards?.ForEach(x => result.Home.AddCard(new Card((CardColor)x.Color, x.PlayerId.HasValue ? players.GetById(x.PlayerId.Value) : null, (CardInfraction)x.Infraction, x.Minute, x.Id)));
            source.Home.Shootout?.ForEach(x => result.Home.AddPenaltyShootout(new PenaltyShootout(x.TakerId.HasValue ? players.GetById(x.TakerId.Value) : null, (PenaltyShootoutResult)x.Result, x.Id)));

            if (source.Home.IsWithdrawn)
                result.Home.DoWithdraw();

            source.Away.Goals?.ForEach(x => result.Away.AddGoal(new Goal((GoalType)x.Type, x.ScorerId.HasValue ? players.GetById(x.ScorerId.Value) : null, x.AssistId.HasValue ? players.GetById(x.AssistId.Value) : null, x.Minute, x.Id)));
            source.Away.Cards?.ForEach(x => result.Away.AddCard(new Card((CardColor)x.Color, x.PlayerId.HasValue ? players.GetById(x.PlayerId.Value) : null, (CardInfraction)x.Infraction, x.Minute, x.Id)));
            source.Away.Shootout?.ForEach(x => result.Away.AddPenaltyShootout(new PenaltyShootout(x.TakerId.HasValue ? players.GetById(x.TakerId.Value) : null, (PenaltyShootoutResult)x.Result, x.Id)));

            if (source.Away.IsWithdrawn)
                result.Away.DoWithdraw();

            switch ((MatchState)source.State)
            {
                case MatchState.None:
                    result.Reset();
                    break;
                case MatchState.InProgress:
                    result.Start();
                    break;
                case MatchState.Suspended:
                    result.Suspend();
                    break;
                case MatchState.Cancelled:
                    result.Cancel();
                    break;
                case MatchState.Played:
                    result.Played();
                    break;
                case MatchState.Postponed:
                    result.Postpone(source.PostponedDate);
                    break;
            }

            result.MarkedAsCreated(source.CreatedAt, source.CreatedBy);
            result.MarkedAsModified(source.ModifiedAt, source.ModifiedBy);

            return result;
        }

        private static Matchday CreateMatchday(this MatchdayPackage source, IMatchdaysProvider parent, IList<Team> teams, IList<Stadium> stadiums, IList<Player> players)
        {
            var result = new Matchday(parent, source.OriginDate, source.Name.OrEmpty(), source.ShortName, source.Id);

            if (source.IsPostponed)
                result.Postpone(source.PostponedDate);

            source.Matches?.ForEach(x => result.AddMatch(x.CreateMatch(parent.ProvideFormat(), teams, stadiums, players)));

            result.MarkedAsCreated(source.CreatedAt, source.CreatedBy);
            result.MarkedAsModified(source.ModifiedAt, source.ModifiedBy);

            return result;
        }

    }
}
