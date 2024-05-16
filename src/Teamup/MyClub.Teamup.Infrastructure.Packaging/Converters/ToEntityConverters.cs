// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using MyNet.Utilities;
using MyClub.Teamup.Domain.ClubAggregate;
using MyClub.Teamup.Domain.CompetitionAggregate;
using MyClub.Teamup.Domain.CycleAggregate;
using MyClub.Teamup.Domain.Enums;
using MyClub.Teamup.Domain.HolidaysAggregate;
using MyClub.Teamup.Domain.MatchAggregate;
using MyClub.Teamup.Domain.PersonAggregate;
using MyClub.Teamup.Domain.ProjectAggregate;
using MyClub.Teamup.Domain.SeasonAggregate;
using MyClub.Teamup.Domain.SendedMailAggregate;
using MyClub.Teamup.Domain.SquadAggregate;
using MyClub.Teamup.Domain.StadiumAggregate;
using MyClub.Teamup.Domain.TacticAggregate;
using MyClub.Teamup.Domain.TeamAggregate;
using MyClub.Teamup.Domain.TrainingAggregate;
using MyClub.Teamup.Infrastructure.Packaging.Models;
using MyClub.Domain.Enums;
using MyNet.Utilities.Geography;
using MyNet.Utilities.Sequences;
using MyClub.Domain;

namespace MyClub.Teamup.Infrastructure.Packaging.Converters
{
    public static class ToEntityConverters
    {
        public static Project CreateProject(this ProjectPackage source)
        {
            var seasons = source.Seasons?.Select(x => x.CreateSeason()).ToArray() ?? [];
            var stadiums = source.Stadiums?.Select(x => x.CreateStadium()).ToArray() ?? [];
            var clubs = source.Clubs?.Select(x => x.CreateClub(stadiums)).ToArray() ?? [];
            var teams = clubs.SelectMany(x => x.Teams).ToArray();
            var players = source.Players?.Select(x => x.CreatePlayer()).ToArray() ?? [];
            var squadPlayers = source.Squads?.FirstOrDefault()?.Players?.Select(x => x.CreateSquadPlayer(players.GetById(x.PlayerId), x.TeamId.HasValue ? teams.GetByIdOrDefault(x.TeamId.Value) : null)).ToArray() ?? [];
            var friendlies = source.Competitions?.Friendlies?.Select(x => x.CreateFriendlyAndSeasons(seasons, teams, stadiums, players)).ToArray() ?? [];
            var cups = source.Competitions?.Cups?.Select(x => x.CreateCupAndSeasons(seasons, teams, stadiums, players)).ToArray() ?? [];
            var leagues = source.Competitions?.Leagues?.Select(x => x.CreateLeagueAndSeasons(seasons, teams, stadiums, players)).ToArray() ?? [];
            var club = clubs.GetById(source.Metadata!.ClubId);

            var result = new Project(source.Metadata!.Name, club, (Category)source.Metadata!.Category, seasons.GetById(source.Metadata!.SeasonId), source.Metadata!.Color, source.Metadata!.Image, source.Metadata.Id)
            {
                Preferences = new(source.Metadata!.Preferences!.TrainingStartTime, source.Metadata!.Preferences!.TrainingDuration),
            };
            if (source.Metadata!.TeamId.HasValue)
                result.MainTeam = club.Teams.GetById(source.Metadata!.TeamId.Value);

            source.Holidays?.ForEach(x => result.AddHolidays(x.CreateHolidays()));
            source.SendedMails?.ForEach(x => result.AddSendedMail(x.CreateSendedMail()));
            source.Tactics?.ForEach(x => result.AddTactic(x.CreateTactic()));
            source.Cycles?.ForEach(x => result.AddCycle(x.CreateCycle()));
            source.TrainingSessions?.ForEach(x => result.AddTrainingSession(x.CreateTrainingSession(players)));
            squadPlayers?.ForEach(x => result.AddPlayer(x));
            friendlies.Concat(leagues.OfType<ICompetition>()).Concat(cups).SelectMany(x => x.Seasons).ForEach(x => result.AddCompetition(x));

            result.MarkedAsCreated(source.Metadata!.CreatedAt, source.Metadata!.CreatedBy);
            result.MarkedAsModified(source.Metadata!.ModifiedAt, source.Metadata!.ModifiedBy);

            return result;
        }

