// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Xml.Serialization;

namespace MyClub.Teamup.Infrastructure.Packaging.Models
{
    [XmlType("preferences")]
    public class ProjectPreferencesPackage
    {
        [XmlElement("trainingStartTime")]
        public TimeSpan TrainingStartTime { get; set; }

        [XmlElement("trainingDuration")]
        public TimeSpan TrainingDuration { get; set; }
    }
}
