// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;

namespace MyClub.Teamup.Domain.PersonAggregate
{
    public class Manager : Person
    {
        public Manager(string firstName, string lastName, Guid? id = null)
            : base(firstName, lastName, id)
        {
        }
    }
}
