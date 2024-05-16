// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using MyNet.Utilities;

namespace MyClub.DatabaseContext.Domain.StadiumAggregate
{
    public class Stadium : Entity
    {
        public Stadium(string name, string ground)
        {
            Name = name;
            Ground = ground;
        }

        public Stadium(Guid id, string name, string ground) : base(id)
        {
            Name = name;
            Ground = ground;
        }

        public string Name { get; set; }

        public string Ground { get; set; }

        public string? Street { get; set; }

        public string? PostalCode { get; set; }

        public string? City { get; set; }

        public string? Country { get; set; }

        public double? Latitude { get; set; }

        public double? Longitude { get; set; }

        public override string ToString() => string.Join(", ", new[] { Name, City }.NotNull());

        public override bool IsSimilar(object? obj) => obj is Stadium other && Name == other.Name && City == other.City;

        public override void SetFrom(object? from)
        {
            if (from is Stadium stadium)
            {
                Street = stadium.Street;
                PostalCode = stadium.PostalCode;
                City = stadium.City;
                Country = stadium.Country;
                Latitude = stadium.Latitude;
                Longitude = stadium.Longitude;
            }
        }
    }
}
