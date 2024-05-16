// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using MyClub.Domain;
using MyClub.Teamup.Domain.Enums;
using MyClub.Teamup.Domain.PersonAggregate;

namespace MyClub.Teamup.Domain.SquadAggregate
{
    public class SquadManager : AuditableEntity
    {
        public SquadManager(Person person, Guid? id = null) : base(id) => Person = person;

        public Person Person { get; }

        public ManagerRole Role { get; set; }

        public override string ToString() => Person.GetFullName();
    }
}
