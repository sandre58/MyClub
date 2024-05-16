// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;

namespace MyClub.Teamup.Domain.TrainingAggregate
{
    public class TrainingSessionTemplate : TrainingSessionBase
    {
        public TrainingSessionTemplate(string theme, Guid? id = null) : base(theme, id) { }
    }
}
