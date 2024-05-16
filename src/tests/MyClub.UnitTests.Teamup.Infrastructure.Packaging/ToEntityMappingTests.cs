// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using MyClub.Domain;
using MyClub.Domain.Enums;
using MyClub.Teamup.Domain.CycleAggregate;
using MyClub.Teamup.Domain.Enums;
using MyClub.Teamup.Domain.HolidaysAggregate;
using MyClub.Teamup.Domain.PersonAggregate;
using MyClub.Teamup.Domain.Randomize;
using MyClub.Teamup.Domain.SendedMailAggregate;
using MyClub.Teamup.Domain.SquadAggregate;
using MyClub.Teamup.Domain.TeamAggregate;
using MyClub.Teamup.Domain.TrainingAggregate;
using MyClub.Teamup.Infrastructure.Packaging.Converters;
using MyClub.Teamup.Infrastructure.Packaging.Models;
using MyNet.Utilities;
using Xunit;

namespace MyClub.UnitTests.Teamup.Infrastructure.Packaging
{
    public class ToEntityMappingTests
    {
        [Fact]
        public void ConvertProjectPackageToProject()
        {
            var item = ProjectFactory.Create();
            var source = item.ToPackage();
            var destination = source.CreateProject();

            Assert.Equal(source.Metadata!.Name, destination.Name);
            Assert.Equal(source.Metadata!.Id, destination.Id);
            Assert.Equal(source.Metadata!.Image, destination.Image);
            Assert.Equal(source.Metadata!.Preferences!.TrainingDuration, destination.Preferences.TrainingDuration);
            Assert.Equal(source.Metadata!.Preferences!.TrainingStartTime, destination.Preferences.TrainingStartTime);

            Assert.Equal(source.Metadata!.CreatedAt, destination.CreatedAt);
            Assert.Equal(source.Metadata!.CreatedBy, destination.CreatedBy);
            Assert.Equal(source.Metadata!.ModifiedAt, destination.ModifiedAt);
            Assert.Equal(source.Metadata!.ModifiedBy, destination.ModifiedBy);

            CheckCollection(source.Squads!.SelectMany(x => x.Players!), destination.Players, CheckSquadPlayer);
            CheckCollection(source.Cycles!, destination.Cycles, CheckCycle);
            CheckCollection(source.TrainingSessions!, destination.TrainingSessions, CheckTrainingSession);
            CheckCollection(source.Holidays!, destination.Holidays, CheckHolidays);
            CheckCollection(source.SendedMails!, destination.SendedMails, CheckSendedMail);
        }

        [Fact]
        public void ConvertContactPackageToEmail()
        {
            var item = PlayerRandomFactory.RandomEmail();
            var source = item.ToPackage();
            var destination = source.CreateEmail();

            CheckEmail(source, destination);
        }

        [Fact]
        public void ConvertContactPackageToPhone()
        {
            var item = PlayerRandomFactory.RandomPhone();
            var source = item.ToPackage();
            var destination = source.CreatePhone();

            CheckPhone(source, destination);
        }

        [Fact]
        public void ConvertInjuryPackageToInjury()
        {
            var item = PlayerRandomFactory.RandomInjury();
            item.MarkedAsModified(DateTime.UtcNow, "System");
            var source = item.ToPackage();
            var destination = source.CreateInjury();

            CheckInjury(source, destination);
        }

        [Fact]
        public void ConvertPlayerAbsencePackageToPlayerAbsence()
        {
            var item = PlayerRandomFactory.RandomAbsence();
            var source = item.ToPackage();
            var destination = source.CreateAbsence();

            CheckAbsence(source, destination);
        }

        [Fact]
        public void ConvertRatedPositionPackageToRatedPosition()
        {
            var item = PlayerRandomFactory.RandomPosition();
            var source = item.ToPackage();
            var destination = source.CreatePosition();

            CheckPosition(source, destination);
        }

        [Fact]
        public void ConvertPlayerPackageToPlayer()
        {
            var item = PlayerRandomFactory.Random();
            var squadItem = PlayerRandomFactory.RandomSquadPlayer(item);
            var source = item.ToPackage();
            var squadSource = squadItem.ToPackage();
            var destination = source.CreatePlayer();
            var squadDestination = squadSource.CreateSquadPlayer(destination);

            CheckPlayer(source, destination);
            CheckSquadPlayer(squadSource, squadDestination);
        }

