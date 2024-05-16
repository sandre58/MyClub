// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using MyClub.Domain.Enums;
using MyClub.Domain.Exceptions;
using MyClub.Teamup.Domain.PersonAggregate;
using MyClub.Teamup.Domain.Randomize;
using MyNet.Utilities.Exceptions;
using Xunit;

namespace MyClub.UnitTests.Teamup.Domain.PlayerAggregate
{
    public class PlayerTests
    {
        private static Player CreateValidPlayer(Action<Player>? setProperties = null)
        {
            var item = new Player("FirstName", "LastName");
            setProperties?.Invoke(item);

            return item;
        }

        private static Absence AddValidAbsence(Player player) => player.AddAbsence(PlayerRandomFactory.RandomAbsence());

        private static Injury AddValidInjury(Player player) => player.AddInjury(PlayerRandomFactory.RandomInjury());

        [Fact]
        public void ThrowExceptionWhenPropertiesIsInvalid()
        {
            Assert.Throws<NullOrEmptyException>(() => _ = new Player("", "LastName"));
            Assert.Throws<NullOrEmptyException>(() => _ = new Player("FirstName", ""));
            Assert.Throws<FutureDateException>(() => _ = CreateValidPlayer(x => x.Birthdate = DateTime.UtcNow.AddDays(1)));
            Assert.Throws<IsNotUpperOrEqualsThanException>(() => _ = CreateValidPlayer(x => x.Height = 99));
            Assert.Throws<IsNotLowerOrEqualsThanException>(() => _ = CreateValidPlayer(x => x.Height = 231));
            Assert.Throws<IsNotUpperOrEqualsThanException>(() => _ = CreateValidPlayer(x => x.Weight = 24));
            Assert.Throws<IsNotLowerOrEqualsThanException>(() => _ = CreateValidPlayer(x => x.Weight = 131));
        }

        [Fact]
        public void PhoneAddedWhenAddPhoneValid()
        {
            var player = CreateValidPlayer();
            var phone = player.AddPhone("00 00 00 00 00");

            Assert.Single(player.Phones);
            Assert.Contains(phone, player.Phones);
        }

        [Fact]
        public void EmailAddedWhenAddEmailValid()
        {
            var player = CreateValidPlayer();
            var email = player.AddEmail("test@test.fr");

            Assert.Single(player.Emails);
            Assert.Contains(email, player.Emails);
        }

        [Fact]
        public void ThrowExceptionWhenAddExistingPhone()
        {
            var player = CreateValidPlayer();
            var phone = player.AddPhone("00 00 00 00 00");

            Assert.Single(player.Phones);
            Assert.Contains(phone, player.Phones);
            Assert.Throws<AlreadyExistsException>(() => _ = player.AddPhone("00 00 00 00 00"));
        }

        [Fact]
        public void ThrowExceptionWhenAddExistingEmail()
        {
            var player = CreateValidPlayer();
            var email = player.AddEmail("test@test.fr");

            Assert.Single(player.Emails);
            Assert.Contains(email, player.Emails);
            Assert.Throws<AlreadyExistsException>(() => _ = player.AddEmail("test@test.fr"));
        }

        [Fact]
        public void PhoneRemovedWhenRemoveExistingPhone()
        {
            var player = CreateValidPlayer();
            var phone = player.AddPhone("00 00 00 00 00");

            Assert.Single(player.Phones);
            Assert.Contains(phone, player.Phones);

            Assert.True(player.RemovePhone("00 00 00 00 00"));
            Assert.Empty(player.Phones);
        }

        [Fact]
        public void EmailRemovedWhenRemoveExistingEmail()
        {
            var player = CreateValidPlayer();
            var email = player.AddEmail("test@test.fr");

            Assert.Single(player.Emails);
            Assert.Contains(email, player.Emails);

            Assert.True(player.RemoveEmail("test@test.fr"));
            Assert.Empty(player.Emails);
        }

        [Fact]
        public void PhoneNotRemovedWhenRemoveNotExistingPhone()
        {
            var player = CreateValidPlayer();
            var phone = player.AddPhone("00 00 00 00 00");

            Assert.Single(player.Phones);
            Assert.Contains(phone, player.Phones);

            Assert.False(player.RemovePhone("01 00 00 00 00"));
            Assert.Single(player.Phones);
        }

        [Fact]
        public void EmailNotRemovedWhenRemoveNotExistingEmail()
        {
            var player = CreateValidPlayer();
            var email = player.AddEmail("test@test.fr");

            Assert.Single(player.Emails);
            Assert.Contains(email, player.Emails);

            Assert.False(player.RemoveEmail("testa@test.fr"));
            Assert.Single(player.Emails);
        }

