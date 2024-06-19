// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using MyNet.Utilities;
using MyNet.Utilities.Extensions;
using MyClub.Domain;
using MyClub.Scorer.Domain.MatchAggregate;
using MyClub.Scorer.Domain.RankingAggregate;

namespace MyClub.Scorer.Domain.CompetitionAggregate
{
    public class Group : Championship, IOrderable
    {
        private string _name = string.Empty;
        private string _shortName = string.Empty;
        private readonly GroupStage _parent;

        internal Group(GroupStage parent, string name, string shortName, Guid? id = null) : base(id)
        {
            _parent = parent;
            Name = name;
            ShortName = shortName ?? name.GetInitials();
        }

        public string Name
        {
            get => _name;
            set => _name = value.IsRequiredOrThrow();
        }

        public string ShortName
        {
            get => _shortName;
            set => _shortName = value.IsRequiredOrThrow();
        }

        public int Order { get; set; }

        public override IEnumerable<Match> GetAllMatches() => _parent.GetAllMatches().Where(x => Teams.Contains(x.HomeTeam) && Teams.Contains(x.AwayTeam));

        public override RankingRules GetRankingRules() => _parent.RankingRules;

        public override string ToString() => Name;

        public override int CompareTo(object? obj) => obj is Group other ? string.Compare(Name, other.Name, StringComparison.OrdinalIgnoreCase) : 1;

        public virtual bool IsSimilar(object? obj) => obj is Group other && Name.Equals(other.Name, StringComparison.OrdinalIgnoreCase);
    }
}

