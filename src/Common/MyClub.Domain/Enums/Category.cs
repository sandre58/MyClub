// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using MyNet.Utilities;

namespace MyClub.Domain.Enums
{
    public class Category : Enumeration<Category>, ISimilar<Category>
    {
        private static int _value = 1;

        public static Category? FromBirthdate(DateTime birthdate) => Enumeration.GetAll<Category>().OrderBy(x => x.Age).ThenByDescending(x => x).FirstOrDefault(x => birthdate.GetAge() <= x.Age);

        public static readonly Category U6 = new(nameof(U6), 5);
        public static readonly Category U7 = new(nameof(U7), 6);
        public static readonly Category U8 = new(nameof(U8), 7);
        public static readonly Category U9 = new(nameof(U9), 8);
        public static readonly Category U10 = new(nameof(U10), 9);
        public static readonly Category U11 = new(nameof(U11), 10);
        public static readonly Category U12 = new(nameof(U12), 11);
        public static readonly Category U13 = new(nameof(U13), 12);
        public static readonly Category U14 = new(nameof(U14), 13);
        public static readonly Category U15 = new(nameof(U15), 14);
        public static readonly Category U16 = new(nameof(U16), 15);
        public static readonly Category U17 = new(nameof(U17), 16);
        public static readonly Category U18 = new(nameof(U18), 17);
        public static readonly Category U19 = new(nameof(U19), 18);
        public static readonly Category U20 = new(nameof(U20), 19);
        public static readonly Category U21 = new(nameof(U21), 20);
        public static readonly Category U22 = new(nameof(U22), 21);
        public static readonly Category U23 = new(nameof(U23), 22);
        public static readonly Category Adult = new(nameof(Adult), 21);
        public static readonly Category Seniors = new(nameof(Seniors), 35);

        public int Age { get; }

        private Category(string name, int age)
            : base(name, _value, $"{nameof(Category)}{name}")
        {
            Age = age;

            IncrementValue();
        }

        private static void IncrementValue() => _value++;

        public bool IsSimilar(Category? obj) => obj is not null && obj.Age == Age;
    }
}
