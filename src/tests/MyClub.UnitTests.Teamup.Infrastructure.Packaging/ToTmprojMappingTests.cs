// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using MyClub.Domain;
using MyClub.Teamup.Domain.CycleAggregate;
using MyClub.Teamup.Domain.HolidaysAggregate;
using MyClub.Teamup.Domain.PersonAggregate;
using MyClub.Teamup.Domain.Randomize;
using MyClub.Teamup.Domain.SendedMailAggregate;
using MyClub.Teamup.Domain.SquadAggregate;
using MyClub.Teamup.Domain.TeamAggregate;
using MyClub.Teamup.Domain.TrainingAggregate;
using MyClub.Teamup.Infrastructure.Packaging.Converters;
using MyClub.Teamup.Infrastructure.Packaging.Models;
using Xunit;

namespace MyClub.UnitTests.Teamup.Infrastructure.Packaging
{
    public class ToPackageMappingTests
    {
        [Fact]
        public void ConvertProjectToProjectPackage()
        {
            var source = ProjectFactory.Create();
            var destination = source.ToPackage();

            Assert.Equal(source.Name, destination.Metadata!.Name);
            Assert.Equal(source.Id, destination.Metadata!.Id);
            Assert.Equal(source.Image, destination.Metadata!.Image);
            Assert.Equal(source.CreatedAt, destination.Metadata!.CreatedAt);
            Assert.Equal(source.CreatedBy, destination.Metadata!.CreatedBy);
            Assert.Equal(source.ModifiedAt, destination.Metadata!.ModifiedAt);
            Assert.Equal(source.ModifiedBy, destination.Metadata!.ModifiedBy);
            Assert.Equal(source.Preferences.TrainingDuration, destination.Metadata!.Preferences!.TrainingDuration);
            Assert.Equal(source.Preferences.TrainingStartTime, destination.Metadata!.Preferences!.TrainingStartTime);

            CheckCollection(source.Players, destination.Squads!.SelectMany(x => x.Players!), CheckSquadPlayer);
            CheckCollection(source.Cycles, destination.Cycles!, CheckCycle);
            CheckCollection(source.TrainingSessions, destination.TrainingSessions!, CheckTrainingSession);
            CheckCollection(source.Holidays, destination.Holidays!, CheckHolidays);
            CheckCollection(source.SendedMails, destination.SendedMails!, CheckSendedMail);
        }

        [Fact]
        public void ConvertEmailToContactPackage()
        {
            var source = PlayerRandomFactory.RandomEmail();
            var destination = source.ToPackage();

            CheckEmail(source, destination);
        }

        [Fact]
        public void ConvertPhoneToContactPackage()
        {
            var source = PlayerRandomFactory.RandomPhone();
            var destination = source.ToPackage();

            CheckPhone(source, destination);
        }

        [Fact]
        public void ConvertInjuryToInjuryPackage()
        {
            var source = PlayerRandomFactory.RandomInjury();
            source.MarkedAsModified(DateTime.UtcNow, "System");
            var destination = source.ToPackage();

            CheckInjury(source, destination);
        }

        [Fact]
        public void ConvertPlayerAbsenceToPlayerAbsencePackage()
        {
            var source = PlayerRandomFactory.RandomAbsence();
            var destination = source.ToPackage();

            CheckAbsence(source, destination);
        }

        [Fact]
        public void ConvertRatedPositionToRatedPositionPackage()
        {
            var source = PlayerRandomFactory.RandomPosition();
            var destination = source.ToPackage();

            CheckPosition(source, destination);
        }

        [Fact]
        public void ConvertPlayerToPlayerPackage()
        {
            var source = PlayerRandomFactory.Random();
            var destination = source.ToPackage();

            CheckPlayer(source, destination);
        }

        [Fact]
        public void ConvertPlayerWithNullAddressToPlayerPackage()
        {
            var source = PlayerRandomFactory.Random();
            source.Address = null;
            var destination = source.ToPackage();

            Assert.Equal(source.Address?.Street, destination.Address?.Street);
            Assert.Equal(source.Address?.City, destination.Address?.City);
            Assert.Equal(source.Address?.PostalCode, destination.Address?.PostalCode);
        }

