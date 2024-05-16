// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.ObjectModel;
using MyClub.Domain;
using MyClub.Teamup.Domain.Enums;
using MyNet.Utilities.Extensions;

namespace MyClub.Teamup.Domain.SendedMailAggregate
{
    public class SendedMail : AuditableEntity, IAggregateRoot
    {
        private string _subject = null!;
        private DateTime _date;

        public SendedMail(DateTime date, string subject, string? body, bool sendACopy = false, Guid? id = null) : base(id)
        {
            Date = date;
            Subject = subject;
            Body = body;
            SendACopy = sendACopy;
        }

        public string Subject
        {
            get => _subject;
            set => _subject = value.IsRequiredOrThrow();
        }

        public string? Body { get; set; }

        public bool SendACopy { get; set; }

        public DateTime Date
        {
            get => _date;
            set => _date = value.IsRequiredOrThrow().IsInPastOrThrow();
        }

        public SendingState State { get; set; } = SendingState.Draft;

        public ObservableCollection<string> ToAddresses { get; } = [];

        public override string? ToString() => Subject;
    }
}