        [Fact]
        public void ConvertPlayerPackageWithNullAddressToPlayer()
        {
            var item = PlayerRandomFactory.Random();
            item.Address = null;
            var source = item.ToPackage();
            var destination = source.CreatePlayer();

            Assert.Equal(source.Address?.Street, destination.Address?.Street);
            Assert.Equal(source.Address?.City, destination.Address?.City);
            Assert.Equal(source.Address?.PostalCode, destination.Address?.PostalCode);
        }

        [Fact]
        public void ConvertTeamPackageToTeam()
        {
            var item = ClubRandomFactory.Random().Teams[0];
            var source = item.ToPackage();
            var destination = source.CreateTeam(item.Club, item.Stadium.OverrideValue);

            CheckTeam(source, destination);
        }

        [Fact]
        public void ConvertSendedMailPackageToSendedMail()
        {
            var item = SendedMailRandomFactory.Random();
            var source = item.ToPackage();
            var destination = source.CreateSendedMail();

            CheckSendedMail(source, destination);
        }

        [Fact]
        public void ConvertHolidaysItemPackageToHolidays()
        {
            var item = PeriodRandomFactory.RandomHolidays();
            var source = item.ToPackage();
            var destination = source.CreateHolidays();

            CheckHolidays(source, destination);
        }

        [Fact]
        public void ConvertCyclePackageToCycle()
        {
            var item = PeriodRandomFactory.RandomCycle();
            var source = item.ToPackage();
            var destination = source.CreateCycle();

            CheckCycle(source, destination);
        }

        [Fact]
        public void ConvertTrainingAttendancePackageToTrainingAttendance()
        {
            var player = PlayerRandomFactory.Random();
            var squadPlayer = PlayerRandomFactory.RandomSquadPlayer(player);
            var item = TrainingSessionRandomFactory.RandomAttendance(squadPlayer);
            var source = item.ToPackage();
            var destination = source.CreateAttendance(player);

            CheckTrainingAttendance(source, destination);
        }

        [Fact]
        public void ConvertTrainingSessionPackageToTrainingSession()
        {
            var team = ClubRandomFactory.Random().Teams[0];
            var players = PlayerRandomFactory.RandomSquadPlayers(team: team).ToList();
            var item = TrainingSessionRandomFactory.Random(teams: [team], players: players);
            var source = item.ToPackage();
            var destination = source.CreateTrainingSession(players.Select(x => x.Player).ToList());

            CheckTrainingSession(source, destination);
        }

        private static void CheckPlayer(PlayerPackage source, Player destination)
        {
            Assert.Equal(source.Id, destination.Id);
            Assert.Equal(source.Address?.Street, destination.Address?.Street);
            Assert.Equal(source.Birthdate, destination.Birthdate);
            Assert.Equal(source.Address?.City, destination.Address?.City);
            Assert.Equal(source.Address?.Country, destination.Address?.Country?.Value);
            Assert.Equal(source.Country, destination.Country?.Value);
            Assert.Equal(source.Category, destination.Category?.Value);
            Assert.Equal(source.Description, destination.Description);
            Assert.Equal(source.FirstName, destination.FirstName);
            Assert.Equal((GenderType)source.Gender, destination.Gender);
            Assert.Equal(source.Height, destination.Height);
            Assert.Equal(source.LastName, destination.LastName);
            Assert.Equal((Laterality)source.Laterality, destination.Laterality);
            Assert.Equal(source.LicenseNumber, destination.LicenseNumber);
            Assert.Equal(source.Photo, destination.Photo);
            Assert.Equal(source.PlaceOfBirth, destination.PlaceOfBirth);
            Assert.Equal(source.Address?.PostalCode, destination.Address?.PostalCode);
            Assert.Equal(source.Weight, destination.Weight);

            Assert.Equal(source.CreatedAt, destination.CreatedAt);
            Assert.Equal(source.CreatedBy, destination.CreatedBy);
            Assert.Equal(source.ModifiedAt, destination.ModifiedAt);
            Assert.Equal(source.ModifiedBy, destination.ModifiedBy);

            CheckCollection(source.Absences!, destination.Absences, CheckAbsence);
            CheckCollection(source.Emails!, destination.Emails, CheckEmail);
            CheckCollection(source.Injuries!, destination.Injuries, CheckInjury);
            CheckCollection(source.Phones!, destination.Phones, CheckPhone);
            CheckCollection(source.Positions!, destination.Positions, CheckPosition);
        }