        [Fact]
        public void ConvertTeamToTeamPackage()
        {
            var source = ClubRandomFactory.Random().Teams[0];
            var destination = source.ToPackage();

            CheckTeam(source, destination);
        }

        [Fact]
        public void ConvertSendedMailToSendedMailPackage()
        {
            var source = SendedMailRandomFactory.Random();
            var destination = source.ToPackage();

            CheckSendedMail(source, destination);
        }

        [Fact]
        public void ConvertHolidaysToHolidaysItemPackage()
        {
            var source = PeriodRandomFactory.RandomHolidays();
            var destination = source.ToPackage();

            CheckHolidays(source, destination);
        }

        [Fact]
        public void ConvertCycleToCyclePackage()
        {
            var source = PeriodRandomFactory.RandomCycle();
            var destination = source.ToPackage();

            CheckCycle(source, destination);
        }

        [Fact]
        public void ConvertTrainingAttendanceToTrainingAttendancePackage()
        {
            var player = PlayerRandomFactory.Random();
            var squadPlayer = PlayerRandomFactory.RandomSquadPlayer(player);
            var source = TrainingSessionRandomFactory.RandomAttendance(squadPlayer);
            var destination = source.ToPackage();

            CheckTrainingAttendance(source, destination);
        }

        [Fact]
        public void ConvertTrainingSessionToTrainingSessionPackage()
        {
            var team = ClubRandomFactory.Random().Teams[0];
            var players = PlayerRandomFactory.RandomSquadPlayers(team: team).ToList();
            var source = TrainingSessionRandomFactory.Random(teams: [team], players: players);
            var destination = source.ToPackage();

            CheckTrainingSession(source, destination);
        }

        private static void CheckPlayer(Player source, PlayerPackage destination)
        {
            Assert.Equal(source.Id, destination.Id);
            Assert.Equal(source.Address?.Street, destination.Address?.Street);
            Assert.Equal(source.Birthdate, destination.Birthdate);
            Assert.Equal(source.Address?.City, destination.Address?.City);
            Assert.Equal(source.Address?.Country?.Value, destination.Address?.Country);
            Assert.Equal(source.Country?.Value, destination.Country);
            Assert.Equal(source.Category?.Value, destination.Category);
            Assert.Equal(source.Description, destination.Description);
            Assert.Equal(source.FirstName, destination.FirstName);
            Assert.Equal((int)source.Gender, destination.Gender);
            Assert.Equal(source.Height, destination.Height);
            Assert.Equal(source.LastName, destination.LastName);
            Assert.Equal((int)source.Laterality, destination.Laterality);
            Assert.Equal(source.LicenseNumber, destination.LicenseNumber);
            Assert.Equal(source.Photo, destination.Photo);
            Assert.Equal(source.PlaceOfBirth, destination.PlaceOfBirth);
            Assert.Equal(source.Address?.PostalCode, destination.Address?.PostalCode);
            Assert.Equal(source.Weight, destination.Weight);

            Assert.Equal(source.CreatedAt, destination.CreatedAt);
            Assert.Equal(source.CreatedBy, destination.CreatedBy);
            Assert.Equal(source.ModifiedAt, destination.ModifiedAt);
            Assert.Equal(source.ModifiedBy, destination.ModifiedBy);

            CheckCollection(source.Absences, destination.Absences!, CheckAbsence);
            CheckCollection(source.Emails, destination.Emails!, CheckEmail);
            CheckCollection(source.Injuries, destination.Injuries!, CheckInjury);
            CheckCollection(source.Phones, destination.Phones!, CheckPhone);
            CheckCollection(source.Positions, destination.Positions!, CheckPosition);
        }