        public static Season CreateSeason(this SeasonPackage source)
        {
            var result = new Season(source.StartDate, source.EndDate, id: source.Id)
            {
                Label = source.Label,
                Code = source.Code,
                Description = source.Description,
                Order = source.Order ?? 0
            };

            result.MarkedAsCreated(source.CreatedAt, source.CreatedBy);
            result.MarkedAsModified(source.ModifiedAt, source.ModifiedBy);

            return result;
        }


        public static Injury CreateInjury(this InjuryPackage source)
        {
            var result = new Injury(date: source.Date, condition: source.Condition, severity: (InjurySeverity)source.Severity, endDate: source.EndDate, type: (InjuryType)source.Type, category: (InjuryCategory)source.Category, id: source.Id)
            {
                Description = source.Description
            };

            result.MarkedAsCreated(source.CreatedAt, source.CreatedBy);
            result.MarkedAsModified(source.ModifiedAt, source.ModifiedBy);

            return result;
        }

        public static Absence CreateAbsence(this AbsencePackage source)
        {
            var result = new Absence(startDate: source.StartDate, endDate: source.EndDate, label: source.Label, id: source.Id)
            {
                Type = (AbsenceType)source.Type
            };

            result.MarkedAsCreated(source.CreatedAt, source.CreatedBy);
            result.MarkedAsModified(source.ModifiedAt, source.ModifiedBy);

            return result;
        }

        public static RatedPosition CreatePosition(this RatedPositionPackage source)
            => new(position: Position.FromValue(source.Position)!, id: source.Id)
            {
                Rating = (PositionRating)source.Rating,
                IsNatural = source.IsNatural
            };

        public static Player CreatePlayer(this PlayerPackage source)
        {
            var result = new Player(firstName: source.FirstName, lastName: source.LastName, id: source.Id)
            {
                Laterality = (Laterality)source.Laterality,
                Height = source.Height,
                Weight = source.Weight,
                Birthdate = source.Birthdate,
                PlaceOfBirth = source.PlaceOfBirth,
                Country = source.Country.HasValue ? Country.FromValue(source.Country.Value) : null,
                Category = source.Category.HasValue ? Category.FromValue(source.Category.Value) : null,
                Photo = source.Photo,
                Gender = (GenderType)source.Gender,
                LicenseNumber = source.LicenseNumber,
                Description = source.Description,
                Address = source.Address?.CreateAddress()
            };

            source.Injuries?.ForEach(x => result.AddInjury(x.CreateInjury()));
            source.Positions?.ForEach(x => result.AddPosition(x.CreatePosition()));
            source.Phones?.ForEach(x => result.AddPhone(x.CreatePhone()));
            source.Emails?.ForEach(x => result.AddEmail(x.CreateEmail()));
            source.Absences?.ForEach(x => result.AddAbsence(x.CreateAbsence()));

            result.MarkedAsCreated(source.CreatedAt, source.CreatedBy);
            result.MarkedAsModified(source.ModifiedAt, source.ModifiedBy);

            return result;
        }

        public static Address CreateAddress(this AddressPackage source) => new(source.Street, source.PostalCode, source.City, source.Country.HasValue ? Country.FromValue(source.Country.Value) : null, source.Latitude, source.Longitude);

        public static Stadium CreateStadium(this StadiumPackage source) => new(source.Name, (Ground)source.Ground, id: source.Id)
        {
            Address = source.Address?.CreateAddress()
        };

