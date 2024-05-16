// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using MyClub.Application.Dtos;
using MyClub.Teamup.Domain.Enums;

namespace MyClub.Teamup.Application.Dtos
{
    public class SendedMailDto : EntityDto
    {
        public string? Subject { get; set; }

        public string? Body { get; set; }

        public bool SendACopy { get; set; }

        public DateTime Date { get; set; }

        public SendingState State { get; set; }

        public List<string>? ToAddresses { get; set; }
    }
}
