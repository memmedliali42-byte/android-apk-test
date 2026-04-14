using System;
using System.Collections.Generic;
using UnityEngine;

namespace InternalDebugMenu
{
    public enum DebugAuditSeverity
    {
        Info,
        Warning,
        Error
    }

    public readonly struct DebugAuditEntry
    {
        public DebugAuditEntry(DateTime timestampUtc, DebugAuditSeverity severity, string category, string action, string payload)
        {
            TimestampUtc = timestampUtc;
            Severity = severity;
            Category = category ?? string.Empty;
            Action = action ?? string.Empty;
            Payload = payload ?? string.Empty;
        }

        public DateTime TimestampUtc { get; }
        public DebugAuditSeverity Severity { get; }
        public string Category { get; }
        public string Action { get; }
        public string Payload { get; }

        public override string ToString()
        {
            return $"{TimestampUtc:HH:mm:ss} [{Severity}] {Category}/{Action} {Payload}";
        }
    }

    public sealed class DebugAuditLogger : MonoBehaviour
    {
        [SerializeField] [Min(16)] private int maximumEntries = 128;
        [SerializeField] private bool mirrorToUnityConsole = true;

        private readonly Queue<DebugAuditEntry> entries = new Queue<DebugAuditEntry>();

        public event Action<DebugAuditEntry> EntryLogged;

        public IReadOnlyCollection<DebugAuditEntry> Entries => entries;

        public void Log(DebugAuditSeverity severity, string category, string action, string payload)
        {
            var entry = new DebugAuditEntry(DateTime.UtcNow, severity, category, action, payload);

            if (entries.Count >= maximumEntries)
            {
                entries.Dequeue();
            }

            entries.Enqueue(entry);

            if (mirrorToUnityConsole)
            {
                switch (severity)
                {
                    case DebugAuditSeverity.Warning:
                        Debug.LogWarning(entry.ToString());
                        break;
                    case DebugAuditSeverity.Error:
                        Debug.LogError(entry.ToString());
                        break;
                    default:
                        Debug.Log(entry.ToString());
                        break;
                }
            }

            EntryLogged?.Invoke(entry);
        }

        public DebugAuditEntry[] Snapshot()
        {
            return entries.ToArray();
        }
    }
}