        [Fact]
        public void PositionAddedWhenAddPositionValid()
        {
            var player = CreateValidPlayer();
            var position = player.AddPosition(Position.CenterAttackingMidfielder);

            Assert.Single(player.Positions);
            Assert.Contains(position, player.Positions);
        }

        [Fact]
        public void ThrowExceptionWhenAddExistingPosition()
        {
            var player = CreateValidPlayer();
            var position = player.AddPosition(Position.CenterAttackingMidfielder);

            Assert.Single(player.Positions);
            Assert.Contains(position, player.Positions);
            Assert.Throws<AlreadyExistsException>(() => _ = player.AddPosition(Position.CenterAttackingMidfielder));
        }

        [Fact]
        public void PositionRemovedWhenRemoveExistingPosition()
        {
            var player = CreateValidPlayer();
            var position = player.AddPosition(Position.CenterAttackingMidfielder);

            Assert.Single(player.Positions);
            Assert.Contains(position, player.Positions);

            Assert.True(player.RemovePosition(Position.CenterAttackingMidfielder));
            Assert.Empty(player.Positions);
        }

        [Fact]
        public void PositionNotRemovedWhenRemoveNotExistingPosition()
        {
            var player = CreateValidPlayer();
            var position = player.AddPosition(Position.CenterAttackingMidfielder);

            Assert.Single(player.Positions);
            Assert.Contains(position, player.Positions);

            Assert.False(player.RemovePosition(Position.CenterMidfielder));
            Assert.Single(player.Positions);
        }

        [Fact]
        public void AbsenceAddedWhenAddAbsenceValid()
        {
            var player = CreateValidPlayer();
            var absence = AddValidAbsence(player);

            Assert.Single(player.Absences);
            Assert.Contains(absence, player.Absences);
        }

        [Fact]
        public void ThrowExceptionWhenAddExistingAbsence()
        {
            var player = CreateValidPlayer();
            var absence = AddValidAbsence(player);

            Assert.Single(player.Absences);
            Assert.Contains(absence, player.Absences);
            Assert.Throws<AlreadyExistsException>(() => _ = player.AddAbsence(absence));
        }

        [Fact]
        public void AbsenceRemovedWhenRemoveExistingAbsence()
        {
            var player = CreateValidPlayer();
            var absence = AddValidAbsence(player);

            Assert.Single(player.Absences);
            Assert.Contains(absence, player.Absences);

            Assert.True(player.RemoveAbsence(absence.Id));
            Assert.Empty(player.Absences);
        }

        [Fact]
        public void InjuryAddedWhenAddInjuryValid()
        {
            var player = CreateValidPlayer();
            var injury = AddValidInjury(player);

            Assert.Single(player.Injuries);
            Assert.Contains(injury, player.Injuries);
        }

        [Fact]
        public void ThrowExceptionWhenAddExistingInjury()
        {
            var player = CreateValidPlayer();
            var injury = AddValidInjury(player);

            Assert.Single(player.Injuries);
            Assert.Contains(injury, player.Injuries);
            Assert.Throws<AlreadyExistsException>(() => _ = player.AddInjury(injury));
        }

        [Fact]
        public void InjuryRemovedWhenRemoveExistingInjury()
        {
            var player = CreateValidPlayer();
            var injury = AddValidInjury(player);

            Assert.Single(player.Injuries);
            Assert.Contains(injury, player.Injuries);

            Assert.True(player.RemoveInjury(injury.Id));
            Assert.Empty(player.Injuries);
        }

        [Fact]
        public void IsInjuredWhenDateIsInInjuredPeriod()
        {
            var player = CreateValidPlayer();
            var injury = AddValidInjury(player);
            injury.Period.SetInterval(DateTime.Today.AddDays(-10), DateTime.Today.AddDays(20));

            Assert.True(player.IsInjuredAtDate(DateTime.Today));
        }

        [Fact]
        public void IsNotInjuredWhenDateIsNotInInjuredPeriod()
        {
            var player = CreateValidPlayer();
            var injury = AddValidInjury(player);
            injury.Period.SetInterval(DateTime.Today.AddDays(-50), DateTime.Today.AddDays(-5));

            Assert.False(player.IsInjuredAtDate(DateTime.Today));
        }

        [Fact]
        public void IsAbsentWhenDateIsInAbsencePeriod()
        {
            var player = CreateValidPlayer();
            var absence = AddValidAbsence(player);
            absence.Period.SetInterval(DateTime.Today.AddDays(-10), DateTime.Today.AddDays(20));

            Assert.True(player.IsAbsentAtDate(DateTime.Today));
        }

        [Fact]
        public void IsNotAbsentWhenDateIsNotInAbsencePeriod()
        {
            var player = CreateValidPlayer();
            _ = AddValidAbsence(player);

            Assert.False(player.IsAbsentAtDate(DateTime.Today.AddDays(40)));
        }
    }
}