        private static void CheckSquadPlayer(SquadPlayerPackage source, SquadPlayer destination)
        {
            Assert.Equal(source.Id, destination.Id);
            Assert.Equal(source.Category, destination.Category?.Value);
            Assert.Equal(source.FromDate, destination.FromDate);
            Assert.Equal(source.IsMutation, destination.IsMutation);
            Assert.Equal(source.Number, destination.Number);
            Assert.Equal(source.ShoesSize, destination.ShoesSize);
            Assert.Equal(source.Size, destination.Size);
            Assert.Equal(source.TeamId, destination.Team?.Id);

            Assert.Equal(source.CreatedAt, destination.CreatedAt);
            Assert.Equal(source.CreatedBy, destination.CreatedBy);
            Assert.Equal(source.ModifiedAt, destination.ModifiedAt);
            Assert.Equal(source.ModifiedBy, destination.ModifiedBy);

            CheckCollection(source.Positions!, destination.Positions, CheckPosition);
        }

        private static void CheckPosition(RatedPositionPackage source, RatedPosition destination)
        {
            Assert.Equal(source.Id, destination.Id);
            Assert.Equal(source.Position, destination.Position.Value);
            Assert.Equal((PositionRating)source.Rating, destination.Rating);
            Assert.Equal(source.IsNatural, destination.IsNatural);
        }

        private static void CheckEmail(ContactPackage source, Email destination)
        {
            Assert.Equal(source.Value, destination.Value);
            Assert.Equal(source.Label, destination.Label);
            Assert.Equal(source.Default, destination.Default);
        }

        private static void CheckPhone(ContactPackage source, Phone destination)
        {
            Assert.Equal(source.Value, destination.Value);
            Assert.Equal(source.Label, destination.Label);
            Assert.Equal(source.Default, destination.Default);
        }

        private static void CheckInjury(InjuryPackage source, Injury destination)
        {
            Assert.Equal(source.Id, destination.Id);
            Assert.Equal(source.Date, destination.Period.Start);
            Assert.Equal(source.EndDate, destination.Period.End);
            Assert.Equal(source.Description, destination.Description);
            Assert.Equal(source.Condition, destination.Condition);
            Assert.Equal((InjuryCategory)source.Category, destination.Category);
            Assert.Equal((InjurySeverity)source.Severity, destination.Severity);
            Assert.Equal((InjuryType)source.Type, destination.Type);

            Assert.Equal(source.CreatedAt, destination.CreatedAt);
            Assert.Equal(source.CreatedBy, destination.CreatedBy);
            Assert.Equal(source.ModifiedAt, destination.ModifiedAt);
            Assert.Equal(source.ModifiedBy, destination.ModifiedBy);
        }

        private static void CheckAbsence(AbsencePackage source, Absence destination)
        {
            Assert.Equal(source.Id, destination.Id);
            Assert.Equal(source.StartDate, destination.Period.Start);
            Assert.Equal(source.EndDate, destination.Period.End);
            Assert.Equal(source.Label, destination.Label);
            Assert.Equal(source.Type, (int)destination.Type);

            Assert.Equal(source.CreatedAt, destination.CreatedAt);
            Assert.Equal(source.CreatedBy, destination.CreatedBy);
            Assert.Equal(source.ModifiedAt, destination.ModifiedAt);
            Assert.Equal(source.ModifiedBy, destination.ModifiedBy);
        }

        private static void CheckTeam(TeamPackage source, Team destination)
        {
            Assert.Equal(source.Id, destination.Id);
            Assert.Equal(source.HomeColor, destination.HomeColor.OverrideValue);
            Assert.Equal(source.AwayColor, destination.AwayColor.OverrideValue);
            Assert.Equal(source.StadiumId, destination.Stadium.OverrideValue?.Id);
            Assert.Equal(source.Name, destination.Name);
            Assert.Equal(source.ShortName, destination.ShortName);
            Assert.Equal(source.Category, destination.Category);
            Assert.Equal(source.Order, destination.Order);

            Assert.Equal(source.CreatedAt, destination.CreatedAt);
            Assert.Equal(source.CreatedBy, destination.CreatedBy);
            Assert.Equal(source.ModifiedAt, destination.ModifiedAt);
            Assert.Equal(source.ModifiedBy, destination.ModifiedBy);
        }

        private static void CheckHolidays(HolidaysItemPackage source, Holidays destination)
        {
            Assert.Equal(source.Id, destination.Id);
            Assert.Equal(source.StartDate, destination.Period.Start);
            Assert.Equal(source.EndDate, destination.Period.End);
            Assert.Equal(source.Label, destination.Label);

            Assert.Equal(source.CreatedAt, destination.CreatedAt);
            Assert.Equal(source.CreatedBy, destination.CreatedBy);
            Assert.Equal(source.ModifiedAt, destination.ModifiedAt);
            Assert.Equal(source.ModifiedBy, destination.ModifiedBy);
        }

