// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System;
using MyNet.Utilities.Logging;

namespace MyClub.Application.Helpers
{
    public static class LogHelper
    {
        public enum ChangeAction
        {
            Add,

            Update,

            Removing,

            Removed
        }

        private static readonly IDictionary<TraceLevel, Action<string>> GroupLogAction = new Dictionary<TraceLevel, Action<string>>
        {
            [TraceLevel.Trace] = x => LogManager.Trace(x),
            [TraceLevel.Debug] = x => LogManager.Debug(x),
            [TraceLevel.Info] = x => LogManager.Info(x),
        };

        public static void LogChangeAction(ChangeAction action, object item, TraceLevel traceLevel = TraceLevel.Info)
        {
            switch (action)
            {
                case ChangeAction.Add:
                    GroupLogAction[traceLevel]($"Item Added : {item}");
                    break;
                case ChangeAction.Removed:
                    GroupLogAction[traceLevel]($"Item Removed : {item}");
                    break;
                case ChangeAction.Update:
                    GroupLogAction[traceLevel]($"Item Updated : {item}");
                    break;
                default:
                    break;
            }
        }
    }
}