        public static TacticPosition CreateTacticPosition(this TacticPositionPackage source)
        {
            var result = new TacticPosition(position: Position.FromValue(source.Position)!, id: source.Id)
            {
                Number = source.Number,
                OffsetX = source.OffsetX,
                OffsetY = source.OffsetY
            };

            result.Instructions.AddRange(source.Instructions.Split(";"));

            return result;
        }

        public static Tactic CreateTactic(this TacticPackage source)
        {
            var result = new Tactic(label: source.Label, id: source.Id)
            {
                Description = source.Description,
                Code = source.Code,
                Order = source.Order ?? 0
            };

            result.Instructions.AddRange(source.Instructions.Split(";"));
            source.Positions?.ForEach(x => result.AddPosition(x.CreateTacticPosition()));

            result.MarkedAsCreated(source.CreatedAt, source.CreatedBy);
            result.MarkedAsModified(source.ModifiedAt, source.ModifiedBy);

            return result;
        }

        public static SquadPlayer CreateSquadPlayer(this SquadPlayerPackage source, Player player, Team? team = null)
        {
            var result = new SquadPlayer(player, id: source.Id)
            {
                Team = team,
                Category = source.Category.HasValue ? Category.FromValue(source.Category.Value) : null,
                ShoesSize = source.ShoesSize,
                LicenseState = (LicenseState)source.LicenseState,
                IsMutation = source.IsMutation,
                Number = source.Number,
                FromDate = source.FromDate,
                Size = source.Size,
            };

            source.Positions?.ForEach(x => result.AddPosition(x.CreatePosition()));

            result.MarkedAsCreated(source.CreatedAt, source.CreatedBy);
            result.MarkedAsModified(source.ModifiedAt, source.ModifiedBy);

            return result;
        }

        public static Squad CreateSquad(this SquadPackage source, Club club, Season season, IList<Player> players, IList<Team> teams)
        {
            var result = new Squad(club, season, (Category)source.Category, source.Label, id: source.Id)
            {
                Order = source.Order ?? 0,
                Code = source.Code,
                Description = source.Description,
            };
            source.Players?.ForEach(x => result.AddPlayer(x.CreateSquadPlayer(players.GetById(x.PlayerId), x.TeamId.HasValue ? teams.GetByIdOrDefault(x.TeamId.Value) : null)));

            result.MarkedAsCreated(source.CreatedAt, source.CreatedBy);
            result.MarkedAsModified(source.ModifiedAt, source.ModifiedBy);

            return result;
        }

        public static Club CreateClub(this ClubPackage source, IList<Stadium> stadiums)
        {
            var result = new Club(source.Name, id: source.Id)
            {
                Name = source.Name,
                ShortName = source.ShortName,
                AwayColor = source.AwayColor,
                HomeColor = source.HomeColor,
                Stadium = source.StadiumId.HasValue ? stadiums.GetByIdOrDefault(source.StadiumId.Value) : null,
                Country = source.Country.HasValue ? Country.FromValue(source.Country.Value) : null,
                Logo = source.Logo,
            };
            source.Teams?.ForEach(x => result.AddTeam(x.CreateTeam(result, x.StadiumId.HasValue ? stadiums.GetByIdOrDefault(x.StadiumId.Value) : null)));

            result.MarkedAsCreated(source.CreatedAt, source.CreatedBy);
            result.MarkedAsModified(source.ModifiedAt, source.ModifiedBy);

            return result;
        }

        public static Team CreateTeam(this TeamPackage source, Club club, Stadium? stadium)
        {
            var result = new Team(club, (Category)source.Category, source.Name, source.ShortName, id: source.Id)
            {
                Order = source.Order ?? 0,
            };

            source.HomeColor.IfNotNull(result.HomeColor.Override, result.HomeColor.Reset);
            source.AwayColor.IfNotNull(result.AwayColor.Override, result.AwayColor.Reset);
            source.StadiumId.IfNotNull(_ => result.Stadium.Override(stadium), result.Stadium.Reset);

            result.MarkedAsCreated(source.CreatedAt, source.CreatedBy);
            result.MarkedAsModified(source.ModifiedAt, source.ModifiedBy);

            return result;
        }

