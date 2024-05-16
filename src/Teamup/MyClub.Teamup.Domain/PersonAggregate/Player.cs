// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.ObjectModel;
using System.Linq;
using MyClub.Domain.Enums;
using MyClub.Domain.Exceptions;
using MyClub.Teamup.Domain.Enums;
using MyNet.Utilities;
using MyNet.Utilities.Sequences;

namespace MyClub.Teamup.Domain.PersonAggregate
{
    public class Player : Person
    {
        public static readonly AcceptableValueRange<int> AcceptableRangeAge = new(4, 110);
        public static readonly AcceptableValueRange<int> AcceptableRangeHeight = new(100, 230);
        public static readonly AcceptableValueRange<int> AcceptableRangeWeight = new(25, 130);

        private int? _height;
        private int? _weight;
        private readonly ObservableCollection<RatedPosition> _positions = [];
        private readonly ObservableCollection<Injury> _injuries = [];
        private readonly ObservableCollection<Absence> _absences = [];

        public Player(string firstName, string lastName, Guid? id = null)
            : base(firstName, lastName, id)
        {
            Positions = new(_positions);
            Injuries = new(_injuries);
            Absences = new(_absences);
        }

        public Laterality Laterality { get; set; } = Laterality.RightHander;

        public int? Height
        {
            get => _height;
            set => _height = AcceptableRangeHeight.ValidateOrThrow(value);
        }

        public int? Weight
        {
            get => _weight;
            set => _weight = AcceptableRangeWeight.ValidateOrThrow(value);
        }

        public ReadOnlyObservableCollection<Injury> Injuries { get; }

        public ReadOnlyObservableCollection<RatedPosition> Positions { get; }

        public ReadOnlyObservableCollection<Absence> Absences { get; }

        #region Absences

        public Absence AddAbsence(DateTime start, DateTime end, string label, AbsenceType type = AbsenceType.InHolidays)
            => AddAbsence(new Absence(start, end, label) { Type = type });

        public Absence AddAbsence(Absence playerAbsence)
        {
            if (_absences.Contains(playerAbsence))
                throw new AlreadyExistsException(nameof(Absences), playerAbsence);

            _absences.Add(playerAbsence);

            return playerAbsence;
        }

        public bool RemoveAbsence(Guid absenceId) => _absences.HasId(absenceId) && _absences.Remove(_absences.GetById(absenceId));

        public void ClearAbsences() => _absences.Clear();

        public bool IsAbsentAtDate(DateTime date) => Absences.Any(x => x.Period.Contains(date));

        #endregion

        public RatedPosition AddPosition(Position position, PositionRating rating = PositionRating.Natural, bool isNatural = false)
            => AddPosition(new RatedPosition(position, rating) { IsNatural = isNatural });

        public RatedPosition AddPosition(RatedPosition ratedPosition)
        {
            if (_positions.Any(x => x.Position == ratedPosition.Position))
                throw new AlreadyExistsException(nameof(Positions), ratedPosition.Position);

            _positions.Add(ratedPosition);

            return ratedPosition;
        }

        public bool RemovePosition(Position position)
            => _positions.Any(x => x.Position == position) && _positions.Remove(_positions.First(x => x.Position == position));

        public Injury AddInjury(DateTime date, string condition, InjurySeverity severity, DateTime? endDate = null, InjuryType type = InjuryType.Other, InjuryCategory category = InjuryCategory.Other, string? description = null)
        {
            var injury = new Injury(date, condition, severity, endDate, type, category)
            {
                Description = description
            };
            return AddInjury(injury);
        }

        public Injury AddInjury(Injury injury)
        {
            if (_injuries.Contains(injury))
                throw new AlreadyExistsException(nameof(Injuries), injury);

            _injuries.Add(injury);

            return injury;
        }

        public bool RemoveInjury(Guid injuryId) => _injuries.HasId(injuryId) && _injuries.Remove(_injuries.GetById(injuryId));

        public bool IsInjuredAtDate(DateTime date) => Injuries.Any(x => x.Period.Contains(date));
    }
}
