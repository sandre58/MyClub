﻿// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

namespace MyClub.Scorer.Wpf.Messages
{
    public class CurrentProjectSavedMessage
    {
        public string FilePath { get; }

        public CurrentProjectSavedMessage(string filePath) => FilePath = filePath;
    }
}
