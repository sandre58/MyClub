// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using MyClub.Scorer.Domain.TeamAggregate;

namespace MyClub.Scorer.Domain.PersonAggregate
{
    public class Player : Person
    {
        public Player(Team team, string firstName, string lastName, Guid? id = null) : base(firstName, lastName, id) => Team = team;

        public Team Team { get; }
    }
}
