// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Text;
using MyClub.Teamup.Domain.PersonAggregate;
using MyClub.Domain;
using MyClub.Domain.Enums;

namespace MyClub.Teamup.Domain.MatchAggregate
{
    public class PenaltyShootout : Entity
    {
        public PenaltyShootout(Player? taker = null, PenaltyShootoutResult result = PenaltyShootoutResult.None, Guid? id = null) : base(id)
        {
            Taker = taker;
            Result = result;
        }

        public Player? Taker { get; set; }

        public PenaltyShootoutResult Result { get; set; }

        public override string ToString()
        {
            var str = new StringBuilder();

            switch (Result)
            {
                case PenaltyShootoutResult.None:
                    str.Append('?');
                    break;
                case PenaltyShootoutResult.Succeeded:
                    str.Append('O');
                    break;
                case PenaltyShootoutResult.Failed:
                    str.Append('X');
                    break;
                default:
                    break;
            }

            if (Taker is not null)
                str.Append($" ({Taker})");

            return str.ToString();
        }
    }
}
