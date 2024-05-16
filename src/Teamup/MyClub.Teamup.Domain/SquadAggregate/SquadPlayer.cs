// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.ObjectModel;
using System.Linq;
using MyClub.Domain;
using MyClub.Domain.Enums;
using MyClub.Domain.Exceptions;
using MyClub.Teamup.Domain.Enums;
using MyClub.Teamup.Domain.PersonAggregate;
using MyClub.Teamup.Domain.TeamAggregate;
using MyNet.Utilities.Sequences;

namespace MyClub.Teamup.Domain.SquadAggregate
{
    public class SquadPlayer : AuditableEntity
    {
        public static readonly AcceptableValueRange<int> AcceptableRangeShoesSize = new(25, 50);
        public static readonly AcceptableValueRange<int> AcceptableRangeNumber = new(1, 99);

        private int? _number;
        private int? _shoesSize;
        private readonly ObservableCollection<RatedPosition> _positions = [];

        public SquadPlayer(Player player, Guid? id = null) : base(id)
        {
            Player = player;
            Positions = new(_positions);
        }

        public Player Player { get; }

        public Category? Category { get; set; }

        public Team? Team { get; set; }

        public LicenseState LicenseState { get; set; }

        public bool IsMutation { get; set; }

        public DateTime? FromDate { get; set; }

        public string? Size { get; set; }

        public int? ShoesSize
        {
            get => _shoesSize;
            set => _shoesSize = AcceptableRangeShoesSize.ValidateOrThrow(value);
        }

        public int? Number
        {
            get => _number;
            set => _number = AcceptableRangeNumber.ValidateOrThrow(value);
        }

        public ReadOnlyObservableCollection<RatedPosition> Positions { get; }

        #region Positions

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

        public void ClearPositions() => _positions.Clear();

        #endregion

        public override string ToString() => Player.GetFullName();
    }
}
