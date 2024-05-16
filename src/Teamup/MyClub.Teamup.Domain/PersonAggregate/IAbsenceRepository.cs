// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using MyClub.Domain;

namespace MyClub.Teamup.Domain.PersonAggregate
{
    public interface IAbsenceRepository : IRepository<Absence>
    {
        Absence Insert(Player player, DateTime startDate, DateTime endDate, string label);
    }
}
