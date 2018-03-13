using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Raven.StructuredLog
{
    /// <summary>
    /// A structured log containing one or more log instances.
    /// </summary>
    public class StructuredLog
    {
        /// <summary>
        /// The ID of the structured log. This will be generated automatically based on a deterministic hash code of the <see cref="MessageTemplate"/>.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// The template of the log messages grouped under this <see cref="StructuredLog"/>.
        /// </summary>
        public string MessageTemplate { get; set; }

        /// <summary>
        /// Gets the total number of times this log has occurred. This may be different than <see cref="Occurrences"/>.Count, because <see cref="Occurrences"/> are trimmed to store a maximum number of logs.
        /// </summary>
        public int OccurrenceCount { get; set; }

        /// <summary>
        /// The log level of the most recent log occurrence.
        /// </summary>
        public LogLevel Level { get; set; }

        /// <summary>
        /// The first time the log occurred.
        /// </summary>
        public DateTimeOffset FirstOccurrence { get; set; }

        /// <summary>
        /// The last time this log occurred.
        /// </summary>
        public DateTimeOffset LastOccurrence { get; set; }

        /// <summary>
        /// The logs that are grouped under this structured log. 
        /// This list is ordered by most recent to least recent.
        /// </summary>
        public List<Log> Occurrences { get; set; } = new List<Log>();

        /// <summary>
        /// The function where the exception occurred.
        /// </summary>
        public string Function { get; set; }

        /// <summary>
        /// The code file where the exception occurred.
        /// </summary>
        public string File { get; set; }

        /// <summary>
        /// The line number where the exception occurred.
        /// </summary>
        public string LineNumber { get; set; }
        
        /// <summary>
        /// Adds a log to the <see cref="Occurrences"/> at the top of the list.
        /// </summary>
        /// <param name="log">The log to add to the occurrences.</param>
        public void AddLog(Log log)
        {
            if (FirstOccurrence == default(DateTimeOffset))
            {
                FirstOccurrence = DateTimeOffset.UtcNow;
            }

            Occurrences.Insert(0, log);
            LastOccurrence = DateTimeOffset.UtcNow;
            OccurrenceCount++;
            Level = log.Level;
            MessageTemplate = log.Template ?? log.Message ?? string.Empty;
            Function = log.Function;
            File = log.File;
            LineNumber = log.LineNumber;

            // We don't store an infinite number of logs inside Occurrences. Trim them down as configured.
            if (Occurrences.Count > RavenStructuredLoggerProvider.MaxStructuredLogOccurrences)
            {
                Occurrences.RemoveAt(Occurrences.Count - 1);
            }
        }
    }
}