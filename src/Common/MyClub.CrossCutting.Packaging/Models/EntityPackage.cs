// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Xml.Serialization;

namespace MyClub.CrossCutting.Packaging.Models
{
    public class EntityPackage
    {
        [XmlAttribute("id")]
        public Guid Id { get; set; }
    }
}