        public static Holidays CreateHolidays(this HolidaysItemPackage source)
        {
            var result = new Holidays(startDate: source.StartDate, endDate: source.EndDate, label: source.Label, id: source.Id);

            result.MarkedAsCreated(source.CreatedAt, source.CreatedBy);
            result.MarkedAsModified(source.ModifiedAt, source.ModifiedBy);

            return result;
        }

        public static Cycle CreateCycle(this CyclePackage source)
        {
            var result = new Cycle(startDate: source.StartDate, endDate: source.EndDate, label: source.Label, id: source.Id)
            {
                Color = source.Color
            };
            result.TechnicalGoals.AddRange(source.TechnicalGoals.Split(";"));
            result.TacticalGoals.AddRange(source.TacticalGoals.Split(";"));
            result.MentalGoals.AddRange(source.MentalGoals.Split(";"));
            result.PhysicalGoals.AddRange(source.PhysicalGoals.Split(";"));

            result.MarkedAsCreated(source.CreatedAt, source.CreatedBy);
            result.MarkedAsModified(source.ModifiedAt, source.ModifiedBy);

            return result;
        }

        private static HalfFormat CreateHalfFormat(this HalfFormatPackage source) => new(source.Number, source.Duration);

        private static MatchFormat CreateMatchFormat(this MatchFormatPackage source) => new(source.RegulationTime?.CreateHalfFormat() ?? HalfFormat.Default, source.ExtraTime?.CreateHalfFormat(), source.NumberOfPenaltyShootouts);

        private static RankingRules CreateRankingRules(this RankingRulesPackage source)
            => new(source.PointsByGamesWon, source.PointsByGamesDrawn, source.PointsByGamesLost, source.SortingColumns.Split(";").Select(x => (RankingSortingColumn)int.Parse(x)).ToList(), source.Labels?.ToDictionary(x => new AcceptableValueRange<int>(x.StartRank, x.EndRank), x => new RankLabel(x.Color, x.Name.OrEmpty(), x.ShortName.OrEmpty(), x.Description)));

        private static Match CreateMatch(this MatchPackage source, MatchFormat matchFormat, IList<Team> teams, IList<Stadium> stadiums, IList<Player> players)
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
                result.Home.DoWithdraw(source.Home.PenaltyPoints);

            source.Away.Goals?.ForEach(x => result.Away.AddGoal(new Goal((GoalType)x.Type, x.ScorerId.HasValue ? players.GetById(x.ScorerId.Value) : null, x.AssistId.HasValue ? players.GetById(x.AssistId.Value) : null, x.Minute, x.Id)));
            source.Away.Cards?.ForEach(x => result.Away.AddCard(new Card((CardColor)x.Color, x.PlayerId.HasValue ? players.GetById(x.PlayerId.Value) : null, (CardInfraction)x.Infraction, x.Minute, x.Id)));
            source.Away.Shootout?.ForEach(x => result.Away.AddPenaltyShootout(new PenaltyShootout(x.TakerId.HasValue ? players.GetById(x.TakerId.Value) : null, (PenaltyShootoutResult)x.Result, x.Id)));

            if (source.Away.IsWithdrawn)
                result.Away.DoWithdraw(source.Away.PenaltyPoints);

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

        private static Matchday CreateMatchday(this MatchdayPackage source, MatchFormat matchFormat, IList<Team> teams, IList<Stadium> stadiums, IList<Player> players)
        {
            var matchFormatInMatchday = source.MatchFormat?.CreateMatchFormat();
            var result = new Matchday(source.Name.OrEmpty(), source.OriginDate, source.ShortName, matchFormatInMatchday == matchFormat ? matchFormat : matchFormatInMatchday, source.Id);

            if (source.IsPostponed)
                result.Postpone(source.PostponedDate);

            source.Matches?.ForEach(x => result.AddMatch(x.CreateMatch(result.MatchFormat, teams, stadiums, players)));

            result.MarkedAsCreated(source.CreatedAt, source.CreatedBy);
            result.MarkedAsModified(source.ModifiedAt, source.ModifiedBy);

            return result;
        }

