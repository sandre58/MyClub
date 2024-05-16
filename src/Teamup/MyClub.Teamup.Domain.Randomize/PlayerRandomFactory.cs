// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using MyClub.CrossCutting.Localization;
using MyClub.Domain;
using MyClub.Domain.Enums;
using MyClub.Teamup.Domain.Enums;
using MyClub.Teamup.Domain.Extensions;
using MyClub.Teamup.Domain.Factories.Extensions;
using MyClub.Teamup.Domain.PersonAggregate;
using MyClub.Teamup.Domain.SeasonAggregate;
using MyClub.Teamup.Domain.SquadAggregate;
using MyClub.Teamup.Domain.TeamAggregate;
using MyNet.Utilities;
using MyNet.Utilities.DateTimes;
using MyNet.Utilities.Generator;
using MyNet.Utilities.Generator.Extensions;
using MyNet.Utilities.Geography;

namespace MyClub.Teamup.Domain.Randomize
{
    public static class PlayerRandomFactory
    {
        private static readonly string[] DefaultPhoneLabels = MyClubResources.EmailLabels.Split(";");
        private static readonly string[] DefaultEmailLabels = MyClubResources.PhoneLabels.Split(";");

        public static IEnumerable<SquadPlayer> RandomSquadPlayers(Category? category = null, Team? team = null, int min = 15, int max = 20)
            => Enumerable.Range(1, RandomGenerator.Int(min, max)).Select(_ => RandomSquadPlayer(Random(), category, team));

        public static Player Random()
        {
            var year = RandomGenerator.Int(1970, 2013);
            var gender = RandomGenerator.Enum<GenderType>();
            var birthdate = new DateTime(year, RandomGenerator.Int(1, 12), RandomGenerator.Int(1, 27), 0, 0, 0, DateTimeKind.Utc);

            var item = new Player(NameGenerator.FirstName(gender == GenderType.Male ? GenderType.Male : GenderType.Female).ToSentence(), NameGenerator.LastName().ToSentence())
            {
                Address = new Address(
                    AddressGenerator.Street(),
                    AddressGenerator.PostalCode(),
                    AddressGenerator.City().ToSentence(),
                    RandomGenerator.Country(),
                    AddressGenerator.Coordinates().Latitude,
                    AddressGenerator.Coordinates().Longitude),
                Country = RandomGenerator.Country(),
                Category = RandomGenerator.ListItem(Enumeration.GetAll<Category>()),
                Birthdate = birthdate,
                Gender = gender,
                Laterality = RandomGenerator.Enum<Laterality>(),
                PlaceOfBirth = AddressGenerator.City().ToSentence(),
                LicenseNumber = string.Join("", RandomGenerator.Digits(10)),
                Description = string.Join(Environment.NewLine, SentenceGenerator.Paragraphs(10, 50, 1, 4, 1, 2)),
                Height = RandomGenerator.Number(150, 210),
                Weight = RandomGenerator.Number(50, 110),
            };

            RandomPhones().ForEach(x => item.AddPhone(x));
            RandomEmails().ForEach(x => item.AddEmail(x));
            RandomInjuries().ForEach(x => item.AddInjury(x));
            RandomPositions().ForEach(x => item.AddPosition(x));
            RandomAbsences().ForEach(x => item.AddAbsence(x));
            item.MarkedAsCreated(DateTime.UtcNow, MyClubResources.System);

            return item;
        }

        public static SquadPlayer RandomSquadPlayer(Player player, Category? category = null, Team? team = null)
        {
            var year = RandomGenerator.Int(1970, 2013);

            var item = new SquadPlayer(player)
            {
                FromDate = new DateTime(RandomGenerator.Int(year + 6, DateTime.Today.Year - 1), 8, 1, 0, 0, 0, DateTimeKind.Utc),
                Size = RandomGenerator.ArrayElement(MyClubResources.SizesList.Split(';')),
                IsMutation = RandomGenerator.Bool(),
                LicenseState = RandomGenerator.Enum<LicenseState>(),
                Category = category,
                Team = team,
                ShoesSize = RandomGenerator.Number(35, 46),
                Number = RandomGenerator.Number(1, 99)
            };

            player.Positions.ForEach(x => item.AddPosition(x));
            item.MarkedAsCreated(DateTime.UtcNow, MyClubResources.System);

            return item;
        }

