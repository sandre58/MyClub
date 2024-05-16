// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using MyClub.Domain;
using MyClub.Domain.Enums;

namespace MyClub.Teamup.Domain.PersonAggregate
{
    public interface IInjuryRepository : IRepository<Injury>
    {
        Injury Insert(Player player, DateTime date, string condition, InjurySeverity severity);
    }
}