        private static Knockout CreateKnockout(this KnockoutPackage source, MatchFormat matchFormat, IList<Team> teams, IList<Stadium> stadiums, IList<Player> players)
        {
            var matchFormatInKnockout = source.MatchFormat?.CreateMatchFormat();
            var cupRulesInKnockout = new CupRules(matchFormatInKnockout == matchFormat || matchFormatInKnockout is null ? matchFormat : matchFormatInKnockout, source.MatchTime);
            var result = new Knockout(source.Name, source.ShortName, source.OriginDate, cupRulesInKnockout, source.Id);

            if (source.IsPostponed)
                result.Postpone(source.PostponedDate);

            var teamsId = source.TeamIds.Split(";").NotNullOrEmpty();

            if (teamsId.Any())
                result.SetTeams(teamsId.Select(x => teams.GetByIdOrDefault(Guid.Parse(x))).NotNull());

            source.Matches?.ForEach(x => result.AddMatch(x.CreateMatch(result.Rules.MatchFormat, teams, stadiums, players)));

            result.MarkedAsCreated(source.CreatedAt, source.CreatedBy);
            result.MarkedAsModified(source.ModifiedAt, source.ModifiedBy);

            return result;
        }

        private static GroupStage CreateGroupStage(this GroupStagePackage source, MatchFormat matchFormat, IList<Team> teams, IList<Stadium> stadiums, IList<Player> players)
        {
            var matchFormatInGroupStage = source.MatchFormat?.CreateMatchFormat();
            var championshipRulesInGroupStage = new ChampionshipRules(matchFormatInGroupStage == matchFormat || matchFormatInGroupStage is null ? matchFormat : matchFormatInGroupStage, source.RankingRules?.CreateRankingRules() ?? RankingRules.Default, source.MatchTime);

            var result = new GroupStage(source.Name, source.ShortName, source.StartDate, source.EndDate, championshipRulesInGroupStage, id: source.Id);

            var teamsId = source.TeamIds.Split(";").NotNullOrEmpty();

            if (teamsId.Any())
                result.SetTeams(teamsId.Select(x => teams.GetByIdOrDefault(Guid.Parse(x))).NotNull());

            source.Groups?.ForEach(x => result.AddGroup(x.CreateGroup(result, result.Teams)));
            source.Matchdays?.ForEach(x => result.AddMatchday(x.CreateMatchday(result.Rules.MatchFormat, result.Teams, stadiums, players)));

            result.MarkedAsCreated(source.CreatedAt, source.CreatedBy);
            result.MarkedAsModified(source.ModifiedAt, source.ModifiedBy);

            return result;
        }

        private static Group CreateGroup(this GroupPackage source, GroupStage groupStage, IList<Team> teams)
        {
            var result = new Group(groupStage, source.Name, source.ShortName, id: source.Id)
            {
                Order = source.Order,
            };

            var teamsId = source.TeamIds.Split(";").NotNullOrEmpty();

            if (teamsId.Any())
                result.SetTeams(teamsId.Select(x => teams.GetByIdOrDefault(Guid.Parse(x))).NotNull());

            result.MarkedAsCreated(source.CreatedAt, source.CreatedBy);
            result.MarkedAsModified(source.ModifiedAt, source.ModifiedBy);

            return result;
        }

        public static Friendly CreateFriendly(this FriendlyPackage source)
        {
            var result = new Friendly(source.Name, source.ShortName, (Category)source.Category, new FriendlyRules(source.MatchFormat?.CreateMatchFormat() ?? MatchFormat.Default, source.MatchTime), id: source.Id)
            {
                Logo = source.Logo
            };

            result.MarkedAsCreated(source.CreatedAt, source.CreatedBy);
            result.MarkedAsModified(source.ModifiedAt, source.ModifiedBy);

            return result;
        }

