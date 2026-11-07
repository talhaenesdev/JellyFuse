using System;
using System.Collections.Generic;
using FlowIoC.BaseModule.Constructables;
using UnityEngine;

namespace Modules.DevToolsModule.Models
{
    /// <summary>
    /// The last errors and exceptions this run logged, newest last, so the developer panel can show them on a
    /// store build, where nothing else reads the log. Unity hands log lines in from any thread, hence the lock.
    /// </summary>
    public sealed class DevErrorLogModel : IConstructable
    {
        private const int MAX_ENTRIES = 60;
        private const int MAX_STACK_LINES = 4;

        private readonly List<string> _entries = new();
        private readonly object _lock = new();

        public bool IsPostConstructed { get; set; }
        public bool IsDeconstructed { get; set; }

        public void PostConstruct() => Application.logMessageReceivedThreaded += OnLog;
        public void Deconstruct() => Application.logMessageReceivedThreaded -= OnLog;

        public IReadOnlyList<string> Entries()
        {
            lock (_lock) return _entries.ToArray();
        }

        public void Clear()
        {
            lock (_lock) _entries.Clear();
        }

        private void OnLog(string message, string stackTrace, LogType type)
        {
            if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert) return;
            string entry = $"[{DateTime.Now:HH:mm:ss}] {type}: {message}";
            string stack = Trim(stackTrace);
            if (stack.Length > 0) entry += "\n" + stack;
            lock (_lock)
            {
                _entries.Add(entry);
                if (_entries.Count > MAX_ENTRIES) _entries.RemoveAt(0);
            }
        }

        /// <summary>The first few lines of the stack: where it broke, without the whole call chain.</summary>
        private static string Trim(string stackTrace)
        {
            if (string.IsNullOrEmpty(stackTrace)) return string.Empty;
            var lines = stackTrace.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            return string.Join("\n", lines, 0, Math.Min(MAX_STACK_LINES, lines.Length)).Trim();
        }
    }
}