        private static void CheckSquadPlayer(SquadPlayer source, SquadPlayerPackage destination)
        {
            Assert.Equal(source.Id, destination.Id);
            Assert.Equal(source.Category?.Value, destination.Category);
            Assert.Equal(source.FromDate, destination.FromDate);
            Assert.Equal(source.IsMutation, destination.IsMutation);
            Assert.Equal(source.Number, destination.Number);
            Assert.Equal(source.ShoesSize, destination.ShoesSize);
            Assert.Equal(source.Size, destination.Size);
            Assert.Equal(source.Team?.Id, destination.TeamId);

            Assert.Equal(source.CreatedAt, destination.CreatedAt);
            Assert.Equal(source.CreatedBy, destination.CreatedBy);
            Assert.Equal(source.ModifiedAt, destination.ModifiedAt);
            Assert.Equal(source.ModifiedBy, destination.ModifiedBy);

            CheckCollection(source.Positions, destination.Positions!, CheckPosition);
        }

        private static void CheckPosition(RatedPosition source, RatedPositionPackage destination)
        {
            Assert.Equal(source.Id, destination.Id);
            Assert.Equal(source.Position.Value, destination.Position);
            Assert.Equal((int)source.Rating, destination.Rating);
            Assert.Equal(source.IsNatural, destination.IsNatural);
        }

        private static void CheckEmail(Email source, ContactPackage destination)
        {
            Assert.Equal(source.Value, destination.Value);
            Assert.Equal(source.Label, destination.Label);
            Assert.Equal(source.Default, destination.Default);
        }

        private static void CheckPhone(Phone source, ContactPackage destination)
        {
            Assert.Equal(source.Value, destination.Value);
            Assert.Equal(source.Label, destination.Label);
            Assert.Equal(source.Default, destination.Default);
        }

        private static void CheckInjury(Injury source, InjuryPackage destination)
        {
            Assert.Equal(source.Id, destination.Id);
            Assert.Equal(source.Period.Start, destination.Date);
            Assert.Equal(source.Period.End, destination.EndDate);
            Assert.Equal(source.Description, destination.Description);
            Assert.Equal(source.Condition, destination.Condition);
            Assert.Equal((int)source.Category, destination.Category);
            Assert.Equal((int)source.Severity, destination.Severity);
            Assert.Equal((int)source.Type, destination.Type);

            Assert.Equal(source.CreatedAt, destination.CreatedAt);
            Assert.Equal(source.CreatedBy, destination.CreatedBy);
            Assert.Equal(source.ModifiedAt, destination.ModifiedAt);
            Assert.Equal(source.ModifiedBy, destination.ModifiedBy);
        }

        private static void CheckAbsence(Absence source, AbsencePackage destination)
        {
            Assert.Equal(source.Id, destination.Id);
            Assert.Equal(source.Period.Start, destination.StartDate);
            Assert.Equal(source.Period.End, destination.EndDate);
            Assert.Equal(source.Label, destination.Label);
            Assert.Equal((int)source.Type, destination.Type);
        }

        private static void CheckTeam(Team source, TeamPackage destination)
        {
            Assert.Equal(source.Id, destination.Id);
            Assert.Equal(source.HomeColor.OverrideValue, destination.HomeColor);
            Assert.Equal(source.AwayColor.OverrideValue, destination.AwayColor);
            Assert.Equal(source.Stadium.OverrideValue?.Id, destination.StadiumId);
            Assert.Equal(source.Name, destination.Name);
            Assert.Equal(source.ShortName, destination.ShortName);
            Assert.Equal(source.Category, destination.Category);
            Assert.Equal(source.Order, destination.Order);

            Assert.Equal(source.CreatedAt, destination.CreatedAt);
            Assert.Equal(source.CreatedBy, destination.CreatedBy);
            Assert.Equal(source.ModifiedAt, destination.ModifiedAt);
            Assert.Equal(source.ModifiedBy, destination.ModifiedBy);
        }

        private static void CheckHolidays(Holidays source, HolidaysItemPackage destination)
        {
            Assert.Equal(source.Id, destination.Id);
            Assert.Equal(source.Period.Start, destination.StartDate);
            Assert.Equal(source.Period.End, destination.EndDate);
            Assert.Equal(source.Label, destination.Label);

            Assert.Equal(source.CreatedAt, destination.CreatedAt);
            Assert.Equal(source.CreatedBy, destination.CreatedBy);
            Assert.Equal(source.ModifiedAt, destination.ModifiedAt);
            Assert.Equal(source.ModifiedBy, destination.ModifiedBy);
        }

