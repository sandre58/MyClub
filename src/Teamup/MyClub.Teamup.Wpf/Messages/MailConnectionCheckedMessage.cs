// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

namespace MyClub.Teamup.Wpf.Messages
{
    internal class MailConnectionCheckedMessage(bool isSuccess)
    {
        public bool IsSuccess { get; } = isSuccess;
    }
}