        public static Friendly CreateFriendlyAndSeasons(this FriendlyPackage source, IList<Season> seasons, IList<Team> teams, IList<Stadium> stadiums, IList<Player> players)
        {
            var result = source.CreateFriendly();
            source.Seasons?.ForEach(x => result.AddSeason(x.CreateFriendlySeason(result, seasons.GetById(x.SeasonId), teams, stadiums, players)));

            return result;
        }

        public static FriendlySeason CreateFriendlySeason(this FriendlySeasonPackage source, Friendly friendly, Season season, IList<Team> teams, IList<Stadium> stadiums, IList<Player> players)
        {
            var result = new FriendlySeason(friendly, season, new FriendlyRules(source.MatchFormat?.CreateMatchFormat() ?? MatchFormat.Default, 15.Hours()), source.StartDate, source.EndDate, id: source.Id);

            var teamsId = source.TeamIds.Split(";").NotNullOrEmpty();

            if (teamsId.Any())
                result.SetTeams(teamsId.Select(x => teams.GetByIdOrDefault(Guid.Parse(x))).NotNull());

            source.Matches?.ForEach(x => result.AddMatch(x.CreateMatch(result.Rules.MatchFormat, result.Teams, stadiums, players)));

            result.MarkedAsCreated(source.CreatedAt, source.CreatedBy);
            result.MarkedAsModified(source.ModifiedAt, source.ModifiedBy);

            return result;
        }

        public static Cup CreateCup(this CupPackage source)
        {
            var result = new Cup(source.Name, source.ShortName, (Category)source.Category, new CupRules(source.MatchFormat?.CreateMatchFormat() ?? MatchFormat.NoDraw, source.MatchTime), id: source.Id)
            {
                Logo = source.Logo,
            };

            result.MarkedAsCreated(source.CreatedAt, source.CreatedBy);
            result.MarkedAsModified(source.ModifiedAt, source.ModifiedBy);

            return result;
        }

        public static Cup CreateCupAndSeasons(this CupPackage source, IList<Season> seasons, IList<Team> teams, IList<Stadium> stadiums, IList<Player> players)
        {
            var result = source.CreateCup();
            source.Seasons?.ForEach(x => result.AddSeason(x.CreateCupSeason(result, seasons.GetById(x.SeasonId), teams, stadiums, players)));

            return result;
        }

        public static CupSeason CreateCupSeason(this CupSeasonPackage source, Cup cup, Season season, IList<Team> teams, IList<Stadium> stadiums, IList<Player> players)
        {
            var result = new CupSeason(cup, season, new CupRules(source.MatchFormat?.CreateMatchFormat() ?? MatchFormat.NoDraw, 15.Hours()), source.StartDate, source.EndDate, id: source.Id);

            var teamsId = source.TeamIds.Split(";").NotNullOrEmpty();

            if (teamsId.Any())
                result.SetTeams(teamsId.Select(x => teams.GetByIdOrDefault(Guid.Parse(x))).NotNull());

            source.Rounds?.ForEach(x =>
            {
                IRound round = x switch
                {
                    GroupStagePackage groupStage => groupStage.CreateGroupStage(result.Rules.MatchFormat, teams, stadiums, players),
                    KnockoutPackage knockout => knockout.CreateKnockout(result.Rules.MatchFormat, teams, stadiums, players),
                    _ => throw new InvalidOperationException("Round type is unknown"),
                };
                result.AddRound(round);
            });

            result.MarkedAsCreated(source.CreatedAt, source.CreatedBy);
            result.MarkedAsModified(source.ModifiedAt, source.ModifiedBy);

            return result;
        }