        private static void CheckSendedMail(SendedMailPackage source, SendedMail destination)
        {
            Assert.Equal(source.Id, destination.Id);
            Assert.Equal(source.Subject, destination.Subject);
            Assert.Equal(source.Date, destination.Date);
            Assert.Equal((SendingState)source.State, destination.State);
            Assert.Equal(source.Body, destination.Body);
            Assert.Equal(source.SendACopy, destination.SendACopy);
            Assert.Equal(source.ToAddresses, string.Join(";", destination.ToAddresses));

            Assert.Equal(source.CreatedAt, destination.CreatedAt);
            Assert.Equal(source.CreatedBy, destination.CreatedBy);
            Assert.Equal(source.ModifiedAt, destination.ModifiedAt);
            Assert.Equal(source.ModifiedBy, destination.ModifiedBy);
        }

        private static void CheckCycle(CyclePackage source, Cycle destination)
        {
            Assert.Equal(source.Id, destination.Id);
            Assert.Equal(source.StartDate, destination.Period.Start);
            Assert.Equal(source.EndDate, destination.Period.End);
            Assert.Equal(source.Color, destination.Color);
            Assert.Equal(source.Label, destination.Label);
            Assert.Equal(source.TechnicalGoals, string.Join(";", destination.TechnicalGoals));
            Assert.Equal(source.TacticalGoals, string.Join(";", destination.TacticalGoals));
            Assert.Equal(source.PhysicalGoals, string.Join(";", destination.PhysicalGoals));
            Assert.Equal(source.MentalGoals, string.Join(";", destination.MentalGoals));

            Assert.Equal(source.CreatedAt, destination.CreatedAt);
            Assert.Equal(source.CreatedBy, destination.CreatedBy);
            Assert.Equal(source.ModifiedAt, destination.ModifiedAt);
            Assert.Equal(source.ModifiedBy, destination.ModifiedBy);
        }

        private static void CheckTrainingAttendance(TrainingAttendancePackage source, TrainingAttendance destination)
        {
            Assert.Equal(source.Id, destination.Id);
            Assert.Equal(source.Reason, destination.Reason);
            Assert.Equal((Attendance)source.Attendance, destination.Attendance);
            Assert.Equal(source.Comment, destination.Comment);
            Assert.Equal(source.Rating, destination.Rating);
            Assert.Equal(source.PlayerId, destination.Player.Id);
        }

        private static void CheckTrainingSession(TrainingSessionPackage source, TrainingSession destination)
        {
            Assert.Equal(source.Id, destination.Id);
            Assert.Equal(source.StartDate, destination.Start);
            Assert.Equal(source.EndDate, destination.End);
            Assert.Equal(source.IsCancelled, destination.IsCancelled);
            Assert.Equal(source.Theme, destination.Theme);
            Assert.Equal(source.Place, destination.Place);
            Assert.Equal(source.TechnicalGoals, string.Join(";", destination.TechnicalGoals));
            Assert.Equal(source.TacticalGoals, string.Join(";", destination.TacticalGoals));
            Assert.Equal(source.PhysicalGoals, string.Join(";", destination.PhysicalGoals));
            Assert.Equal(source.MentalGoals, string.Join(";", destination.MentalGoals));
            Assert.Equal(source.Stages, string.Join(";", destination.Stages));
            Assert.Equal(source.TeamIds, string.Join(";", destination.TeamIds));

            Assert.Equal(source.CreatedAt, destination.CreatedAt);
            Assert.Equal(source.CreatedBy, destination.CreatedBy);
            Assert.Equal(source.ModifiedAt, destination.ModifiedAt);
            Assert.Equal(source.ModifiedBy, destination.ModifiedBy);

            CheckCollection(source.Attendances!, destination.Attendances, CheckTrainingAttendance);
        }

        private static void CheckCollection<TSource, TDestination>(IEnumerable<TSource> source, IEnumerable<TDestination> destination, Action<TSource, TDestination> checkMethod)
        {
            var sourceList = source.ToList();
            var destinationList = destination.ToList();

            Assert.Equal(sourceList.Count, destinationList.Count);

            for (var i = 0; i < sourceList.Count; i++)
                checkMethod(sourceList[i], destinationList[i]);
        }
    }
}
