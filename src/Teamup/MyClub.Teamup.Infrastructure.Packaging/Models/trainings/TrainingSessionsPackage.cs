// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Xml.Serialization;

namespace MyClub.Teamup.Infrastructure.Packaging.Models
{
    [XmlRoot("trainingSessions", Namespace = XmlConstants.MyClubNamespace)]
    public class TrainingSessionsPackage : List<TrainingSessionPackage>
    {
        public TrainingSessionsPackage() { }

        public TrainingSessionsPackage(IEnumerable<TrainingSessionPackage> enumerable) : base(enumerable)
        {
        }
    }
}
