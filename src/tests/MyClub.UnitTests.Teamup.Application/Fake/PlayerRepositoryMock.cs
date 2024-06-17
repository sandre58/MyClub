// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using DocumentFormat.OpenXml.Spreadsheet;
using MyClub.Teamup.Domain.SquadAggregate;
using MyNet.Utilities;

namespace MyClub.UnitTests.Teamup.Application.Fake
{
    internal class PlayerRepositoryMock : ISquadPlayerRepository
    {
        private readonly List<SquadPlayer> _players = [];

        public ReadOnlyCollection<SquadPlayer> Players => _players.AsReadOnly();

        public SquadPlayer Insert(SquadPlayer item)
        {
            _players.Add(item);
            return item;
        }

        public IEnumerable<SquadPlayer> GetAll() => _players;

        public SquadPlayer? GetById(Guid id) => _players.Find(x => id == x.Id);

        public bool Remove(Guid id) => _players.Remove(GetById(id)!);

        public void Save() => throw new NotImplementedException();

        public SquadPlayer Update(SquadPlayer item)
        {
            var oldItem = GetById(item.Id)!;
            oldItem.Player.FirstName = item.Player.FirstName;
            oldItem.Player.LastName = item.Player.LastName;
            return oldItem;
        }

        public SquadPlayer GetByPlayerId(Guid playerId) => _players.First(x => x.Player.Id == playerId);

        public IEnumerable<SquadPlayer> InsertRange(IEnumerable<SquadPlayer> items) => items.Select(Insert);

        public IEnumerable<SquadPlayer> UpdateRange(IEnumerable<SquadPlayer> items) => items.Select(Update);

        public int RemoveRange(IEnumerable<Guid> ids) => ids.Count(Remove);
    }
}