        private static void CheckSendedMail(SendedMail source, SendedMailPackage destination)
        {
            Assert.Equal(source.Id, destination.Id);
            Assert.Equal(source.Subject, destination.Subject);
            Assert.Equal(source.Date, destination.Date);
            Assert.Equal((int)source.State, destination.State);
            Assert.Equal(source.Body, destination.Body);
            Assert.Equal(source.SendACopy, destination.SendACopy);
            Assert.Equal(string.Join(";", source.ToAddresses), destination.ToAddresses);

            Assert.Equal(source.CreatedAt, destination.CreatedAt);
            Assert.Equal(source.CreatedBy, destination.CreatedBy);
            Assert.Equal(source.ModifiedAt, destination.ModifiedAt);
            Assert.Equal(source.ModifiedBy, destination.ModifiedBy);
        }

        private static void CheckCycle(Cycle source, CyclePackage destination)
        {
            Assert.Equal(source.Id, destination.Id);
            Assert.Equal(source.Period.Start, destination.StartDate);
            Assert.Equal(source.Period.End, destination.EndDate);
            Assert.Equal(source.Color, destination.Color);
            Assert.Equal(source.Label, destination.Label);
            Assert.Equal(string.Join(";", source.TechnicalGoals), destination.TechnicalGoals);
            Assert.Equal(string.Join(";", source.TacticalGoals), destination.TacticalGoals);
            Assert.Equal(string.Join(";", source.PhysicalGoals), destination.PhysicalGoals);
            Assert.Equal(string.Join(";", source.MentalGoals), destination.MentalGoals);

            Assert.Equal(source.CreatedAt, destination.CreatedAt);
            Assert.Equal(source.CreatedBy, destination.CreatedBy);
            Assert.Equal(source.ModifiedAt, destination.ModifiedAt);
            Assert.Equal(source.ModifiedBy, destination.ModifiedBy);
        }

        private static void CheckTrainingAttendance(TrainingAttendance source, TrainingAttendancePackage destination)
        {
            Assert.Equal(source.Id, destination.Id);
            Assert.Equal(source.Reason, destination.Reason);
            Assert.Equal((int)source.Attendance, destination.Attendance);
            Assert.Equal(source.Comment, destination.Comment);
            Assert.Equal(source.Rating, destination.Rating);
            Assert.Equal(source.Player.Id, destination.PlayerId);
        }

        private static void CheckTrainingSession(TrainingSession source, TrainingSessionPackage destination)
        {
            Assert.Equal(source.Id, destination.Id);
            Assert.Equal(source.Start, destination.StartDate);
            Assert.Equal(source.End, destination.EndDate);
            Assert.Equal(source.IsCancelled, destination.IsCancelled);
            Assert.Equal(source.Theme, destination.Theme);
            Assert.Equal(source.Place, destination.Place);
            Assert.Equal(string.Join(";", source.TechnicalGoals), destination.TechnicalGoals);
            Assert.Equal(string.Join(";", source.TacticalGoals), destination.TacticalGoals);
            Assert.Equal(string.Join(";", source.PhysicalGoals), destination.PhysicalGoals);
            Assert.Equal(string.Join(";", source.MentalGoals), destination.MentalGoals);
            Assert.Equal(string.Join(";", source.Stages), destination.Stages);
            Assert.Equal(string.Join(";", source.TeamIds), destination.TeamIds);

            Assert.Equal(source.CreatedAt, destination.CreatedAt);
            Assert.Equal(source.CreatedBy, destination.CreatedBy);
            Assert.Equal(source.ModifiedAt, destination.ModifiedAt);
            Assert.Equal(source.ModifiedBy, destination.ModifiedBy);

            CheckCollection(source.Attendances, destination.Attendances!, CheckTrainingAttendance);
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
