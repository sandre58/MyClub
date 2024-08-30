// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.


// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Xml.Serialization;

namespace MyClub.Scorer.Infrastructure.Packaging.Models
{
    public class ProjectPreferencesPackage
    {
        [XmlElement("TreatNoStadiumAsWarning")]
        public bool TreatNoStadiumAsWarning { get; set; }
    }
}
