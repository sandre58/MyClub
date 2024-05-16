// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using MyClub.Domain;
using MyClub.Domain.Enums;
using MyClub.Domain.Exceptions;
using MyClub.Teamup.Domain.ClubAggregate;
using MyClub.Teamup.Domain.CompetitionAggregate;
using MyClub.Teamup.Domain.CycleAggregate;
using MyClub.Teamup.Domain.HolidaysAggregate;
using MyClub.Teamup.Domain.PersonAggregate;
using MyClub.Teamup.Domain.SeasonAggregate;
using MyClub.Teamup.Domain.SendedMailAggregate;
using MyClub.Teamup.Domain.SquadAggregate;
using MyClub.Teamup.Domain.TacticAggregate;
using MyClub.Teamup.Domain.TeamAggregate;
using MyClub.Teamup.Domain.TrainingAggregate;
using MyNet.Utilities;
using MyNet.Utilities.Exceptions;

namespace MyClub.Teamup.Domain.ProjectAggregate
{
    public sealed class Project : AuditableEntity, IAggregateRoot
    {
        private readonly ObservableCollection<SquadPlayer> _players = [];
        private readonly ObservableCollection<Tactic> _tactics = [];
        private readonly ObservableCollection<Holidays> _holidays = [];
        private readonly ObservableCollection<SendedMail> _sendedMails = [];
        private readonly ObservableCollection<Cycle> _cycles = [];
        private readonly ObservableCollection<TrainingSession> _trainingSessions = [];
        private readonly ObservableCollection<ICompetitionSeason> _competitions = [];

        public Project(string name,
                       Club club,
                       Category category,
                       Season season,
                       string color,
                       byte[]? image = null,
                       Guid? id = null) : base(id)
        {
            Name = name;
            Club = club;
            Category = category;
            Season = season;
            Color = color;
            Image = image;

            Players = new(_players);
            Holidays = new(_holidays);
            Tactics = new(_tactics);
            SendedMails = new(_sendedMails);
            Cycles = new(_cycles);
            TrainingSessions = new(_trainingSessions);
            Competitions = new(_competitions);
        }

        public Club Club { get; }

        public string Name { get; set; }

        public byte[]? Image { get; set; }

        public string Color { get; set; }

        public Season Season { get; }

        public Category Category { get; }

        public ProjectPreferences Preferences { get; set; } = new(new TimeSpan(17, 30, 0), new TimeSpan(1, 30, 0));

        public Team? MainTeam { get; set; }

        public ReadOnlyObservableCollection<SquadPlayer> Players { get; }

        public ReadOnlyObservableCollection<Holidays> Holidays { get; }

        public ReadOnlyObservableCollection<Tactic> Tactics { get; }

        public ReadOnlyObservableCollection<SendedMail> SendedMails { get; }

        public ReadOnlyObservableCollection<TrainingSession> TrainingSessions { get; }

        public ReadOnlyObservableCollection<Cycle> Cycles { get; }

        public ReadOnlyObservableCollection<ICompetitionSeason> Competitions { get; }

        #region Players

        public SquadPlayer AddPlayer(SquadPlayer player)
        {
            if (Players.Any(x => x.Player == player.Player))
                throw new AlreadyExistsException(nameof(Players), player);

            if (player.Team is not null && !Club.Teams.Any(x => x.Id == player.Team.Id))
                throw new NotFoundException(player.Team.Id);

            _players.Add(player);

            return player;
        }

        public SquadPlayer AddPlayer(Player player) => AddPlayer(new SquadPlayer(player));

        public bool RemovePlayer(SquadPlayer player) => _players.Remove(player);

        #endregion

        #region Teams

        public bool RemoveTeam(Team team, bool removePlayersOfSquad = true)
        {
            // Update players
            Players.Where(x => x.Team?.Id == team.Id).ToList().ForEach(x =>
            {
                if (removePlayersOfSquad)
                    RemovePlayer(x);
                else
                    x.Team = null;
            });

            // Update trainings
            TrainingSessions.ForEach(x => x.TeamIds.Remove(team.Id));
            TrainingSessions.Where(x => !x.TeamIds.Any()).ToList().ForEach(x => RemoveTrainingSession(x));

            var result = Club.RemoveTeam(team);

            if (Equals(MainTeam, team))
                MainTeam = null;

            return result;
        }

        #endregion

        #region Tactics

        public Tactic AddTactic(Tactic tactic)
        {
            _tactics.Add(tactic);

            return tactic;
        }

        public bool RemoveTactic(Tactic tactic) => _tactics.Remove(tactic);

        #endregion

        #region Holidays

        public Holidays AddHolidays(Holidays holidays)
        {
            _holidays.Add(holidays);

            return holidays;
        }

        public Holidays AddHolidays(DateTime startDate, DateTime endDate, string label)
            => AddHolidays(new Holidays(startDate, endDate, label));

        public bool RemoveHolidays(Holidays holidays) => _holidays.Remove(holidays);

        #endregion

        #region SendedMails

        public SendedMail AddSendedMail(SendedMail sendedMail)
        {
            _sendedMails.Add(sendedMail);

            return sendedMail;
        }

        public SendedMail AddSendedMail(DateTime date, string subject, string body, IEnumerable<string> addresses, bool sendACopy = false)
        {
            var sendedMail = new SendedMail(date, subject, body, sendACopy);
            sendedMail.ToAddresses.AddRange(addresses);

            return AddSendedMail(sendedMail);
        }

        public bool RemoveSendedMail(SendedMail sendedMail) => _sendedMails.Remove(sendedMail);

        #endregion

        #region Trainings

        public TrainingSession AddTrainingSession(TrainingSession session)
        {
            if (!Season.Period.Contains(session.Start))
                throw new OutOfRangeException(nameof(TrainingSession), Season.Period.Start, Season.Period.End);

            _trainingSessions.Add(session);

            return session;
        }

        public TrainingSession AddTrainingSession(DateTime date, TimeSpan duration, string? theme = null, string? place = null)
            => AddTrainingSession(new TrainingSession(date, date.AddFluentTimeSpan(duration)) { Theme = theme.OrEmpty(), Place = place });

        public bool RemoveTrainingSession(TrainingSession session) => _trainingSessions.Remove(session);

        #endregion

        #region Cycles

        public Cycle AddCycle(Cycle cycle)
        {
            _cycles.Add(cycle);

            return cycle;
        }

        public Cycle AddCycle(DateTime startDate, DateTime endDate, string label)
            => AddCycle(new Cycle(startDate, endDate, label));

        public bool RemoveCycle(Cycle cycle) => _cycles.Remove(cycle);

        #endregion

        #region Competitions

        public ICompetitionSeason AddCompetition(ICompetitionSeason competition)
        {
            if (Competitions.Contains(competition))
                throw new AlreadyExistsException(nameof(Competitions), competition);

            _competitions.Add(competition);

            return competition;
        }

        public bool RemoveCompetition(ICompetitionSeason competition) => _competitions.Remove(competition);

        #endregion

        public override string ToString() => Name;
    }
}