        public static IEnumerable<Phone> RandomPhones(int min = 0, int max = 3) => Enumerable.Range(1, RandomGenerator.Int(min, max)).Select(_ => RandomPhone());

        public static IEnumerable<Email> RandomEmails(int min = 0, int max = 3) => Enumerable.Range(1, RandomGenerator.Int(min, max)).Select(_ => RandomEmail());

        public static IEnumerable<Injury> RandomInjuries(DateTime? minDate = null, int min = 0, int max = 3) => Enumerable.Range(1, RandomGenerator.Int(min, max)).Select(_ => RandomInjury(minDate));

        public static IEnumerable<Absence> RandomAbsences(Period? period = null, int min = 0, int max = 3) => Enumerable.Range(1, RandomGenerator.Int(0, 5)).Select(_ => RandomAbsence(period));

        public static IEnumerable<RatedPosition> RandomPositions(int min = 0, int max = 3)
        {
            var positions = RandomGenerator.ListItems(Position.GetPlayerPositions().ToList(), RandomGenerator.Int(min, max));

            if (positions.Count == 0) return [];

            var ratedPositions = new List<RatedPosition> { new(positions[0], PositionRating.Natural) { IsNatural = true } };

            if (positions.Count > 1)
            {
                for (var i = 1; i < positions.Count; i++)
                {
                    ratedPositions.Add(new(positions[i], RandomGenerator.Enum(PositionRating.Inefficient, PositionRating.Natural)));
                }
            }

            return ratedPositions;
        }

        public static Phone RandomPhone()
            => new(RandomGenerator.PhoneNumber(), RandomGenerator.ArrayElement(DefaultPhoneLabels), RandomGenerator.Bool());

        public static Email RandomEmail()
            => new(InternetGenerator.FreeEmail(), RandomGenerator.ArrayElement(DefaultEmailLabels), RandomGenerator.Bool());

        public static Injury RandomInjury(DateTime? minDate = null)
        {
            var type = RandomGenerator.Enum<InjuryType>();
            var duration = RandomGenerator.Int(3, 150);
            var severity = InjurySeverity.Severe;
            var date = RandomGenerator.Date(minDate ?? DateTime.Today.AddYears(-2), DateTime.Today);

            switch (duration)
            {
                case < 10:
                    severity = InjurySeverity.Minor;
                    break;
                case < 20:
                    severity = InjurySeverity.Moderate;
                    break;
                case < 45:
                    severity = InjurySeverity.Serious;
                    break;
                default:
                    break;
            }

            var item = new Injury(date, type == InjuryType.Other ? MyClubResources.UnkownReason : type.GetDefaultCondition(), severity, date.AddDays(duration), type, type.ToDefaultCategory())
            {
                Description = SentenceGenerator.Paragraph(10, 2)
            };
            item.MarkedAsCreated(DateTime.UtcNow, MyClubResources.System);

            return item;
        }

        public static RatedPosition RandomPosition(bool isNatural = false)
            => new(RandomGenerator.ListItem(Position.GetPlayerPositions().ToList()), isNatural ? PositionRating.Natural : RandomGenerator.Enum(PositionRating.Inefficient, PositionRating.Natural))
            {
                IsNatural = isNatural
            };

        public static Absence RandomAbsence(Period? period = null)
        {
            var absencePeriod = period ?? Season.CurrentYear.Period;
            var type = RandomGenerator.Enum<AbsenceType>();
            var startDate = RandomGenerator.Date(absencePeriod.Start, absencePeriod.End).BeginningOfDay();
            var daysDuration = RandomGenerator.Int(0, 15);
            var endDate = startDate.AddDays(daysDuration).EndOfDay();
            var item = new Absence(startDate, endDate < absencePeriod.End ? endDate : absencePeriod.End, type.GetDefaultLabel())
            {
                Type = type
            };

            item.MarkedAsCreated(DateTime.UtcNow, MyClubResources.System);

            return item;
        }
    }
}
