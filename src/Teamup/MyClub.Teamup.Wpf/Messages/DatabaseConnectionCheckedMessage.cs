// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

namespace MyClub.Teamup.Wpf.Messages
{
    internal class DatabaseConnectionCheckedMessage(string name, string host, bool isSuccess)
    {
        public bool IsSuccess { get; } = isSuccess;

        public string Name { get; } = name;

        public string Host { get; } = host;
    }
}