        public static League CreateLeague(this LeaguePackage source)
        {
            var result = new League(source.Name, source.ShortName, (Category)source.Category, new LeagueRules(source.MatchFormat?.CreateMatchFormat() ?? MatchFormat.Default, source.RankingRules?.CreateRankingRules() ?? RankingRules.Default, source.MatchTime), id: source.Id)
            {
                Logo = source.Logo,
            };

            result.MarkedAsCreated(source.CreatedAt, source.CreatedBy);
            result.MarkedAsModified(source.ModifiedAt, source.ModifiedBy);

            return result;
        }

        public static League CreateLeagueAndSeasons(this LeaguePackage source, IList<Season> seasons, IList<Team> teams, IList<Stadium> stadiums, IList<Player> players)
        {
            var result = source.CreateLeague();
            source.Seasons?.ForEach(x => result.AddSeason(x.CreateLeagueSeason(result, seasons.GetById(x.SeasonId), teams, stadiums, players)));

            return result;
        }

        public static LeagueSeason CreateLeagueSeason(this SeasonLeaguePackage source, League league, Season season, IList<Team> teams, IList<Stadium> stadiums, IList<Player> players)
        {
            var result = new LeagueSeason(league, season, new LeagueRules(source.MatchFormat?.CreateMatchFormat() ?? MatchFormat.Default, source.RankingRules?.CreateRankingRules() ?? RankingRules.Default), source.StartDate, source.EndDate, id: source.Id);

            var teamsId = source.TeamIds.Split(";").NotNullOrEmpty();

            if (teamsId.Any())
                result.SetTeams(teamsId.Select(x => teams.GetByIdOrDefault(Guid.Parse(x))).NotNull());

            source.Matchdays?.ForEach(x => result.AddMatchday(x.CreateMatchday(result.Rules.MatchFormat, result.Teams, stadiums, players)));

            result.MarkedAsCreated(source.CreatedAt, source.CreatedBy);
            result.MarkedAsModified(source.ModifiedAt, source.ModifiedBy);

            return result;
        }

        public static TrainingAttendance CreateAttendance(this TrainingAttendancePackage source, Player player)
            => new(player: player, attendance: (Attendance)source.Attendance, rating: source.Rating, id: source.Id)
            {
                Reason = source.Reason,
                Comment = source.Comment
            };

        public static TrainingSession CreateTrainingSession(this TrainingSessionPackage source, IList<Player> players)
        {
            var result = new TrainingSession(start: source.StartDate, end: source.EndDate, id: source.Id)
            {
                Theme = source.Theme.OrThrow(),
                Place = source.Place,
            };

            result.TechnicalGoals.AddRange(source.TechnicalGoals.Split(";"));
            result.TacticalGoals.AddRange(source.TacticalGoals.Split(";"));
            result.MentalGoals.AddRange(source.MentalGoals.Split(";"));
            result.PhysicalGoals.AddRange(source.PhysicalGoals.Split(";"));
            result.Stages.AddRange(source.Stages.Split(";"));
            result.TeamIds.AddRange(source.TeamIds.Split(";").Select(Guid.Parse));
            source.Attendances?.ForEach(z => result.AddAttendance(z.CreateAttendance(players.GetById(z.PlayerId))));

            if (source.IsCancelled)
                result.Cancel();

            result.MarkedAsCreated(source.CreatedAt, source.CreatedBy);
            result.MarkedAsModified(source.ModifiedAt, source.ModifiedBy);

            return result;
        }

        public static SendedMail CreateSendedMail(this SendedMailPackage source)
        {
            var result = new SendedMail(date: source.Date, subject: source.Subject, body: source.Body, sendACopy: source.SendACopy, id: source.Id)
            {
                State = (SendingState)source.State
            };

            result.ToAddresses.AddRange(source.ToAddresses.Split(";"));

            result.MarkedAsCreated(source.CreatedAt, source.CreatedBy);
            result.MarkedAsModified(source.ModifiedAt, source.ModifiedBy);

            return result;
        }

        public static Email CreateEmail(this ContactPackage source) => new(value: source.Value, label: source.Label, source.Default);

        public static Phone CreatePhone(this ContactPackage source) => new(value: source.Value, label: source.Label, source.Default);
    }
}
