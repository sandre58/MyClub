// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using MyClub.CrossCutting.Localization;
using MyClub.Teamup.Domain.Enums;
using MyClub.Teamup.Domain.Extensions;
using MyClub.Teamup.Domain.SquadAggregate;
using MyClub.Teamup.Domain.TeamAggregate;
using MyClub.Teamup.Domain.TrainingAggregate;
using MyNet.Utilities;
using MyNet.Utilities.Generator;
using MyNet.Utilities.Helpers;

namespace MyClub.Teamup.Domain.Randomize
{
    public static class TrainingSessionRandomFactory
    {
        public static IEnumerable<TrainingSession> RandomTrainingSessions(DateTime seasonStartDate, DateTime seasonEndDate, IEnumerable<Team> teams, IEnumerable<SquadPlayer>? players = null, int minDays = 1, int maxDays = 4)
        {
            if (!teams.Any()) return [];

            var trainingSessionDates = GetDates(seasonStartDate, seasonEndDate, minDays, maxDays);
            return trainingSessionDates.Select(x => Random(teams, x, players));
        }

        public static TrainingSession Random(IEnumerable<Team> teams, DateTime? date = null, IEnumerable<SquadPlayer>? players = null)
        {
            var startDateTime = (date ?? RandomGenerator.Date(DateTime.Today.BeginningOfYear(), DateTime.Today.EndOfYear())).DiscardTime().IncreaseTime(new TimeSpan(17, 0, 0)).ToUniversalTime();
            var endDateTime = startDateTime.IncreaseTime(new TimeSpan(1, 30, 0)).ToUniversalTime();
            var item = new TrainingSession(startDateTime, endDateTime)
            {
                Place = string.Empty,
                Theme = SentenceGenerator.Words(3, 5).ToSentence()
            };

            var squadIds = teams.Any() ? RandomGenerator.ListItems(teams.Select(x => x.Id).ToList(), RandomGenerator.Number(1, teams.Count())) : [Guid.NewGuid()];
            item.TeamIds.AddRange(squadIds);
            item.Stages.Add(RandomGenerator.ArrayElement(MyClubResources.TrainingStagesList.Split(";")));
            EnumerableHelper.Iteration(RandomGenerator.Int(1, 4), _ => item.TechnicalGoals.Add(SentenceGenerator.Sentence(3, 5)));
            EnumerableHelper.Iteration(RandomGenerator.Int(1, 4), _ => item.TacticalGoals.Add(SentenceGenerator.Sentence(3, 5)));
            EnumerableHelper.Iteration(RandomGenerator.Int(1, 4), _ => item.PhysicalGoals.Add(SentenceGenerator.Sentence(3, 5)));
            EnumerableHelper.Iteration(RandomGenerator.Int(1, 4), _ => item.MentalGoals.Add(SentenceGenerator.Sentence(3, 5)));

            if (players is not null && item.End < DateTime.Today)
                AddAttendances(item, players.Where(y => y.Team is not null && item.TeamIds.Contains(y.Team.Id)).ToList());

            item.MarkedAsCreated(DateTime.UtcNow, MyClubResources.System);

            return item;
        }

        private static void AddAttendances(TrainingSession trainingSession, IEnumerable<SquadPlayer> players)
        {
            var countPlayers = players.Count();
            var countPresents = Math.Ceiling(countPlayers * RandomGenerator.Double(0.7));
            var countAbsents = countPlayers - countPresents;

            var attendances = new List<Attendance>();
            for (var i = 0; i < countPresents; i++)
                attendances.Add(Attendance.Present);

            for (var i = 0; i < countAbsents; i++)
                attendances.Add(RandomGenerator.Enum(Attendance.Unknown, Attendance.Present));

            var randomAttendances = RandomGenerator.Shuffle(attendances).ToList();

            var idx = 0;
            foreach (var player in players.Select(x => x.Player))
            {
                var absence = player.Absences.LastOrDefault(x => x.Period.Contains(trainingSession.Start));
                var attendance = absence is not null
                    ? absence.Type.ToAttendance()
                    : player.IsInjuredAtDate(trainingSession.Start)
                    ? Attendance.Injured
                    : randomAttendances[idx];
                double? rating = attendance == Attendance.Present ? Math.Round(RandomGenerator.Double(1.0, 10.0) * 2, MidpointRounding.AwayFromZero) / 2 : null;
                var reason = attendance == Attendance.Apology ? absence?.Label ?? SentenceGenerator.Sentence(5, 10) : string.Empty;
                var trainingAttendance = new TrainingAttendance(player, attendance, rating)
                {
                    Reason = reason,
                    Comment = SentenceGenerator.Paragraph(5, 25, 1, 5)
                };

                trainingSession.AddAttendance(trainingAttendance);
                idx++;
            }
        }

        public static TrainingAttendance RandomAttendance(SquadPlayer player)
        {
            var attendance = RandomGenerator.Enum<Attendance>();
            double? rating = attendance == Attendance.Present ? Math.Round(RandomGenerator.Double(1.0, 10.0) * 2, MidpointRounding.AwayFromZero) / 2 : null;
            var reason = attendance == Attendance.Apology ? SentenceGenerator.Sentence(5, 10) : string.Empty;
            return new(player.Player, attendance, rating)
            {
                Reason = reason,
                Comment = SentenceGenerator.Paragraph(5, 25, 1, 5)
            };
        }

        private static List<DateTime> GetDates(DateTime startDate, DateTime endDate, int minDays = 1, int maxDays = 4)
        {
            var dates = new List<DateTime>();
            var days = new List<DayOfWeek>();
            var nbDays = RandomGenerator.Int(minDays, maxDays);
            for (var i = 0; i < nbDays; i++)
                days.Add(RandomGenerator.Enum(days.ToArray()));

            for (var date = startDate; date < endDate; date = date.AddDays(1))
            {
                if (days.Contains(date.DayOfWeek))
                    dates.Add(date);
            }

            return dates;
        }
    }
}
