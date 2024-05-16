// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.ObjectModel;
using System.Linq;
using MyClub.Domain;
using MyClub.Domain.Enums;
using MyClub.Domain.Exceptions;

namespace MyClub.Teamup.Domain.TacticAggregate
{
    public class Tactic : LabelEntity, IAggregateRoot
    {
        private readonly ObservableCollection<TacticPosition> _positions = [];

        public Tactic(string label, Guid? id = null) : base(label, id) => Positions = new(_positions);

        public ReadOnlyObservableCollection<TacticPosition> Positions { get; }

        public ObservableCollection<string> Instructions { get; } = [];

        public TacticPosition AddPosition(Position position, int? defaultNumber = null) => AddPosition(new TacticPosition(position) { Number = defaultNumber });

        public TacticPosition AddPosition(TacticPosition tacticPosition)
        {
            if (_positions.Any(x => x.Position == tacticPosition.Position))
                throw new AlreadyExistsException(nameof(Positions), tacticPosition.Position);

            _positions.Add(tacticPosition);

            return tacticPosition;
        }

        public bool RemovePosition(Position position)
            => _positions.Any(x => x.Position == position) && _positions.Remove(_positions.First(x => x.Position == position));
    }
}
